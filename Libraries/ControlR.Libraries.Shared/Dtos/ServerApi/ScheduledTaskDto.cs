namespace ControlR.Libraries.Shared.Dtos.ServerApi;

[MessagePackObject(keyAsPropertyName: true)]
public record ScheduledTaskDto(
  Guid Id,
  string Name,
  string Description,
  string TaskType,
  Guid? ScriptId,
  string? ScriptName,
  string CronExpression,
  string TimeZone,
  bool IsEnabled,
  IReadOnlyList<Guid> TargetDeviceIds,
  IReadOnlyList<Guid> TargetGroupIds,
  Guid CreatorUserId,
  DateTimeOffset? LastRunAt,
  DateTimeOffset? NextRunAt) : IHasPrimaryKey;

[MessagePackObject(keyAsPropertyName: true)]
public record ScheduledTaskCreateRequestDto(
  string Name,
  string Description,
  string TaskType,
  Guid? ScriptId,
  string CronExpression,
  string TimeZone,
  IReadOnlyList<Guid> TargetDeviceIds,
  IReadOnlyList<Guid> TargetGroupIds);

[MessagePackObject(keyAsPropertyName: true)]
public record ScheduledTaskUpdateRequestDto(
  Guid Id,
  string Name,
  string Description,
  string TaskType,
  Guid? ScriptId,
  string CronExpression,
  string TimeZone,
  bool IsEnabled,
  IReadOnlyList<Guid> TargetDeviceIds,
  IReadOnlyList<Guid> TargetGroupIds);

[MessagePackObject(keyAsPropertyName: true)]
public record ScheduledTaskExecutionDto(
  Guid Id,
  Guid ScheduledTaskId,
  Guid? ScriptExecutionId,
  DateTimeOffset StartedAt,
  DateTimeOffset? CompletedAt,
  string Status) : IHasPrimaryKey;
