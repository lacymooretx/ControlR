using System.Runtime.CompilerServices;

namespace ControlR.DesktopClient.Common.ServiceInterfaces;

public interface IWebcamCapturer
{
  /// <summary>
  /// Captures JPEG frames from the webcam as an async stream.
  /// </summary>
  /// <param name="preferredWidth">Preferred capture width.</param>
  /// <param name="preferredHeight">Preferred capture height.</param>
  /// <param name="cameraIndex">Camera device index (0 = default).</param>
  /// <param name="cancellationToken">Token to stop capture.</param>
  /// <returns>An async enumerable of JPEG-encoded frame bytes.</returns>
  IAsyncEnumerable<byte[]> CaptureFrames(
    int preferredWidth,
    int preferredHeight,
    int cameraIndex,
    CancellationToken cancellationToken);

  /// <summary>
  /// Checks whether webcam capture is available on this system.
  /// </summary>
  /// <returns>True if FFmpeg is available and webcam capture is supported.</returns>
  bool IsAvailable();
}
