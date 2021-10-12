namespace Cogs.Windows;

/// <summary>
/// Wraps Win32 API methods dealing with the cursor
/// </summary>
public sealed class Cursor
{
    /// <summary>
    /// Gets the current position of the cursor
    /// </summary>
    public static (int x, int y) GetPosition()
    {
        NativeMethods.GetCursorPos(out var point);
        return (point.X, point.Y);
    }

    /// <summary>
    /// Sets the current position of the cursor
    /// </summary>
    /// <param name="x">The x coordinate of the position to which to set the cursor</param>
    /// <param name="y">The y coordinate of the position to which to set the cursor</param>
    public static void SetPosition(int x, int y) =>
        NativeMethods.SetCursorPos(x, y);
}
