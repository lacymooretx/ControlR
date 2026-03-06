namespace ControlR.Libraries.Shared.Dtos.ServerApi;

[MessagePackObject(keyAsPropertyName: true)]
public record AutomationSuggestionDto(
  Guid Id,
  Guid? DeviceId,
  string SuggestionType,
  string Title,
  string Description,
  Guid? SuggestedScriptId,
  float Confidence,
  string Status,
  DateTimeOffset CreatedAt) : IHasPrimaryKey;

[MessagePackObject(keyAsPropertyName: true)]
public record AutomationSuggestionUpdateRequestDto(
  string Status);
