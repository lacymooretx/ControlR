using ControlR.Libraries.Shared.Dtos.ServerApi;
using Cronos;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/scheduled-tasks")]
[ApiController]
[Authorize(Roles = $"{RoleNames.TenantAdministrator}")]
public class ScheduledTasksController : ControllerBase
{
  [HttpPost]
  public async Task<ActionResult<ScheduledTaskDto>> CreateScheduledTask(
    [FromServices] AppDb appDb,
    [FromBody] ScheduledTaskCreateRequestDto dto)
  {
    if (!User.TryGetTenantId(out var tenantId))
    {
      return NotFound("User tenant not found.");
    }

    if (!User.TryGetUserId(out var userId))
    {
      return Unauthorized();
    }

    if (!TryParseCron(dto.CronExpression, out _))
    {
      return BadRequest("Invalid cron expression.");
    }

    var task = new ScheduledTask
    {
      CreatorUserId = userId,
      CronExpression = dto.CronExpression,
      Description = dto.Description,
      Name = dto.Name,
      ScriptId = dto.ScriptId,
      TargetDeviceIds = dto.TargetDeviceIds.ToList(),
      TargetGroupIds = dto.TargetGroupIds.ToList(),
      TaskType = dto.TaskType,
      TenantId = tenantId,
      TimeZone = dto.TimeZone,
    };

    task.NextRunAt = CalculateNextRun(dto.CronExpression, dto.TimeZone);

    await appDb.ScheduledTasks.AddAsync(task);
    await appDb.SaveChangesAsync();

    return Ok(task.ToDto());
  }

  [HttpDelete("{taskId:guid}")]
  public async Task<ActionResult> DeleteScheduledTask(
    [FromServices] AppDb appDb,
    [FromRoute] Guid taskId)
  {
    var task = await appDb.ScheduledTasks
      .FirstOrDefaultAsync(x => x.Id == taskId);

    if (task is null)
    {
      return NotFound();
    }

    appDb.ScheduledTasks.Remove(task);
    await appDb.SaveChangesAsync();

    return NoContent();
  }

  [HttpGet]
  public async Task<ActionResult<ScheduledTaskDto[]>> GetAllScheduledTasks(
    [FromServices] AppDb appDb)
  {
    var tasks = await appDb.ScheduledTasks
      .AsNoTracking()
      .Include(x => x.Script)
      .OrderBy(x => x.Name)
      .ToListAsync();

    var dtos = tasks.Select(x => x.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpGet("{taskId:guid}")]
  public async Task<ActionResult<ScheduledTaskDto>> GetScheduledTask(
    [FromServices] AppDb appDb,
    [FromRoute] Guid taskId)
  {
    var task = await appDb.ScheduledTasks
      .AsNoTracking()
      .Include(x => x.Script)
      .FirstOrDefaultAsync(x => x.Id == taskId);

    if (task is null)
    {
      return NotFound();
    }

    return Ok(task.ToDto());
  }

  [HttpGet("{taskId:guid}/executions")]
  public async Task<ActionResult<ScheduledTaskExecutionDto[]>> GetTaskExecutions(
    [FromServices] AppDb appDb,
    [FromRoute] Guid taskId,
    [FromQuery] int count = 20)
  {
    var executions = await appDb.ScheduledTaskExecutions
      .AsNoTracking()
      .Where(x => x.ScheduledTaskId == taskId)
      .OrderByDescending(x => x.StartedAt)
      .Take(count)
      .ToListAsync();

    var dtos = executions.Select(x => x.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpPost("{taskId:guid}/trigger")]
  public async Task<ActionResult<ScheduledTaskExecutionDto>> TriggerScheduledTask(
    [FromServices] AppDb appDb,
    [FromServices] ISchedulerService schedulerService,
    [FromRoute] Guid taskId)
  {
    var task = await appDb.ScheduledTasks
      .Include(x => x.Script)
      .FirstOrDefaultAsync(x => x.Id == taskId);

    if (task is null)
    {
      return NotFound();
    }

    var executionResult = await schedulerService.ExecuteTask(task);
    if (!executionResult.IsSuccess)
    {
      return BadRequest(executionResult.Reason);
    }

    return Ok(executionResult.Value.ToDto());
  }

  [HttpPut]
  public async Task<ActionResult<ScheduledTaskDto>> UpdateScheduledTask(
    [FromServices] AppDb appDb,
    [FromBody] ScheduledTaskUpdateRequestDto dto)
  {
    var task = await appDb.ScheduledTasks
      .Include(x => x.Script)
      .FirstOrDefaultAsync(x => x.Id == dto.Id);

    if (task is null)
    {
      return NotFound();
    }

    if (!TryParseCron(dto.CronExpression, out _))
    {
      return BadRequest("Invalid cron expression.");
    }

    task.CronExpression = dto.CronExpression;
    task.Description = dto.Description;
    task.IsEnabled = dto.IsEnabled;
    task.Name = dto.Name;
    task.ScriptId = dto.ScriptId;
    task.TargetDeviceIds = dto.TargetDeviceIds.ToList();
    task.TargetGroupIds = dto.TargetGroupIds.ToList();
    task.TaskType = dto.TaskType;
    task.TimeZone = dto.TimeZone;
    task.NextRunAt = dto.IsEnabled ? CalculateNextRun(dto.CronExpression, dto.TimeZone) : null;

    await appDb.SaveChangesAsync();

    return Ok(task.ToDto());
  }

  private static DateTimeOffset? CalculateNextRun(string cronExpression, string timeZone)
  {
    if (!TryParseCron(cronExpression, out var cron) || cron is null)
    {
      return null;
    }

    var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
    return cron.GetNextOccurrence(DateTimeOffset.UtcNow, tz);
  }

  private static bool TryParseCron(string expression, out CronExpression? result)
  {
    try
    {
      result = CronExpression.Parse(expression);
      return true;
    }
    catch
    {
      result = null;
      return false;
    }
  }
}
