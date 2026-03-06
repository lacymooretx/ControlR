using System.Security.Claims;
using System.Security.Cryptography;
using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/support-sessions")]
[ApiController]
[Authorize(Roles = RoleNames.TenantAdministrator)]
public class SupportSessionsController : ControllerBase
{
  [HttpGet]
  public async Task<ActionResult<SupportSessionDto[]>> GetSupportSessions(
    [FromServices] AppDb appDb,
    [FromQuery] string? status = null)
  {
    var query = appDb.SupportSessions.AsNoTracking();

    if (!string.IsNullOrEmpty(status) && Enum.TryParse<SupportSessionStatus>(status, true, out var statusEnum))
    {
      query = query.Where(s => s.Status == statusEnum);
    }

    var sessions = await query
      .OrderByDescending(s => s.CreatedAt)
      .ToListAsync();

    var dtos = sessions.Select(s => s.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpPost]
  public async Task<ActionResult<SupportSessionDto>> CreateSupportSession(
    [FromServices] AppDb appDb,
    [FromBody] SupportSessionCreateRequestDto request)
  {
    var tenantId = User.FindFirstValue("TenantId");
    if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var tenantGuid))
    {
      return BadRequest("Invalid tenant.");
    }

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
    {
      return BadRequest("Invalid user.");
    }

    var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email);

    var accessCode = GenerateAccessCode();

    var expirationMinutes = request.ExpirationMinutes;
    if (expirationMinutes <= 0 || expirationMinutes > 480)
    {
      expirationMinutes = 60;
    }

    var session = new SupportSession
    {
      AccessCode = accessCode,
      ClientName = request.ClientName,
      ClientEmail = request.ClientEmail,
      CreatorUserId = userGuid,
      CreatorUserName = userName,
      ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes),
      Notes = request.Notes,
      Status = SupportSessionStatus.WaitingForClient,
      TenantId = tenantGuid,
    };

    await appDb.SupportSessions.AddAsync(session);
    await appDb.SaveChangesAsync();

    return Ok(session.ToDto());
  }

  [HttpPost("join")]
  [AllowAnonymous]
  public async Task<ActionResult<SupportSessionJoinResponseDto>> JoinSupportSession(
    [FromServices] IDbContextFactory<AppDb> dbContextFactory,
    [FromBody] SupportSessionJoinRequestDto request)
  {
    if (string.IsNullOrWhiteSpace(request.AccessCode))
    {
      return BadRequest("Access code is required.");
    }

    // Use a factory-created context without tenant filter for anonymous access.
    await using var appDb = await dbContextFactory.CreateDbContextAsync();

    var session = await appDb.SupportSessions
      .FirstOrDefaultAsync(s =>
        s.AccessCode == request.AccessCode &&
        s.Status == SupportSessionStatus.WaitingForClient &&
        s.ExpiresAt > DateTimeOffset.UtcNow);

    if (session is null)
    {
      return NotFound("Invalid or expired access code.");
    }

    session.ClientName = request.ClientName ?? session.ClientName;
    session.IsUsed = true;
    session.SessionStartedAt = DateTimeOffset.UtcNow;
    session.Status = SupportSessionStatus.InProgress;

    await appDb.SaveChangesAsync();

    var serverUrl = $"{Request.Scheme}://{Request.Host}";

    var response = new SupportSessionJoinResponseDto(
      session.Id,
      serverUrl,
      session.TenantId,
      session.CreatorUserName);

    return Ok(response);
  }

  [HttpDelete("{id:guid}")]
  public async Task<ActionResult> CancelSupportSession(
    [FromServices] AppDb appDb,
    [FromRoute] Guid id)
  {
    var session = await appDb.SupportSessions.FindAsync(id);
    if (session is null)
    {
      return NotFound();
    }

    if (session.Status is SupportSessionStatus.Completed or SupportSessionStatus.Expired)
    {
      return BadRequest("Cannot cancel a completed or expired session.");
    }

    session.Status = SupportSessionStatus.Cancelled;
    await appDb.SaveChangesAsync();

    return NoContent();
  }

  [HttpPut("{id:guid}/complete")]
  public async Task<ActionResult<SupportSessionDto>> CompleteSupportSession(
    [FromServices] AppDb appDb,
    [FromRoute] Guid id)
  {
    var session = await appDb.SupportSessions.FindAsync(id);
    if (session is null)
    {
      return NotFound();
    }

    session.Status = SupportSessionStatus.Completed;
    session.SessionEndedAt = DateTimeOffset.UtcNow;
    await appDb.SaveChangesAsync();

    return Ok(session.ToDto());
  }

  private static string GenerateAccessCode()
  {
    // Generate a random 8-digit numeric code.
    var code = RandomNumberGenerator.GetInt32(10000000, 100000000);
    return code.ToString("D8");
  }
}
