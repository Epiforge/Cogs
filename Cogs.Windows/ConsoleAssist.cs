using Cogs.Windows.NativeInterop;
using System.Diagnostics;

namespace Cogs.Windows
{
    public static class ConsoleAssist
    {
        public static bool AttachTo(Process process) => Methods.AttachConsole(process.Id);

        public static bool AttachToParentProcess() => Methods.AttachConsole(-1);
    }
}
