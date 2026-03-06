namespace ControlR.Web.Server.Services;

public class RecordingCleanupService(
  IServiceScopeFactory scopeFactory,
  ILogger<RecordingCleanupService> logger) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    logger.LogInformation("Recording cleanup service started.");

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        await CleanupStaleRecordings(stoppingToken);
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error cleaning up stale recordings.");
      }

      await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
    }

    logger.LogInformation("Recording cleanup service stopped.");
  }

  private async Task CleanupStaleRecordings(CancellationToken stoppingToken)
  {
    using var scope = scopeFactory.CreateScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDb>>();
    await using var appDb = await dbFactory.CreateDbContextAsync(stoppingToken);

    var cutoff = DateTimeOffset.UtcNow.AddHours(-24);

    var staleRecordings = await appDb.SessionRecordings
      .Where(r =>
        r.Status == SessionRecordingStatus.Recording &&
        r.SessionStartedAt < cutoff)
      .ToListAsync(stoppingToken);

    if (staleRecordings.Count == 0)
    {
      return;
    }

    foreach (var recording in staleRecordings)
    {
      recording.Status = SessionRecordingStatus.Failed;
      recording.SessionEndedAt = DateTimeOffset.UtcNow;

      // Recount frames from disk if path exists
      if (Directory.Exists(recording.StoragePath))
      {
        var files = Directory.GetFiles(recording.StoragePath, "*.jpg");
        recording.FrameCount = files.Length;
        recording.StorageSizeBytes = files.Sum(f => new FileInfo(f).Length);
        if (files.Length > 0)
        {
          recording.DurationMs = (long)(recording.SessionEndedAt.Value - recording.SessionStartedAt).TotalMilliseconds;
        }
      }
    }

    await appDb.SaveChangesAsync(stoppingToken);

    logger.LogInformation("Marked {Count} stale recordings as failed.", staleRecordings.Count);
  }
}
