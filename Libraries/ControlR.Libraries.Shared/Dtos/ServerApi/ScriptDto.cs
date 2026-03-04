namespace ControlR.Libraries.Shared.Dtos.ServerApi;

[MessagePackObject(keyAsPropertyName: true)]
public record SavedScriptDto(
  Guid Id,
  string Name,
  string Description,
  string ScriptContent,
  string ScriptType,
  Guid CreatorUserId,
  bool IsPublishedToClients) : IHasPrimaryKey;

[MessagePackObject(keyAsPropertyName: true)]
public record SavedScriptCreateRequestDto(
  string Name,
  string Description,
  string ScriptContent,
  string ScriptType,
  bool IsPublishedToClients);

[MessagePackObject(keyAsPropertyName: true)]
public record SavedScriptUpdateRequestDto(
  Guid Id,
  string Name,
  string Description,
  string ScriptContent,
  string ScriptType,
  bool IsPublishedToClients);

[MessagePackObject(keyAsPropertyName: true)]
public record ScriptExecutionDto(
  Guid Id,
  Guid? ScriptId,
  string? ScriptName,
  string ScriptType,
  Guid InitiatedByUserId,
  DateTimeOffset StartedAt,
  DateTimeOffset? CompletedAt,
  string Status,
  IReadOnlyList<ScriptExecutionResultDto> Results) : IHasPrimaryKey;

[MessagePackObject(keyAsPropertyName: true)]
public record ScriptExecutionResultDto(
  Guid Id,
  Guid DeviceId,
  string? DeviceName,
  int? ExitCode,
  string? StandardOutput,
  string? StandardError,
  string Status,
  DateTimeOffset? StartedAt,
  DateTimeOffset? CompletedAt) : IHasPrimaryKey;

[MessagePackObject(keyAsPropertyName: true)]
public record ExecuteScriptRequestDto(
  Guid? ScriptId,
  string? AdHocScriptContent,
  string ScriptType,
  IReadOnlyList<Guid> TargetDeviceIds);
