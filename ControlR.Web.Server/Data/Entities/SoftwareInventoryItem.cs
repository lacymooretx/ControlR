using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class SoftwareInventoryItem : TenantEntityBase
{
  public Guid DeviceId { get; set; }

  public DateTimeOffset? InstallDate { get; set; }

  public DateTimeOffset LastReportedAt { get; set; }

  [StringLength(500)]
  public required string Name { get; set; }

  [StringLength(200)]
  public string Publisher { get; set; } = string.Empty;

  [StringLength(100)]
  public string Version { get; set; } = string.Empty;
}
