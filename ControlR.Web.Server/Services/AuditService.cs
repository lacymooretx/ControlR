using System.Threading.Channels;
using ControlR.Libraries.Shared.Enums;
using ControlR.Web.Server.Data;
using ControlR.Web.Server.Data.Entities;

namespace ControlR.Web.Server.Services;

public interface IAuditService
{
  void LogEvent(
    Guid tenantId,
    string eventType,
    string action,
    Guid? actorUserId = null,
    string? actorUserName = null,
    Guid? targetDeviceId = null,
    string? targetDeviceName = null,
    string? sourceIpAddress = null,
    string? details = null,
    Guid? sessionId = null);
}

public class AuditService : IAuditService
{
  private readonly Channel<AuditLog> _channel = Channel.CreateBounded<AuditLog>(
    new BoundedChannelOptions(10_000)
    {
      FullMode = BoundedChannelFullMode.DropOldest,
      SingleReader = true
    });

  internal ChannelReader<AuditLog> Reader => _channel.Reader;

  public void LogEvent(
    Guid tenantId,
    string eventType,
    string action,
    Guid? actorUserId = null,
    string? actorUserName = null,
    Guid? targetDeviceId = null,
    string? targetDeviceName = null,
    string? sourceIpAddress = null,
    string? details = null,
    Guid? sessionId = null)
  {
    var entry = new AuditLog
    {
      TenantId = tenantId,
      EventType = eventType,
      Action = action,
      ActorUserId = actorUserId,
      ActorUserName = actorUserName,
      TargetDeviceId = targetDeviceId,
      TargetDeviceName = targetDeviceName,
      SourceIpAddress = sourceIpAddress,
      Timestamp = DateTimeOffset.UtcNow,
      Details = details,
      SessionId = sessionId
    };

    _channel.Writer.TryWrite(entry);
  }
}

public class AuditLogBackgroundService(
  IServiceScopeFactory scopeFactory,
  AuditService auditService,
  IWebhookDispatcher webhookDispatcher,
  ILogger<AuditLogBackgroundService> logger) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    logger.LogInformation("Audit log background service started.");

    var batch = new List<AuditLog>(50);

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        batch.Clear();

        // Wait for the first item
        if (await auditService.Reader.WaitToReadAsync(stoppingToken))
        {
          // Drain available items up to batch size
          while (batch.Count < 50 && auditService.Reader.TryRead(out var entry))
          {
            batch.Add(entry);
          }
        }

        if (batch.Count > 0)
        {
          await FlushBatch(batch, stoppingToken);
        }
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error processing audit log batch.");
        await Task.Delay(1000, stoppingToken);
      }
    }

    // Drain remaining items on shutdown
    batch.Clear();
    while (auditService.Reader.TryRead(out var entry))
    {
      batch.Add(entry);
    }

    if (batch.Count > 0)
    {
      await FlushBatch(batch, CancellationToken.None);
    }

    logger.LogInformation("Audit log background service stopped.");
  }

  private static string? MapToWebhookEvent(string eventType, string action)
  {
    return (eventType, action) switch
    {
      ("RemoteControl", "Start") => "session.remote_control.start",
      ("RemoteControl", "End") => "session.remote_control.end",
      ("Terminal", "Start") => "session.terminal.start",
      ("Terminal", "End") => "session.terminal.end",
      ("FileTransfer", "Upload") => "file.uploaded",
      ("FileTransfer", "Download") => "file.downloaded",
      ("PowerState", "Shutdown") => "device.power.shutdown",
      ("PowerState", "Restart") => "device.power.restart",
      _ => null,
    };
  }

  private async Task FlushBatch(List<AuditLog> batch, CancellationToken ct)
  {
    using var scope = scopeFactory.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();

    db.AuditLogs.AddRange(batch);
    await db.SaveChangesAsync(ct);

    foreach (var entry in batch)
    {
      var webhookEventType = MapToWebhookEvent(entry.EventType, entry.Action);
      if (webhookEventType is not null)
      {
        webhookDispatcher.Dispatch(webhookEventType, entry.TenantId, new
        {
          auditLogId = entry.Id,
          eventType = entry.EventType,
          action = entry.Action,
          actorUserId = entry.ActorUserId,
          actorUserName = entry.ActorUserName,
          targetDeviceId = entry.TargetDeviceId,
          targetDeviceName = entry.TargetDeviceName,
          timestamp = entry.Timestamp,
          details = entry.Details,
        });
      }
    }

    logger.LogDebug("Flushed {Count} audit log entries.", batch.Count);
  }
}
