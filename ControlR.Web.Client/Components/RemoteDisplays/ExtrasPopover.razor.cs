using ControlR.Libraries.Shared.Dtos.RemoteControlDtos;

namespace ControlR.Web.Client.Components.RemoteDisplays;

public partial class ExtrasPopover : DisposableComponent
{
  private AvailableResolutionDto[]? _availableResolutions;
  private bool _isChangingResolution;
  private bool _isLoadingResolutions;
  private string? _resolutionError;
  private string? _selectedResolution;

  // Print state
  private PrinterInfoDto[]? _availablePrinters;
  private bool _isLoadingPrinters;
  private bool _isPrinting;
  private string? _selectedPrinter;
  private string? _printFileName;
  private byte[]? _printFileData;

  [Inject]
  public required IDeviceState DeviceState { get; init; }
  [Inject]
  public required ILogger<ExtrasPopover> Logger { get; init; }
  [Inject]
  public required IRemoteControlState RemoteControlState { get; init; }
  [Inject]
  public required IViewerRemoteControlStream RemoteControlStream { get; init; }
  [Inject]
  public required ISnackbar Snackbar { get; init; }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (firstRender)
    {
      Disposables.AddRange(
        RemoteControlState.OnStateChanged(() => InvokeAsync(StateHasChanged)),
        RemoteControlStream.RegisterMessageHandler(this, HandleDtoReceived));
    }
    await base.OnAfterRenderAsync(firstRender);
  }

  private void HandleMetricsToggled(bool value)
  {
    RemoteControlState.IsMetricsEnabled = value;
  }

  private async Task HandleLoadResolutions()
  {
    try
    {
      if (RemoteControlStream.State != System.Net.WebSockets.WebSocketState.Open)
      {
        Snackbar.Add("No active remote session", Severity.Error);
        return;
      }

      var displayId = RemoteControlState.SelectedDisplay?.DisplayId;
      if (string.IsNullOrEmpty(displayId))
      {
        Snackbar.Add("No display selected", Severity.Error);
        return;
      }

      _isLoadingResolutions = true;
      _resolutionError = null;
      StateHasChanged();

      using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
      await RemoteControlStream.SendGetAvailableResolutions(displayId, cts.Token);
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error while requesting available resolutions.");
      _isLoadingResolutions = false;
      _resolutionError = "Failed to request resolutions.";
      Snackbar.Add("An error occurred while requesting resolutions", Severity.Error);
      StateHasChanged();
    }
  }

  private async Task HandleResolutionChanged(string value)
  {
    try
    {
      if (string.IsNullOrEmpty(value))
      {
        return;
      }

      if (RemoteControlStream.State != System.Net.WebSockets.WebSocketState.Open)
      {
        Snackbar.Add("No active remote session", Severity.Error);
        return;
      }

      var displayId = RemoteControlState.SelectedDisplay?.DisplayId;
      if (string.IsNullOrEmpty(displayId))
      {
        Snackbar.Add("No display selected", Severity.Error);
        return;
      }

      // Parse "WIDTHxHEIGHT@REFRESH"
      var parts = value.Split(['x', '@']);
      if (parts.Length != 3 ||
          !int.TryParse(parts[0], out var width) ||
          !int.TryParse(parts[1], out var height) ||
          !int.TryParse(parts[2], out var refreshRate))
      {
        Snackbar.Add("Invalid resolution format", Severity.Error);
        return;
      }

      _isChangingResolution = true;
      _selectedResolution = value;
      StateHasChanged();

      Snackbar.Add($"Changing resolution to {width}x{height} @ {refreshRate}Hz", Severity.Info);

      using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
      await RemoteControlStream.SendChangeResolution(displayId, width, height, refreshRate, cts.Token);
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error while changing resolution.");
      _isChangingResolution = false;
      Snackbar.Add("An error occurred while changing resolution", Severity.Error);
      StateHasChanged();
    }
  }

  private async Task HandleLoadPrinters()
  {
    try
    {
      if (RemoteControlStream.State != System.Net.WebSockets.WebSocketState.Open)
      {
        Snackbar.Add("No active remote session", Severity.Error);
        return;
      }

      _isLoadingPrinters = true;
      StateHasChanged();

      using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
      await RemoteControlStream.SendGetPrinters(cts.Token);
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error while requesting printers.");
      _isLoadingPrinters = false;
      Snackbar.Add("An error occurred while requesting printers", Severity.Error);
      StateHasChanged();
    }
  }

  private async Task HandlePrintFileSelected(IBrowserFile file)
  {
    try
    {
      const long maxFileSize = 50 * 1024 * 1024; // 50 MB
      _printFileName = file.Name;
      using var stream = file.OpenReadStream(maxFileSize);
      using var ms = new System.IO.MemoryStream();
      await stream.CopyToAsync(ms);
      _printFileData = ms.ToArray();
      StateHasChanged();
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error reading print file.");
      Snackbar.Add("Failed to read file", Severity.Error);
      _printFileName = null;
      _printFileData = null;
    }
  }

  private async Task HandlePrintClicked()
  {
    try
    {
      if (string.IsNullOrEmpty(_selectedPrinter) || _printFileData is null || _printFileName is null)
      {
        return;
      }

      if (RemoteControlStream.State != System.Net.WebSockets.WebSocketState.Open)
      {
        Snackbar.Add("No active remote session", Severity.Error);
        return;
      }

      _isPrinting = true;
      StateHasChanged();

      Snackbar.Add($"Sending print job to {_selectedPrinter}", Severity.Info);

      using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
      await RemoteControlStream.SendPrintJob(_selectedPrinter, _printFileName, _printFileData, 1, cts.Token);
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error while sending print job.");
      _isPrinting = false;
      Snackbar.Add("An error occurred while sending print job", Severity.Error);
      StateHasChanged();
    }
  }

  private async Task HandleDtoReceived(DtoWrapper wrapper)
  {
    try
    {
      switch (wrapper.DtoType)
      {
        case DtoType.GetAvailableResolutionsResult:
          {
            var dto = wrapper.GetPayload<GetAvailableResolutionsResultDto>();
            _isLoadingResolutions = false;

            if (dto.IsSuccess)
            {
              _availableResolutions = dto.Resolutions;
              _resolutionError = null;

              // Set current resolution as selected
              var currentDisplay = RemoteControlState.SelectedDisplay;
              if (currentDisplay is not null)
              {
                var currentWidth = (int)currentDisplay.Width;
                var currentHeight = (int)currentDisplay.Height;
                var match = _availableResolutions
                  .FirstOrDefault(r => r.Width == currentWidth && r.Height == currentHeight);
                if (match is not null)
                {
                  _selectedResolution = $"{match.Width}x{match.Height}@{match.RefreshRate}";
                }
              }
            }
            else
            {
              _resolutionError = dto.ErrorMessage ?? "Failed to get resolutions.";
              Snackbar.Add(_resolutionError, Severity.Warning);
            }

            await InvokeAsync(StateHasChanged);
            break;
          }
        case DtoType.ChangeResolutionResult:
          {
            var dto = wrapper.GetPayload<ChangeResolutionResultDto>();
            _isChangingResolution = false;

            if (dto.IsSuccess)
            {
              Snackbar.Add("Resolution changed successfully", Severity.Success);
            }
            else
            {
              Snackbar.Add($"Failed to change resolution: {dto.ErrorMessage}", Severity.Error);
            }

            await InvokeAsync(StateHasChanged);
            break;
          }
        case DtoType.GetPrintersResult:
          {
            var dto = wrapper.GetPayload<GetPrintersResultDto>();
            _isLoadingPrinters = false;

            if (dto.Printers.Length > 0)
            {
              _availablePrinters = dto.Printers;
              _selectedPrinter = dto.Printers.FirstOrDefault(p => p.IsDefault)?.Name
                ?? dto.Printers[0].Name;
            }
            else
            {
              Snackbar.Add("No printers found on remote device", Severity.Warning);
            }

            await InvokeAsync(StateHasChanged);
            break;
          }
        case DtoType.PrintJobResult:
          {
            var dto = wrapper.GetPayload<PrintJobResultDto>();
            _isPrinting = false;

            if (dto.IsSuccess)
            {
              Snackbar.Add("Print job sent successfully", Severity.Success);
            }
            else
            {
              Snackbar.Add($"Print failed: {dto.ErrorMessage}", Severity.Error);
            }

            await InvokeAsync(StateHasChanged);
            break;
          }
        default:
          break;
      }
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error while handling received DTO of type {DtoType}.", wrapper.DtoType);
    }
  }
}
