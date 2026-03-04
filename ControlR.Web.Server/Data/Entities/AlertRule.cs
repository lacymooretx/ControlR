using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class AlertRule : TenantEntityBase
{
  public Guid CreatorUserId { get; set; }

  [StringLength(50)]
  public string Duration { get; set; } = "0";

  public bool IsEnabled { get; set; } = true;

  [StringLength(50)]
  public required string MetricType { get; set; }

  [StringLength(200)]
  public required string Name { get; set; }

  [StringLength(500)]
  public string NotificationRecipients { get; set; } = string.Empty;

  [StringLength(20)]
  public required string Operator { get; set; }

  public List<Guid> TargetDeviceIds { get; set; } = [];

  public List<Guid> TargetGroupIds { get; set; } = [];

  public double ThresholdValue { get; set; }
}
