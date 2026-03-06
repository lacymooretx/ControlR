using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class PendingPatch : TenantEntityBase
{
  public Guid DeviceId { get; set; }

  public DateTimeOffset DetectedAt { get; set; }

  public DateTimeOffset? InstalledAt { get; set; }

  [StringLength(2000)]
  public string? Description { get; set; }

  public bool IsCritical { get; set; }

  public bool IsImportant { get; set; }

  public long SizeBytes { get; set; }

  [StringLength(50)]
  public string Status { get; set; } = "Pending";

  [StringLength(500)]
  public required string Title { get; set; }

  [StringLength(200)]
  public required string UpdateId { get; set; }
}
