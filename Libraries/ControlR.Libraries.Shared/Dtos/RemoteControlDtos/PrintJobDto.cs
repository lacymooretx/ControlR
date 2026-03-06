namespace ControlR.Libraries.Shared.Dtos.RemoteControlDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record PrintJobDto(string PrinterName, string FileName, byte[] FileData, int Copies);
