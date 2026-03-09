using ControlR.Libraries.DataRedaction;

namespace ControlR.Web.Server.Options;

public class AppOptions
{
  public const string SectionKey = "AppOptions";

  public bool AllowAgentsToSelfBootstrap { get; init; }
  public string BrandingStoragePath { get; init; } = "./data/branding";
  public string? AuthenticatorIssuerName { get; init; }
  public bool DisableEmailSending { get; init; }
  public string? DockerGatewayIp { get; init; }
  public bool EnableCloudflareProxySupport { get; init; }
  public bool EnableNetworkTrust { get; init; }
  public bool EnablePublicRegistration { get; init; }
  public string? EntraIdClientId { get; init; }
  [ProtectedDataClassification]
  public string? EntraIdClientSecret { get; init; }
  public string? EntraIdInstance { get; init; }
  public string? EntraIdTenantId { get; init; }
  public string? GitHubClientId { get; init; }
  [ProtectedDataClassification]
  public string? GitHubClientSecret { get; init; }
  public string? InMemoryDatabaseName { get; init; }
  public string[] KnownNetworks { get; init; } = [];
  public string[] KnownProxies { get; init; } = [];
  public long MaxFileTransferSize { get; init; } = 100 * 1024 * 1024; // 100 MB default
  public string? MicrosoftClientId { get; init; }
  [ProtectedDataClassification]
  public string? MicrosoftClientSecret { get; init; }
  public int PersonalAccessTokenLifetimeDays { get; init; } = 0;
  public bool PersistPasskeyLogin { get; init; }
  public string RecordingsStoragePath { get; init; } = "./data/recordings";
  public bool RequireUserEmailConfirmation { get; init; }
  public string ToolboxStoragePath { get; init; } = "./data/toolbox";
  [ProtectedDataClassification]
  public string? Smtp2GoApiKey { get; init; }
  public string? SmtpDisplayName { get; init; }
  public string? SmtpEmail { get; init; }
  public bool UseHttpLogging { get; init; }
  public bool UseInMemoryDatabase { get; init; }
}
