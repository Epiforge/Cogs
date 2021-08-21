namespace Cogs.Windows;

/// <summary>
/// Represents the current Windows theme
/// </summary>
public sealed class Theme : SyncDisposable
{
    /// <summary>
    /// Instantiates a new instance of the <see cref="Theme"/> class
    /// </summary>
    public Theme()
    {
        synchronizationContext = SynchronizationContext.Current;

        colorKey = colorHive.OpenSubKey(colorKeyName) ?? throw new PlatformNotSupportedException($"The DWM key (\"{colorKeyName}\") could not be found");
        color = FetchColor();
        try
        {
            colorKeyWatcher = new ManagementEventWatcher(new WqlEventQuery("RegistryValueChangeEvent") { Condition = $"Hive = '{Sanitize(colorHive.Name)}' AND KeyPath = '{Sanitize(colorKeyName)}' AND ValueName = '{Sanitize(colorValueName)}'" });
            colorKeyWatcher.EventArrived += ColorKeyWatcherEventArrived;
            colorKeyWatcher.Start();
        }
        catch (ManagementException)
        {
            // Oh, I see how it is
        }

        isDarkKey = isDarkHive.OpenSubKey(isDarkKeyName) ?? throw new PlatformNotSupportedException($"The Personalize key (\"{isDarkKeyName}\") could not be found");
        isDark = FetchIsDark();
        try
        {
            isDarkKeyWatcher = new ManagementEventWatcher(new WqlEventQuery("RegistryValueChangeEvent") { Condition = $"Hive = '{Sanitize(isDarkHive.Name)}' AND KeyPath = '{Sanitize(isDarkKeyName)}' AND ValueName = '{Sanitize(isDarkValueName)}'" });
            isDarkKeyWatcher.EventArrived += IsDarkKeyWatcherEventArrived;
            isDarkKeyWatcher.Start();
        }
        catch (ManagementException)
        {
            // Fine then
        }
    }

    readonly RegistryKey colorHive = Registry.Users;
    private Color color;
    readonly RegistryKey colorKey;
    readonly string colorKeyName = $@"{WindowsIdentity.GetCurrent().User}\Software\Microsoft\Windows\DWM";
    readonly ManagementEventWatcher? colorKeyWatcher;
    readonly string colorValueName = "ColorizationColor";
    readonly int defaultColorValue = unchecked((int)0xc42947cc);
    readonly int defaultIsDarkValue = 1;
    readonly RegistryKey isDarkHive = Registry.Users;
    bool isDark;
    readonly RegistryKey isDarkKey;
    readonly ManagementEventWatcher? isDarkKeyWatcher;
    readonly string isDarkKeyName = $@"{WindowsIdentity.GetCurrent().User}\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    readonly string isDarkValueName = "AppsUseLightTheme";
    readonly SynchronizationContext synchronizationContext;

    /// <summary>
    /// Gets the accent color of the current Windows theme
    /// </summary>
    public Color Color
    {
        get => color;
        set => SetBackedProperty(ref color, in value);
    }

    /// <summary>
    /// Gets whether the current Windows theme is dark
    /// </summary>
    public bool IsDark
    {
        get => isDark;
        set => SetBackedProperty(ref isDark, in value);
    }

    void ColorKeyWatcherEventArrived(object sender, EventArrivedEventArgs e) => UsingContext(() => Color = FetchColor());

    /// <summary>
    /// Frees, releases, or resets unmanaged resources
    /// </summary>
    /// <param name="disposing"><c>false</c> if invoked by the finalizer because the object is being garbage collected; otherwise, <c>true</c></param>
    /// <returns><c>true</c> if disposal completed; otherwise, <c>false</c></returns>
    protected override bool Dispose(bool disposing)
    {
        if (disposing)
        {
            colorKey?.Dispose();
            isDarkKey?.Dispose();
        }
        return true;
    }

    Color FetchColor() => Color.FromArgb((int)(colorKey?.GetValue(colorValueName) ?? defaultColorValue));

    bool FetchIsDark() => (int)(isDarkKey?.GetValue(isDarkValueName) ?? defaultIsDarkValue) == 0;

    void IsDarkKeyWatcherEventArrived(object sender, EventArrivedEventArgs e) => UsingContext(() => IsDark = FetchIsDark());

    void UsingContext(Action action)
    {
        if (synchronizationContext != null)
            synchronizationContext.Post(state => action(), null);
        else
            action();
    }

    static string Sanitize(string value) => value.Replace(@"\", @"\\", StringComparison.OrdinalIgnoreCase).Replace("'", @"\'", StringComparison.OrdinalIgnoreCase);
}
