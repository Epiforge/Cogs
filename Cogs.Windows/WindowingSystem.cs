namespace Cogs.Windows;

/// <summary>
/// Provides methods for dealing with the windowing system
/// </summary>
public static class WindowingSystem
{
    /// <summary>
    /// Gets/sets the handle of the foreground window
    /// </summary>
    /// <exception cref="Win32Exception">The specified window could not be brought to the foreground (see: https://docs.microsoft.com/windows/win32/api/winuser/nf-winuser-setforegroundwindow)</exception>
    public static IntPtr ForegroundWindow
    {
        get => NativeMethods.GetForegroundWindow();
        set
        {
            if (!NativeMethods.SetForegroundWindow(value))
                throw new Win32Exception("Invoking SetForegroundWindow did not succeed; the return value was zero (see: https://docs.microsoft.com/windows/win32/api/winuser/nf-winuser-setforegroundwindow)");
        }
    }

    /// <summary>
    /// Gets the position of the foreground window
    /// </summary>
    /// <exception cref="Exception">There is no foreground window</exception>
    /// <exception cref="Win32Exception">The position of the foreground window could not be retrieved (see: https://docs.microsoft.com/windows/win32/api/winuser/nf-winuser-getwindowrect)</exception>
    public static (int left, int top, int right, int bottom) ForegroundWindowPosition
    {
        get
        {
            var hWnd = ForegroundWindow;
            if (hWnd == IntPtr.Zero || hWnd == NativeMethods.GetDesktopWindow() || hWnd == NativeMethods.GetShellWindow())
                throw new Exception("There is no foreground window");
            if (!NativeMethods.GetWindowRect(hWnd, out var rect))
                throw new Win32Exception("Invoking GetWindowRect did not succeed; the return value was zero (see: https://docs.microsoft.com/windows/win32/api/winuser/nf-winuser-getwindowrect)");
            return (rect.Left, rect.Top, rect.Right, rect.Bottom);
        }
    }

    /// <summary>
    /// Gets a handle (or pointer) to the foreground window
    /// </summary>
    [Obsolete("Use the ForegroundWindow property")]
    public static IntPtr GetForegroundWindow() =>
        NativeMethods.GetForegroundWindow();

    /// <summary>
    /// Brings the thread that created the specified window into the foreground and activates the window if the caller has permission to do so; otherwise, causes the specified window to flash in the Taskbar
    /// </summary>
    /// <param name="windowHandle">A handle (or pointer) to the window to be foregrounded</param>
    /// <returns><c>true</c> if setting the foreground window succeeded; otherwise, <c>false</c></returns>
    [Obsolete("Use the ForegroundWindow property")]
    public static bool SetForegroundWindow(IntPtr windowHandle) =>
        NativeMethods.SetForegroundWindow(windowHandle);
}
