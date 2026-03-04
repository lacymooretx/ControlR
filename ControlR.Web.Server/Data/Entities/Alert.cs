using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class Alert : TenantEntityBase
{
  public DateTimeOffset? AcknowledgedAt { get; set; }

  public AlertRule? AlertRule { get; set; }

  public Guid AlertRuleId { get; set; }

  [StringLength(2000)]
  public string Details { get; set; } = string.Empty;

  public Guid DeviceId { get; set; }

  [StringLength(100)]
  public string DeviceName { get; set; } = string.Empty;

  public DateTimeOffset? ResolvedAt { get; set; }

  [StringLength(50)]
  public string Status { get; set; } = "Active";

  public DateTimeOffset TriggeredAt { get; set; }
}
