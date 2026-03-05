using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace ControlR.Agent.Common.Services.Terminal.Interop;

[SupportedOSPlatform("windows")]
internal static class ConPtyInterop
{
  internal const uint PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;
  internal const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
  internal const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;

  [DllImport("kernel32.dll", SetLastError = true)]
  internal static extern int CreatePseudoConsole(
    Coord size,
    SafeFileHandle hInput,
    SafeFileHandle hOutput,
    uint dwFlags,
    out IntPtr phPC);

  [DllImport("kernel32.dll", SetLastError = true)]
  internal static extern int ResizePseudoConsole(IntPtr hPC, Coord size);

  [DllImport("kernel32.dll", SetLastError = true)]
  internal static extern void ClosePseudoConsole(IntPtr hPC);

  [DllImport("kernel32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  internal static extern bool InitializeProcThreadAttributeList(
    IntPtr lpAttributeList,
    int dwAttributeCount,
    int dwFlags,
    ref IntPtr lpSize);

  [DllImport("kernel32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  internal static extern bool UpdateProcThreadAttribute(
    IntPtr lpAttributeList,
    uint dwFlags,
    IntPtr attribute,
    IntPtr lpValue,
    IntPtr cbSize,
    IntPtr lpPreviousValue,
    IntPtr lpReturnSize);

  [DllImport("kernel32.dll", SetLastError = true)]
  internal static extern void DeleteProcThreadAttributeList(IntPtr lpAttributeList);

  [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
  [return: MarshalAs(UnmanagedType.Bool)]
  internal static extern bool CreateProcessW(
    string? lpApplicationName,
    string lpCommandLine,
    IntPtr lpProcessAttributes,
    IntPtr lpThreadAttributes,
    [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
    uint dwCreationFlags,
    IntPtr lpEnvironment,
    string? lpCurrentDirectory,
    ref StartupInfoEx lpStartupInfo,
    out ProcessInfo lpProcessInformation);

  [DllImport("kernel32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  internal static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

  [DllImport("kernel32.dll", SetLastError = true)]
  internal static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

  [DllImport("kernel32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  internal static extern bool CloseHandle(IntPtr hObject);

  [DllImport("kernel32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  internal static extern bool CreatePipe(
    out SafeFileHandle hReadPipe,
    out SafeFileHandle hWritePipe,
    IntPtr lpPipeAttributes,
    uint nSize);

  [StructLayout(LayoutKind.Sequential)]
  internal struct Coord
  {
    public short X;
    public short Y;

    public Coord(short x, short y)
    {
      X = x;
      Y = y;
    }
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  internal struct StartupInfoEx
  {
    public StartupInfo StartupInfo;
    public IntPtr lpAttributeList;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  internal struct StartupInfo
  {
    public int cb;
    public string lpReserved;
    public string lpDesktop;
    public string lpTitle;
    public uint dwX;
    public uint dwY;
    public uint dwXSize;
    public uint dwYSize;
    public uint dwXCountChars;
    public uint dwYCountChars;
    public uint dwFillAttribute;
    public uint dwFlags;
    public ushort wShowWindow;
    public ushort cbReserved2;
    public IntPtr lpReserved2;
    public IntPtr hStdInput;
    public IntPtr hStdOutput;
    public IntPtr hStdError;
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct ProcessInfo
  {
    public IntPtr hProcess;
    public IntPtr hThread;
    public int dwProcessId;
    public int dwThreadId;
  }
}
