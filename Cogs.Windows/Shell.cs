using System;
using System.Reflection;

namespace Cogs.Windows
{
    public static class Shell
    {
        public static void CreateShortcut(string shortcutPath, string targetPath, string? iconLocation = null, int iconIndex = 0, string? startIn = null)
        {
            if (Type.GetTypeFromProgID("WScript.Shell") is Type shellType && Activator.CreateInstance(shellType) is { } shell)
            {
                if (shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath }) is { } shortcut)
                {
                    var shortcutType = shortcut.GetType();
                    shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
                    if (startIn is { })
                        shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { startIn });
                    if (iconLocation is { })
                        shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { $"{iconLocation},{iconIndex}" });
                    shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
                }
            }
        }
    }
}
