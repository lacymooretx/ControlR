using ControlR.Libraries.Shared.Helpers;
using ControlR.Web.Server.Options;
using Microsoft.Extensions.Options;

namespace ControlR.Web.Server.Services;

public interface IPersonalAccessTokenManager
{
  Task<Result<CreatePersonalAccessTokenResponseDto>> CreateToken(CreatePersonalAccessTokenRequestDto request, Guid userId);
  Task<Result> Delete(Guid id, Guid userId);
  Task<IEnumerable<PersonalAccessTokenDto>> GetForUser(Guid userId);
  Task<Result<PersonalAccessTokenDto>> Update(Guid id, UpdatePersonalAccessTokenRequestDto request, Guid userId);
  /// <summary>
  /// Validates the provided personal access token and returns the associated user and tenant ID if valid.
  /// </summary>
  /// <param name="token">The personal access token to validate.</param>
  /// <returns>The associated user and tenant ID if valid, otherwise an error.</returns>
  Task<Result<PersonalAccessTokenValidationResult>> ValidateToken(string token);
}

public class PersonalAccessTokenManager(
  AppDb appDb,
  IOptionsMonitor<AppOptions> appOptions,
  TimeProvider timeProvider,
  IPasswordHasher<string> passwordHasher) : IPersonalAccessTokenManager
{
  private readonly AppDb _appDb = appDb;
  private readonly IOptionsMonitor<AppOptions> _appOptions = appOptions;
  private readonly IPasswordHasher<string> _passwordHasher = passwordHasher;
  private readonly TimeProvider _timeProvider = timeProvider;

  public async Task<Result<CreatePersonalAccessTokenResponseDto>> CreateToken(CreatePersonalAccessTokenRequestDto request, Guid userId)
  {
    try
    {
      var plainTextKey = RandomGenerator.CreateApiKey();
      var hashedKey = _passwordHasher.HashPassword(string.Empty, plainTextKey);
      var now = _timeProvider.GetUtcNow();
      var expiresAt = GetTokenExpiry(now);

      var personalAccessToken = new PersonalAccessToken
      {
        Name = request.Name,
        HashedKey = hashedKey,
        UserId = userId,
        ExpiresAt = expiresAt
      };

      _appDb.PersonalAccessTokens.Add(personalAccessToken);
      await _appDb.SaveChangesAsync();

      var hexKey = Convert.ToHexString(personalAccessToken.Id.ToByteArray());
      var combinedKey = $"{hexKey}:{plainTextKey}";
      var response = new CreatePersonalAccessTokenResponseDto(MapToDto(personalAccessToken), combinedKey);
      return Result.Ok(response);
    }
    catch (Exception ex)
    {
      return Result.Fail<CreatePersonalAccessTokenResponseDto>(ex, "Failed to create personal access token");
    }
  }

  public async Task<Result> Delete(Guid id, Guid userId)
  {
    try
    {
      var personalAccessToken = await _appDb.PersonalAccessTokens
        .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

      if (personalAccessToken is null)
      {
        return Result.Fail("Personal access token not found");
      }

      personalAccessToken.RevokedAt = _timeProvider.GetUtcNow();
      await _appDb.SaveChangesAsync();

      return Result.Ok();
    }
    catch (Exception ex)
    {
      return Result.Fail(ex, "Failed to delete personal access token");
    }
  }

  public async Task<IEnumerable<PersonalAccessTokenDto>> GetForUser(Guid userId)
  {
    var personalAccessTokens = await _appDb.PersonalAccessTokens
      .Where(x => x.UserId == userId && x.RevokedAt == null)
      .OrderByDescending(x => x.CreatedAt)
      .ToListAsync();

    return personalAccessTokens.Select(MapToDto);
  }

  public async Task<Result<PersonalAccessTokenDto>> Update(Guid id, UpdatePersonalAccessTokenRequestDto request, Guid userId)
  {
    try
    {
      var personalAccessToken = await _appDb.PersonalAccessTokens
        .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

      if (personalAccessToken is null)
      {
        return Result.Fail<PersonalAccessTokenDto>("Personal access token not found");
      }

      if (personalAccessToken.RevokedAt is not null)
      {
        return Result.Fail<PersonalAccessTokenDto>("Personal access token has been revoked");
      }

      personalAccessToken.Name = request.Name;
      await _appDb.SaveChangesAsync();

      return Result.Ok(MapToDto(personalAccessToken));
    }
    catch (Exception ex)
    {
      return Result.Fail<PersonalAccessTokenDto>(ex, "Failed to update personal access token");
    }
  }

  public async Task<Result<PersonalAccessTokenValidationResult>> ValidateToken(string token)
  {
    try
    {
      var parts = token.Split(':', 2);
      if (parts.Length != 2)
      {
        return Result.Fail<PersonalAccessTokenValidationResult>("Invalid personal access token format");
      }

      var tokenIdBytes = Convert.FromHexString(parts[0]);
      var tokenId = new Guid(tokenIdBytes);
      
      var storedToken = await _appDb.PersonalAccessTokens
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(x => x.Id == tokenId);

      if (storedToken is null)
      {
        return Result.Fail<PersonalAccessTokenValidationResult>("Invalid personal access token");
      }

      if (storedToken.RevokedAt is not null)
      {
        return Result.Fail<PersonalAccessTokenValidationResult>("Personal access token has been revoked");
      }

      var now = _timeProvider.GetUtcNow();
      var effectiveExpiry = storedToken.ExpiresAt ?? GetTokenExpiry(storedToken.CreatedAt);
      if (effectiveExpiry is not null && effectiveExpiry <= now)
      {
        return Result.Fail<PersonalAccessTokenValidationResult>("Personal access token has expired");
      }

      var isValid = _passwordHasher.VerifyHashedPassword(string.Empty, storedToken.HashedKey, parts[1]) == PasswordVerificationResult.Success;

      if (!isValid)
      {
        return Result.Fail<PersonalAccessTokenValidationResult>("Invalid personal access token");
      }

      // Update last used timestamp
      storedToken.LastUsed = now;
      await _appDb.SaveChangesAsync();

      var result = PersonalAccessTokenValidationResult.Success(storedToken.UserId);
      return Result.Ok(result);
    }
    catch (Exception ex)
    {
      return Result.Fail<PersonalAccessTokenValidationResult>(ex, "Failed to validate personal access token");
    }
  }

  private PersonalAccessTokenDto MapToDto(PersonalAccessToken personalAccessToken)
  {
    var expiresAt = personalAccessToken.ExpiresAt ?? GetTokenExpiry(personalAccessToken.CreatedAt);
    return new PersonalAccessTokenDto(
      personalAccessToken.Id,
      personalAccessToken.Name,
      personalAccessToken.CreatedAt,
      personalAccessToken.LastUsed,
      expiresAt,
      personalAccessToken.RevokedAt);
  }

  private DateTimeOffset? GetTokenExpiry(DateTimeOffset referenceTime)
  {
    var lifetimeDays = _appOptions.CurrentValue.PersonalAccessTokenLifetimeDays;
    if (lifetimeDays <= 0)
    {
      return null;
    }

    return referenceTime.AddDays(lifetimeDays);
  }
}
