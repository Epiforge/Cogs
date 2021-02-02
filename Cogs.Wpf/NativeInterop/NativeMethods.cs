using Cogs.Wpf.NativeInterop.Types;
using System;
using System.Runtime.InteropServices;

namespace Cogs.Wpf.NativeInterop
{
    static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool EnableMenuItem(IntPtr hMenu, Types.SystemCommand uIDEnableItem, MenuStatus uEnable);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttribData data);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hwnd, WindowMessage msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int TrackPopupMenuEx(IntPtr hmenu, TrackPopupMenuFlags fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);
    }
}
