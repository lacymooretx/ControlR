using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class DeviceGroup : TenantEntityBase
{
  [StringLength(500)]
  public string Description { get; set; } = string.Empty;

  public List<Device>? Devices { get; set; }

  [StringLength(50)]
  public string GroupType { get; set; } = "Custom";

  [StringLength(100)]
  public required string Name { get; set; }

  public DeviceGroup? ParentGroup { get; set; }

  public Guid? ParentGroupId { get; set; }

  public int SortOrder { get; set; }

  public List<DeviceGroup>? SubGroups { get; set; }
}
