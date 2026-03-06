using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Drawing;
using ControlR.DesktopClient.Common.Models;
using ControlR.DesktopClient.Common.ServiceInterfaces;
using ControlR.DesktopClient.Windows.Helpers;
using ControlR.Libraries.NativeInterop.Windows;
using Microsoft.Extensions.Logging;
using ControlR.Libraries.Shared.Primitives;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace ControlR.DesktopClient.Windows.Services;

internal class DisplayManagerWindows(
  IWin32Interop win32Interop,
  IWindowsMessagePump messagePump,
  ILogger<DisplayManagerWindows> logger) : IDisplayManager
{
  private readonly Lock _displayLock = new();
  private readonly ConcurrentDictionary<string, DisplayInfo> _displays = new();
  private readonly ILogger<DisplayManagerWindows> _logger = logger;
  private readonly IWindowsMessagePump _messagePump = messagePump;
  private readonly IWin32Interop _win32Interop = win32Interop;

  private nint _privacyWindow = nint.Zero;

  public bool IsPrivacyScreenEnabled => _privacyWindow != nint.Zero;

  public async Task<Point> ConvertPercentageLocationToAbsolute(string displayName, double percentX, double percentY)
  {
    var findResult = await TryFindDisplay(displayName);
    if (!findResult.IsSuccess)
    {
      return Point.Empty;
    }

    var bounds = findResult.Value.MonitorArea;
    var absoluteX = (int)(bounds.Left + bounds.Width * percentX);
    var absoluteY = (int)(bounds.Top + bounds.Height * percentY);

    return new Point(absoluteX, absoluteY);
  }

  public Task<ImmutableList<DisplayInfo>> GetDisplays()
  {
    lock (_displayLock)
    {
      EnsureDisplaysLoaded();

      var displayDtos = _displays
        .Values
        .ToImmutableList();

      return Task.FromResult(displayDtos);
    }
  }

  public async Task<DisplayInfo?> GetPrimaryDisplay()
  {
    lock (_displayLock)
    {
      EnsureDisplaysLoaded();
      return _displays.Values.FirstOrDefault(x => x.IsPrimary)
        ?? _displays.Values.FirstOrDefault();
    }
  }

  public Task<Rectangle> GetVirtualScreenBounds()
  {
    var width = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXVIRTUALSCREEN);
    var height = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYVIRTUALSCREEN);
    var left = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_XVIRTUALSCREEN);
    var top = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_YVIRTUALSCREEN);
    return Task.FromResult(new Rectangle(left, top, width, height));
  }

  public Task ReloadDisplays()
  {
    lock (_displayLock)
    {
      ReloadDisplaysImpl();
    }
    return Task.CompletedTask;
  }

  public Task<Result> ChangeResolution(string displayId, int width, int height, int? refreshRate)
  {
    try
    {
      lock (_displayLock)
      {
        EnsureDisplaysLoaded();
        if (!_displays.TryGetValue(displayId, out _))
        {
          return Task.FromResult(Result.Fail($"Display '{displayId}' not found."));
        }
      }

      unsafe
      {
        var devMode = new DEVMODEW { dmSize = (ushort)sizeof(DEVMODEW) };

        // Get current settings as a baseline
        if (!PInvoke.EnumDisplaySettings(displayId, ENUM_DISPLAY_SETTINGS_MODE.ENUM_CURRENT_SETTINGS, ref devMode))
        {
          return Task.FromResult(Result.Fail("Failed to get current display settings."));
        }

        devMode.dmPelsWidth = (uint)width;
        devMode.dmPelsHeight = (uint)height;
        devMode.dmFields = DEVMODE_FIELD_FLAGS.DM_PELSWIDTH | DEVMODE_FIELD_FLAGS.DM_PELSHEIGHT;

        if (refreshRate.HasValue)
        {
          devMode.dmDisplayFrequency = (uint)refreshRate.Value;
          devMode.dmFields |= DEVMODE_FIELD_FLAGS.DM_DISPLAYFREQUENCY;
        }

        // Test first (CDS_TEST = 0x00000002)
        var testResult = PInvoke.ChangeDisplaySettingsEx(
          displayId,
          &devMode,
          HWND.Null,
          (uint)2,
          (void*)null);

        if (testResult != DISP_CHANGE.DISP_CHANGE_SUCCESSFUL)
        {
          var errorMsg = testResult switch
          {
            DISP_CHANGE.DISP_CHANGE_BADMODE => "The resolution is not supported by this display.",
            DISP_CHANGE.DISP_CHANGE_BADPARAM => "Invalid parameter.",
            DISP_CHANGE.DISP_CHANGE_FAILED => "The display driver failed the request.",
            DISP_CHANGE.DISP_CHANGE_NOTUPDATED => "Unable to write settings to the registry.",
            DISP_CHANGE.DISP_CHANGE_RESTART => "A restart is required for this resolution change.",
            _ => $"ChangeDisplaySettingsEx test failed with code {testResult}."
          };
          return Task.FromResult(Result.Fail(errorMsg));
        }

        // Apply the change dynamically (0 = session-scoped, no registry write)
        var applyResult = PInvoke.ChangeDisplaySettingsEx(
          displayId,
          &devMode,
          HWND.Null,
          (uint)0,
          (void*)null);

        if (applyResult != DISP_CHANGE.DISP_CHANGE_SUCCESSFUL)
        {
          return Task.FromResult(Result.Fail($"ChangeDisplaySettingsEx failed with code {applyResult}."));
        }

        _logger.LogInformation(
          "Changed resolution for display {DisplayId} to {Width}x{Height} @ {RefreshRate}Hz",
          displayId, width, height, refreshRate);

        // Reload displays to pick up the new resolution
        lock (_displayLock)
        {
          ReloadDisplaysImpl();
        }

        return Task.FromResult(Result.Ok());
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error changing resolution for display {DisplayId}.", displayId);
      return Task.FromResult(Result.Fail($"Error changing resolution: {ex.Message}"));
    }
  }

  public Task<Result<(int Width, int Height, int RefreshRate)[]>> GetAvailableResolutions(string displayId)
  {
    try
    {
      lock (_displayLock)
      {
        EnsureDisplaysLoaded();
        if (!_displays.TryGetValue(displayId, out _))
        {
          return Task.FromResult(Result.Fail<(int, int, int)[]>($"Display '{displayId}' not found."));
        }
      }

      var resolutions = new HashSet<(int Width, int Height, int RefreshRate)>();

      unsafe
      {
        var devMode = new DEVMODEW { dmSize = (ushort)sizeof(DEVMODEW) };
        uint modeIndex = 0;

        while (PInvoke.EnumDisplaySettings(displayId, (ENUM_DISPLAY_SETTINGS_MODE)modeIndex, ref devMode))
        {
          // Only include modes with at least 8bpp (skip low-color modes)
          if (devMode.dmBitsPerPel >= 8)
          {
            resolutions.Add(((int)devMode.dmPelsWidth, (int)devMode.dmPelsHeight, (int)devMode.dmDisplayFrequency));
          }
          modeIndex++;
        }
      }

      var sorted = resolutions
        .OrderByDescending(r => r.Width)
        .ThenByDescending(r => r.Height)
        .ThenByDescending(r => r.RefreshRate)
        .ToArray();

      return Task.FromResult(Result.Ok(sorted));
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting available resolutions for display {DisplayId}.", displayId);
      return Task.FromResult(Result.Fail<(int, int, int)[]>($"Error getting resolutions: {ex.Message}"));
    }
  }

  public async Task<Result> SetPrivacyScreen(bool isEnabled)
  {
    try
    {
      if (isEnabled)
      {
        if (_privacyWindow != nint.Zero)
        {
          _logger.LogWarning("Privacy screen is already enabled");
          return Result.Ok();
        }

        var bounds = await GetVirtualScreenBounds();
        _privacyWindow = await _messagePump.InvokeOnWindowThread(() =>
          _win32Interop.CreatePrivacyScreenWindow(
            bounds.Left,
            bounds.Top,
            bounds.Width,
            bounds.Height));

        if (_privacyWindow == nint.Zero)
        {
          _logger.LogError("Failed to create privacy screen window");
          return Result.Fail("Failed to create privacy screen window");
        }

        _logger.LogInformation("Enabled privacy screen");
        return Result.Ok();
      }
      else
      {
        if (_privacyWindow == nint.Zero)
        {
          _logger.LogWarning("Privacy screen is not enabled");
          return Result.Ok();
        }

        var windowHandle = _privacyWindow;
        _privacyWindow = nint.Zero;

        await _messagePump.InvokeOnWindowThread(() =>
          _win32Interop.DestroyPrivacyScreenWindow(windowHandle));

        _privacyWindow = nint.Zero;
        _logger.LogInformation("Disabled privacy screen");
        return Result.Ok();
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to {Action} privacy screen", isEnabled ? "enable" : "disable");
      return Result.Fail($"Failed to {(isEnabled ? "enable" : "disable")} privacy screen");
    }
  }

  public Task<Result<DisplayInfo>> TryFindDisplay(string deviceName)
  {
    lock (_displayLock)
    {
      EnsureDisplaysLoaded();
      if (_displays.TryGetValue(deviceName, out var display))
      {
        return Task.FromResult(Result.Ok(display));
      }
      return Task.FromResult(Result.Fail<DisplayInfo>("Display not found."));
    }
  }

  private void EnsureDisplaysLoaded()
  {
    // Must be called within lock
    if (_displays.IsEmpty)
    {
      ReloadDisplaysImpl();
    }
  }

  private void ReloadDisplaysImpl()
  {
    // Must be called within lock
    try
    {
      _displays.Clear();
      foreach (var display in DisplayEnumHelperWindows.GetDisplays())
      {
        _displays[display.DeviceName] = display;
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error loading display list.");
    }
  }
}