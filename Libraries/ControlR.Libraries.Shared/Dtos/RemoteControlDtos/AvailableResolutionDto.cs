using MessagePack;

namespace ControlR.Libraries.Shared.Dtos.RemoteControlDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record AvailableResolutionDto(int Width, int Height, int RefreshRate);
