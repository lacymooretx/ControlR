namespace ControlR.Libraries.Shared.Dtos.ServerApi;

[MessagePackObject(keyAsPropertyName: true)]
public record PendingPatchDto(
  Guid Id,
  Guid DeviceId,
  string UpdateId,
  string Title,
  string? Description,
  bool IsImportant,
  bool IsCritical,
  long SizeBytes,
  DateTimeOffset DetectedAt,
  DateTimeOffset? InstalledAt,
  string Status) : IHasPrimaryKey;

[MessagePackObject(keyAsPropertyName: true)]
public record PatchInstallationDto(
  Guid Id,
  Guid DeviceId,
  Guid InitiatedByUserId,
  DateTimeOffset InitiatedAt,
  DateTimeOffset? CompletedAt,
  int TotalCount,
  int InstalledCount,
  int FailedCount,
  string Status) : IHasPrimaryKey;

[MessagePackObject(keyAsPropertyName: true)]
public record PatchScanRequestDto(Guid DeviceId);

[MessagePackObject(keyAsPropertyName: true)]
public record PatchInstallRequestDto(Guid DeviceId, string[] UpdateIds);
