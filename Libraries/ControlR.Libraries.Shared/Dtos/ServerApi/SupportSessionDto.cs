namespace ControlR.Libraries.Shared.Dtos.ServerApi;

public record SupportSessionDto(
  Guid Id,
  string AccessCode,
  string? ClientName,
  string? ClientEmail,
  Guid CreatorUserId,
  string? CreatorUserName,
  Guid? DeviceId,
  string? DeviceName,
  DateTimeOffset ExpiresAt,
  bool IsUsed,
  string? Notes,
  DateTimeOffset? SessionStartedAt,
  DateTimeOffset? SessionEndedAt,
  SupportSessionStatusDto Status,
  DateTimeOffset CreatedAt);

public enum SupportSessionStatusDto
{
  Pending,
  WaitingForClient,
  InProgress,
  Completed,
  Expired,
  Cancelled
}

public record SupportSessionCreateRequestDto(
  string? ClientName,
  string? ClientEmail,
  string? Notes,
  int ExpirationMinutes = 60);

public record SupportSessionJoinRequestDto(
  string AccessCode,
  string? ClientName);

public record SupportSessionJoinResponseDto(
  Guid SessionId,
  string ServerUrl,
  Guid TenantId,
  string? TechnicianName);
