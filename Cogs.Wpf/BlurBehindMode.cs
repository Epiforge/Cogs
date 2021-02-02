namespace Cogs.Wpf
{
    /// <summary>
    /// Modes for blurring behind a window
    /// </summary>
    public enum BlurBehindMode
    {
        /// <summary>
        /// Do not blur behind the window
        /// </summary>
        Off,

        /// <summary>
        /// Blur behind the window
        /// </summary>
        On,

        /// <summary>
        /// Blur behind the window when it is active
        /// </summary>
        OnActivated,

        /// <summary>
        /// Blur behind the window when it is not active
        /// </summary>
        OnDeactivated
    }
}
