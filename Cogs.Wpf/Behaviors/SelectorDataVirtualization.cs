namespace Cogs.Wpf.Behaviors;

/// <summary>
/// Sets the items source of a selector to a collection that loads elements as they are needed for display and keeps selected elements loaded
/// </summary>
/// <typeparam name="TControl"></typeparam>
public abstract class SelectorDataVirtualization<TControl> :
    ItemsControlDataVirtualization<TControl>
    where TControl : Selector
{
    void AssociatedObjectSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        foreach (var item in e.RemovedItems)
            if (item is IDataVirtualizationItem typedItem)
                typedItem.Unload();
        foreach (var item in e.AddedItems)
            if (item is IDataVirtualizationItem typedItem)
                typedItem.Load();
    }

    /// <summary>
    /// Gets the items currently selected by the selector
    /// </summary>
    /// <returns>The items currently selected</returns>
    protected abstract IEnumerable<IDataVirtualizationItem> GetSelectedItems();

    /// <summary>
    /// Called after the behavior is attached to an associated object
    /// </summary>
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.SelectionChanged += AssociatedObjectSelectionChanged;
        foreach (var item in GetSelectedItems())
            item.Load();
    }

    /// <summary>
    /// Called when the behavior is being detached from its associated object, but before it has actually occurred
    /// </summary>
    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.SelectionChanged -= AssociatedObjectSelectionChanged;
        foreach (var item in GetSelectedItems())
            item.Unload();
    }
}
