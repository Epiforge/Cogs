namespace Cogs.Wpf.Behaviors;

/// <summary>
/// Deselects all items when empty space in a <see cref="ListView"/> is clicked
/// </summary>
public sealed class DeselectAllOnEmptySpaceClicked :
    Behavior<ListView>
{
    void AssociatedObjectPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt)) == ModifierKeys.None &&
            VisualTreeHelper.HitTest(AssociatedObject, e.GetPosition(AssociatedObject)).VisualHit is { } visualHit &&
            visualHit.GetVisualAncestor<ListViewItem, GridViewColumnHeader, ScrollBar>() is null)
            AssociatedObject.UnselectAll();
    }

    /// <summary>
    /// Called after the behavior is attached to an <see cref="Behavior{ListView}.AssociatedObject"/>
    /// </summary>
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseDown += AssociatedObjectPreviewMouseDown;
    }

    /// <summary>
    /// Called after the behavior is detached from an <see cref="Behavior{ListView}.AssociatedObject"/>
    /// </summary>
    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.PreviewMouseDown -= AssociatedObjectPreviewMouseDown;
    }
}
