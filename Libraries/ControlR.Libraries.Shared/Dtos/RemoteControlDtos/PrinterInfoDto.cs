namespace ControlR.Libraries.Shared.Dtos.RemoteControlDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record PrinterInfoDto(string Name, bool IsDefault, bool IsOnline);
