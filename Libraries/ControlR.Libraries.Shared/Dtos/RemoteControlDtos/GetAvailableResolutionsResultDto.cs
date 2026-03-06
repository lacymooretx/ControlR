using MessagePack;

namespace ControlR.Libraries.Shared.Dtos.RemoteControlDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record GetAvailableResolutionsResultDto(
  bool IsSuccess,
  string? ErrorMessage,
  AvailableResolutionDto[] Resolutions);
