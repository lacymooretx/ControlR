using System.Diagnostics;
using System.Runtime.CompilerServices;
using ControlR.Libraries.Shared.Dtos.HubDtos;
using Microsoft.Extensions.Logging;

namespace ControlR.Libraries.DevicesCommon.Services;

public class WebcamCapturer(ILogger<WebcamCapturer> logger) : IWebcamCapturer
{
  private readonly ILogger<WebcamCapturer> _logger = logger;
  private string? _ffmpegPath;

  public bool IsAvailable()
  {
    return FindFfmpeg() is not null;
  }

  public async Task<WebcamInfoDto[]> EnumerateCameras()
  {
    var ffmpegPath = FindFfmpeg();
    if (ffmpegPath is null)
    {
      return [];
    }

    try
    {
      if (OperatingSystem.IsWindows())
      {
        return await EnumerateWindowsCameras(ffmpegPath);
      }

      if (OperatingSystem.IsMacOS())
      {
        return await EnumerateMacCameras(ffmpegPath);
      }

      if (OperatingSystem.IsLinux())
      {
        return EnumerateLinuxCameras();
      }
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to enumerate cameras.");
    }

    return [new WebcamInfoDto(0, "Default Camera")];
  }

  public async IAsyncEnumerable<byte[]> CaptureFrames(
    int preferredWidth,
    int preferredHeight,
    int cameraIndex,
    [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    var ffmpegPath = FindFfmpeg();
    if (ffmpegPath is null)
    {
      _logger.LogWarning("FFmpeg not found. Webcam capture is not available.");
      yield break;
    }

    var args = BuildFfmpegArgs(preferredWidth, preferredHeight, cameraIndex);
    _logger.LogInformation("Starting webcam capture: {FfmpegPath} {Args}", ffmpegPath, args);

    using var process = new Process();
    process.StartInfo = new ProcessStartInfo
    {
      FileName = ffmpegPath,
      Arguments = args,
      UseShellExecute = false,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true
    };

    try
    {
      process.Start();

      // Discard stderr to prevent buffer deadlock.
      _ = Task.Run(async () =>
      {
        try
        {
          while (!process.HasExited && !cancellationToken.IsCancellationRequested)
          {
            var line = await process.StandardError.ReadLineAsync(cancellationToken);
            if (line is null) break;
            _logger.LogDebug("FFmpeg: {Line}", line);
          }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
          _logger.LogDebug(ex, "FFmpeg stderr reader ended.");
        }
      }, cancellationToken);

      var stream = process.StandardOutput.BaseStream;

      await foreach (var frame in ParseMjpegStream(stream, cancellationToken))
      {
        yield return frame;
      }
    }
    finally
    {
      if (!process.HasExited)
      {
        try
        {
          process.Kill(entireProcessTree: true);
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "Error killing FFmpeg process.");
        }
      }
    }
  }

  private string? FindFfmpeg()
  {
    if (_ffmpegPath is not null)
    {
      return _ffmpegPath;
    }

    string[] candidates;

    if (OperatingSystem.IsWindows())
    {
      candidates =
      [
        "ffmpeg.exe",
        @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
        @"C:\ffmpeg\bin\ffmpeg.exe"
      ];
    }
    else if (OperatingSystem.IsMacOS())
    {
      candidates =
      [
        "ffmpeg",
        "/opt/homebrew/bin/ffmpeg",
        "/usr/local/bin/ffmpeg",
        "/usr/bin/ffmpeg"
      ];
    }
    else
    {
      candidates =
      [
        "ffmpeg",
        "/usr/bin/ffmpeg",
        "/usr/local/bin/ffmpeg"
      ];
    }

    foreach (var candidate in candidates)
    {
      try
      {
        using var process = Process.Start(new ProcessStartInfo
        {
          FileName = candidate,
          Arguments = "-version",
          UseShellExecute = false,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          CreateNoWindow = true
        });

        if (process is not null)
        {
          process.WaitForExit(3000);
          if (process.ExitCode == 0)
          {
            _ffmpegPath = candidate;
            _logger.LogInformation("Found FFmpeg at: {Path}", candidate);
            return _ffmpegPath;
          }
        }
      }
      catch
      {
        // Not found at this path, try next.
      }
    }

    _logger.LogWarning("FFmpeg not found in any expected location.");
    return null;
  }

  private string BuildFfmpegArgs(int width, int height, int cameraIndex)
  {
    string inputFormat;
    string device;
    string extraInputArgs = string.Empty;

    if (OperatingSystem.IsWindows())
    {
      var deviceName = GetWindowsWebcamDevice(cameraIndex);
      inputFormat = "dshow";
      device = $"video={deviceName}";
      extraInputArgs = $"-video_size {width}x{height} ";
    }
    else if (OperatingSystem.IsMacOS())
    {
      inputFormat = "avfoundation";
      device = $"{cameraIndex}:none";
      extraInputArgs = $"-video_size {width}x{height} ";
    }
    else
    {
      inputFormat = "v4l2";
      device = $"/dev/video{cameraIndex}";
      extraInputArgs = $"-video_size {width}x{height} ";
    }

    return $"-f {inputFormat} -framerate 10 {extraInputArgs}" +
           $"-i \"{device}\" " +
           $"-f mjpeg -q:v 5 -r 5 pipe:1";
  }

