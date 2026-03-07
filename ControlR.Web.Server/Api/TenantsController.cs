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
  /// Idempotent provisioning endpoint. Creates tenant + admin user + PAT if they don't exist.
  /// Returns existing resources if they do.
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

    // 2. Find or create admin user in this tenant
    var normalizedEmail = request.AdminEmail.Trim().ToLowerInvariant();
    var user = await appDb.Users
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail.ToUpperInvariant());

    if (user is not null && user.TenantId != tenant.Id)
    {
      return Conflict($"User '{normalizedEmail}' already exists in a different tenant.");
    }

    if (user is null)
    {
      var password = request.AdminPassword ?? RandomGenerator.CreateAccessToken()[..16];
      var createResult = await userCreator.CreateUser(normalizedEmail, password, tenant.Id);

      if (!createResult.Succeeded)
      {
        var errors = string.Join("; ", createResult.IdentityResult.Errors.Select(e => e.Description));
        return BadRequest($"Failed to create user: {errors}");
      }

      user = createResult.User!;

      // Assign tenant admin roles
      await userManager.AddToRoleAsync(user, RoleNames.TenantAdministrator);
      await userManager.AddToRoleAsync(user, RoleNames.DeviceSuperUser);
      await userManager.AddToRoleAsync(user, RoleNames.AgentInstaller);
      await userManager.AddToRoleAsync(user, RoleNames.InstallerKeyManager);

      userCreated = true;
      logger.LogInformation("Provisioned admin user '{Email}' in tenant '{TenantName}'.", normalizedEmail, request.TenantName);
    }

    // 3. Find existing PAT or create a new one
    var existingPats = await appDb.PersonalAccessTokens
      .IgnoreQueryFilters()
      .Where(p => p.UserId == user.Id)
      .ToListAsync();

    string plainTextToken;

    var provisionPat = existingPats.FirstOrDefault(p => p.Name.StartsWith("Provisioned-"));

    if (provisionPat is not null && !patCreated)
    {
      // PAT exists but we can't retrieve the plaintext. Create a new one and delete the old.
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

    return Ok(new ProvisionTenantResponseDto(
      tenant.Id,
      request.TenantName,
      user.Id,
      normalizedEmail,
      plainTextToken,
      tenantCreated,
      userCreated,
      patCreated));
  }
}
