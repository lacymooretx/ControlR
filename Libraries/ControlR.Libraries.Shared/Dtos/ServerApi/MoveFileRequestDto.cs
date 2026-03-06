namespace ControlR.Libraries.Shared.Dtos.ServerApi;

public record MoveFileRequestDto(
  Guid DeviceId,
  string SourcePath,
  string DestinationPath);
