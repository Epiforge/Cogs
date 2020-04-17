using Cogs.Wpf.NativeInterop.Types;
using System;
using System.Runtime.InteropServices;

namespace Cogs.Wpf.NativeInterop
{
    static class Methods
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttribData data);
    }
}
