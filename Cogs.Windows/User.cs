namespace Cogs.Windows;

/// <summary>
/// Provides properties concerning the user
/// </summary>
public static class User
{
    /// <summary>
    /// Gets the amount of time the user has been idle
    /// </summary>
    public static TimeSpan IdleTime
    {
        get
        {
            var lastInput = new LastInputInfo();
            lastInput.Size = Marshal.SizeOf(lastInput);
            lastInput.Time = 0;
            if (NativeMethods.GetLastInputInfo(ref lastInput))
            {
                var systemTicks = NativeMethods.GetTickCount();
                return TimeSpan.FromMilliseconds(systemTicks < lastInput.Time ? systemTicks + (uint.MaxValue - lastInput.Time) : systemTicks - lastInput.Time);
            }
            return TimeSpan.Zero;
        }
    }
}
