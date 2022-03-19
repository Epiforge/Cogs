namespace Cogs.Wpf.Behaviors;

/// <summary>
/// Sets the items source of a combo box to a collection that loads elements as they are needed for display and keeps selected element loaded
/// </summary>
public class ComboBoxDataVirtualization :
    SelectorDataVirtualization<ComboBox>
{
    ScrollViewer? scrollViewer;

    async void AssociatedObjectDropDownOpened(object? sender, EventArgs e)
    {
        if (scrollViewer is null)
        {
            scrollViewer = await AssociatedObject.Dispatcher.InvokeAsync(() => GetScrollViewer(), DispatcherPriority.ContextIdle);
            LinkScrollViewer();
        }
    }

    /// <summary>
    /// Gets the items currently selected by the combo box
    /// </summary>
    /// <returns>The items currently selected</returns>
    protected override IEnumerable<IDataVirtualizationItem> GetSelectedItems()
    {
        if (AssociatedObject is { } comboBox &&
            comboBox.SelectedItem is IDataVirtualizationItem selectedItem)
            yield return selectedItem;
    }

    /// <summary>
    /// Gets the scroll viewer control the viewport size of which will be used to manage the data virtualization list's load capacity
    /// </summary>
    /// <returns>The scroll viewer control or <c>null</c></returns>
    protected override ScrollViewer? GetScrollViewer()
    {
        if (this.scrollViewer is null &&
            AssociatedObject is ComboBox comboBox &&
            comboBox.Template?.FindName("PART_Popup", comboBox) is Popup popup &&
            popup.Child.GetVisualDescendent<ScrollViewer>() is { } scrollViewer)
            this.scrollViewer = scrollViewer;
        return this.scrollViewer;
    }

    /// <summary>
    /// Called after the behavior is attached to an associated object
    /// </summary>
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.DropDownOpened += AssociatedObjectDropDownOpened;
    }

    /// <summary>
    /// Called when the behavior is being detached from its associated object, but before it has actually occurred
    /// </summary>
    protected override void OnDetaching()
    {
        base.OnDetaching();
        scrollViewer = null;
        AssociatedObject.DropDownOpened -= AssociatedObjectDropDownOpened;
    }
}
