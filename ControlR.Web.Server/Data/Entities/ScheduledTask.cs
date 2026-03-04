using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class ScheduledTask : TenantEntityBase
{
  public Guid CreatorUserId { get; set; }

  [StringLength(100)]
  public required string CronExpression { get; set; }

  [StringLength(500)]
  public string Description { get; set; } = string.Empty;

  public bool IsEnabled { get; set; } = true;

  public DateTimeOffset? LastRunAt { get; set; }

  [StringLength(200)]
  public required string Name { get; set; }

  public DateTimeOffset? NextRunAt { get; set; }

  public SavedScript? Script { get; set; }

  public Guid? ScriptId { get; set; }

  public List<Guid> TargetDeviceIds { get; set; } = [];

  public List<Guid> TargetGroupIds { get; set; } = [];

  [StringLength(50)]
  public required string TaskType { get; set; }

  [StringLength(100)]
  public string TimeZone { get; set; } = "UTC";
}
