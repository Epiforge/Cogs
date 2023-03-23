namespace Cogs.Windows.NativeInterop.Types;

[StructLayout(LayoutKind.Sequential)]
struct FlashWInfo
{
    public uint Size;
    public IntPtr WindowHandle;
    public FlashWindowExFlags Flags;
    public uint Count;
    public uint Timeout;
}
