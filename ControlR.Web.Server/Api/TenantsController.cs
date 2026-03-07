using ControlR.Libraries.Shared.Helpers;
using ControlR.Web.Server.Services.Users;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = RoleNames.ServerAdministrator)]
public class TenantsController : ControllerBase
{
  [HttpGet]
  public async Task<ActionResult<List<TenantResponseDto>>> GetAll(
    [FromServices] AppDb appDb)
  {
    var tenants = await appDb.Tenants
      .IgnoreQueryFilters()
      .OrderBy(t => t.Name)
      .Select(t => new TenantResponseDto(t.Id, t.Name, t.CreatedAt))
      .ToListAsync();

    return Ok(tenants);
  }

  [HttpGet("{id:guid}")]
  public async Task<ActionResult<TenantResponseDto>> Get(
    [FromRoute] Guid id,
    [FromServices] AppDb appDb)
  {
    var tenant = await appDb.Tenants
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(t => t.Id == id);

    if (tenant is null)
    {
      return NotFound();
    }

    return Ok(new TenantResponseDto(tenant.Id, tenant.Name, tenant.CreatedAt));
  }

  [HttpPost]
  public async Task<ActionResult<TenantResponseDto>> Create(
    [FromBody] CreateTenantRequestDto request,
    [FromServices] AppDb appDb)
  {
    var exists = await appDb.Tenants
      .IgnoreQueryFilters()
      .AnyAsync(t => t.Name == request.Name);

    if (exists)
    {
      return Conflict($"A tenant with name '{request.Name}' already exists.");
    }

    var tenant = new Tenant { Name = request.Name };
    appDb.Tenants.Add(tenant);
    await appDb.SaveChangesAsync();

    return CreatedAtAction(nameof(Get), new { id = tenant.Id },
      new TenantResponseDto(tenant.Id, tenant.Name, tenant.CreatedAt));
  }

  [HttpPut("{id:guid}")]
  public async Task<ActionResult<TenantResponseDto>> Update(
    [FromRoute] Guid id,
    [FromBody] UpdateTenantRequestDto request,
    [FromServices] AppDb appDb)
  {
    var tenant = await appDb.Tenants
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(t => t.Id == id);

    if (tenant is null)
    {
      return NotFound();
    }

    var nameConflict = await appDb.Tenants
      .IgnoreQueryFilters()
      .AnyAsync(t => t.Name == request.Name && t.Id != id);

    if (nameConflict)
    {
      return Conflict($"A tenant with name '{request.Name}' already exists.");
    }

    tenant.Name = request.Name;
    await appDb.SaveChangesAsync();

    return Ok(new TenantResponseDto(tenant.Id, tenant.Name, tenant.CreatedAt));
  }

  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Delete(
    [FromRoute] Guid id,
    [FromServices] AppDb appDb,
    [FromServices] ILogger<TenantsController> logger)
  {
    var tenant = await appDb.Tenants
      .IgnoreQueryFilters()
      .Include(t => t.Users)
      .Include(t => t.Devices)
      .FirstOrDefaultAsync(t => t.Id == id);

    if (tenant is null)
    {
      return NotFound();
    }

    var deviceCount = tenant.Devices?.Count ?? 0;
    var userCount = tenant.Users?.Count ?? 0;

    if (deviceCount > 0 || userCount > 0)
    {
      return BadRequest($"Cannot delete tenant with {userCount} user(s) and {deviceCount} device(s). Remove them first.");
    }

    appDb.Tenants.Remove(tenant);
    await appDb.SaveChangesAsync();

    logger.LogInformation("Tenant {TenantId} ({TenantName}) deleted.", id, tenant.Name);
    return NoContent();
  }

