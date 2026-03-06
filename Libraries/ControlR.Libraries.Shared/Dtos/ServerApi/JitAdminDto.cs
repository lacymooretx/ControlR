namespace ControlR.Libraries.Shared.Dtos.ServerApi;

public record JitAdminAccountDto(
  Guid Id,
  Guid DeviceId,
  string? DeviceName,
  string Username,
  Guid CreatedByUserId,
  string? CreatedByUserName,
  DateTimeOffset ExpiresAt,
  DateTimeOffset? DeletedAt,
  JitAdminAccountStatusDto Status,
  DateTimeOffset CreatedAt);

public enum JitAdminAccountStatusDto
{
  Active,
  Expired,
  ManuallyDeleted,
  Failed
}

public record CreateJitAdminRequestDto(
  Guid DeviceId,
  int TtlMinutes = 60);

public record CreateJitAdminResponseDto(
  JitAdminAccountDto Account,
  string Password);
