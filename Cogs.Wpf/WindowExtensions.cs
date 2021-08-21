namespace Cogs.Wpf;

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
            window.SetCurrentValue(FrameworkElement.WidthProperty, closestWorkingArea.Width);
        if (window.Height > closestWorkingArea.Height)
            window.SetCurrentValue(FrameworkElement.HeightProperty, closestWorkingArea.Height);
        if (window.Left < closestWorkingArea.Left)
            window.SetCurrentValue(Window.LeftProperty, closestWorkingArea.Left);
        if (window.Top < closestWorkingArea.Top)
            window.SetCurrentValue(Window.TopProperty, closestWorkingArea.Top);
        if (window.Left + window.Width > closestWorkingArea.Right)
            window.SetCurrentValue(Window.LeftProperty, closestWorkingArea.Right - window.Width);
        if (window.Top + window.Height > closestWorkingArea.Bottom)
            window.SetCurrentValue(Window.TopProperty, closestWorkingArea.Bottom - window.Height);
    }
}
