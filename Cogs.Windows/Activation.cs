namespace Cogs.Windows;

/// <summary>
/// Provides information relating to Windows Activation
/// </summary>
public static class Activation
{
    static readonly Lazy<string> lazyProductKey = new Lazy<string>(ProductKeyDecoder.GetWindowsProductKeyFromRegistry);

    /// <summary>
    /// Gets the Windows product key
    /// </summary>
    public static string ProductKey => lazyProductKey.Value;
}
