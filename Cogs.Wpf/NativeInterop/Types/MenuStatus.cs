namespace Cogs.Wpf.NativeInterop.Types;

[Flags]
enum MenuStatus : uint
{
    BYCOMMAND = 0x0,
    BYPOSITION = 0x400,
    DISABLED = 0x2,
    ENABLED = 0x0,
    GRAYED = 0x1
}
