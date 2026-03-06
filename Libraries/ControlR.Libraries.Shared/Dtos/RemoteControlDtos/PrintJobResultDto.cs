namespace ControlR.Libraries.Shared.Dtos.RemoteControlDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record PrintJobResultDto(bool IsSuccess, string? ErrorMessage);
