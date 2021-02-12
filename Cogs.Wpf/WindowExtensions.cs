using System.Windows;

namespace Cogs.Wpf
{
    /// <summary>
    /// Provides extension methods for windows
    /// </summary>
    public static class WindowExtensions
    {
        /// <summary>
        /// Moves the specified window the minimum amount to be completely contained within the closest working area
        /// </summary>
        /// <param name="window">The window for which to safeguard position</param>
        public static void SafeguardPosition(this Window window)
        {
            var closestWorkingArea = Screen.GetWorkingArea(window);
            if (closestWorkingArea == Rect.Empty)
                return;
            if (window.Width > closestWorkingArea.Width)
                window.Width = closestWorkingArea.Width;
            if (window.Height > closestWorkingArea.Height)
                window.Height = closestWorkingArea.Height;
            if (window.Left < closestWorkingArea.Left)
                window.Left = closestWorkingArea.Left;
            if (window.Top < closestWorkingArea.Top)
                window.Top = closestWorkingArea.Top;
            if (window.Left + window.Width > closestWorkingArea.Right)
                window.Left = closestWorkingArea.Right - window.Width;
            if (window.Top + window.Height > closestWorkingArea.Bottom)
                window.Top = closestWorkingArea.Bottom - window.Height;
        }
    }
}
