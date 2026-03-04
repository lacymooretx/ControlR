using System.Net.Http.Json;
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

      using var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url);
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
      for (var attempt = 1; attempt <= 3; attempt++)
      {
        await DeliverWebhook(appDb, subscription, webhookEvent.EventType, webhookEvent.Payload, attempt, ct);

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
}
