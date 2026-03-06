namespace ControlR.Web.Server.Services;

public interface ISuggestionEngine
{
  Task GenerateSuggestions(CancellationToken ct);
}

public class SuggestionEngineService(
  IServiceScopeFactory scopeFactory,
  ILogger<SuggestionEngineService> logger) : ISuggestionEngine
{
  public async Task GenerateSuggestions(CancellationToken ct)
  {
    using var scope = scopeFactory.CreateScope();
    var appDb = scope.ServiceProvider.GetRequiredService<AppDb>();

    var tenants = await appDb.Tenants
      .AsNoTracking()
      .Select(t => t.Id)
      .ToListAsync(ct);

    foreach (var tenantId in tenants)
    {
      await EvaluateHighCpu(appDb, tenantId, ct);
      await EvaluateHighDisk(appDb, tenantId, ct);
      await EvaluateFrequentAlerts(appDb, tenantId, ct);
      await EvaluateStaleDevices(appDb, tenantId, ct);
    }

    await appDb.SaveChangesAsync(ct);
  }

  private async Task EvaluateHighCpu(AppDb appDb, Guid tenantId, CancellationToken ct)
  {
    var cutoff = DateTimeOffset.UtcNow.AddHours(-1);

    var highCpuDevices = await appDb.DeviceMetricSnapshots
      .AsNoTracking()
      .Where(m => m.TenantId == tenantId && m.Timestamp > cutoff && m.CpuPercent > 90)
      .Select(m => m.DeviceId)
      .Distinct()
      .ToListAsync(ct);

    foreach (var deviceId in highCpuDevices)
    {
      var exists = await appDb.AutomationSuggestions
        .AnyAsync(s =>
          s.TenantId == tenantId &&
          s.DeviceId == deviceId &&
          s.SuggestionType == SuggestionType.HighCpu &&
          s.Status == SuggestionStatus.New, ct);

      if (exists)
      {
        continue;
      }

      var suggestion = new AutomationSuggestion
      {
        TenantId = tenantId,
        DeviceId = deviceId,
        SuggestionType = SuggestionType.HighCpu,
        Title = "High CPU Usage Detected",
        Description = "CPU usage has exceeded 90% in recent metrics. Consider running a diagnostic script to identify resource-intensive processes.",
        Confidence = 0.8f,
        Status = SuggestionStatus.New,
      };

      await appDb.AutomationSuggestions.AddAsync(suggestion, ct);
      logger.LogInformation("Created HighCpu suggestion for device {DeviceId} in tenant {TenantId}.", deviceId, tenantId);
    }
  }

  private async Task EvaluateHighDisk(AppDb appDb, Guid tenantId, CancellationToken ct)
  {
    var cutoff = DateTimeOffset.UtcNow.AddHours(-1);

    var highDiskDevices = await appDb.DeviceMetricSnapshots
      .AsNoTracking()
      .Where(m => m.TenantId == tenantId && m.Timestamp > cutoff && m.DiskPercent > 95)
      .Select(m => m.DeviceId)
      .Distinct()
      .ToListAsync(ct);

    foreach (var deviceId in highDiskDevices)
    {
      var exists = await appDb.AutomationSuggestions
        .AnyAsync(s =>
          s.TenantId == tenantId &&
          s.DeviceId == deviceId &&
          s.SuggestionType == SuggestionType.HighDisk &&
          s.Status == SuggestionStatus.New, ct);

      if (exists)
      {
        continue;
      }

      var suggestion = new AutomationSuggestion
      {
        TenantId = tenantId,
        DeviceId = deviceId,
        SuggestionType = SuggestionType.HighDisk,
        Title = "High Disk Usage Detected",
        Description = "Disk usage has exceeded 95% in recent metrics. Consider running a cleanup script to free disk space.",
        Confidence = 0.85f,
        Status = SuggestionStatus.New,
      };

      await appDb.AutomationSuggestions.AddAsync(suggestion, ct);
      logger.LogInformation("Created HighDisk suggestion for device {DeviceId} in tenant {TenantId}.", deviceId, tenantId);
    }
  }

  private async Task EvaluateFrequentAlerts(AppDb appDb, Guid tenantId, CancellationToken ct)
  {
    var cutoff = DateTimeOffset.UtcNow.AddHours(-24);

    var frequentAlertDevices = await appDb.Alerts
      .AsNoTracking()
      .Where(a => a.TenantId == tenantId && a.TriggeredAt > cutoff)
      .GroupBy(a => a.DeviceId)
      .Where(g => g.Count() >= 5)
      .Select(g => g.Key)
      .ToListAsync(ct);

    foreach (var deviceId in frequentAlertDevices)
    {
      var exists = await appDb.AutomationSuggestions
        .AnyAsync(s =>
          s.TenantId == tenantId &&
          s.DeviceId == deviceId &&
          s.SuggestionType == SuggestionType.FrequentAlerts &&
          s.Status == SuggestionStatus.New, ct);

      if (exists)
      {
        continue;
      }

      var suggestion = new AutomationSuggestion
      {
        TenantId = tenantId,
        DeviceId = deviceId,
        SuggestionType = SuggestionType.FrequentAlerts,
        Title = "Frequent Alerts Detected",
        Description = "This device has triggered 5 or more alerts in the last 24 hours. Consider investigating the root cause.",
        Confidence = 0.75f,
        Status = SuggestionStatus.New,
      };

      await appDb.AutomationSuggestions.AddAsync(suggestion, ct);
      logger.LogInformation("Created FrequentAlerts suggestion for device {DeviceId} in tenant {TenantId}.", deviceId, tenantId);
    }
  }

  private async Task EvaluateStaleDevices(AppDb appDb, Guid tenantId, CancellationToken ct)
  {
    var staleCutoff = DateTimeOffset.UtcNow.AddDays(-7);

    var staleDevices = await appDb.Devices
      .AsNoTracking()
      .Where(d => d.TenantId == tenantId && d.LastSeen < staleCutoff && !d.IsOnline)
      .Select(d => d.Id)
      .ToListAsync(ct);

    foreach (var deviceId in staleDevices)
    {
      var exists = await appDb.AutomationSuggestions
        .AnyAsync(s =>
          s.TenantId == tenantId &&
          s.DeviceId == deviceId &&
          s.SuggestionType == SuggestionType.StaleDevice &&
          s.Status == SuggestionStatus.New, ct);

      if (exists)
      {
        continue;
      }

      var suggestion = new AutomationSuggestion
      {
        TenantId = tenantId,
        DeviceId = deviceId,
        SuggestionType = SuggestionType.StaleDevice,
        Title = "Stale Device Detected",
        Description = "This device has not been seen for over 7 days. Consider checking connectivity or removing the device.",
        Confidence = 0.7f,
        Status = SuggestionStatus.New,
      };

      await appDb.AutomationSuggestions.AddAsync(suggestion, ct);
      logger.LogInformation("Created StaleDevice suggestion for device {DeviceId} in tenant {TenantId}.", deviceId, tenantId);
    }
  }
}

public class SuggestionEngineBackgroundService(
  ISuggestionEngine suggestionEngine,
  ILogger<SuggestionEngineBackgroundService> logger) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    logger.LogInformation("Suggestion engine background service started.");

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        await suggestionEngine.GenerateSuggestions(stoppingToken);
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error generating automation suggestions.");
      }

      await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
    }

    logger.LogInformation("Suggestion engine background service stopped.");
  }
}
