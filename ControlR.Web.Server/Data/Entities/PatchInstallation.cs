using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class PatchInstallation : TenantEntityBase
{
  public DateTimeOffset? CompletedAt { get; set; }

  public Guid DeviceId { get; set; }

  public int FailedCount { get; set; }

  public Guid InitiatedByUserId { get; set; }

  public DateTimeOffset InitiatedAt { get; set; }

  public int InstalledCount { get; set; }

  [StringLength(50)]
  public string Status { get; set; } = "Pending";

  public int TotalCount { get; set; }
}
