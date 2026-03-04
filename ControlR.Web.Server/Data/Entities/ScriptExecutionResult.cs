using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class ScriptExecutionResult : TenantEntityBase
{
  public DateTimeOffset? CompletedAt { get; set; }

  public Guid DeviceId { get; set; }

  public string? DeviceName { get; set; }

  public int? ExitCode { get; set; }

  public ScriptExecution? ScriptExecution { get; set; }

  public Guid ScriptExecutionId { get; set; }

  public string? StandardError { get; set; }

  public string? StandardOutput { get; set; }

  public DateTimeOffset? StartedAt { get; set; }

  [StringLength(50)]
  public string Status { get; set; } = "Pending";
}
