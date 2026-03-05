using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ControlR.Web.Client.Components.Pages.DeviceAccess;

public partial class PtyTerminal
{
  private readonly string _containerId = $"pty-terminal-{Guid.NewGuid():N}";
  private readonly Guid _terminalId = Guid.NewGuid();
  private DotNetObjectReference<PtyTerminal>? _dotNetRef;
  private bool _initialized;

  [Parameter]
  public required Guid DeviceId { get; set; }

  [Inject]
  public required IMessenger Messenger { get; init; }

  [Inject]
  public required IHubConnection<IViewerHub> ViewerHub { get; init; }

  [Inject]
  public required ISnackbar Snackbar { get; init; }

  [Inject]
  public required ILogger<PtyTerminal> Logger { get; init; }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    await base.OnAfterRenderAsync(firstRender);

    if (firstRender)
    {
      Messenger.Register<DtoReceivedMessage<PtyOutputDto>>(this, HandlePtyOutput);
      _dotNetRef = DotNetObjectReference.Create(this);

      await WaitForJsModule();

      var result = await JsModule.InvokeAsync<TerminalDimensions?>(
        "initTerminal",
        _containerId,
        _dotNetRef,
        80,
        24);

      if (result is null)
      {
        Snackbar.Add("Failed to initialize terminal", Severity.Error);
        Logger.LogError("xterm.js initialization returned null for container {ContainerId}", _containerId);
        return;
      }

      var createResult = await ViewerHub.Server.CreatePtySession(
        DeviceId,
        _terminalId,
        result.Cols,
        result.Rows);

      if (!createResult.IsSuccess)
      {
        Snackbar.Add("Failed to start PTY session on agent", Severity.Error);
        Logger.LogError("CreatePtySession failed: {Reason}", createResult.Reason);
        return;
      }

      _initialized = true;
      Logger.LogInformation("PTY terminal initialized. ID: {TerminalId}, Size: {Cols}x{Rows}",
        _terminalId, result.Cols, result.Rows);
    }
  }

  [JSInvokable]
  public async Task OnTerminalInput(string base64Data)
  {
    if (!_initialized)
    {
      return;
    }

    try
    {
      var data = Convert.FromBase64String(base64Data);
      var dto = new PtyInputDto(_terminalId, data);
      var result = await ViewerHub.Server.SendPtyInput(DeviceId, dto);

      if (!result.IsSuccess)
      {
        Logger.LogWarning("SendPtyInput failed: {Reason}", result.Reason);
      }
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error sending PTY input.");
    }
  }

  [JSInvokable]
  public async Task OnTerminalResize(int cols, int rows)
  {
    if (!_initialized)
    {
      return;
    }

    try
    {
      var dto = new PtyResizeDto(_terminalId, cols, rows);
      var result = await ViewerHub.Server.ResizePty(DeviceId, dto);

      if (!result.IsSuccess)
      {
        Logger.LogWarning("ResizePty failed: {Reason}", result.Reason);
      }
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error resizing PTY.");
    }
  }

  protected override async ValueTask DisposeAsync(bool disposing)
  {
    if (disposing)
    {
      Messenger.UnregisterAll(this);

      if (_initialized)
      {
        try
        {
          await ViewerHub.Server.ClosePtySession(DeviceId, _terminalId);
        }
        catch (Exception ex)
        {
          Logger.LogError(ex, "Error closing PTY session.");
        }
      }

      if (IsJsModuleReady)
      {
        try
        {
          await JsModule.InvokeVoidAsync("dispose", _containerId);
        }
        catch (JSDisconnectedException)
        {
          // Expected during circuit disconnect
        }
        catch (Exception ex)
        {
          Logger.LogError(ex, "Error disposing JS terminal.");
        }
      }

      _dotNetRef?.Dispose();
    }

    await base.DisposeAsync(disposing);
  }

  private async Task HandlePtyOutput(object subscriber, DtoReceivedMessage<PtyOutputDto> message)
  {
    var dto = message.Dto;

    if (dto.TerminalId != _terminalId)
    {
      return;
    }

    if (!IsJsModuleReady)
    {
      return;
    }

    try
    {
      await JsModule.InvokeVoidAsync("writeOutput", _containerId, dto.Data);
    }
    catch (JSDisconnectedException)
    {
      // Expected during circuit disconnect
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error writing PTY output to terminal.");
    }
  }

  private async Task FocusTerminal()
  {
    if (IsJsModuleReady)
    {
      try
      {
        await JsModule.InvokeVoidAsync("focus", _containerId);
      }
      catch
      {
        // Ignore focus errors
      }
    }
  }

  // JSON-serializable for JS interop
  private class TerminalDimensions
  {
    public int Cols { get; set; }
    public int Rows { get; set; }
  }
}
