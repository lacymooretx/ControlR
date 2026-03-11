namespace ControlR.Libraries.Shared.Dtos.RemoteControlDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record WebcamFrameDto(byte[] JpegData, int Width, int Height, long TimestampMs);
