using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/client-portal")]
[ApiController]
[Authorize]
public class ClientPortalController : ControllerBase
{
  [HttpPost("assignments")]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult<ClientDeviceAssignmentDto>> AssignDevice(
    [FromServices] AppDb appDb,
    [FromBody] ClientDeviceAssignmentCreateRequestDto request)
  {
    if (!User.TryGetTenantId(out var tenantId))
    {
      return NotFound("User tenant not found.");
    }

    var existing = await appDb.ClientDeviceAssignments
      .FirstOrDefaultAsync(x =>
        x.ClientUserId == request.ClientUserId &&
        x.DeviceId == request.DeviceId);

    if (existing is not null)
    {
      existing.ExpiresAt = request.ExpiresAt;
      await appDb.SaveChangesAsync();
      return Ok(await ToDto(appDb, existing));
    }

    var assignment = new ClientDeviceAssignment
    {
      ClientUserId = request.ClientUserId,
      DeviceId = request.DeviceId,
      ExpiresAt = request.ExpiresAt,
      TenantId = tenantId,
    };

    await appDb.ClientDeviceAssignments.AddAsync(assignment);
    await appDb.SaveChangesAsync();

    return Ok(await ToDto(appDb, assignment));
  }

  [HttpGet("assignments/by-user/{userId:guid}")]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult<ClientDeviceAssignmentDto[]>> GetAssignments(
    [FromServices] AppDb appDb,
    [FromRoute] Guid userId)
  {
    var assignments = await appDb.ClientDeviceAssignments
      .AsNoTracking()
      .Include(x => x.ClientUser)
      .Include(x => x.Device)
      .Where(x => x.ClientUserId == userId)
      .ToListAsync();

    var dtos = assignments.Select(x => new ClientDeviceAssignmentDto(
      x.Id,
      x.ClientUserId,
      x.ClientUser?.UserName,
      x.DeviceId,
      x.Device?.Name,
      x.ExpiresAt)).ToArray();

    return Ok(dtos);
  }

  [HttpGet("devices")]
  [Authorize(Roles = RoleNames.ClientUser)]
  public async Task<ActionResult<DeviceResponseDto[]>> GetClientDevices(
    [FromServices] AppDb appDb,
    [FromServices] IAgentVersionProvider agentVersionProvider)
  {
    if (!User.TryGetUserId(out var userId))
    {
      return Unauthorized();
    }

    // Get devices from explicit assignments
    var assignedDeviceIds = await appDb.ClientDeviceAssignments
      .AsNoTracking()
      .Where(x =>
        x.ClientUserId == userId &&
        (x.ExpiresAt == null || x.ExpiresAt > DateTimeOffset.UtcNow))
      .Select(x => x.DeviceId)
      .ToListAsync();

    // Get devices from tag-based access
    var user = await appDb.Users
      .AsNoTracking()
      .Include(x => x.Tags)
      .FirstOrDefaultAsync(x => x.Id == userId);

    var tagDeviceIds = new List<Guid>();
    if (user?.Tags?.Count > 0)
    {
      var userTagIds = user.Tags.Select(t => t.Id).ToList();
      tagDeviceIds = await appDb.Devices
        .AsNoTracking()
        .Where(d => d.Tags != null && d.Tags.Any(t => userTagIds.Contains(t.Id)))
        .Select(d => d.Id)
        .ToListAsync();
    }

    var allDeviceIds = assignedDeviceIds.Union(tagDeviceIds).Distinct().ToList();

    var versionResult = await agentVersionProvider.TryGetAgentVersion();
    var currentVersion = versionResult.IsSuccess ? versionResult.Value : null;

    var devices = await appDb.Devices
      .AsNoTracking()
      .Include(x => x.Tags)
      .Include(x => x.DeviceGroup)
      .Where(x => allDeviceIds.Contains(x.Id))
      .ToListAsync();

    var dtos = devices.Select(d =>
    {
      var isOutdated = currentVersion is not null &&
        Version.TryParse(d.AgentVersion, out var deviceVersion) &&
        deviceVersion < currentVersion;
      return d.ToDto(isOutdated);
    }).ToArray();

    return Ok(dtos);
  }

  [HttpDelete("assignments/{assignmentId:guid}")]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult> RemoveAssignment(
    [FromServices] AppDb appDb,
    [FromRoute] Guid assignmentId)
  {
    var assignment = await appDb.ClientDeviceAssignments
      .FirstOrDefaultAsync(x => x.Id == assignmentId);

    if (assignment is null)
    {
      return NotFound();
    }

    appDb.ClientDeviceAssignments.Remove(assignment);
    await appDb.SaveChangesAsync();

    return NoContent();
  }

  private static async Task<ClientDeviceAssignmentDto> ToDto(AppDb appDb, ClientDeviceAssignment assignment)
  {
    var userName = await appDb.Users
      .AsNoTracking()
      .Where(x => x.Id == assignment.ClientUserId)
      .Select(x => x.UserName)
      .FirstOrDefaultAsync();

    var deviceName = await appDb.Devices
      .AsNoTracking()
      .Where(x => x.Id == assignment.DeviceId)
      .Select(x => x.Name)
      .FirstOrDefaultAsync();

    return new ClientDeviceAssignmentDto(
      assignment.Id,
      assignment.ClientUserId,
      userName,
      assignment.DeviceId,
      deviceName,
      assignment.ExpiresAt);
  }
}
