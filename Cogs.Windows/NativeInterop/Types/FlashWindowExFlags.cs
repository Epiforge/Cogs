namespace Cogs.Windows.NativeInterop.Types;

enum FlashWindowExFlags : uint
{
    /// <summary>
    /// Flash both the window caption and taskbar button (FLASHW_CAPTION | FLASHW_TRAY flags)
    /// </summary>
    FLASHW_ALL = 0x00000003,
    /// <summary>
    /// Flash the window caption
    /// </summary>
    FLASHW_CAPTION = 0x00000001,
    /// <summary>
    /// Stop flashing
    /// </summary>
    FLASHW_STOP = 0,
    /// <summary>
    /// Flash continuously, until the FLASHW_STOP flag is set
    /// </summary>
    FLASHW_TIMER = 0x00000004,
    /// <summary>
    /// Flash continuously until the window comes to the foreground
    /// </summary>
    FLASHW_TIMERNOFG = 0x0000000C,
    /// <summary>
    /// Flash the taskbar button
    /// </summary>
    FLASHW_TRAY = 0x00000002
}
