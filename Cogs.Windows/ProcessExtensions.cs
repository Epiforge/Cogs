using Cogs.Windows.NativeInterop;
using Cogs.Windows.NativeInterop.Types;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Cogs.Windows
{
    /// <summary>
    /// Provides extension methods for processes
    /// </summary>
    public static class ProcessExtensions
    {
        /// <summary>
        /// Close the main window of the specified process
        /// </summary>
        /// <param name="process">The process of which to close the main window</param>
        /// <returns></returns>
        public static async Task CloseMainWindowAsync(this Process process)
        {
            if (process is null)
                throw new ArgumentNullException(nameof(process));
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

        /// <summary>
        /// Gets the parent process of the specified process
        /// </summary>
        /// <param name="process">The process of which to get the parent process</param>
        public static Process? GetParentProcess(this Process process)
        {
            if (process is null)
                throw new ArgumentNullException(nameof(process));
            var processBasicInformation = new ProcessBasicInformation();
            var status = NativeMethods.NtQueryInformationProcess(process.Handle, 0, ref processBasicInformation, Marshal.SizeOf(processBasicInformation), out _);
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
