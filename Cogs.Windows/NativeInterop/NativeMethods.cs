namespace Cogs.Windows.NativeInterop;

static class NativeMethods
{
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AttachConsole([MarshalAs(UnmanagedType.U4)] int dwProcessId);

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out Types.Point lpPoint);

    [DllImport("ntdll.dll")]
    public static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ProcessBasicInformation processInformation, int processInformationLength, out int returnLength);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetCursorPos(int x, int y);
}
