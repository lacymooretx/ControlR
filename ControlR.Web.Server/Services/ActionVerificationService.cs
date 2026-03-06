using Microsoft.Extensions.Caching.Memory;

namespace ControlR.Web.Server.Services;

public interface IActionVerificationService
{
  bool IsVerified(Guid userId);
  DateTimeOffset? GetExpiresAt(Guid userId);
  void SetVerified(Guid userId, TimeSpan duration);
  void Revoke(Guid userId);
}

public class ActionVerificationService(IMemoryCache cache) : IActionVerificationService
{
  private const string Prefix = "action-verification-";
  private readonly IMemoryCache _cache = cache;

  public bool IsVerified(Guid userId)
  {
    return _cache.TryGetValue($"{Prefix}{userId}", out _);
  }

  public DateTimeOffset? GetExpiresAt(Guid userId)
  {
    if (_cache.TryGetValue($"{Prefix}{userId}", out DateTimeOffset expiresAt))
    {
      return expiresAt;
    }
    return null;
  }

  public void SetVerified(Guid userId, TimeSpan duration)
  {
    var expiresAt = DateTimeOffset.UtcNow.Add(duration);
    _cache.Set($"{Prefix}{userId}", expiresAt, duration);
  }

  public void Revoke(Guid userId)
  {
    _cache.Remove($"{Prefix}{userId}");
  }
}
