using MessagePack;

namespace ControlR.Libraries.Shared.Dtos.ServerApi;

[MessagePackObject(keyAsPropertyName: true)]
public record AuditLogDto(
  Guid Id,
  string EventType,
  string Action,
  Guid? ActorUserId,
  string? ActorUserName,
  Guid? TargetDeviceId,
  string? TargetDeviceName,
  string? SourceIpAddress,
  DateTimeOffset Timestamp,
  DateTimeOffset? EndTimestamp,
  TimeSpan? Duration,
  string? Details,
  Guid? SessionId) : IHasPrimaryKey;

[MessagePackObject(keyAsPropertyName: true)]
public record AuditLogSearchRequestDto(
  DateTimeOffset? StartDate,
  DateTimeOffset? EndDate,
  string? EventType,
  string? ActorUserName,
  Guid? TargetDeviceId,
  string? SearchText,
  int Skip = 0,
  int Take = 50);

[MessagePackObject(keyAsPropertyName: true)]
public record AuditLogSearchResponseDto(
  IReadOnlyList<AuditLogDto> Items,
  int TotalCount);
