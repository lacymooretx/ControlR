namespace ControlR.Libraries.Shared.Dtos.ServerApi;

public record BrandingSettingsDto(
  Guid Id,
  string ProductName,
  string PrimaryColor,
  string? SecondaryColor,
  string? LogoFileName,
  bool HasLogo);

public record UpdateBrandingRequestDto(
  string ProductName,
  string PrimaryColor,
  string? SecondaryColor);
