using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ControlR.Agent.Common.Services.Terminal.Interop;
using Microsoft.Win32.SafeHandles;

namespace ControlR.Agent.Common.Services.Terminal;

public interface IPtyTerminalSession : IDisposable
{
  event EventHandler? ProcessExited;
  bool IsDisposed { get; }
  Guid TerminalId { get; }
  Task<Result> WriteInput(byte[] data, CancellationToken cancellationToken);
  Result Resize(int cols, int rows);
}

internal class PtyTerminalSession : IPtyTerminalSession
{
  private const int ReadBufferSize = 16384;

  private readonly Guid _terminalId;
  private readonly string _viewerConnectionId;
  private readonly IHubConnection<IAgentHub> _hubConnection;
  private readonly ILogger<PtyTerminalSession> _logger;
  private readonly CancellationTokenSource _sessionCts = new();

  // Windows ConPTY
  private IntPtr _pseudoConsoleHandle;
  private SafeFileHandle? _pipeWriteInput;
  private SafeFileHandle? _pipeReadOutput;
  private FileStream? _writeStream;
  private FileStream? _readStream;
  private IntPtr _processHandle;
  private IntPtr _threadHandle;

  // Unix forkpty
  private int _masterFd = -1;
  private int _childPid = -1;

  private Task? _readTask;

  public event EventHandler? ProcessExited;
  public bool IsDisposed { get; private set; }
  public Guid TerminalId => _terminalId;

  public PtyTerminalSession(
    Guid terminalId,
    string viewerConnectionId,
    IHubConnection<IAgentHub> hubConnection,
    ILogger<PtyTerminalSession> logger)
  {
    _terminalId = terminalId;
    _viewerConnectionId = viewerConnectionId;
    _hubConnection = hubConnection;
    _logger = logger;
  }

  public async Task Initialize(int cols, int rows)
  {
    if (OperatingSystem.IsWindows())
    {
      InitializeWindows(cols, rows);
    }
    else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
    {
      InitializeUnix(cols, rows);
    }
    else
    {
      throw new PlatformNotSupportedException("PTY is not supported on this platform.");
    }

    _readTask = Task.Run(() => ReadLoop(_sessionCts.Token));
    _logger.LogInformation("PTY session initialized. ID: {TerminalId}, Cols: {Cols}, Rows: {Rows}",
      _terminalId, cols, rows);
  }

  public Task<Result> WriteInput(byte[] data, CancellationToken cancellationToken)
  {
    try
    {
      if (IsDisposed)
      {
        return Task.FromResult(Result.Fail("PTY session is disposed."));
      }

      if (OperatingSystem.IsWindows())
      {
        _writeStream?.Write(data, 0, data.Length);
        _writeStream?.Flush();
      }
      else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
      {
        WriteInputUnix(data);
      }

      return Task.FromResult(Result.Ok());
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error writing to PTY.");
      return Task.FromResult(Result.Fail("Error writing to PTY."));
    }
  }

  public Result Resize(int cols, int rows)
  {
    try
    {
      if (IsDisposed)
      {
        return Result.Fail("PTY session is disposed.");
      }

      if (OperatingSystem.IsWindows())
      {
        var coord = new ConPtyInterop.Coord((short)cols, (short)rows);
        var hr = ConPtyInterop.ResizePseudoConsole(_pseudoConsoleHandle, coord);
        if (hr != 0)
        {
          return Result.Fail($"ResizePseudoConsole failed with HRESULT: 0x{hr:X8}");
        }
      }
      else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
      {
        var resizeResult = ResizeUnix(cols, rows);
        if (!resizeResult.IsSuccess)
        {
          return resizeResult;
        }
      }

      _logger.LogDebug("PTY resized to {Cols}x{Rows}", cols, rows);
      return Result.Ok();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error resizing PTY.");
      return Result.Fail("Error resizing PTY.");
    }
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (IsDisposed)
    {
      return;
    }

    IsDisposed = true;

    if (disposing)
    {
      _sessionCts.Cancel();
      _sessionCts.Dispose();
    }

    if (OperatingSystem.IsWindows())
    {
      DisposeWindows();
    }
    else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
    {
      DisposeUnix();
    }

    ProcessExited?.Invoke(this, EventArgs.Empty);
  }

