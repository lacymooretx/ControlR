using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/suggestions")]
[ApiController]
[Authorize]
public class SuggestionsController : ControllerBase
{
  [HttpGet]
  [Authorize(Roles = $"{RoleNames.TenantAdministrator}")]
  public async Task<ActionResult<AutomationSuggestionDto[]>> GetSuggestions(
    [FromServices] AppDb appDb,
    [FromQuery] string? status = null,
    [FromQuery] int count = 50)
  {
    IQueryable<AutomationSuggestion> query = appDb.AutomationSuggestions
      .AsNoTracking();

    if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SuggestionStatus>(status, true, out var statusEnum))
    {
      query = query.Where(x => x.Status == statusEnum);
    }

    var suggestions = await query
      .OrderByDescending(x => x.CreatedAt)
      .Take(count)
      .ToListAsync();

    var dtos = suggestions.Select(x => x.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpPut("{suggestionId:guid}")]
  [Authorize(Roles = $"{RoleNames.TenantAdministrator}")]
  public async Task<ActionResult<AutomationSuggestionDto>> UpdateSuggestion(
    [FromServices] AppDb appDb,
    [FromRoute] Guid suggestionId,
    [FromBody] AutomationSuggestionUpdateRequestDto request)
  {
    var suggestion = await appDb.AutomationSuggestions
      .FirstOrDefaultAsync(x => x.Id == suggestionId);

    if (suggestion is null)
    {
      return NotFound();
    }

    if (!Enum.TryParse<SuggestionStatus>(request.Status, true, out var newStatus))
    {
      return BadRequest("Invalid status value.");
    }

    suggestion.Status = newStatus;
    await appDb.SaveChangesAsync();

    return Ok(suggestion.ToDto());
  }
}
