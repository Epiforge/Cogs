namespace Cogs.Disposal;

/// <summary>
/// Represents the arguments for the <see cref="INotifyDisposalOverridden.DisposalOverridden"/>, <see cref="INotifyDisposed.Disposed"/>, and <see cref="INotifyDisposing.Disposing"/> events
/// </summary>
public class DisposalNotificationEventArgs :
    EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DisposalNotificationEventArgs"/> class
    /// </summary>
    /// <param name="isFinalizer"><c>true</c> if the object is being disposed by the finalizer; otherwise, <c>false</c></param>
    public DisposalNotificationEventArgs(bool isFinalizer) =>
        IsFinalizer = isFinalizer;

    /// <summary>
    /// Gets whether the object is being disposed by the finalizer
    /// </summary>
    public bool IsFinalizer { get; }

    /// <summary>
    /// Gets a reusable instance of arguments for when disposal is ocurring because <see cref="IDisposable.Dispose"/> or <see cref="IAsyncDisposable.DisposeAsync"/> was called
    /// </summary>
    public static DisposalNotificationEventArgs ByCallingDispose { get; } = new DisposalNotificationEventArgs(false);

    /// <summary>
    /// Gets a reusable instance of arguments for when disposal is ocurring because the object is being garbage collected
    /// </summary>
    public static DisposalNotificationEventArgs ByFinalizer { get; } = new DisposalNotificationEventArgs(true);
}