  [SupportedOSPlatform("windows")]
  private void InitializeWindows(int cols, int rows)
  {
    // Create pipes for ConPTY I/O
    if (!ConPtyInterop.CreatePipe(out var inputReadSide, out _pipeWriteInput!, IntPtr.Zero, 0))
    {
      throw new InvalidOperationException($"CreatePipe (input) failed: {Marshal.GetLastWin32Error()}");
    }

    if (!ConPtyInterop.CreatePipe(out _pipeReadOutput!, out var outputWriteSide, IntPtr.Zero, 0))
    {
      inputReadSide.Dispose();
      throw new InvalidOperationException($"CreatePipe (output) failed: {Marshal.GetLastWin32Error()}");
    }

    var size = new ConPtyInterop.Coord((short)cols, (short)rows);
    var hr = ConPtyInterop.CreatePseudoConsole(size, inputReadSide, outputWriteSide, 0, out _pseudoConsoleHandle);
    if (hr != 0)
    {
      inputReadSide.Dispose();
      outputWriteSide.Dispose();
      throw new InvalidOperationException($"CreatePseudoConsole failed: 0x{hr:X8}");
    }

    // Close the pipe sides owned by ConPTY
    inputReadSide.Dispose();
    outputWriteSide.Dispose();

    // Create the child process with ConPTY
    StartProcessWithPseudoConsole();

    // Set up streams for I/O
    _writeStream = new FileStream(_pipeWriteInput, FileAccess.Write);
    _readStream = new FileStream(_pipeReadOutput, FileAccess.Read);
  }

  [SupportedOSPlatform("windows")]
  private void StartProcessWithPseudoConsole()
  {
    // Initialize the attribute list
    var lpSize = IntPtr.Zero;
    ConPtyInterop.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref lpSize);
    var attributeList = Marshal.AllocHGlobal(lpSize);

