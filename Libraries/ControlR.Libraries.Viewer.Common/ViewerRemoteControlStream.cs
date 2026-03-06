using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using Bitbound.SimpleMessenger;
using ControlR.Libraries.Shared.Services.Buffers;
using ControlR.Libraries.WebSocketRelay.Client;

namespace ControlR.Libraries.Viewer.Common;

public interface IViewerRemoteControlStream : IManagedRelayStream
{
  Task RequestClipboardText(Guid sessionId, CancellationToken cancellationToken);
  Task RequestKeyFrame(CancellationToken cancellationToken);
  Task SendAnnotationClear(CancellationToken cancellationToken);
  Task SendAnnotationStroke(float[] pointsX, float[] pointsY, string color, float thickness, Guid strokeId, CancellationToken cancellationToken);
  Task SendChangeDisplaysRequest(string displayId, CancellationToken cancellationToken);
  Task SendClipboardText(string text, Guid sessionId, CancellationToken cancellationToken);
  Task SendCloseStreamingSession(CancellationToken cancellationToken);
  Task SendKeyEvent(string key, string? code, bool isPressed, CancellationToken cancellationToken);
  Task SendKeyboardStateReset(CancellationToken cancellationToken);
  Task SendMouseButtonEvent(int button, bool isPressed, double percentX, double percentY,
    CancellationToken cancellationToken);
  Task SendMouseClick(int button, bool isDoubleClick, double percentX, double percentY,
    CancellationToken cancellationToken);
  Task SendPointerMove(double percentX, double percentY, CancellationToken cancellationToken);
  Task SendToggleBlockInput(bool isEnabled, CancellationToken cancellationToken);
  Task SendTogglePrivacyScreen(bool isEnabled, CancellationToken cancellationToken);
  Task SendTypeText(string text, CancellationToken cancellationToken);
  Task SendGetAvailableResolutions(string displayId, CancellationToken cancellationToken);
  Task SendChangeResolution(string displayId, int width, int height, int? refreshRate, CancellationToken cancellationToken);
  Task SendWheelScroll(double percentX, double percentY, double scrollY, double scrollX,
    CancellationToken cancellationToken);
  Task SendGetPrinters(CancellationToken cancellationToken);
  Task SendPrintJob(string printerName, string fileName, byte[] fileData, int copies, CancellationToken cancellationToken);
  Task SendAudioControl(bool isEnabled, int sampleRate, int channels, CancellationToken cancellationToken);
}

