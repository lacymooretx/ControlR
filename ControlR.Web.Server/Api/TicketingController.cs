using System.Security.Claims;
using ControlR.Libraries.Shared.Dtos.ServerApi;
using ControlR.Libraries.Shared.Enums;
using ControlR.Web.Server.Services.Ticketing;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/ticketing")]
[ApiController]
[Authorize(Roles = RoleNames.TenantAdministrator)]
public class TicketingController : ControllerBase
{
  // --- Integration CRUD ---

  [HttpGet("integrations")]
  public async Task<ActionResult<TicketingIntegrationDto[]>> GetAllIntegrations(
    [FromServices] AppDb appDb)
  {
    var integrations = await appDb.TicketingIntegrations
      .AsNoTracking()
      .OrderBy(x => x.Name)
      .ToListAsync();

    var dtos = integrations.Select(x => x.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpGet("integrations/{id:guid}")]
  public async Task<ActionResult<TicketingIntegrationDto>> GetIntegration(
    [FromServices] AppDb appDb,
    [FromRoute] Guid id)
  {
    var integration = await appDb.TicketingIntegrations
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == id);

    if (integration is null)
    {
      return NotFound();
    }

    return Ok(integration.ToDto());
  }

  [HttpPost("integrations")]
  public async Task<ActionResult<TicketingIntegrationDto>> CreateIntegration(
    [FromServices] AppDb appDb,
    [FromServices] ICredentialEncryptionService encryptionService,
    [FromBody] CreateTicketingIntegrationDto dto)
  {
    if (!User.TryGetTenantId(out var tenantId))
    {
      return NotFound("User tenant not found.");
    }

    var encryptedApiKey = encryptionService.Encrypt(dto.ApiKey, tenantId);

    var integration = new TicketingIntegration
    {
      Name = dto.Name,
      Provider = dto.Provider,
      BaseUrl = dto.BaseUrl,
      EncryptedApiKey = encryptedApiKey,
      DefaultProject = dto.DefaultProject,
      FieldMappingJson = dto.FieldMappingJson,
      TenantId = tenantId,
    };

    await appDb.TicketingIntegrations.AddAsync(integration);
    await appDb.SaveChangesAsync();

    return Ok(integration.ToDto());
  }

  [HttpPut("integrations/{id:guid}")]
  public async Task<ActionResult<TicketingIntegrationDto>> UpdateIntegration(
    [FromServices] AppDb appDb,
    [FromServices] ICredentialEncryptionService encryptionService,
    [FromRoute] Guid id,
    [FromBody] UpdateTicketingIntegrationDto dto)
  {
    if (!User.TryGetTenantId(out var tenantId))
    {
      return NotFound("User tenant not found.");
    }

    var integration = await appDb.TicketingIntegrations
      .FirstOrDefaultAsync(x => x.Id == id);

    if (integration is null)
    {
      return NotFound();
    }

    integration.Name = dto.Name;
    integration.Provider = dto.Provider;
    integration.BaseUrl = dto.BaseUrl;
    integration.DefaultProject = dto.DefaultProject;
    integration.IsEnabled = dto.IsEnabled;
    integration.FieldMappingJson = dto.FieldMappingJson;

    if (!string.IsNullOrEmpty(dto.ApiKey))
    {
      integration.EncryptedApiKey = encryptionService.Encrypt(dto.ApiKey, tenantId);
    }

    await appDb.SaveChangesAsync();

    return Ok(integration.ToDto());
  }

  [HttpDelete("integrations/{id:guid}")]
  public async Task<ActionResult> DeleteIntegration(
    [FromServices] AppDb appDb,
    [FromRoute] Guid id)
  {
    var integration = await appDb.TicketingIntegrations.FindAsync(id);
    if (integration is null)
    {
      return NotFound();
    }

    appDb.TicketingIntegrations.Remove(integration);
    await appDb.SaveChangesAsync();

    return NoContent();
  }

  // --- Ticket creation ---

  [HttpPost("tickets")]
  public async Task<ActionResult<TicketLinkDto>> CreateTicket(
    [FromServices] AppDb appDb,
    [FromServices] ICredentialEncryptionService encryptionService,
    [FromServices] IEnumerable<ITicketingProvider> ticketingProviders,
    [FromBody] CreateTicketRequestDto dto)
  {
    if (!User.TryGetTenantId(out var tenantId))
    {
      return NotFound("User tenant not found.");
    }

    if (!User.TryGetUserId(out var userId))
    {
      return Unauthorized();
    }

    var integration = await appDb.TicketingIntegrations
      .FirstOrDefaultAsync(x => x.Id == dto.IntegrationId);

    if (integration is null)
    {
      return NotFound("Integration not found.");
    }

    if (!integration.IsEnabled)
    {
      return BadRequest("Integration is disabled.");
    }

    // Find a matching provider, or fall back to the Custom (webhook) provider
    var provider = ticketingProviders.FirstOrDefault(p => p.ProviderType == integration.Provider)
      ?? ticketingProviders.FirstOrDefault(p => p.ProviderType == TicketingProvider.Custom);

    if (provider is null)
    {
      return StatusCode(500, "No ticketing provider available.");
    }

    string decryptedApiKey;
    try
    {
      decryptedApiKey = encryptionService.Decrypt(integration.EncryptedApiKey, tenantId);
    }
    catch (Exception)
    {
      return StatusCode(500, "Failed to decrypt API key.");
    }

    var createResult = await provider.CreateTicket(integration, decryptedApiKey, dto);
    if (!createResult.IsSuccess)
    {
      return StatusCode(500, createResult.Reason);
    }

    var externalTicketId = createResult.Value;
    var externalUrl = $"{integration.BaseUrl.TrimEnd('/')}/ticket/{externalTicketId}";

    var ticketLink = new TicketLink
    {
      ExternalTicketId = externalTicketId,
      ExternalTicketUrl = externalUrl,
      Provider = integration.Provider,
      Subject = dto.Subject,
      DeviceId = dto.DeviceId,
      SessionId = dto.SessionId,
      AlertId = dto.AlertId,
      CreatedByUserId = userId,
      TenantId = tenantId,
    };

    await appDb.TicketLinks.AddAsync(ticketLink);
    await appDb.SaveChangesAsync();

    return Ok(ticketLink.ToDto());
  }

  // --- Ticket link queries ---

  [HttpGet("tickets")]
  public async Task<ActionResult<TicketLinkDto[]>> GetTicketLinks(
    [FromServices] AppDb appDb,
    [FromQuery] Guid? deviceId = null,
    [FromQuery] Guid? sessionId = null,
    [FromQuery] Guid? alertId = null)
  {
    var query = appDb.TicketLinks.AsNoTracking().AsQueryable();

    if (deviceId.HasValue)
    {
      query = query.Where(x => x.DeviceId == deviceId.Value);
    }

    if (sessionId.HasValue)
    {
      query = query.Where(x => x.SessionId == sessionId.Value);
    }

    if (alertId.HasValue)
    {
      query = query.Where(x => x.AlertId == alertId.Value);
    }

    var links = await query
      .OrderByDescending(x => x.CreatedAt)
      .Take(100)
      .ToListAsync();

    var dtos = links.Select(x => x.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpDelete("tickets/{id:guid}")]
  public async Task<ActionResult> DeleteTicketLink(
    [FromServices] AppDb appDb,
    [FromRoute] Guid id)
  {
    var link = await appDb.TicketLinks.FindAsync(id);
    if (link is null)
    {
      return NotFound();
    }

    appDb.TicketLinks.Remove(link);
    await appDb.SaveChangesAsync();

    return NoContent();
  }
}
