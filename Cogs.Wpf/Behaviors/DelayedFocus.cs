namespace Cogs.Wpf.Behaviors;

/// <summary>
/// Focuses an element after a specified delay
/// </summary>
public class DelayedFocus :
    Behavior<UIElement>
{
    Dispatcher? dispatcher;

    /// <summary>
    /// Gets/sets the delay before the focus operation occurs
    /// </summary>
    public TimeSpan? Delay { get; set; }

    async Task DelayCallbackAsync()
    {
        await Task.Delay(Delay ?? TimeSpan.Zero).ConfigureAwait(false);
        if (dispatcher is not null)
            await dispatcher.InvokeAsync(FocusCallback).Task.ConfigureAwait(false);
    }

    void FocusCallback() =>
        AssociatedObject?.GetVisualDescendent<UIElement>(element => element.Focusable)?.Focus();

    /// <summary>
    /// Called after the behavior is attached to an <see cref="Behavior{UIElement}.AssociatedObject"/>
    /// </summary>
    protected override void OnAttached()
    {
        base.OnAttached();
        dispatcher = AssociatedObject.Dispatcher;
        Task.Run(DelayCallbackAsync);
    }
}
