using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/device-groups")]
[ApiController]
[Authorize]
public class DeviceGroupsController : ControllerBase
{
  [HttpPost("bulk-assign")]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult> BulkAssignDevices(
    [FromServices] AppDb appDb,
    [FromBody] BulkAssignDeviceGroupRequestDto request)
  {
    var devices = await appDb.Devices
      .Where(x => request.DeviceIds.Contains(x.Id))
      .ToListAsync();

    foreach (var device in devices)
    {
      device.DeviceGroupId = request.GroupId;
    }

    await appDb.SaveChangesAsync();
    return NoContent();
  }

  [HttpPost]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult<DeviceGroupDto>> CreateDeviceGroup(
    [FromServices] AppDb appDb,
    [FromBody] DeviceGroupCreateRequestDto dto)
  {
    if (!User.TryGetTenantId(out var tenantId))
    {
      return NotFound("User tenant not found.");
    }

    var group = new DeviceGroup
    {
      Description = dto.Description,
      GroupType = dto.GroupType,
      Name = dto.Name,
      ParentGroupId = dto.ParentGroupId,
      SortOrder = dto.SortOrder,
      TenantId = tenantId,
    };

    await appDb.DeviceGroups.AddAsync(group);
    await appDb.SaveChangesAsync();

    return Ok(group.ToDto());
  }

  [HttpDelete("{groupId:guid}")]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult> DeleteDeviceGroup(
    [FromServices] AppDb appDb,
    [FromRoute] Guid groupId)
  {
    var group = await appDb.DeviceGroups
      .FirstOrDefaultAsync(x => x.Id == groupId);

    if (group is null)
    {
      return NotFound();
    }

    // Unassign devices from this group
    var devices = await appDb.Devices
      .Where(x => x.DeviceGroupId == groupId)
      .ToListAsync();

    foreach (var device in devices)
    {
      device.DeviceGroupId = null;
    }

    // Move sub-groups to parent
    var subGroups = await appDb.DeviceGroups
      .Where(x => x.ParentGroupId == groupId)
      .ToListAsync();

    foreach (var subGroup in subGroups)
    {
      subGroup.ParentGroupId = group.ParentGroupId;
    }

    appDb.DeviceGroups.Remove(group);
    await appDb.SaveChangesAsync();

    return NoContent();
  }

  [HttpGet]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult<DeviceGroupDto[]>> GetAllDeviceGroups(
    [FromServices] AppDb appDb)
  {
    var groups = await appDb.DeviceGroups
      .AsNoTracking()
      .Include(x => x.Devices)
      .Include(x => x.SubGroups)
      .OrderBy(x => x.SortOrder)
      .ThenBy(x => x.Name)
      .ToListAsync();

    var dtos = groups.Select(x => x.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpGet("{groupId:guid}")]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult<DeviceGroupDto>> GetDeviceGroup(
    [FromServices] AppDb appDb,
    [FromRoute] Guid groupId)
  {
    var group = await appDb.DeviceGroups
      .AsNoTracking()
      .Include(x => x.Devices)
      .Include(x => x.SubGroups)
      .FirstOrDefaultAsync(x => x.Id == groupId);

    if (group is null)
    {
      return NotFound();
    }

    return Ok(group.ToDto());
  }

  [HttpPut("{deviceId:guid}/group")]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult> SetDeviceGroup(
    [FromServices] AppDb appDb,
    [FromRoute] Guid deviceId,
    [FromBody] Guid? groupId)
  {
    var device = await appDb.Devices
      .FirstOrDefaultAsync(x => x.Id == deviceId);

    if (device is null)
    {
      return NotFound();
    }

    device.DeviceGroupId = groupId;
    await appDb.SaveChangesAsync();
    return NoContent();
  }

  [HttpPut]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult<DeviceGroupDto>> UpdateDeviceGroup(
    [FromServices] AppDb appDb,
    [FromBody] DeviceGroupUpdateRequestDto dto)
  {
    var group = await appDb.DeviceGroups
      .Include(x => x.Devices)
      .Include(x => x.SubGroups)
      .FirstOrDefaultAsync(x => x.Id == dto.Id);

    if (group is null)
    {
      return NotFound();
    }

    group.Description = dto.Description;
    group.GroupType = dto.GroupType;
    group.Name = dto.Name;
    group.ParentGroupId = dto.ParentGroupId;
    group.SortOrder = dto.SortOrder;

    await appDb.SaveChangesAsync();

    return Ok(group.ToDto());
  }
}
