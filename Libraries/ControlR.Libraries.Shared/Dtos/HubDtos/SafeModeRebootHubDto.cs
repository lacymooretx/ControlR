namespace ControlR.Libraries.Shared.Dtos.HubDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record SafeModeRebootRequestHubDto(
  bool WithNetworking = true);

[MessagePackObject(keyAsPropertyName: true)]
public record SafeModeRebootResultHubDto(
  bool IsSuccess,
  string? ErrorMessage);
