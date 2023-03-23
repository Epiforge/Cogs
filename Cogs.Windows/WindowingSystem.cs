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
    /// Flashes the specified window one time
    /// </summary>
    /// <param name="windowHandle">A handle to the window to be flashed</param>
    /// <param name="invert">Whether to provide the visual invert clue to the user</param>
    /// <returns><c>true</c> if the window caption was drawn as active before the call</returns>
    public static bool FlashWindow(IntPtr windowHandle, bool invert) =>
        NativeMethods.FlashWindow(windowHandle, invert);

    /// <summary>
    /// Flashes the specified window continuously
    /// </summary>
    /// <param name="windowHandle">A handle to the window to be flashed</param>
    /// <returns><c>true</c> if the window caption was drawn as active before the call</returns>
    public static bool FlashWindowCaptionAndTaskbarIcon(IntPtr windowHandle)
    {
        var info = new FlashWInfo
        {
            WindowHandle = windowHandle,
            Flags = FlashWindowExFlags.FLASHW_ALL | FlashWindowExFlags.FLASHW_TIMER
        };
        return NativeMethods.FlashWindowEx(ref info);
    }

    /// <summary>
    /// Flashes the specified window continuously a specified number of times
    /// </summary>
    /// <param name="windowHandle">A handle to the window to be flashed</param>
    /// <param name="times">The number of times to flash the window</param>
    /// <returns><c>true</c> if the window caption was drawn as active before the call</returns>
    public static bool FlashWindowCaptionAndTaskbarIcon(IntPtr windowHandle, int times)
    {
        var info = new FlashWInfo
        {
            WindowHandle = windowHandle,
            Flags = FlashWindowExFlags.FLASHW_ALL,
            Count = (uint)times
        };
        return NativeMethods.FlashWindowEx(ref info);
    }

    /// <summary>
    /// Flashes the specified window continuously until the window comes to the foreground
    /// </summary>
    /// <param name="windowHandle">A handle to the window to be flashed</param>
    /// <returns><c>true</c> if the window caption was drawn as active before the call</returns>
    public static bool FlashWindowCaptionAndTaskbarIconUntilActivated(IntPtr windowHandle)
    {
        var info = new FlashWInfo
        {
            WindowHandle = windowHandle,
            Flags = FlashWindowExFlags.FLASHW_ALL | FlashWindowExFlags.FLASHW_TIMERNOFG
        };
        return NativeMethods.FlashWindowEx(ref info);
    }

    /// <summary>
    /// Flashes the specified window's caption continuously
    /// </summary>
    /// <param name="windowHandle">A handle to the window to be flashed</param>
    /// <returns><c>true</c> if the window caption was drawn as active before the call</returns>
    public static bool FlashWindowCaption(IntPtr windowHandle)
    {
        var info = new FlashWInfo
        {
            WindowHandle = windowHandle,
            Flags = FlashWindowExFlags.FLASHW_CAPTION | FlashWindowExFlags.FLASHW_TIMER
        };
        return NativeMethods.FlashWindowEx(ref info);
    }

    /// <summary>
    /// Flashes the specified window's caption continuously until the window comes to the foreground
    /// </summary>
    /// <param name="windowHandle">A handle to the window to be flashed</param>
    /// <param name="times">The number of times to flash the window</param>
    /// <returns><c>true</c> if the window caption was drawn as active before the call</returns>
    public static bool FlashWindowCaption(IntPtr windowHandle, int times)
    {
        var info = new FlashWInfo
        {
            WindowHandle = windowHandle,
            Flags = FlashWindowExFlags.FLASHW_CAPTION,
            Count = (uint)times
        };
        return NativeMethods.FlashWindowEx(ref info);
    }

    /// <summary>
    /// Flashes the specified window's caption continuously until the window comes to the foreground
    /// </summary>
    /// <param name="windowHandle">A handle to the window to be flashed</param>
    /// <returns><c>true</c> if the window caption was drawn as active before the call</returns>
    public static bool FlashWindowCaptionUntilActivated(IntPtr windowHandle)
    {
        var info = new FlashWInfo
        {
            WindowHandle = windowHandle,
            Flags = FlashWindowExFlags.FLASHW_CAPTION | FlashWindowExFlags.FLASHW_TIMERNOFG
        };
        return NativeMethods.FlashWindowEx(ref info);
    }

    /// <summary>
    /// Flashes the specified window's task bar icon continuously
    /// </summary>
    /// <param name="windowHandle">A handle to the window to be flashed</param>
    /// <returns><c>true</c> if the window caption was drawn as active before the call</returns>
    public static bool FlashWindowTaskbarIcon(IntPtr windowHandle)
    {
        var info = new FlashWInfo
        {
            WindowHandle = windowHandle,
            Flags = FlashWindowExFlags.FLASHW_TRAY | FlashWindowExFlags.FLASHW_TIMER
        };
        return NativeMethods.FlashWindowEx(ref info);
    }

    /// <summary>
    /// Flashes the specified window's task bar icon continuously until the window comes to the foreground
    /// </summary>
    /// <param name="windowHandle">A handle to the window to be flashed</param>
    /// <param name="times">The number of times to flash the window</param>
    /// <returns><c>true</c> if the window caption was drawn as active before the call</returns>
    public static bool FlashWindowTaskbarIcon(IntPtr windowHandle, int times)
    {
        var info = new FlashWInfo
        {
            WindowHandle = windowHandle,
            Flags = FlashWindowExFlags.FLASHW_TRAY,
            Count = (uint)times
        };
        return NativeMethods.FlashWindowEx(ref info);
    }

    /// <summary>
    /// Flashes the specified window's task bar icon continuously until the window comes to the foreground
    /// </summary>
    /// <param name="windowHandle">A handle to the window to be flashed</param>
    /// <returns><c>true</c> if the window caption was drawn as active before the call</returns>
    public static bool FlashWindowTaskbarIconUntilActivated(IntPtr windowHandle)
    {
        var info = new FlashWInfo
        {
            WindowHandle = windowHandle,
            Flags = FlashWindowExFlags.FLASHW_TRAY | FlashWindowExFlags.FLASHW_TIMERNOFG
        };
        return NativeMethods.FlashWindowEx(ref info);
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

    /// <summary>
    /// Stops the specified window from flashing continuously
    /// </summary>
    /// <param name="windowHandle">A handle to the window that is flashing</param>
    /// <returns><c>true</c> if the window caption was drawn as active before the call</returns>
    public static bool StopFlashingWindow(IntPtr windowHandle)
    {
        var info = new FlashWInfo
        {
            WindowHandle = windowHandle,
            Flags = FlashWindowExFlags.FLASHW_STOP
        };
        return NativeMethods.FlashWindowEx(ref info);
    }
}
