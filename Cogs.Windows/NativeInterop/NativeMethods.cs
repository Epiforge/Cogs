namespace Cogs.Windows.NativeInterop;

static class NativeMethods
{
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AttachConsole([MarshalAs(UnmanagedType.U4)] int dwProcessId);

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out Types.Point lpPoint);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool GetLastInputInfo(ref LastInputInfo lastInputInfo);

    [DllImport("user32.dll")]
    public static extern IntPtr GetShellWindow();

    [DllImport("kernel32.dll")]
    public static extern uint GetTickCount();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);

    [DllImport("ntdll.dll")]
    public static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ProcessBasicInformation processInformation, int processInformationLength, out int returnLength);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}
