using ControlR.Libraries.Shared.Constants;

namespace ControlR.Web.Client.Services;

internal class BrandingStateClient(
  IControlrApi controlrApi,
  ILogger<BrandingStateClient> logger) : IBrandingState
{
  private readonly IControlrApi _controlrApi = controlrApi;
  private readonly ILogger<BrandingStateClient> _logger = logger;

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
      var result = await _controlrApi.GetBranding();
      if (result.IsSuccess)
      {
        ProductName = result.Value.ProductName;
        PrimaryColor = result.Value.PrimaryColor;
        SecondaryColor = result.Value.SecondaryColor;
        LogoUrl = result.Value.HasLogo
          ? $"{HttpConstants.BrandingEndpoint}/logo"
          : null;
        IsLoaded = true;
      }
      else
      {
        _logger.LogError("Failed to get branding settings: {Reason}", result.Reason);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while getting branding settings.");
    }
  }
}
