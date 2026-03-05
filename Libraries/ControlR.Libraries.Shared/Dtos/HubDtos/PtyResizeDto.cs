namespace ControlR.Libraries.Shared.Dtos.HubDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record PtyResizeDto(
    Guid TerminalId,
    int Cols,
    int Rows)
{
  public string? ViewerConnectionId { get; set; }
}
