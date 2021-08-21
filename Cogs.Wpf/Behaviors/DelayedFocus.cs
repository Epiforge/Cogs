namespace Cogs.Wpf.Behaviors;

/// <summary>
/// Focuses an element after a specified delay
/// </summary>
public class DelayedFocus : Behavior<UIElement>
{
    Dispatcher? dispatcher;

    /// <summary>
    /// Gets/sets the delay before the focus operation occurs
    /// </summary>
    public TimeSpan? Delay { get; set; }

    async void DelayCallback(object? state)
    {
        await Task.Delay(Delay ?? TimeSpan.Zero).ConfigureAwait(false);
        if (dispatcher is not null)
            await dispatcher.InvokeAsync(FocusCallback).Task.ConfigureAwait(false);
    }

    void FocusCallback() => FindFocusableUIElement(AssociatedObject)?.Focus();

    UIElement? FindFocusableUIElement(UIElement element)
    {
        if (element.Focusable)
            return element;
        for (int i = 0, ii = VisualTreeHelper.GetChildrenCount(element); i < ii; ++i)
            if (VisualTreeHelper.GetChild(element, i) is UIElement childElement && FindFocusableUIElement(childElement) is { } focusableChildElement)
                return focusableChildElement;
        return null;
    }

    /// <summary>
    /// Called after the behavior is attached to an <see cref="Behavior{UIElement}.AssociatedObject"/>
    /// </summary>
    protected override void OnAttached()
    {
        base.OnAttached();
        dispatcher = AssociatedObject.Dispatcher;
        ThreadPool.QueueUserWorkItem(DelayCallback);
    }
}
