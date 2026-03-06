namespace ControlR.Libraries.Shared.Dtos.ServerApi;

public record ActionVerificationRequestDto(string Password);

public record ActionVerificationStatusDto(bool IsVerified, DateTimeOffset? ExpiresAt);
