using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class ScriptExecution : TenantEntityBase
{
  public string? AdHocScriptContent { get; set; }

  public DateTimeOffset? CompletedAt { get; set; }

  public Guid InitiatedByUserId { get; set; }

  public List<ScriptExecutionResult>? Results { get; set; }

  public SavedScript? Script { get; set; }

  public Guid? ScriptId { get; set; }

  [System.ComponentModel.DataAnnotations.StringLength(20)]
  public required string ScriptType { get; set; }

  public DateTimeOffset StartedAt { get; set; }

  [System.ComponentModel.DataAnnotations.StringLength(50)]
  public string Status { get; set; } = "Pending";
}
