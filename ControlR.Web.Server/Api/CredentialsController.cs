using System.Security.Claims;
using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/credentials")]
[ApiController]
[Authorize(Roles = RoleNames.TenantAdministrator)]
public class CredentialsController : ControllerBase
{
  [HttpGet]
  public async Task<ActionResult<StoredCredentialDto[]>> GetAllCredentials(
    [FromServices] AppDb appDb)
  {
    var credentials = await appDb.StoredCredentials
      .AsNoTracking()
      .OrderBy(x => x.Name)
      .ToListAsync();

    var dtos = credentials.Select(x => x.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpGet("{id:guid}")]
  public async Task<ActionResult<StoredCredentialDto>> GetCredential(
    [FromServices] AppDb appDb,
    [FromRoute] Guid id)
  {
    var credential = await appDb.StoredCredentials
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == id);

    if (credential is null)
    {
      return NotFound();
    }

    return Ok(credential.ToDto());
  }

  [RequiresVerification]
  [HttpPost("{id:guid}/retrieve")]
  public async Task<ActionResult<StoredCredentialWithPasswordDto>> RetrieveCredentialPassword(
    [FromServices] AppDb appDb,
    [FromServices] ICredentialEncryptionService encryptionService,
    [FromServices] IAuditService auditService,
    [FromRoute] Guid id)
  {
    if (!User.TryGetTenantId(out var tenantId))
    {
      return NotFound("User tenant not found.");
    }

    if (!User.TryGetUserId(out var userId))
    {
      return Unauthorized();
    }

    var credential = await appDb.StoredCredentials
      .FirstOrDefaultAsync(x => x.Id == id);

    if (credential is null)
    {
      return NotFound();
    }

    string decryptedPassword;
    try
    {
      decryptedPassword = encryptionService.Decrypt(credential.EncryptedPassword, tenantId);
    }
    catch (Exception)
    {
      return StatusCode(500, "Failed to decrypt credential.");
    }

    // Update access tracking
    credential.LastAccessedAt = DateTimeOffset.UtcNow;
    credential.AccessCount++;
    await appDb.SaveChangesAsync();

    // Audit log the password retrieval
    var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email);
    var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();

    auditService.LogEvent(
      tenantId,
      eventType: "CredentialVault",
      action: "RetrievePassword",
      actorUserId: userId,
      actorUserName: userName,
      details: $"Retrieved password for credential '{credential.Name}' (ID: {credential.Id})",
      sourceIpAddress: sourceIp);

