using Cogs.Windows.NativeInterop.Types;
using System;
using System.Runtime.InteropServices;

namespace Cogs.Windows.NativeInterop
{
    static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AttachConsole([MarshalAs(UnmanagedType.U4)] int dwProcessId);

        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ProcessBasicInformation processInformation, int processInformationLength, out int returnLength);
    }
}
