namespace ControlR.Libraries.Shared.Dtos.ServerApi;

[MessagePackObject(keyAsPropertyName: true)]
public record AlertRuleDto(
  Guid Id,
  string Name,
  string MetricType,
  double ThresholdValue,
  string Operator,
  string Duration,
  bool IsEnabled,
  IReadOnlyList<Guid> TargetDeviceIds,
  IReadOnlyList<Guid> TargetGroupIds,
  string NotificationRecipients,
  Guid CreatorUserId) : IHasPrimaryKey;

[MessagePackObject(keyAsPropertyName: true)]
public record AlertRuleCreateRequestDto(
  string Name,
  string MetricType,
  double ThresholdValue,
  string Operator,
  string Duration,
  IReadOnlyList<Guid> TargetDeviceIds,
  IReadOnlyList<Guid> TargetGroupIds,
  string NotificationRecipients);

[MessagePackObject(keyAsPropertyName: true)]
public record AlertRuleUpdateRequestDto(
  Guid Id,
  string Name,
  string MetricType,
  double ThresholdValue,
  string Operator,
  string Duration,
  bool IsEnabled,
  IReadOnlyList<Guid> TargetDeviceIds,
  IReadOnlyList<Guid> TargetGroupIds,
  string NotificationRecipients);

[MessagePackObject(keyAsPropertyName: true)]
public record AlertDto(
  Guid Id,
  Guid AlertRuleId,
  string? AlertRuleName,
  Guid DeviceId,
  string DeviceName,
  DateTimeOffset TriggeredAt,
  DateTimeOffset? AcknowledgedAt,
  DateTimeOffset? ResolvedAt,
  string Status,
  string Details) : IHasPrimaryKey;

[MessagePackObject(keyAsPropertyName: true)]
public record DeviceMetricSnapshotDto(
  DateTimeOffset Timestamp,
  double CpuPercent,
  double MemoryPercent,
  double DiskPercent);
