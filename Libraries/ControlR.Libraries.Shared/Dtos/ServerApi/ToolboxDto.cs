namespace ControlR.Libraries.Shared.Dtos.ServerApi;

public record ToolboxItemDto(
  Guid Id,
  string Name,
  string? Description,
  string FileName,
  string? Category,
  string? Version,
  long FileSizeBytes,
  string? Sha256Hash,
  Guid UploadedByUserId,
  string? UploadedByUserName,
  int DeploymentCount,
  DateTimeOffset CreatedAt);

public record ToolboxItemCreateRequestDto(
  string Name,
  string? Description,
  string? Category,
  string? Version);

public record ToolboxItemUpdateRequestDto(
  Guid Id,
  string Name,
  string? Description,
  string? Category,
  string? Version);

public record ToolboxDeployRequestDto(
  Guid ToolboxItemId,
  Guid[] DeviceIds,
  string? TargetPath,
  bool AutoExecute);
