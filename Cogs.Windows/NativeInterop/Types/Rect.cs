namespace Cogs.Windows.NativeInterop.Types;

[StructLayout(LayoutKind.Sequential)]
struct Rect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}
