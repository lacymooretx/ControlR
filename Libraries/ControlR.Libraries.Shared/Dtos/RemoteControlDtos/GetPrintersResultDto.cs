namespace ControlR.Libraries.Shared.Dtos.RemoteControlDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record GetPrintersResultDto(PrinterInfoDto[] Printers);
