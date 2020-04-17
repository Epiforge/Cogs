using Cogs.Windows.NativeInterop;
using Cogs.Windows.NativeInterop.Types;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Cogs.Windows
{
    public static class ProcessExtensions
    {
        public static async Task CloseMainWindowAsync(this Process process)
        {
            var taskCompletionSource = new TaskCompletionSource<object?>();
            void exited(object sender, EventArgs e) => taskCompletionSource.SetResult(null);
            process.Exited += exited;
            process.EnableRaisingEvents = true;
            try
            {
                process.CloseMainWindow();
            }
            catch (InvalidOperationException)
            {
                taskCompletionSource.SetResult(null);
            }
            await taskCompletionSource.Task.ConfigureAwait(false);
        }

        public static Process? GetParentProcess(this Process process)
        {
            var processBasicInformation = new ProcessBasicInformation();
            var status = Methods.NtQueryInformationProcess(process.Handle, 0, ref processBasicInformation, Marshal.SizeOf(processBasicInformation), out _);
            if (status != 0)
                throw new Win32Exception(status);
            try
            {
                return Process.GetProcessById(processBasicInformation.InheritedFromUniqueProcessId.ToInt32());
            }
            catch
            {
                return null;
            }
        }
    }
}