public class ViewerRemoteControlStream(
  TimeProvider timeProvider,
  IMessenger messenger,
  IMemoryProvider memoryProvider,
  IWaiter waiter,
  ILogger<ViewerRemoteControlStream> logger)
  : ManagedRelayStream(timeProvider, messenger, memoryProvider, waiter, logger), IViewerRemoteControlStream
{
  private readonly ILogger<ViewerRemoteControlStream> _logger = logger;
  private readonly IWaiter _waiter = waiter;

  public async Task RequestClipboardText(Guid sessionId, CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new RequestClipboardTextDto();
        var wrapper = DtoWrapper.Create(dto, DtoType.RequestClipboardText);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendAnnotationClear(CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new AnnotationClearDto();
        var wrapper = DtoWrapper.Create(dto, DtoType.AnnotationClear);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendAnnotationStroke(float[] pointsX, float[] pointsY, string color, float thickness, Guid strokeId, CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new AnnotationStrokeDto(pointsX, pointsY, color, thickness, strokeId);
        var wrapper = DtoWrapper.Create(dto, DtoType.AnnotationStroke);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task RequestKeyFrame(CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new RequestKeyFrameDto();
        var wrapper = DtoWrapper.Create(dto, DtoType.RequestKeyFrame);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendChangeDisplaysRequest(string displayId, CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new ChangeDisplaysDto(displayId);
        var wrapper = DtoWrapper.Create(dto, DtoType.ChangeDisplays);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendClipboardText(string text, Guid sessionId, CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new ClipboardTextDto(text, sessionId);
        var wrapper = DtoWrapper.Create(dto, DtoType.ClipboardText);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendCloseStreamingSession(CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new CloseStreamingSessionRequestDto();
        var wrapper = DtoWrapper.Create(dto, DtoType.CloseRemoteControlSession);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendKeyEvent(string key, string? code, bool isPressed, CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new KeyEventDto(key, code ?? string.Empty, isPressed);
        var wrapper = DtoWrapper.Create(dto, DtoType.KeyEvent);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendKeyboardStateReset(CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new ResetKeyboardStateDto();
        var wrapper = DtoWrapper.Create(dto, DtoType.ResetKeyboardState);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendMouseButtonEvent(
    int button,
    bool isPressed,
    double percentX,
    double percentY,
    CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new MouseButtonEventDto(button, isPressed, percentX, percentY);
        var wrapper = DtoWrapper.Create(dto, DtoType.MouseButtonEvent);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendMouseClick(int button, bool isDoubleClick, double percentX, double percentY,
    CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new MouseClickDto(button, isDoubleClick, percentX, percentY);
        var wrapper = DtoWrapper.Create(dto, DtoType.MouseClick);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendPointerMove(double percentX, double percentY, CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new MovePointerDto(percentX, percentY);
        var wrapper = DtoWrapper.Create(dto, DtoType.MovePointer);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendToggleBlockInput(bool isEnabled, CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new ToggleBlockInputDto(isEnabled);
        var wrapper = DtoWrapper.Create(dto, DtoType.ToggleBlockInput);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendTogglePrivacyScreen(bool isEnabled, CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new TogglePrivacyScreenDto(isEnabled);
        var wrapper = DtoWrapper.Create(dto, DtoType.TogglePrivacyScreen);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendTypeText(string text, CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new TypeTextDto(text);
        var wrapper = DtoWrapper.Create(dto, DtoType.TypeText);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendGetAvailableResolutions(string displayId, CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new GetAvailableResolutionsDto(displayId);
        var wrapper = DtoWrapper.Create(dto, DtoType.GetAvailableResolutions);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendChangeResolution(string displayId, int width, int height, int? refreshRate, CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new ChangeResolutionDto(displayId, width, height, refreshRate);
        var wrapper = DtoWrapper.Create(dto, DtoType.ChangeResolution);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendWheelScroll(double percentX, double percentY, double scrollY, double scrollX,
    CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new WheelScrollDto(percentX, percentY, scrollY, scrollX);
        var wrapper = DtoWrapper.Create(dto, DtoType.WheelScroll);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendGetPrinters(CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new GetPrintersDto();
        var wrapper = DtoWrapper.Create(dto, DtoType.GetPrinters);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendPrintJob(string printerName, string fileName, byte[] fileData, int copies, CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new PrintJobDto(printerName, fileName, fileData, copies);
        var wrapper = DtoWrapper.Create(dto, DtoType.PrintJob);
        await Send(wrapper, cancellationToken);
      });
  }

  public async Task SendAudioControl(bool isEnabled, int sampleRate, int channels, CancellationToken cancellationToken)
  {
    await TrySend(
      async () =>
      {
        var dto = new AudioControlDto(isEnabled, sampleRate, channels);
        var wrapper = DtoWrapper.Create(dto, DtoType.AudioControl);
        await Send(wrapper, cancellationToken);
      });
  }

  private async Task TrySend(Func<Task> func, [CallerMemberName] string callerName = "")
  {
    try
    {
      using var _ = _logger.BeginScope(callerName);
      await WaitForConnection();
      await func.Invoke();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while sending message via websocket stream..");
    }
  }

  private async Task WaitForConnection()
  {
    if (State == WebSocketState.Open)
    {
      return;
    }

    using var cts = new CancellationTokenSource();
    cts.CancelAfter(TimeSpan.FromSeconds(30));

    await _waiter.WaitFor(
      condition: () => State == WebSocketState.Open || IsDisposed,
      pollingDelay: TimeSpan.FromMilliseconds(100),
      cancellationToken: cts.Token);
  }
}