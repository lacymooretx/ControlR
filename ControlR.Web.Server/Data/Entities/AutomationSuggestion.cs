using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public enum SuggestionType
{
  HighCpu,
  HighMemory,
  HighDisk,
  FrequentAlerts,
  StaleDevice
}

public enum SuggestionStatus
{
  New,
  Accepted,
  Dismissed,
  Expired
}

public class AutomationSuggestion : TenantEntityBase
{
  public Guid? DeviceId { get; set; }

  public SuggestionType SuggestionType { get; set; }

  [StringLength(200)]
  public required string Title { get; set; }

  [StringLength(2000)]
  public string Description { get; set; } = string.Empty;

  public Guid? SuggestedScriptId { get; set; }

  public SavedScript? SuggestedScript { get; set; }

  public float Confidence { get; set; }

  public SuggestionStatus Status { get; set; } = SuggestionStatus.New;
}
