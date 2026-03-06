namespace ControlR.Libraries.Shared.Dtos.RemoteControlDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record AudioControlDto(bool IsEnabled, int SampleRate, int Channels);
