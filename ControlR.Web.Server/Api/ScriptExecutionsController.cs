using ControlR.Libraries.Shared.Constants;
using ControlR.Libraries.Shared.Dtos.HubDtos;
using ControlR.Libraries.Shared.Dtos.ServerApi;
using ControlR.Libraries.Shared.Hubs.Clients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ControlR.Web.Server.Api;

[Route("api/script-executions")]
[ApiController]
[Authorize]
public class ScriptExecutionsController : ControllerBase
{
  [RequiresVerification]
  [HttpPost]
  [Authorize(Roles = $"{RoleNames.TenantAdministrator},{RoleNames.DeviceSuperUser}")]
  public async Task<ActionResult<ScriptExecutionDto>> ExecuteScript(
    [FromBody] ExecuteScriptRequestDto request,
    [FromServices] AppDb appDb,
    [FromServices] IAuthorizationService authorizationService,
    [FromServices] IHubContext<AgentHub, IAgentHubClient> agentHub,
    [FromServices] IAuditService auditService,
    [FromServices] ILogger<ScriptExecutionsController> logger)
  {
    if (!User.TryGetUserId(out var userId))
    {
      return Unauthorized();
    }

    if (!User.TryGetTenantId(out var tenantId))
    {
      return Unauthorized();
    }

    // Resolve script content and type
    string scriptContent;
    string scriptType;
    Guid? scriptId = null;

    if (request.ScriptId is not null)
    {
      var script = await appDb.SavedScripts
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == request.ScriptId);

      if (script is null)
      {
        return NotFound("Script not found.");
      }

      scriptContent = script.ScriptContent;
      scriptType = script.ScriptType;
      scriptId = script.Id;
    }
    else
    {
      if (string.IsNullOrWhiteSpace(request.AdHocScriptContent))
      {
        return BadRequest("Either ScriptId or AdHocScriptContent must be provided.");
      }

      scriptContent = request.AdHocScriptContent;
      scriptType = request.ScriptType;
    }

    // Authorize against each target device
    var isServerAdmin = User.IsInRole(RoleNames.ServerAdministrator);
    var devicesQuery = isServerAdmin
      ? appDb.Devices.IgnoreQueryFilters()
      : appDb.Devices;

    var authorizedDevices = new List<Device>();
    foreach (var deviceId in request.TargetDeviceIds)
    {
      var device = await devicesQuery
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == deviceId);

      if (device is null)
      {
        continue;
      }

      var authResult = await authorizationService.AuthorizeAsync(
        User, device, DeviceAccessByDeviceResourcePolicy.PolicyName);

      if (authResult.Succeeded)
      {
        authorizedDevices.Add(device);
      }
    }

    if (authorizedDevices.Count == 0)
    {
      return BadRequest("No authorized or online devices found.");
    }

    // Create execution record
    var execution = new ScriptExecution
    {
      AdHocScriptContent = request.ScriptId is null ? scriptContent : null,
      InitiatedByUserId = userId,
      ScriptId = scriptId,
      ScriptType = scriptType,
      StartedAt = DateTimeOffset.UtcNow,
      Status = "Running",
      TenantId = tenantId,
    };

    await appDb.ScriptExecutions.AddAsync(execution);
    await appDb.SaveChangesAsync();

    // Create result records for each device
    var results = new List<ScriptExecutionResult>();
    foreach (var device in authorizedDevices)
    {
      var result = new ScriptExecutionResult
      {
        DeviceId = device.Id,
        DeviceName = device.Name,
        ScriptExecutionId = execution.Id,
        Status = "Pending",
        TenantId = tenantId,
      };
      results.Add(result);
    }

    await appDb.ScriptExecutionResults.AddRangeAsync(results);
    await appDb.SaveChangesAsync();

    // Fan out to agents
    foreach (var device in authorizedDevices)
    {
      var resultId = results.First(r => r.DeviceId == device.Id).Id;
      var hubDto = new ScriptExecutionRequestHubDto(
        execution.Id,
        resultId,
        scriptContent,
        scriptType);

      try
      {
        await agentHub.Clients
          .Client(device.ConnectionId)
          .ExecuteScript(hubDto);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Failed to send script to device {DeviceId}.", device.Id);
      }
    }

    // Reload with nav properties for DTO
    var savedExecution = await appDb.ScriptExecutions
      .AsNoTracking()
      .Include(x => x.Script)
      .Include(x => x.Results)
      .FirstAsync(x => x.Id == execution.Id);

    auditService.LogEvent(
      tenantId,
      AuditEventTypes.ScriptExecution,
      AuditActions.Execute,
      actorUserId: userId,
      actorUserName: User.Identity?.Name,
      targetDeviceId: authorizedDevices.First().Id,
      targetDeviceName: authorizedDevices.First().Name,
      sourceIpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
      details: $"API Execution: {execution.Id}, Devices: {authorizedDevices.Count}");

    return Ok(savedExecution.ToDto());
  }

  [HttpGet("{executionId:guid}")]
  public async Task<ActionResult<ScriptExecutionDto>> GetExecution(
    [FromServices] AppDb appDb,
    [FromRoute] Guid executionId)
  {
    var execution = await appDb.ScriptExecutions
      .AsNoTracking()
      .Include(x => x.Script)
      .Include(x => x.Results)
      .FirstOrDefaultAsync(x => x.Id == executionId);

    if (execution is null)
    {
      return NotFound();
    }

    return Ok(execution.ToDto());
  }

  [HttpGet]
  public async Task<ActionResult<ScriptExecutionDto[]>> GetRecentExecutions(
    [FromServices] AppDb appDb,
    [FromQuery] int count = 50)
  {
    var executions = await appDb.ScriptExecutions
      .AsNoTracking()
      .Include(x => x.Script)
      .Include(x => x.Results)
      .OrderByDescending(x => x.StartedAt)
      .Take(count)
      .ToListAsync();

    var dtos = executions.Select(x => x.ToDto()).ToArray();
    return Ok(dtos);
  }
}
