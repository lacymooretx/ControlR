namespace ControlR.Libraries.Shared.Dtos.HubDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record MoveFileHubDto(string SourcePath, string DestinationPath);
