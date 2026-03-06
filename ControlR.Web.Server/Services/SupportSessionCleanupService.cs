namespace ControlR.Web.Server.Services;

public class SupportSessionCleanupService(
  IServiceScopeFactory scopeFactory,
  ILogger<SupportSessionCleanupService> logger) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    logger.LogInformation("Support session cleanup service started.");

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        await CleanupExpiredSessions(stoppingToken);
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error cleaning up expired support sessions.");
      }

      await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
    }

    logger.LogInformation("Support session cleanup service stopped.");
  }

  private async Task CleanupExpiredSessions(CancellationToken stoppingToken)
  {
    using var scope = scopeFactory.CreateScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDb>>();
    await using var appDb = await dbFactory.CreateDbContextAsync(stoppingToken);

    var now = DateTimeOffset.UtcNow;

    var expiredSessions = await appDb.SupportSessions
      .Where(s =>
        s.ExpiresAt < now &&
        (s.Status == SupportSessionStatus.Pending || s.Status == SupportSessionStatus.WaitingForClient))
      .ToListAsync(stoppingToken);

    if (expiredSessions.Count == 0)
    {
      return;
    }

    foreach (var session in expiredSessions)
    {
      session.Status = SupportSessionStatus.Expired;
    }

    await appDb.SaveChangesAsync(stoppingToken);

    logger.LogInformation("Marked {Count} support sessions as expired.", expiredSessions.Count);
  }
}
