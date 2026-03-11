namespace ControlR.Libraries.Shared.Dtos.RemoteControlDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record WebcamControlDto(bool IsEnabled, int PreferredWidth, int PreferredHeight, int CameraIndex);
