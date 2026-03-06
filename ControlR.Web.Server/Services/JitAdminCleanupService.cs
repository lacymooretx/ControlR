using ControlR.Libraries.Shared.Dtos.HubDtos;
using ControlR.Libraries.Shared.Hubs.Clients;
using Microsoft.AspNetCore.SignalR;

namespace ControlR.Web.Server.Services;

public class JitAdminCleanupService(
  IServiceScopeFactory scopeFactory,
  IHubContext<AgentHub, IAgentHubClient> agentHub,
  ILogger<JitAdminCleanupService> logger) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    logger.LogInformation("JIT Admin cleanup service started.");

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        await CleanupExpiredAccounts(stoppingToken);
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error cleaning up expired JIT admin accounts.");
      }

      await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
    }

    logger.LogInformation("JIT Admin cleanup service stopped.");
  }

  private async Task CleanupExpiredAccounts(CancellationToken stoppingToken)
  {
    using var scope = scopeFactory.CreateScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDb>>();
    await using var appDb = await dbFactory.CreateDbContextAsync(stoppingToken);

    var now = DateTimeOffset.UtcNow;

    // Find active accounts past expiry, ignoring tenant query filter
    var expiredAccounts = await appDb.JitAdminAccounts
      .IgnoreQueryFilters()
      .Where(a => a.Status == JitAdminAccountStatus.Active && a.ExpiresAt < now)
      .ToListAsync(stoppingToken);

    if (expiredAccounts.Count == 0)
    {
      return;
    }

    logger.LogInformation("Found {Count} expired JIT admin accounts to clean up.", expiredAccounts.Count);

    foreach (var account in expiredAccounts)
    {
      // Try to send delete command to the agent
      try
      {
        var device = await appDb.Devices
          .IgnoreQueryFilters()
          .AsNoTracking()
          .FirstOrDefaultAsync(d => d.Id == account.DeviceId, stoppingToken);

        if (device is not null && !string.IsNullOrWhiteSpace(device.ConnectionId))
        {
          var hubDto = new DeleteJitAdminRequestHubDto(account.Username);
          await agentHub.Clients
            .Client(device.ConnectionId)
            .DeleteJitAdminAccount(hubDto);

          logger.LogInformation(
            "Sent delete command for expired JIT account {Username} on device {DeviceId}.",
            account.Username, account.DeviceId);
        }
        else
        {
          logger.LogWarning(
            "Device {DeviceId} is offline. JIT account {Username} marked as expired but agent could not be notified.",
            account.DeviceId, account.Username);
        }
      }
      catch (Exception ex)
      {
        logger.LogWarning(ex,
          "Failed to send delete command for expired JIT account {Username} on device {DeviceId}.",
          account.Username, account.DeviceId);
      }

      account.Status = JitAdminAccountStatus.Expired;
      account.DeletedAt = now;
    }

    await appDb.SaveChangesAsync(stoppingToken);

    logger.LogInformation("Marked {Count} JIT admin accounts as expired.", expiredAccounts.Count);
  }
}
