using ControlR.Libraries.Shared.Dtos.HubDtos;
using ControlR.Libraries.Shared.Hubs.Clients;
using Cronos;
using Microsoft.AspNetCore.SignalR;

namespace ControlR.Web.Server.Services;

public interface ISchedulerService
{
  Task<Result<ScheduledTaskExecution>> ExecuteTask(ScheduledTask task);
}

public class SchedulerService(
  IServiceScopeFactory scopeFactory,
  IHubContext<AgentHub, IAgentHubClient> agentHub,
  ILogger<SchedulerService> logger) : ISchedulerService
{
  private readonly IHubContext<AgentHub, IAgentHubClient> _agentHub = agentHub;
  private readonly ILogger<SchedulerService> _logger = logger;
  private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

  public async Task<Result<ScheduledTaskExecution>> ExecuteTask(ScheduledTask task)
  {
    try
    {
      using var scope = _scopeFactory.CreateScope();
      var appDb = scope.ServiceProvider.GetRequiredService<AppDb>();

      var execution = new ScheduledTaskExecution
      {
        ScheduledTaskId = task.Id,
        StartedAt = DateTimeOffset.UtcNow,
        Status = "Running",
        TenantId = task.TenantId,
      };

      await appDb.ScheduledTaskExecutions.AddAsync(execution);

      if (task.TaskType == "Script")
      {
        var scriptResult = await ExecuteScriptTask(appDb, task, execution);
        if (!scriptResult.IsSuccess)
        {
          execution.Status = "Failed";
          execution.CompletedAt = DateTimeOffset.UtcNow;
          await appDb.SaveChangesAsync();
          return Result.Fail<ScheduledTaskExecution>(scriptResult.Reason);
        }
      }
      else
      {
        execution.Status = "Failed";
        execution.CompletedAt = DateTimeOffset.UtcNow;
        await appDb.SaveChangesAsync();
        return Result.Fail<ScheduledTaskExecution>($"Unsupported task type: {task.TaskType}");
      }

      await appDb.SaveChangesAsync();
      return Result.Ok(execution);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error executing scheduled task {TaskId}.", task.Id);
      return Result.Fail<ScheduledTaskExecution>($"Error executing task: {ex.Message}");
    }
  }

  private async Task<Result> ExecuteScriptTask(AppDb appDb, ScheduledTask task, ScheduledTaskExecution execution)
  {
    if (task.ScriptId is null)
    {
      return Result.Fail("Script task has no script assigned.");
    }

    var script = task.Script ?? await appDb.SavedScripts
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == task.ScriptId);

    if (script is null)
    {
      return Result.Fail("Script not found.");
    }

    // Resolve target devices from device IDs and group IDs
    var deviceIds = new HashSet<Guid>(task.TargetDeviceIds);

    if (task.TargetGroupIds.Count > 0)
    {
      var groupDeviceIds = await appDb.Devices
        .AsNoTracking()
        .Where(d => d.DeviceGroupId != null && task.TargetGroupIds.Contains(d.DeviceGroupId.Value))
        .Select(d => d.Id)
        .ToListAsync();

      foreach (var id in groupDeviceIds)
      {
        deviceIds.Add(id);
      }
    }

    if (deviceIds.Count == 0)
    {
      return Result.Fail("No target devices found.");
    }

    var devices = await appDb.Devices
      .AsNoTracking()
      .Where(d => deviceIds.Contains(d.Id) && d.IsOnline)
      .ToListAsync();

    if (devices.Count == 0)
    {
      return Result.Fail("No online devices found.");
    }

    // Create script execution
    var scriptExecution = new ScriptExecution
    {
      InitiatedByUserId = task.CreatorUserId,
      ScriptId = script.Id,
      ScriptType = script.ScriptType,
      StartedAt = DateTimeOffset.UtcNow,
      Status = "Running",
      TenantId = task.TenantId,
    };

    await appDb.ScriptExecutions.AddAsync(scriptExecution);

    var results = new List<ScriptExecutionResult>();
    foreach (var device in devices)
    {
      var result = new ScriptExecutionResult
      {
        DeviceId = device.Id,
        DeviceName = device.Name,
        ScriptExecutionId = scriptExecution.Id,
        Status = "Pending",
        TenantId = task.TenantId,
      };
      results.Add(result);
    }

    await appDb.ScriptExecutionResults.AddRangeAsync(results);
    execution.ScriptExecutionId = scriptExecution.Id;
    await appDb.SaveChangesAsync();

    // Fan out to agents
    foreach (var device in devices)
    {
      var resultId = results.First(r => r.DeviceId == device.Id).Id;
      var hubDto = new ScriptExecutionRequestHubDto(
        scriptExecution.Id,
        resultId,
        script.ScriptContent,
        script.ScriptType);

      try
      {
        await _agentHub.Clients
          .Client(device.ConnectionId)
          .ExecuteScript(hubDto);
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "Failed to send script to device {DeviceId}.", device.Id);
        var failedResult = results.First(r => r.DeviceId == device.Id);
        failedResult.Status = "Failed";
        failedResult.StandardError = $"Failed to send script to agent: {ex.Message}";
        failedResult.CompletedAt = DateTimeOffset.UtcNow;
      }
    }

    await appDb.SaveChangesAsync();
    return Result.Ok();
  }
}

public class SchedulerBackgroundService(
  IServiceScopeFactory scopeFactory,
  ISchedulerService schedulerService,
  ILogger<SchedulerBackgroundService> logger) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    logger.LogInformation("Scheduler background service started.");

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        await EvaluateScheduledTasks(stoppingToken);
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error evaluating scheduled tasks.");
      }

      await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
    }

    logger.LogInformation("Scheduler background service stopped.");
  }

  private static DateTimeOffset? CalculateNextRun(string cronExpression, string timeZone)
  {
    try
    {
      var cron = CronExpression.Parse(cronExpression);
      var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
      return cron.GetNextOccurrence(DateTimeOffset.UtcNow, tz);
    }
    catch (Exception)
    {
      return null;
    }
  }

  private async Task EvaluateScheduledTasks(CancellationToken stoppingToken)
  {
    using var scope = scopeFactory.CreateScope();
    var appDb = scope.ServiceProvider.GetRequiredService<AppDb>();

    var now = DateTimeOffset.UtcNow;

    var dueTasks = await appDb.ScheduledTasks
      .Include(x => x.Script)
      .Where(x => x.IsEnabled && x.NextRunAt != null && x.NextRunAt <= now)
      .ToListAsync(stoppingToken);

    foreach (var task in dueTasks)
    {
      logger.LogInformation("Executing scheduled task {TaskId}: {TaskName}", task.Id, task.Name);

      var result = await schedulerService.ExecuteTask(task);
      if (result.IsSuccess)
      {
        logger.LogInformation("Scheduled task {TaskId} executed successfully.", task.Id);
      }
      else
      {
        logger.LogWarning("Scheduled task {TaskId} failed: {Reason}", task.Id, result.Reason);
      }

      // Update last run and calculate next run
      task.LastRunAt = now;
      task.NextRunAt = CalculateNextRun(task.CronExpression, task.TimeZone);

      await appDb.SaveChangesAsync(stoppingToken);
    }
  }
}
