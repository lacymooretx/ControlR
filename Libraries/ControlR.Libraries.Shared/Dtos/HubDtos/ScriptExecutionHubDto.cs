namespace ControlR.Libraries.Shared.Dtos.HubDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record ScriptExecutionRequestHubDto(
  Guid ExecutionId,
  Guid ResultId,
  string ScriptContent,
  string ScriptType);

[MessagePackObject(keyAsPropertyName: true)]
public record ScriptExecutionResultHubDto(
  Guid ExecutionId,
  Guid ResultId,
  Guid DeviceId,
  int ExitCode,
  string StandardOutput,
  string StandardError,
  string Status);
