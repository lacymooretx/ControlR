using System.ComponentModel.DataAnnotations;

namespace ControlR.Libraries.Shared.Dtos.ServerApi;

public record TenantResponseDto(
  Guid Id,
  string? Name,
  DateTimeOffset CreatedAt);

public record CreateTenantRequestDto(
  [Required]
  [StringLength(200, MinimumLength = 1)]
  string Name);

public record UpdateTenantRequestDto(
  [Required]
  [StringLength(200, MinimumLength = 1)]
  string Name);

public record ProvisionTenantRequestDto(
  [Required]
  [StringLength(200, MinimumLength = 1)]
  string TenantName,

  [Required]
  [EmailAddress]
  string AdminEmail,

  [StringLength(256, MinimumLength = 6)]
  string? AdminPassword);

public record ProvisionTenantResponseDto(
  Guid TenantId,
  string TenantName,
  Guid UserId,
  string AdminEmail,
  string PersonalAccessToken,
  bool TenantCreated,
  bool UserCreated,
  bool PatCreated);
