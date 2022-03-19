namespace Cogs.Wpf;

/// <summary>
/// Supports being loaded and unloaded for data virtualization purposes (should maintain its own thread-safe load/unload count and only load once at a time)
/// </summary>
public interface IDataVirtualizationItem
{
    /// <summary>
    /// A data virtualizer wants this item to be loaded
    /// </summary>
    void Load();

    /// <summary>
    /// A data virtualizer no longer wants this item to be loaded
    /// </summary>
    void Unload();
}
