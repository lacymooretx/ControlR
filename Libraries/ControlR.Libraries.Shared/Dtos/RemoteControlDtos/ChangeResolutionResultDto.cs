using MessagePack;

namespace ControlR.Libraries.Shared.Dtos.RemoteControlDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record ChangeResolutionResultDto(bool IsSuccess, string? ErrorMessage);
