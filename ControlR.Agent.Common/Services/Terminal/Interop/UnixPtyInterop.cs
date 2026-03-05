using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ControlR.Agent.Common.Services.Terminal.Interop;

[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
internal static class UnixPtyInterop
{
  // forkpty is in libutil on Linux, libc on macOS
  [DllImport("libutil", EntryPoint = "forkpty", SetLastError = true)]
  private static extern int ForkPtyLinux(
    out int master,
    IntPtr name,
    IntPtr termp,
    ref WinSize winp);

  [DllImport("libc", EntryPoint = "forkpty", SetLastError = true)]
  private static extern int ForkPtyMac(
    out int master,
    IntPtr name,
    IntPtr termp,
    ref WinSize winp);

  [DllImport("libc", EntryPoint = "read", SetLastError = true)]
  internal static extern int Read(int fd, byte[] buf, int count);

  [DllImport("libc", EntryPoint = "write", SetLastError = true)]
  internal static extern int Write(int fd, byte[] buf, int count);

  [DllImport("libc", EntryPoint = "close", SetLastError = true)]
  internal static extern int Close(int fd);

  [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
  internal static extern int Ioctl(int fd, ulong request, ref WinSize ws);

  [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
  internal static extern int Kill(int pid, int sig);

  [DllImport("libc", EntryPoint = "waitpid", SetLastError = true)]
  internal static extern int WaitPid(int pid, out int status, int options);

  [DllImport("libc", EntryPoint = "execvp", SetLastError = true)]
  internal static extern int Execvp(
    [MarshalAs(UnmanagedType.LPStr)] string file,
    [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string?[] argv);

  // TIOCSWINSZ values differ by platform
  internal static ulong TIOCSWINSZ => RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
    ? 0x80087467UL  // macOS
    : 0x5414UL;     // Linux

  internal const int SIGTERM = 15;
  internal const int SIGKILL = 9;
  internal const int WNOHANG = 1;

  internal static int ForkPty(out int master, ref WinSize winp)
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
      return ForkPtyMac(out master, IntPtr.Zero, IntPtr.Zero, ref winp);
    }

    return ForkPtyLinux(out master, IntPtr.Zero, IntPtr.Zero, ref winp);
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct WinSize
  {
    public ushort ws_row;
    public ushort ws_col;
    public ushort ws_xpixel;
    public ushort ws_ypixel;

    public WinSize(int rows, int cols)
    {
      ws_row = (ushort)rows;
      ws_col = (ushort)cols;
      ws_xpixel = 0;
      ws_ypixel = 0;
    }
  }
}
