namespace Cogs.Disposal;

/// <summary>
/// Provides an overridable mechanism for releasing unmanaged resources asynchronously or synchronously
/// </summary>
public abstract class Disposable :
    PropertyChangeNotifier,
    IAsyncDisposable,
    IDisposable,
    INotifyDisposalOverridden,
    IDisposalStatus,
    INotifyDisposed,
    INotifyDisposing
{
    /// <summary>
    /// Finalizes this object
    /// </summary>
    ~Disposable()
    {
        var e = DisposalNotificationEventArgs.ByFinalizer;
        OnDisposing(e);
        Dispose(false);
        IsDisposed = true;
        OnDisposed(e);
    }

    readonly AsyncLock disposalAccess = new();
    bool isDisposed;

    /// <summary>
    /// Gets whether this object has been disposed
    /// </summary>
	public bool IsDisposed
    {
        get => isDisposed;
        private set => SetBackedProperty(ref isDisposed, in value, IsDisposedPropertyChanging, IsDisposedPropertyChanged);
    }

    /// <summary>
    /// Occurs when this object's disposal has been overridden
    /// </summary>
    public event EventHandler<DisposalNotificationEventArgs>? DisposalOverridden;

    /// <summary>
    /// Occurs when this object has been disposed
    /// </summary>
    public event EventHandler<DisposalNotificationEventArgs>? Disposed;

    /// <summary>
    /// Occurs when this object is being disposed
    /// </summary>
    public event EventHandler<DisposalNotificationEventArgs>? Disposing;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
    /// </summary>
    public virtual void Dispose()
    {
        using (disposalAccess.Lock())
            if (!IsDisposed)
            {
                var e = DisposalNotificationEventArgs.ByCallingDispose;
                OnDisposing(e);
                if (IsDisposed = Dispose(true))
                {
                    OnDisposed(e);
                    GC.SuppressFinalize(this);
                }
                else
                    OnDisposalOverridden(e);
            }
    }

    /// <summary>
    /// Frees, releases, or resets unmanaged resources
    /// </summary>
    /// <param name="disposing">false if invoked by the finalizer because the object is being garbage collected; otherwise, true</param>
    /// <returns>true if disposal completed; otherwise, false</returns>
    protected abstract bool Dispose(bool disposing);

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
    /// </summary>
    public virtual async ValueTask DisposeAsync()
    {
        using (await disposalAccess.LockAsync().ConfigureAwait(false))
            if (!IsDisposed)
            {
                var e = DisposalNotificationEventArgs.ByCallingDispose;
                OnDisposing(e);
                if (IsDisposed = await DisposeAsync(true).ConfigureAwait(false))
                {
                    OnDisposed(e);
                    GC.SuppressFinalize(this);
                }
                else
                    OnDisposalOverridden(e);
            }
    }

    /// <summary>
    /// Frees, releases, or resets unmanaged resources
    /// </summary>
    /// <param name="disposing">false if invoked by the finalizer because the object is being garbage collected; otherwise, true</param>
    /// <returns>true if disposal completed; otherwise, false</returns>
    protected abstract ValueTask<bool> DisposeAsync(bool disposing);

    /// <summary>
    /// Raises the <see cref="DisposalOverridden"/> event with the specified arguments
    /// </summary>
    /// <param name="e">The event arguments</param>
    protected virtual void OnDisposalOverridden(DisposalNotificationEventArgs e) => DisposalOverridden?.Invoke(this, e);

    /// <summary>
    /// Raises the <see cref="Disposed"/> event with the specified arguments
    /// </summary>
    /// <param name="e">The event arguments</param>
    protected virtual void OnDisposed(DisposalNotificationEventArgs e) => Disposed?.Invoke(this, e);

    /// <summary>
    /// Raises the <see cref="Disposing"/> event with the specified arguments
    /// </summary>
    /// <param name="e">The event arguments</param>
    protected virtual void OnDisposing(DisposalNotificationEventArgs e) => Disposing?.Invoke(this, e);

    /// <summary>
    /// Ensure the object has not been disposed
    /// </summary>
	/// <exception cref="ObjectDisposedException">The object has already been disposed</exception>
	protected void ThrowIfDisposed()
    {
        if (isDisposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    internal static readonly PropertyChangedEventArgs IsDisposedPropertyChanged = new(nameof(IsDisposed));
    internal static readonly PropertyChangingEventArgs IsDisposedPropertyChanging = new(nameof(IsDisposed));
}
