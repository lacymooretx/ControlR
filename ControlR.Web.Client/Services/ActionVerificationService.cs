namespace ControlR.Web.Client.Services;

public interface IActionVerificationGuard
{
  Task<bool> EnsureVerified();
}

public class ActionVerificationGuard(
  IControlrApi controlrApi,
  IDialogService dialogService,
  ILogger<ActionVerificationGuard> logger) : IActionVerificationGuard
{
  private readonly IControlrApi _controlrApi = controlrApi;
  private readonly IDialogService _dialogService = dialogService;
  private readonly ILogger<ActionVerificationGuard> _logger = logger;

  public async Task<bool> EnsureVerified()
  {
    try
    {
      // Check if already verified
      var statusResult = await _controlrApi.GetActionVerificationStatus();
      if (statusResult.IsSuccess && statusResult.Value.IsVerified)
      {
        return true;
      }

      // Show verification dialog
      var options = new DialogOptions
      {
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
        CloseOnEscapeKey = true
      };

      var dialog = await _dialogService.ShowAsync<ActionVerificationDialog>(
        "Identity Verification",
        options);

      var result = await dialog.Result;
      return result is { Canceled: false };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error during action verification.");
      return false;
    }
  }
}
