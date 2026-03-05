namespace ControlR.Libraries.Shared.Dtos.HubDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record PtyOutputDto(
    Guid TerminalId,
    byte[] Data);
