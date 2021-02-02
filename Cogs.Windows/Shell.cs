using System;
using System.Reflection;

namespace Cogs.Windows
{
    /// <summary>
    /// Wraps methods of the WScript.Shell COM object
    /// </summary>
    public static class Shell
    {
        /// <summary>
        /// Creates a Windows short-cut
        /// </summary>
        /// <param name="shortcutPath">The path at which to create the short-cut</param>
        /// <param name="targetPath">The path the short-cut should target</param>
        /// <param name="iconLocation">The path to the file containing the short-cut icon</param>
        /// <param name="iconIndex">The index of the icon</param>
        /// <param name="startIn">The working directory in which to launch the target of the short-cut</param>
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
