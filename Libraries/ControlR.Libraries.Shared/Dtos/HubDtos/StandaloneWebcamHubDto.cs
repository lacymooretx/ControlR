namespace ControlR.Libraries.Shared.Dtos.HubDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record StandaloneWebcamFrameDto(
  byte[] JpegData,
  int Width,
  int Height);

[MessagePackObject(keyAsPropertyName: true)]
public record WebcamInfoDto(
  int Index,
  string Name);
