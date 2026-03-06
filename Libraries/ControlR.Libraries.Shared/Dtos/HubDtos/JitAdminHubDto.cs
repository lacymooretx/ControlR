namespace ControlR.Libraries.Shared.Dtos.HubDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record CreateJitAdminRequestHubDto(
  string Username,
  string Password,
  int TtlMinutes);

[MessagePackObject(keyAsPropertyName: true)]
public record CreateJitAdminResultHubDto(
  bool IsSuccess,
  string? ErrorMessage);

[MessagePackObject(keyAsPropertyName: true)]
public record DeleteJitAdminRequestHubDto(
  string Username);

[MessagePackObject(keyAsPropertyName: true)]
public record DeleteJitAdminResultHubDto(
  bool IsSuccess,
  string? ErrorMessage);