    return Ok(new StoredCredentialWithPasswordDto(
      credential.Id,
      credential.Name,
      credential.Username,
      decryptedPassword,
      credential.Domain));
  }

  [HttpPost]
  public async Task<ActionResult<StoredCredentialDto>> CreateCredential(
    [FromServices] AppDb appDb,
    [FromServices] ICredentialEncryptionService encryptionService,
    [FromServices] IAuditService auditService,
    [FromBody] CreateCredentialRequestDto dto)
  {
    if (!User.TryGetTenantId(out var tenantId))
    {
      return NotFound("User tenant not found.");
    }

    if (!User.TryGetUserId(out var userId))
    {
      return Unauthorized();
    }

    var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email);

    var encryptedPassword = encryptionService.Encrypt(dto.Password, tenantId);

    var credential = new StoredCredential
    {
      Name = dto.Name,
      Description = dto.Description,
      Username = dto.Username,
      EncryptedPassword = encryptedPassword,
      Domain = dto.Domain,
      DeviceId = dto.DeviceId,
      DeviceGroupId = dto.DeviceGroupId,
      Category = dto.Category,
      CreatedByUserId = userId,
      CreatedByUserName = userName,
      TenantId = tenantId,
    };

    await appDb.StoredCredentials.AddAsync(credential);
    await appDb.SaveChangesAsync();

    auditService.LogEvent(
      tenantId,
      eventType: "CredentialVault",
      action: "Create",
      actorUserId: userId,
      actorUserName: userName,
      details: $"Created credential '{credential.Name}' (ID: {credential.Id})",
      sourceIpAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

    return Ok(credential.ToDto());
  }

  [HttpPut("{id:guid}")]
  public async Task<ActionResult<StoredCredentialDto>> UpdateCredential(
    [FromServices] AppDb appDb,
    [FromServices] ICredentialEncryptionService encryptionService,
    [FromServices] IAuditService auditService,
    [FromRoute] Guid id,
    [FromBody] UpdateCredentialRequestDto dto)
  {
    if (!User.TryGetTenantId(out var tenantId))
    {
      return NotFound("User tenant not found.");
    }

    if (!User.TryGetUserId(out var userId))
    {
      return Unauthorized();
    }

    var credential = await appDb.StoredCredentials
      .FirstOrDefaultAsync(x => x.Id == id);

    if (credential is null)
    {
      return NotFound();
    }

    credential.Name = dto.Name;
    credential.Description = dto.Description;
    credential.Username = dto.Username;
    credential.Domain = dto.Domain;
    credential.DeviceId = dto.DeviceId;
    credential.DeviceGroupId = dto.DeviceGroupId;
    credential.Category = dto.Category;

    // Only update password if a new one is provided
    if (!string.IsNullOrEmpty(dto.Password))
    {
      credential.EncryptedPassword = encryptionService.Encrypt(dto.Password, tenantId);
    }

    await appDb.SaveChangesAsync();

    var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email);
    auditService.LogEvent(
      tenantId,
      eventType: "CredentialVault",
      action: "Update",
      actorUserId: userId,
      actorUserName: userName,
      details: $"Updated credential '{credential.Name}' (ID: {credential.Id})",
      sourceIpAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

    return Ok(credential.ToDto());
  }

  [RequiresVerification]
  [HttpDelete("{id:guid}")]
  public async Task<ActionResult> DeleteCredential(
    [FromServices] AppDb appDb,
    [FromServices] IAuditService auditService,
    [FromRoute] Guid id)
  {
    if (!User.TryGetTenantId(out var tenantId))
    {
      return NotFound("User tenant not found.");
    }

    if (!User.TryGetUserId(out var userId))
    {
      return Unauthorized();
    }

    var credential = await appDb.StoredCredentials
      .FirstOrDefaultAsync(x => x.Id == id);

    if (credential is null)
    {
      return NotFound();
    }

    var credentialName = credential.Name;
    appDb.StoredCredentials.Remove(credential);
    await appDb.SaveChangesAsync();

    var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email);
    auditService.LogEvent(
      tenantId,
      eventType: "CredentialVault",
      action: "Delete",
      actorUserId: userId,
      actorUserName: userName,
      details: $"Deleted credential '{credentialName}' (ID: {id})",
      sourceIpAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

    return NoContent();
  }

  [HttpGet("for-device/{deviceId:guid}")]
  public async Task<ActionResult<StoredCredentialDto[]>> GetCredentialsForDevice(
    [FromServices] AppDb appDb,
    [FromRoute] Guid deviceId)
  {
    // Get the device to find its group
    var device = await appDb.Devices
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == deviceId);

    if (device is null)
    {
      return NotFound("Device not found.");
    }

    // Get credentials that are:
    // 1. Scoped to this specific device, OR
    // 2. Scoped to this device's group (if it has one), OR
    // 3. Tenant-wide (no device or group scope)
    var query = appDb.StoredCredentials
      .AsNoTracking()
      .Where(x =>
        x.DeviceId == deviceId ||
        (device.DeviceGroupId != null && x.DeviceGroupId == device.DeviceGroupId) ||
        (x.DeviceId == null && x.DeviceGroupId == null));

    var credentials = await query
      .OrderBy(x => x.Name)
      .ToListAsync();

    var dtos = credentials.Select(x => x.ToDto()).ToArray();
    return Ok(dtos);
  }
}
