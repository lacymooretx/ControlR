using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class AuditLog : TenantEntityBase
{
  [StringLength(100)]
  public required string Action { get; set; }

  public Guid? ActorUserId { get; set; }

  [StringLength(256)]
  public string? ActorUserName { get; set; }

  [StringLength(4000)]
  public string? Details { get; set; }

  public TimeSpan? Duration { get; set; }

  public DateTimeOffset? EndTimestamp { get; set; }

  [StringLength(50)]
  public required string EventType { get; set; }

  public Guid? SessionId { get; set; }

  [StringLength(45)]
  public string? SourceIpAddress { get; set; }

  public Guid? TargetDeviceId { get; set; }

  [StringLength(256)]
  public string? TargetDeviceName { get; set; }

  public DateTimeOffset Timestamp { get; set; }
}