  /// <summary>
  /// Idempotent provisioning endpoint. Finds or creates a tenant by name.
  /// Optionally creates an admin user and PAT if AdminEmail is provided.
  /// </summary>
  [HttpPost("provision")]
  public async Task<ActionResult<ProvisionTenantResponseDto>> Provision(
    [FromBody] ProvisionTenantRequestDto request,
    [FromServices] AppDb appDb,
    [FromServices] UserManager<AppUser> userManager,
    [FromServices] IUserCreator userCreator,
    [FromServices] IPersonalAccessTokenManager patManager,
    [FromServices] ILogger<TenantsController> logger)
  {
    var tenantCreated = false;
    var userCreated = false;
    var patCreated = false;
    Guid? userId = null;
    string? adminEmail = null;
    string? plainTextToken = null;

    // 1. Find or create tenant by name
    var tenant = await appDb.Tenants
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(t => t.Name == request.TenantName);

    if (tenant is null)
    {
      tenant = new Tenant { Name = request.TenantName };
      appDb.Tenants.Add(tenant);
      await appDb.SaveChangesAsync();
      tenantCreated = true;
      logger.LogInformation("Provisioned new tenant '{TenantName}' (ID: {TenantId}).", request.TenantName, tenant.Id);
    }

    // 2. If AdminEmail provided, find or create admin user + PAT
    if (!string.IsNullOrWhiteSpace(request.AdminEmail))
    {
      adminEmail = request.AdminEmail.Trim().ToLowerInvariant();
      var user = await appDb.Users
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(u => u.NormalizedEmail == adminEmail.ToUpperInvariant());

      if (user is not null && user.TenantId != tenant.Id)
      {
        return Conflict($"User '{adminEmail}' already exists in a different tenant.");
      }

      if (user is null)
      {
        var password = request.AdminPassword ?? RandomGenerator.CreateAccessToken()[..16];
        var createResult = await userCreator.CreateUser(adminEmail, password, tenant.Id);

        if (!createResult.Succeeded)
        {
          var errors = string.Join("; ", createResult.IdentityResult.Errors.Select(e => e.Description));
          return BadRequest($"Failed to create user: {errors}");
        }

        user = createResult.User!;

        await userManager.AddToRoleAsync(user, RoleNames.TenantAdministrator);
        await userManager.AddToRoleAsync(user, RoleNames.DeviceSuperUser);
        await userManager.AddToRoleAsync(user, RoleNames.AgentInstaller);
        await userManager.AddToRoleAsync(user, RoleNames.InstallerKeyManager);

        userCreated = true;
        logger.LogInformation("Provisioned admin user '{Email}' in tenant '{TenantName}'.", adminEmail, request.TenantName);
      }

      userId = user.Id;

      // Replace any existing provisioned PAT (can't retrieve plaintext of old one)
      var provisionPat = await appDb.PersonalAccessTokens
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(p => p.UserId == user.Id && p.Name.StartsWith("Provisioned-"));

      if (provisionPat is not null)
      {
        appDb.PersonalAccessTokens.Remove(provisionPat);
        await appDb.SaveChangesAsync();
      }

      var patResult = await patManager.CreateToken(
        new CreatePersonalAccessTokenRequestDto($"Provisioned-{request.TenantName}"),
        user.Id);

      if (!patResult.IsSuccess)
      {
        return BadRequest($"Failed to create PAT: {patResult.Reason}");
      }

      plainTextToken = patResult.Value.PlainTextToken;
      patCreated = true;
    }

    return Ok(new ProvisionTenantResponseDto(
      tenant.Id,
      request.TenantName,
      tenantCreated,
      userId,
      adminEmail,
      plainTextToken,
      userCreated,
      patCreated));
  }

  /// <summary>
  /// Find a device by name across all tenants. Used by ImmyBot metascript
  /// to locate a device before reassigning it to the correct tenant.
  /// </summary>
  [HttpGet("devices")]
  public async Task<ActionResult<DeviceResponseDto>> FindDeviceByName(
    [FromQuery] string name,
    [FromServices] AppDb appDb)
  {
    if (string.IsNullOrWhiteSpace(name))
    {
      return BadRequest("Device name is required.");
    }

    var device = await appDb.Devices
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(d => d.Name == name);

    if (device is null)
    {
      return NotFound($"Device '{name}' not found.");
    }

    return Ok(device.ToDto(isOutdated: false));
  }

  /// <summary>
  /// Reassign a device to a different tenant.
  /// </summary>
  [HttpPut("devices/{deviceId:guid}/tenant")]
  public async Task<ActionResult<ReassignDeviceTenantResponseDto>> ReassignDeviceTenant(
    [FromRoute] Guid deviceId,
    [FromBody] ReassignDeviceTenantRequestDto request,
    [FromServices] AppDb appDb,
    [FromServices] ILogger<TenantsController> logger)
  {
    var device = await appDb.Devices
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(d => d.Id == deviceId);

    if (device is null)
    {
      return NotFound("Device not found.");
    }

    var newTenant = await appDb.Tenants
      .IgnoreQueryFilters()
      .AnyAsync(t => t.Id == request.NewTenantId);

    if (!newTenant)
    {
      return BadRequest("Target tenant not found.");
    }

    var previousTenantId = device.TenantId;

    if (previousTenantId == request.NewTenantId)
    {
      return Ok(new ReassignDeviceTenantResponseDto(
        device.Id, device.Name, previousTenantId, request.NewTenantId));
    }

    device.TenantId = request.NewTenantId;

    // Clear device group assignment (groups are tenant-scoped)
    device.DeviceGroupId = null;

    // Move related tenant-scoped records
    await appDb.SoftwareInventoryItems
      .IgnoreQueryFilters()
      .Where(s => s.DeviceId == deviceId)
      .ExecuteUpdateAsync(s => s.SetProperty(x => x.TenantId, request.NewTenantId));

    await appDb.InstalledUpdates
      .IgnoreQueryFilters()
      .Where(u => u.DeviceId == deviceId)
      .ExecuteUpdateAsync(u => u.SetProperty(x => x.TenantId, request.NewTenantId));

    await appDb.PendingPatches
      .IgnoreQueryFilters()
      .Where(p => p.DeviceId == deviceId)
      .ExecuteUpdateAsync(p => p.SetProperty(x => x.TenantId, request.NewTenantId));

    await appDb.SaveChangesAsync();

    logger.LogInformation(
      "Device {DeviceId} ({DeviceName}) reassigned from tenant {OldTenantId} to {NewTenantId}.",
      deviceId, device.Name, previousTenantId, request.NewTenantId);

    return Ok(new ReassignDeviceTenantResponseDto(
      device.Id, device.Name, previousTenantId, request.NewTenantId));
  }
}
