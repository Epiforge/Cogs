namespace Cogs.Windows;

/// <summary>
/// Provides methods for dealing with the windowing system
/// </summary>
public static class WindowingSystem
{
    /// <summary>
    /// Gets a handle (or pointer) to the foreground window
    /// </summary>
    public static IntPtr GetForegroundWindow() =>
        NativeMethods.GetForegroundWindow();

    /// <summary>
    /// Brings the thread that created the specified window into the foreground and activates the window if the caller has permission to do so; otherwise, causes the specified window to flash in the Taskbar
    /// </summary>
    /// <param name="windowHandle">A handle (or pointer) to the window to be foregrounded</param>
    /// <returns><c>true</c> if setting the foreground window succeeded; otherwise, <c>false</c></returns>
    public static bool SetForegroundWindow(IntPtr windowHandle) =>
        NativeMethods.SetForegroundWindow(windowHandle);
}
