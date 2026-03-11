using Bitbound.SimpleMessenger;
using ControlR.Libraries.Messenger.Extensions.Messages;
using ControlR.Libraries.Shared.Dtos.HubDtos;
using ControlR.Libraries.Shared.Hubs;
using ControlR.Libraries.Signalr.Client.Extensions;
using ControlR.Libraries.Viewer.Common.State;
using Microsoft.AspNetCore.Components;

namespace ControlR.Web.Client.Components.Pages.DeviceAccess;

public partial class StandaloneWebcamViewer : ComponentBase, IDisposable
{
  private bool _isStreaming;
  private string? _webcamImageDataUri;
  private WebcamInfoDto[] _cameras = [];
  private int _selectedCameraIndex;
  private bool _cameraListReceived;

  [Inject]
  public required IDeviceState DeviceAccessState { get; init; }

  [SupplyParameterFromQuery]
  public required Guid DeviceId { get; init; }

  [Inject]
  public required IMessenger Messenger { get; init; }

  [Inject]
  public required IHubConnection<IViewerHub> ViewerHub { get; init; }

  [Inject]
  public required ISnackbar Snackbar { get; init; }

  [Inject]
  public required ILogger<StandaloneWebcamViewer> Logger { get; init; }

  protected override void OnInitialized()
  {
    Messenger.Register<DtoReceivedMessage<StandaloneWebcamFrameDto>>(this, HandleWebcamFrame);
    Messenger.Register<DtoReceivedMessage<WebcamInfoDto[]>>(this, HandleWebcamList);
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (firstRender)
    {
      await RequestCameraList();
    }
  }

  public void Dispose()
  {
    Messenger.UnregisterAll(this);

    if (_isStreaming)
    {
      _ = StopWebcam();
    }

    GC.SuppressFinalize(this);
  }

  private async Task HandleCameraChanged(int newIndex)
  {
    _selectedCameraIndex = newIndex;

    if (_isStreaming)
    {
      await StopWebcam();
      await StartWebcam();
    }
  }

  private async Task RequestCameraList()
  {
    try
    {
      _cameraListReceived = false;
      await InvokeAsync(StateHasChanged);

      var result = await ViewerHub.Server.GetStandaloneWebcamList(DeviceId);
      if (!result.IsSuccess)
      {
        Logger.LogWarning("Failed to get camera list: {Reason}", result.Reason);
        _cameraListReceived = true;
        await InvokeAsync(StateHasChanged);
      }
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error requesting camera list.");
      _cameraListReceived = true;
      await InvokeAsync(StateHasChanged);
    }
  }

  private async Task StartWebcam()
  {
    try
    {
      _webcamImageDataUri = null;
      _isStreaming = true;
      await InvokeAsync(StateHasChanged);

      var result = await ViewerHub.Server.StartStandaloneWebcam(
        DeviceId, _selectedCameraIndex, 640, 480);

      if (!result.IsSuccess)
      {
        Snackbar.Add($"Failed to start camera: {result.Reason}", Severity.Error);
        _isStreaming = false;
        await InvokeAsync(StateHasChanged);
      }
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error starting webcam.");
      Snackbar.Add("Failed to start camera", Severity.Error);
      _isStreaming = false;
      await InvokeAsync(StateHasChanged);
    }
  }

  private async Task StopWebcam()
  {
    try
    {
      _isStreaming = false;
      _webcamImageDataUri = null;
      await InvokeAsync(StateHasChanged);

      var result = await ViewerHub.Server.StopStandaloneWebcam(DeviceId);
      if (!result.IsSuccess)
      {
        Logger.LogWarning("Failed to stop webcam: {Reason}", result.Reason);
      }
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error stopping webcam.");
    }
  }

  private async Task HandleWebcamFrame(object subscriber, DtoReceivedMessage<StandaloneWebcamFrameDto> message)
  {
    if (!_isStreaming)
    {
      return;
    }

    var frame = message.Dto;
    var base64 = Convert.ToBase64String(frame.JpegData);
    _webcamImageDataUri = $"data:image/jpeg;base64,{base64}";
    await InvokeAsync(StateHasChanged);
  }

  private async Task HandleWebcamList(object subscriber, DtoReceivedMessage<WebcamInfoDto[]> message)
  {
    _cameras = message.Dto;
    _cameraListReceived = true;
    if (_cameras.Length > 0 && _selectedCameraIndex >= _cameras.Length)
    {
      _selectedCameraIndex = 0;
    }
    await InvokeAsync(StateHasChanged);
  }
}
