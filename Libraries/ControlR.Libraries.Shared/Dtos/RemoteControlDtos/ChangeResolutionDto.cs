using MessagePack;

namespace ControlR.Libraries.Shared.Dtos.RemoteControlDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record ChangeResolutionDto(string DisplayId, int Width, int Height, int? RefreshRate);
