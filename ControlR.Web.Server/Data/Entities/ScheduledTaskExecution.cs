using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class ScheduledTaskExecution : TenantEntityBase
{
  public DateTimeOffset? CompletedAt { get; set; }

  public ScheduledTask? ScheduledTask { get; set; }

  public Guid ScheduledTaskId { get; set; }

  public ScriptExecution? ScriptExecution { get; set; }

  public Guid? ScriptExecutionId { get; set; }

  public DateTimeOffset StartedAt { get; set; }

  [StringLength(50)]
  public string Status { get; set; } = "Pending";
}
