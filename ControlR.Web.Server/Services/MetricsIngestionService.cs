using System.Threading.Channels;

namespace ControlR.Web.Server.Services;

public interface IMetricsIngestionService
{
  void RecordSnapshot(Guid deviceId, Guid tenantId, double cpuPercent, double memoryPercent, double diskPercent);
}

public class MetricsIngestionService : IMetricsIngestionService
{
  private readonly Channel<DeviceMetricSnapshot> _channel = Channel.CreateBounded<DeviceMetricSnapshot>(
    new BoundedChannelOptions(10000)
    {
      FullMode = BoundedChannelFullMode.DropOldest
    });

  public ChannelReader<DeviceMetricSnapshot> Reader => _channel.Reader;

  public void RecordSnapshot(Guid deviceId, Guid tenantId, double cpuPercent, double memoryPercent, double diskPercent)
  {
    var snapshot = new DeviceMetricSnapshot
    {
      CpuPercent = cpuPercent,
      DeviceId = deviceId,
      DiskPercent = diskPercent,
      MemoryPercent = memoryPercent,
      TenantId = tenantId,
      Timestamp = DateTimeOffset.UtcNow,
    };

    _channel.Writer.TryWrite(snapshot);
  }
}

public class MetricsIngestionBackgroundService(
  IServiceScopeFactory scopeFactory,
  MetricsIngestionService metricsIngestionService,
  ILogger<MetricsIngestionBackgroundService> logger) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    logger.LogInformation("Metrics ingestion background service started.");

    var batch = new List<DeviceMetricSnapshot>(100);

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        batch.Clear();

        if (await metricsIngestionService.Reader.WaitToReadAsync(stoppingToken))
        {
          while (batch.Count < 100 && metricsIngestionService.Reader.TryRead(out var snapshot))
          {
            batch.Add(snapshot);
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
        logger.LogError(ex, "Error processing metrics batch.");
        await Task.Delay(1000, stoppingToken);
      }
    }
  }

  private async Task FlushBatch(List<DeviceMetricSnapshot> batch, CancellationToken ct)
  {
    using var scope = scopeFactory.CreateScope();
    var appDb = scope.ServiceProvider.GetRequiredService<AppDb>();

    await appDb.DeviceMetricSnapshots.AddRangeAsync(batch, ct);
    await appDb.SaveChangesAsync(ct);

    logger.LogDebug("Flushed {Count} metric snapshots.", batch.Count);
  }
}
