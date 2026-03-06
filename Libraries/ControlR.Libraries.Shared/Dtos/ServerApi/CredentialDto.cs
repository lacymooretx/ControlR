namespace ControlR.Libraries.Shared.Dtos.ServerApi;

/// <summary>
/// Listed credential - NO password included.
/// </summary>
public record StoredCredentialDto(
  Guid Id,
  string Name,
  string? Description,
  string Username,
  string? Domain,
  Guid? DeviceId,
  Guid? DeviceGroupId,
  string? Category,
  Guid CreatedByUserId,
  string? CreatedByUserName,
  DateTimeOffset? LastAccessedAt,
  int AccessCount,
  DateTimeOffset CreatedAt);

/// <summary>
/// Full credential with decrypted password - returned only on explicit retrieve with audit logging.
/// </summary>
public record StoredCredentialWithPasswordDto(
  Guid Id,
  string Name,
  string Username,
  string Password,
  string? Domain);

public record CreateCredentialRequestDto(
  string Name,
  string? Description,
  string Username,
  string Password,
  string? Domain,
  Guid? DeviceId,
  Guid? DeviceGroupId,
  string? Category);

public record UpdateCredentialRequestDto(
  Guid Id,
  string Name,
  string? Description,
  string Username,
  string? Password, // null = don't change
  string? Domain,
  Guid? DeviceId,
  Guid? DeviceGroupId,
  string? Category);
