using System;

namespace Cogs.Wpf.NativeInterop.Types
{
    [Flags]
    enum TrackPopupMenuFlags : uint
    {
        LEFTALIGN = 0x0000,
        RETURNCMD = 0x0100
    }
}
