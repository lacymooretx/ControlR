using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/script-executions")]
[ApiController]
[Authorize]
public class ScriptExecutionsController : ControllerBase
{
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
