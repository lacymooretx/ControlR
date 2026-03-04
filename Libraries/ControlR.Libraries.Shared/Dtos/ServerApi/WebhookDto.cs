namespace ControlR.Libraries.Shared.Dtos.ServerApi;

[MessagePackObject(keyAsPropertyName: true)]
public record WebhookSubscriptionDto(
  Guid Id,
  string Name,
  string Url,
  string[] EventTypes,
  bool IsEnabled,
  bool IsDisabledDueToFailures,
  int FailureCount,
  DateTimeOffset? LastTriggeredAt,
  int? LastStatus) : IHasPrimaryKey;

[MessagePackObject(keyAsPropertyName: true)]
public record WebhookCreateRequestDto(
  string Name,
  string Url,
  string Secret,
  string[] EventTypes);

[MessagePackObject(keyAsPropertyName: true)]
public record WebhookUpdateRequestDto(
  Guid Id,
  string Name,
  string Url,
  string? Secret,
  string[] EventTypes,
  bool IsEnabled);

[MessagePackObject(keyAsPropertyName: true)]
public record WebhookDeliveryLogDto(
  Guid Id,
  Guid WebhookSubscriptionId,
  string EventType,
  DateTimeOffset AttemptedAt,
  int? HttpStatusCode,
  bool IsSuccess,
  string ResponseBody,
  string ErrorMessage,
  int AttemptNumber) : IHasPrimaryKey;

[MessagePackObject(keyAsPropertyName: true)]
public record WebhookTestRequestDto(
  Guid WebhookId);
