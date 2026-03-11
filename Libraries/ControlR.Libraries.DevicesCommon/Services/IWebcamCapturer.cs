using ControlR.Libraries.Shared.Dtos.HubDtos;

namespace ControlR.Libraries.DevicesCommon.Services;

public interface IWebcamCapturer
{
  /// <summary>
  /// Captures JPEG frames from the webcam as an async stream.
  /// </summary>
  IAsyncEnumerable<byte[]> CaptureFrames(
    int preferredWidth,
    int preferredHeight,
    int cameraIndex,
    CancellationToken cancellationToken);

  /// <summary>
  /// Checks whether webcam capture is available on this system.
  /// </summary>
  bool IsAvailable();

  /// <summary>
  /// Enumerates available webcam devices.
  /// </summary>
  Task<WebcamInfoDto[]> EnumerateCameras();
}
