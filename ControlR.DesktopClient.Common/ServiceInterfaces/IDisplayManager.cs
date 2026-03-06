using System.Collections.Immutable;
using System.Drawing;
using ControlR.DesktopClient.Common.Models;
using ControlR.Libraries.Shared.Primitives;

namespace ControlR.DesktopClient.Common.ServiceInterfaces;

public interface IDisplayManager
{
  bool IsPrivacyScreenEnabled
  {
    get => false;
  }
  Task<Result> ChangeResolution(string displayId, int width, int height, int? refreshRate);
  Task<Point> ConvertPercentageLocationToAbsolute(string displayName, double percentX, double percentY);
  Task<Result<(int Width, int Height, int RefreshRate)[]>> GetAvailableResolutions(string displayId);
  Task<ImmutableList<DisplayInfo>> GetDisplays();
  Task<DisplayInfo?> GetPrimaryDisplay();
  Task<Rectangle> GetVirtualScreenBounds();
  Task ReloadDisplays();
  Task<Result> SetPrivacyScreen(bool isEnabled);
  Task<Result<DisplayInfo>> TryFindDisplay(string deviceName);
}