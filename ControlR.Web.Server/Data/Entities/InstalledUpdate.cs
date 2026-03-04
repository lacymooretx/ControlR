using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class InstalledUpdate : TenantEntityBase
{
  public Guid DeviceId { get; set; }

  public DateTimeOffset? InstalledOn { get; set; }

  public DateTimeOffset LastReportedAt { get; set; }

  [StringLength(500)]
  public required string Title { get; set; }

  [StringLength(100)]
  public required string UpdateId { get; set; }
}