  private string GetWindowsWebcamDevice(int cameraIndex)
  {
    var ffmpegPath = FindFfmpeg();
    if (ffmpegPath is null)
    {
      return "Integrated Camera";
    }

    try
    {
      var devices = GetWindowsVideoDevices(ffmpegPath);

      if (cameraIndex < devices.Count)
      {
        _logger.LogInformation("Found Windows webcam device: {DeviceName}", devices[cameraIndex]);
        return devices[cameraIndex];
      }

      if (devices.Count > 0)
      {
        _logger.LogWarning("Camera index {Index} out of range. Using first device: {DeviceName}",
          cameraIndex, devices[0]);
        return devices[0];
      }
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to enumerate Windows webcam devices.");
    }

    return "Integrated Camera";
  }

  private List<string> GetWindowsVideoDevices(string ffmpegPath)
  {
    using var process = new Process();
    process.StartInfo = new ProcessStartInfo
    {
      FileName = ffmpegPath,
      Arguments = "-list_devices true -f dshow -i dummy",
      UseShellExecute = false,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true
    };

    process.Start();
    var stderr = process.StandardError.ReadToEnd();
    process.WaitForExit(5000);

    var videoDevices = new List<string>();
    foreach (var line in stderr.Split('\n'))
    {
      if (line.Contains("(video)"))
      {
        var start = line.IndexOf('"');
        var end = line.IndexOf('"', start + 1);
        if (start >= 0 && end > start)
        {
          videoDevices.Add(line.Substring(start + 1, end - start - 1));
        }
      }
    }

    return videoDevices;
  }

  private async Task<WebcamInfoDto[]> EnumerateWindowsCameras(string ffmpegPath)
  {
    var devices = await Task.Run(() => GetWindowsVideoDevices(ffmpegPath));
    if (devices.Count == 0)
    {
      return [];
    }

    return devices.Select((name, index) => new WebcamInfoDto(index, name)).ToArray();
  }

  private async Task<WebcamInfoDto[]> EnumerateMacCameras(string ffmpegPath)
  {
    using var process = new Process();
    process.StartInfo = new ProcessStartInfo
    {
      FileName = ffmpegPath,
      Arguments = "-f avfoundation -list_devices true -i \"\"",
      UseShellExecute = false,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true
    };

    process.Start();
    var stderr = await process.StandardError.ReadToEndAsync();
    process.WaitForExit(5000);

    var cameras = new List<WebcamInfoDto>();
    var inVideoSection = false;

    foreach (var line in stderr.Split('\n'))
    {
      if (line.Contains("AVFoundation video devices:"))
      {
        inVideoSection = true;
        continue;
      }

      if (line.Contains("AVFoundation audio devices:"))
      {
        break;
      }

      if (inVideoSection && line.Contains(']'))
      {
        // Lines look like: [AVFoundation ...] [0] FaceTime HD Camera
        var bracketStart = line.LastIndexOf('[');
        var bracketEnd = line.LastIndexOf(']');
        if (bracketStart >= 0 && bracketEnd > bracketStart)
        {
          var indexStr = line.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
          if (int.TryParse(indexStr, out var index))
          {
            var name = line[(bracketEnd + 1)..].Trim();
            if (!string.IsNullOrEmpty(name))
            {
              cameras.Add(new WebcamInfoDto(index, name));
            }
          }
        }
      }
    }

    return cameras.ToArray();
  }

  private static WebcamInfoDto[] EnumerateLinuxCameras()
  {
    var cameras = new List<WebcamInfoDto>();

    for (var i = 0; i < 10; i++)
    {
      var devicePath = $"/dev/video{i}";
      if (File.Exists(devicePath))
      {
        cameras.Add(new WebcamInfoDto(i, $"Camera {i} ({devicePath})"));
      }
    }

    return cameras.ToArray();
  }

  private static async IAsyncEnumerable<byte[]> ParseMjpegStream(
    Stream stream,
    [EnumeratorCancellation] CancellationToken ct)
  {
    var frame = new MemoryStream(capacity: 65536);
    var readBuf = new byte[8192];
    var inFrame = false;
    byte prev = 0;

    while (!ct.IsCancellationRequested)
    {
      int n;
      try
      {
        n = await stream.ReadAsync(readBuf, ct);
      }
      catch (OperationCanceledException)
      {
        yield break;
      }

      if (n == 0)
      {
        yield break;
      }

      for (var i = 0; i < n; i++)
      {
        var cur = readBuf[i];

        if (!inFrame)
        {
          if (prev == 0xFF && cur == 0xD8)
          {
            frame.SetLength(0);
            frame.WriteByte(0xFF);
            frame.WriteByte(0xD8);
            inFrame = true;
          }
        }
        else
        {
          frame.WriteByte(cur);

          if (prev == 0xFF && cur == 0xD9)
          {
            yield return frame.ToArray();
            frame.SetLength(0);
            inFrame = false;
          }
        }

        prev = cur;
      }
    }
  }
}
