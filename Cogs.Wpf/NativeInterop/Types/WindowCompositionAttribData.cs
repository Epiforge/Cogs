using System;
using System.Runtime.InteropServices;

namespace Cogs.Wpf.NativeInterop.Types
{
    [StructLayout(LayoutKind.Sequential)]
    struct WindowCompositionAttribData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }
}
