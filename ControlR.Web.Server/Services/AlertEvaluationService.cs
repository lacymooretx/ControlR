namespace ControlR.Web.Server.Services;

public class AlertEvaluationService(
  IServiceScopeFactory scopeFactory,
  IWebhookDispatcher webhookDispatcher,
  ILogger<AlertEvaluationService> logger) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    logger.LogInformation("Alert evaluation service started.");

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        await EvaluateAlertRules(stoppingToken);
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error evaluating alert rules.");
      }

      await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
    }

    logger.LogInformation("Alert evaluation service stopped.");
  }

  private static bool EvaluateThreshold(double value, string op, double threshold)
  {
    return op switch
    {
      ">" => value > threshold,
      ">=" => value >= threshold,
      "<" => value < threshold,
      "<=" => value <= threshold,
      "==" => Math.Abs(value - threshold) < 0.01,
      _ => false,
    };
  }

  private static double? GetMetricValue(Device device, string metricType)
  {
    return metricType switch
    {
      "CPU" => device.CpuUtilization,
      "Memory" => device.TotalMemory > 0 ? (device.UsedMemory / device.TotalMemory) * 100 : null,
      "Disk" => device.TotalStorage > 0 ? (device.UsedStorage / device.TotalStorage) * 100 : null,
      _ => null,
    };
  }

  private async Task EvaluateAlertRules(CancellationToken stoppingToken)
  {
    using var scope = scopeFactory.CreateScope();
    var appDb = scope.ServiceProvider.GetRequiredService<AppDb>();

    var rules = await appDb.AlertRules
      .Where(r => r.IsEnabled)
      .ToListAsync(stoppingToken);

    if (rules.Count == 0)
    {
      return;
    }

    foreach (var rule in rules)
    {
      await EvaluateRule(appDb, rule, stoppingToken);
    }

    await appDb.SaveChangesAsync(stoppingToken);
  }

  private async Task EvaluateRule(AppDb appDb, AlertRule rule, CancellationToken stoppingToken)
  {
    // Resolve target devices
    var deviceIds = new HashSet<Guid>(rule.TargetDeviceIds);

    if (rule.TargetGroupIds.Count > 0)
    {
      var groupDeviceIds = await appDb.Devices
        .AsNoTracking()
        .Where(d => d.DeviceGroupId != null && rule.TargetGroupIds.Contains(d.DeviceGroupId.Value))
        .Select(d => d.Id)
        .ToListAsync(stoppingToken);

      foreach (var id in groupDeviceIds)
      {
        deviceIds.Add(id);
      }
    }

    // If no targets specified, apply to all online devices in the tenant
    IQueryable<Device> devicesQuery;
    if (deviceIds.Count > 0)
    {
      devicesQuery = appDb.Devices
        .AsNoTracking()
        .Where(d => deviceIds.Contains(d.Id) && d.IsOnline);
    }
    else
    {
      devicesQuery = appDb.Devices
        .AsNoTracking()
        .Where(d => d.TenantId == rule.TenantId && d.IsOnline);
    }

    var devices = await devicesQuery.ToListAsync(stoppingToken);

    foreach (var device in devices)
    {
      var metricValue = GetMetricValue(device, rule.MetricType);
      if (metricValue is null)
      {
        continue;
      }

      var isTriggered = EvaluateThreshold(metricValue.Value, rule.Operator, rule.ThresholdValue);

      // Check for existing active alert
      var existingAlert = await appDb.Alerts
        .FirstOrDefaultAsync(a =>
          a.AlertRuleId == rule.Id &&
          a.DeviceId == device.Id &&
          a.Status == "Active",
          stoppingToken);

      if (isTriggered && existingAlert is null)
      {
        // Create new alert
        var alert = new Alert
        {
          AlertRuleId = rule.Id,
          Details = $"{rule.MetricType} is {metricValue.Value:F1}% (threshold: {rule.Operator} {rule.ThresholdValue}%)",
          DeviceId = device.Id,
          DeviceName = device.Name,
          Status = "Active",
          TenantId = rule.TenantId,
          TriggeredAt = DateTimeOffset.UtcNow,
        };

        await appDb.Alerts.AddAsync(alert, stoppingToken);
        logger.LogInformation(
          "Alert triggered: {RuleName} on {DeviceName} - {MetricType} = {Value:F1}%",
          rule.Name, device.Name, rule.MetricType, metricValue.Value);

        webhookDispatcher.Dispatch("alert.triggered", rule.TenantId, new
        {
          alertRuleName = rule.Name,
          deviceId = device.Id,
          deviceName = device.Name,
          metricType = rule.MetricType,
          metricValue = metricValue.Value,
          threshold = rule.ThresholdValue,
          @operator = rule.Operator,
          details = alert.Details,
          triggeredAt = alert.TriggeredAt,
        });
      }
      else if (!isTriggered && existingAlert is not null)
      {
        // Auto-resolve
        existingAlert.Status = "Resolved";
        existingAlert.ResolvedAt = DateTimeOffset.UtcNow;
        logger.LogInformation(
          "Alert resolved: {RuleName} on {DeviceName} - {MetricType} = {Value:F1}%",
          rule.Name, device.Name, rule.MetricType, metricValue.Value);

        webhookDispatcher.Dispatch("alert.resolved", rule.TenantId, new
        {
          alertRuleName = rule.Name,
          deviceId = device.Id,
          deviceName = device.Name,
          metricType = rule.MetricType,
          metricValue = metricValue.Value,
          resolvedAt = existingAlert.ResolvedAt,
        });
      }
    }
  }
}
