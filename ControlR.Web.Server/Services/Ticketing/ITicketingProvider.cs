using ControlR.Libraries.Shared.Enums;

namespace ControlR.Web.Server.Services.Ticketing;

public interface ITicketingProvider
{
  TicketingProvider ProviderType { get; }

  /// <summary>
  /// Creates a ticket in the external system and returns the external ticket ID.
  /// </summary>
  /// <param name="integration">The integration configuration.</param>
  /// <param name="decryptedApiKey">The decrypted API key for authentication.</param>
  /// <param name="request">The ticket creation request.</param>
  Task<Result<string>> CreateTicket(
    TicketingIntegration integration,
    string decryptedApiKey,
    CreateTicketRequestDto request);
}
