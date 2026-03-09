using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace ControlR.Web.Server.Services;

public interface IWebhookDispatcher
{
  void Dispatch(string eventType, Guid tenantId, object payload);
}

public class WebhookDispatcher : IWebhookDispatcher
{
  private readonly Channel<WebhookEvent> _channel = Channel.CreateBounded<WebhookEvent>(
    new BoundedChannelOptions(5000)
    {
      FullMode = BoundedChannelFullMode.DropOldest
    });

  public ChannelReader<WebhookEvent> Reader => _channel.Reader;

  public void Dispatch(string eventType, Guid tenantId, object payload)
  {
    _channel.Writer.TryWrite(new WebhookEvent(eventType, tenantId, payload));
  }
}

public record WebhookEvent(string EventType, Guid TenantId, object Payload);

public class WebhookDispatcherBackgroundService(
  IServiceScopeFactory scopeFactory,
  IHttpClientFactory httpClientFactory,
  WebhookDispatcher dispatcher,
  ILogger<WebhookDispatcherBackgroundService> logger) : BackgroundService
{
  private const int MaxWebhookLogMessageLength = 2000;
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
  };

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    logger.LogInformation("Webhook dispatcher started.");

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        if (await dispatcher.Reader.WaitToReadAsync(stoppingToken))
        {
          while (dispatcher.Reader.TryRead(out var webhookEvent))
          {
            await ProcessEvent(webhookEvent, stoppingToken);
          }
        }
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error processing webhook event.");
        await Task.Delay(1000, stoppingToken);
      }
    }
  }

  private static string ComputeHmac(string secret, string body)
  {
    var keyBytes = Encoding.UTF8.GetBytes(secret);
    var bodyBytes = Encoding.UTF8.GetBytes(body);
    var hash = HMACSHA256.HashData(keyBytes, bodyBytes);
    return Convert.ToHexStringLower(hash);
  }

  private async Task DeliverWebhook(
    AppDb appDb,
    WebhookSubscription subscription,
    Uri targetUri,
    string eventType,
    object payload,
    int attemptNumber,
    CancellationToken ct)
  {
    var jsonBody = JsonSerializer.Serialize(new
    {
      eventType,
      timestamp = DateTimeOffset.UtcNow,
      data = payload
    }, JsonOptions);

    var signature = ComputeHmac(subscription.Secret, jsonBody);

    var log = new WebhookDeliveryLog
    {
      AttemptedAt = DateTimeOffset.UtcNow,
      AttemptNumber = attemptNumber,
      EventType = eventType,
      TenantId = subscription.TenantId,
      WebhookSubscriptionId = subscription.Id,
    };

    try
    {
      using var client = httpClientFactory.CreateClient("Webhook");
      client.Timeout = TimeSpan.FromSeconds(10);

      using var request = new HttpRequestMessage(HttpMethod.Post, targetUri);
      request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
      request.Headers.Add("X-ControlR-Signature", signature);
      request.Headers.Add("X-ControlR-Event", eventType);

      using var response = await client.SendAsync(request, ct);

      log.HttpStatusCode = (int)response.StatusCode;
      log.IsSuccess = response.IsSuccessStatusCode;

      if (!response.IsSuccessStatusCode)
      {
        log.ResponseBody = await response.Content.ReadAsStringAsync(ct);
      }

      subscription.LastTriggeredAt = DateTimeOffset.UtcNow;
      subscription.LastStatus = log.HttpStatusCode;

      if (response.IsSuccessStatusCode)
      {
        subscription.FailureCount = 0;
      }
      else
      {
        subscription.FailureCount++;
      }
    }
    catch (Exception ex)
    {
      log.ErrorMessage = ex.Message;
      log.IsSuccess = false;
      subscription.FailureCount++;

      logger.LogWarning(ex, "Webhook delivery failed for {WebhookName} ({Url}).", subscription.Name, subscription.Url);
    }

    // Auto-disable after 10 consecutive failures
    if (subscription.FailureCount >= 10)
    {
      subscription.IsDisabledDueToFailures = true;
      logger.LogWarning("Webhook {WebhookName} disabled after {Count} consecutive failures.", subscription.Name, subscription.FailureCount);
    }

    await appDb.WebhookDeliveryLogs.AddAsync(log, ct);
    await appDb.SaveChangesAsync(ct);
  }

  private async Task ProcessEvent(WebhookEvent webhookEvent, CancellationToken ct)
  {
    using var scope = scopeFactory.CreateScope();
    var appDb = scope.ServiceProvider.GetRequiredService<AppDb>();

    var subscriptions = await appDb.WebhookSubscriptions
      .Where(s => s.TenantId == webhookEvent.TenantId &&
                  s.IsEnabled &&
                  !s.IsDisabledDueToFailures &&
                  s.EventTypes.Contains(webhookEvent.EventType))
      .ToListAsync(ct);

    foreach (var subscription in subscriptions)
    {
      var validationResult = await ValidateWebhookUriAsync(subscription.Url, ct);
      if (!validationResult.IsValid || validationResult.Uri is null)
      {
        await RecordBlockedWebhookAsync(
          appDb,
          subscription,
          webhookEvent.EventType,
          validationResult.ErrorMessage,
          ct);
        continue;
      }

      for (var attempt = 1; attempt <= 3; attempt++)
      {
        await DeliverWebhook(
          appDb,
          subscription,
          validationResult.Uri,
          webhookEvent.EventType,
          webhookEvent.Payload,
          attempt,
          ct);

        if (subscription.FailureCount == 0)
        {
          break;
        }

        if (attempt < 3)
        {
          var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
          await Task.Delay(delay, ct);
        }
      }
    }
  }

  private async Task RecordBlockedWebhookAsync(
    AppDb appDb,
    WebhookSubscription subscription,
    string eventType,
    string errorMessage,
    CancellationToken ct)
  {
    var sanitizedErrorMessage = errorMessage.Length > MaxWebhookLogMessageLength
      ? errorMessage[..MaxWebhookLogMessageLength]
      : errorMessage;

    logger.LogWarning(
      "Blocked webhook delivery for {WebhookName} ({Url}): {Reason}",
      subscription.Name,
      subscription.Url,
      sanitizedErrorMessage);

    var log = new WebhookDeliveryLog
    {
      AttemptedAt = DateTimeOffset.UtcNow,
      AttemptNumber = 1,
      EventType = eventType,
      TenantId = subscription.TenantId,
      WebhookSubscriptionId = subscription.Id,
      ErrorMessage = sanitizedErrorMessage,
      IsSuccess = false
    };

    subscription.LastTriggeredAt = DateTimeOffset.UtcNow;
    subscription.LastStatus = null;
    subscription.FailureCount++;

    if (subscription.FailureCount >= 10)
    {
      subscription.IsDisabledDueToFailures = true;
      logger.LogWarning(
        "Webhook {WebhookName} disabled after {Count} consecutive failures.",
        subscription.Name,
        subscription.FailureCount);
    }

    await appDb.WebhookDeliveryLogs.AddAsync(log, ct);
    await appDb.SaveChangesAsync(ct);
  }

  private static async Task<WebhookUrlValidationResult> ValidateWebhookUriAsync(string url, CancellationToken ct)
  {
    var trimmedUrl = url.Trim();
    if (string.IsNullOrWhiteSpace(trimmedUrl))
    {
      return WebhookUrlValidationResult.Fail("Webhook URL is missing.");
    }

    if (!Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var uri))
    {
      return WebhookUrlValidationResult.Fail("Invalid URL.");
    }

    if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
    {
      return WebhookUrlValidationResult.Fail("Only HTTPS webhook URLs are allowed.");
    }

    if (!string.IsNullOrWhiteSpace(uri.UserInfo))
    {
      return WebhookUrlValidationResult.Fail("Webhook URL must not include user info.");
    }

    if (uri.IsLoopback)
    {
      return WebhookUrlValidationResult.Fail("Loopback addresses are not allowed.");
    }

    var host = uri.Host;
    if (string.IsNullOrWhiteSpace(host))
    {
      return WebhookUrlValidationResult.Fail("Webhook URL host is missing.");
    }

    if (IsBlockedHostname(host))
    {
      return WebhookUrlValidationResult.Fail("Webhook URL host is not allowed.");
    }

    if (IPAddress.TryParse(host, out var ipAddress))
    {
      return IsPublicIp(ipAddress)
        ? WebhookUrlValidationResult.Success(uri)
        : WebhookUrlValidationResult.Fail("Webhook IP address is not public.");
    }

    IPAddress[] resolvedAddresses;
    try
    {
      resolvedAddresses = await Dns.GetHostAddressesAsync(host, ct);
    }
    catch (Exception ex)
    {
      return WebhookUrlValidationResult.Fail($"DNS resolution failed: {ex.Message}");
    }

    if (resolvedAddresses.Length == 0)
    {
      return WebhookUrlValidationResult.Fail("DNS resolution returned no addresses.");
    }

    if (resolvedAddresses.Any(address => !IsPublicIp(address)))
    {
      return WebhookUrlValidationResult.Fail("Webhook URL resolves to a non-public IP address.");
    }

    return WebhookUrlValidationResult.Success(uri);
  }

  private static bool IsBlockedHostname(string host)
  {
    if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    if (host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    if (host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    return false;
  }

  private static bool IsPublicIp(IPAddress address)
  {
    if (IPAddress.IsLoopback(address))
    {
      return false;
    }

    if (address.IsIPv4MappedToIPv6)
    {
      return IsPublicIp(address.MapToIPv4());
    }

    if (address.AddressFamily == AddressFamily.InterNetwork)
    {
      var bytes = address.GetAddressBytes();
      var b0 = bytes[0];
      var b1 = bytes[1];
      var b2 = bytes[2];

      if (b0 == 0 || b0 == 10 || b0 == 127)
      {
        return false;
      }

      if (b0 == 100 && b1 >= 64 && b1 <= 127)
      {
        return false;
      }

      if (b0 == 169 && b1 == 254)
      {
        return false;
      }

      if (b0 == 172 && b1 >= 16 && b1 <= 31)
      {
        return false;
      }

      if (b0 == 192 && b1 == 0 && (b2 == 0 || b2 == 2))
      {
        return false;
      }

      if (b0 == 192 && b1 == 88 && b2 == 99)
      {
        return false;
      }

      if (b0 == 192 && b1 == 168)
      {
        return false;
      }

      if (b0 == 198 && (b1 == 18 || b1 == 19))
      {
        return false;
      }

      if (b0 == 198 && b1 == 51 && b2 == 100)
      {
        return false;
      }

      if (b0 == 203 && b1 == 0 && b2 == 113)
      {
        return false;
      }

      if (b0 >= 224)
      {
        return false;
      }

      return true;
    }

    if (address.AddressFamily == AddressFamily.InterNetworkV6)
    {
      var bytes = address.GetAddressBytes();

      var isUnspecified = true;
      for (var i = 0; i < bytes.Length; i++)
      {
        if (bytes[i] != 0)
        {
          isUnspecified = false;
          break;
        }
      }

      if (isUnspecified)
      {
        return false;
      }

      var isLoopback = bytes[15] == 1;
      for (var i = 0; i < 15 && isLoopback; i++)
      {
        if (bytes[i] != 0)
        {
          isLoopback = false;
        }
      }

      if (isLoopback)
      {
        return false;
      }

      if (bytes[0] == 0xFF)
      {
        return false;
      }

      if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80)
      {
        return false;
      }

      if ((bytes[0] & 0xFE) == 0xFC)
      {
        return false;
      }

      if (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0D && bytes[3] == 0xB8)
      {
        return false;
      }

      return true;
    }

    return false;
  }

  private sealed record WebhookUrlValidationResult(bool IsValid, Uri? Uri, string ErrorMessage)
  {
    public static WebhookUrlValidationResult Success(Uri uri)
    {
      return new WebhookUrlValidationResult(true, uri, string.Empty);
    }

    public static WebhookUrlValidationResult Fail(string errorMessage)
    {
      return new WebhookUrlValidationResult(false, null, errorMessage);
    }
  }
}
