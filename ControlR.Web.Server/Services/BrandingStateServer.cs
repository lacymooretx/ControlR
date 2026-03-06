using ControlR.Libraries.Shared.Constants;
using ControlR.Web.Client.Services;

namespace ControlR.Web.Server.Services;

internal class BrandingStateServer(
  IDbContextFactory<AppDb> dbFactory,
  ILogger<BrandingStateServer> logger) : IBrandingState
{
  private readonly IDbContextFactory<AppDb> _dbFactory = dbFactory;
  private readonly ILogger<BrandingStateServer> _logger = logger;

  public string ProductName { get; private set; } = "ControlR";
  public string PrimaryColor { get; private set; } = "#2196F3";
  public string? SecondaryColor { get; private set; }
  public string? LogoUrl { get; private set; }
  public bool IsLoaded { get; private set; }

  public async Task LoadAsync()
  {
    if (IsLoaded)
    {
      return;
    }

    await FetchBranding();
  }

  public async Task RefreshAsync()
  {
    await FetchBranding();
  }

  private async Task FetchBranding()
  {
    try
    {
      await using var db = await _dbFactory.CreateDbContextAsync();
      var settings = await db.BrandingSettings
        .AsNoTracking()
        .FirstOrDefaultAsync();

      if (settings is not null)
      {
        ProductName = settings.ProductName;
        PrimaryColor = settings.PrimaryColor;
        SecondaryColor = settings.SecondaryColor;
        LogoUrl = !string.IsNullOrEmpty(settings.LogoStoragePath)
          ? $"{HttpConstants.BrandingEndpoint}/logo"
          : null;
      }

      IsLoaded = true;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while getting branding settings.");
    }
  }
}
