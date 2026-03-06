using System.Net.Http.Json;
using ControlR.Libraries.Shared.Enums;

namespace ControlR.Web.Server.Services.Ticketing;

public class WebhookTicketingProvider(
  IHttpClientFactory httpClientFactory,
  ILogger<WebhookTicketingProvider> logger) : ITicketingProvider
{
  public TicketingProvider ProviderType => TicketingProvider.Custom;

  public async Task<Result<string>> CreateTicket(
    TicketingIntegration integration,
    string decryptedApiKey,
    CreateTicketRequestDto request)
  {
    try
    {
      using var client = httpClientFactory.CreateClient("Ticketing");
      client.DefaultRequestHeaders.Add("X-Api-Key", decryptedApiKey);

      var payload = new
      {
        subject = request.Subject,
        description = request.Description,
        priority = request.Priority,
        project = integration.DefaultProject,
        deviceId = request.DeviceId,
        sessionId = request.SessionId,
        alertId = request.AlertId,
        provider = integration.Provider.ToString(),
        timestamp = DateTimeOffset.UtcNow,
      };

      using var response = await client.PostAsJsonAsync(integration.BaseUrl, payload);
      response.EnsureSuccessStatusCode();

      var responseBody = await response.Content.ReadAsStringAsync();

      // Try to extract an ID from the response; fall back to a generated one
      var ticketId = !string.IsNullOrWhiteSpace(responseBody) && responseBody.Length < 500
        ? responseBody.Trim().Trim('"')
        : Guid.NewGuid().ToString();

      return Result.Ok(ticketId);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to create ticket via webhook to {Url}.", integration.BaseUrl);
      return Result.Fail<string>($"Webhook call failed: {ex.Message}");
    }
  }
}
