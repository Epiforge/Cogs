namespace Cogs.Wpf.Behaviors;

/// <summary>
/// Sets the items source of a list box to a collection that loads elements as they are needed for display and keeps selected elements loaded
/// </summary>
public class ListBoxDataVirtualization :
    SelectorDataVirtualization<ListBox>
{
    ScrollViewer? scrollViewer;

    /// <summary>
    /// Gets the items currently selected by the list box
    /// </summary>
    /// <returns>The items currently selected</returns>
    protected override IEnumerable<IDataVirtualizationItem> GetSelectedItems() =>
        AssociatedObject is { } listBox ? listBox.SelectedItems.OfType<IDataVirtualizationItem>() : Enumerable.Empty<IDataVirtualizationItem>();

    /// <summary>
    /// Gets the scroll viewer control the viewport size of which will be used to manage the data virtualization list's load capacity
    /// </summary>
    /// <returns>The scroll viewer control or <c>null</c></returns>
    protected override ScrollViewer? GetScrollViewer()
    {
        if (this.scrollViewer is null &&
            AssociatedObject is { } listBox &&
            listBox.GetVisualDescendent<ScrollViewer>() is { } scrollViewer)
            this.scrollViewer = scrollViewer;
        return this.scrollViewer;
    }

    /// <summary>
    /// Called after the behavior is attached to an associated object
    /// </summary>
    protected override void OnAttached()
    {
        GetScrollViewer();
        base.OnAttached();
    }

    /// <summary>
    /// Called when the behavior is being detached from its associated object, but before it has actually occurred
    /// </summary>
    protected override void OnDetaching()
    {
        base.OnDetaching();
        scrollViewer = null;
    }
}