namespace ControlR.Libraries.Shared.Dtos.HubDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record PtyInputDto(
    Guid TerminalId,
    byte[] Data)
{
  public string? ViewerConnectionId { get; set; }
}