    try
    {
      if (!ConPtyInterop.InitializeProcThreadAttributeList(attributeList, 1, 0, ref lpSize))
      {
        throw new InvalidOperationException($"InitializeProcThreadAttributeList failed: {Marshal.GetLastWin32Error()}");
      }

      if (!ConPtyInterop.UpdateProcThreadAttribute(
            attributeList,
            0,
            (IntPtr)ConPtyInterop.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
            _pseudoConsoleHandle,
            (IntPtr)IntPtr.Size,
            IntPtr.Zero,
            IntPtr.Zero))
      {
        throw new InvalidOperationException($"UpdateProcThreadAttribute failed: {Marshal.GetLastWin32Error()}");
      }

      var si = new ConPtyInterop.StartupInfoEx();
      si.StartupInfo.cb = Marshal.SizeOf<ConPtyInterop.StartupInfoEx>();
      si.lpAttributeList = attributeList;

      var commandLine = "powershell.exe";

      if (!ConPtyInterop.CreateProcessW(
            null,
            commandLine,
            IntPtr.Zero,
            IntPtr.Zero,
            false,
            ConPtyInterop.EXTENDED_STARTUPINFO_PRESENT,
            IntPtr.Zero,
            null,
            ref si,
            out var pi))
      {
        throw new InvalidOperationException($"CreateProcessW failed: {Marshal.GetLastWin32Error()}");
      }

      _processHandle = pi.hProcess;
      _threadHandle = pi.hThread;

      // Wait for process exit in background
      _ = Task.Run(() => MonitorWindowsProcess(pi.hProcess));
    }
    finally
    {
      ConPtyInterop.DeleteProcThreadAttributeList(attributeList);
      Marshal.FreeHGlobal(attributeList);
    }
  }

  [SupportedOSPlatform("windows")]
  private void MonitorWindowsProcess(IntPtr hProcess)
  {
    ConPtyInterop.WaitForSingleObject(hProcess, 0xFFFFFFFF); // INFINITE
    if (!IsDisposed)
    {
      _logger.LogInformation("PTY child process exited. Session: {TerminalId}", _terminalId);
      Dispose();
    }
  }

  [SupportedOSPlatform("linux")]
  [SupportedOSPlatform("macos")]
  private void InitializeUnix(int cols, int rows)
  {
    var ws = new UnixPtyInterop.WinSize(rows, cols);
    var pid = UnixPtyInterop.ForkPty(out _masterFd, ref ws);

    if (pid < 0)
    {
      throw new InvalidOperationException($"forkpty failed with errno: {Marshal.GetLastWin32Error()}");
    }

    if (pid == 0)
    {
      // Child process - exec a shell
      var shell = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/bash";
      UnixPtyInterop.Execvp(shell, [shell, null]);
      // If execvp returns, it failed
      Environment.Exit(1);
    }

    // Parent process
    _childPid = pid;

    // Monitor child process exit
    _ = Task.Run(() => MonitorUnixProcess(pid));
  }

  [SupportedOSPlatform("linux")]
  [SupportedOSPlatform("macos")]
  private void MonitorUnixProcess(int pid)
  {
    while (!_sessionCts.IsCancellationRequested)
    {
      var result = UnixPtyInterop.WaitPid(pid, out _, UnixPtyInterop.WNOHANG);
      if (result == pid)
      {
        // Child has exited
        if (!IsDisposed)
        {
          _logger.LogInformation("PTY child process exited. Session: {TerminalId}", _terminalId);
          Dispose();
        }
        return;
      }

      if (result < 0)
      {
        // Error or no child
        return;
      }

      Thread.Sleep(500);
    }
  }

  private async Task ReadLoop(CancellationToken cancellationToken)
  {
    var buffer = new byte[ReadBufferSize];

    try
    {
      while (!cancellationToken.IsCancellationRequested && !IsDisposed)
      {
        int bytesRead;

        if (OperatingSystem.IsWindows())
        {
          if (_readStream is null)
          {
            break;
          }

          bytesRead = await _readStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
          bytesRead = await ReadUnixAsync(buffer, cancellationToken);
        }
        else
        {
          break;
        }

        if (bytesRead <= 0)
        {
          _logger.LogDebug("PTY read returned 0 bytes, session ending. ID: {TerminalId}", _terminalId);
          break;
        }

        var data = buffer[..bytesRead];
        var outputDto = new PtyOutputDto(_terminalId, data);
        await _hubConnection.Server.SendPtyOutputToViewer(_viewerConnectionId, outputDto);
      }
    }
    catch (OperationCanceledException)
    {
      // Expected on dispose
    }
    catch (IOException) when (IsDisposed)
    {
      // Expected when pipes close during dispose
    }
    catch (Exception ex)
    {
      if (!IsDisposed)
      {
        _logger.LogError(ex, "Error in PTY read loop. Session: {TerminalId}", _terminalId);
      }
    }
    finally
    {
      if (!IsDisposed)
      {
        Dispose();
      }
    }
  }

  [SupportedOSPlatform("linux")]
  [SupportedOSPlatform("macos")]
  private void WriteInputUnix(byte[] data)
  {
    if (_masterFd >= 0)
    {
      UnixPtyInterop.Write(_masterFd, data, data.Length);
    }
  }

  [SupportedOSPlatform("linux")]
  [SupportedOSPlatform("macos")]
  private Result ResizeUnix(int cols, int rows)
  {
    if (_masterFd >= 0)
    {
      var ws = new UnixPtyInterop.WinSize(rows, cols);
      var result = UnixPtyInterop.Ioctl(_masterFd, UnixPtyInterop.TIOCSWINSZ, ref ws);
      if (result < 0)
      {
        return Result.Fail($"ioctl TIOCSWINSZ failed with errno: {Marshal.GetLastWin32Error()}");
      }
    }

    return Result.Ok();
  }

  [SupportedOSPlatform("linux")]
  [SupportedOSPlatform("macos")]
  private Task<int> ReadUnixAsync(byte[] buffer, CancellationToken cancellationToken)
  {
    return Task.Run(() =>
    {
      if (_masterFd < 0)
      {
        return 0;
      }

      return UnixPtyInterop.Read(_masterFd, buffer, buffer.Length);
    }, cancellationToken);
  }

  [SupportedOSPlatform("windows")]
  private void DisposeWindows()
  {
    try
    {
      _writeStream?.Dispose();
      _readStream?.Dispose();
      _pipeWriteInput?.Dispose();
      _pipeReadOutput?.Dispose();

      if (_pseudoConsoleHandle != IntPtr.Zero)
      {
        ConPtyInterop.ClosePseudoConsole(_pseudoConsoleHandle);
        _pseudoConsoleHandle = IntPtr.Zero;
      }

      if (_processHandle != IntPtr.Zero)
      {
        ConPtyInterop.TerminateProcess(_processHandle, 0);
        ConPtyInterop.CloseHandle(_processHandle);
        _processHandle = IntPtr.Zero;
      }

      if (_threadHandle != IntPtr.Zero)
      {
        ConPtyInterop.CloseHandle(_threadHandle);
        _threadHandle = IntPtr.Zero;
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error disposing Windows PTY resources.");
    }
  }

  [SupportedOSPlatform("linux")]
  [SupportedOSPlatform("macos")]
  private void DisposeUnix()
  {
    try
    {
      if (_masterFd >= 0)
      {
        UnixPtyInterop.Close(_masterFd);
        _masterFd = -1;
      }

      if (_childPid > 0)
      {
        UnixPtyInterop.Kill(_childPid, UnixPtyInterop.SIGTERM);

        // Give process time to clean up
        Thread.Sleep(100);

        var result = UnixPtyInterop.WaitPid(_childPid, out _, UnixPtyInterop.WNOHANG);
        if (result == 0)
        {
          // Still running, force kill
          UnixPtyInterop.Kill(_childPid, UnixPtyInterop.SIGKILL);
          UnixPtyInterop.WaitPid(_childPid, out _, 0);
        }

        _childPid = -1;
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error disposing Unix PTY resources.");
    }
  }
}
