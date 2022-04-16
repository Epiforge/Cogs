namespace Cogs.Disposal;

/// <summary>
/// Represents a cache of key-value pairs which, once disposed by all retrievers, are removed (values must implement <see cref="DisposableValuesCache{TKey, TValue}.Value"/>)
/// </summary>
/// <typeparam name="TKey">The type of the keys</typeparam>
/// <typeparam name="TValue">The type of the values</typeparam>
public class DisposableValuesCache<TKey, TValue>
    where TKey : notnull
    where TValue : DisposableValuesCache<TKey, TValue>.Value, new()
{
    /// <summary>
    /// Instantiates a new instance of <see cref="DisposableValuesCache{TKey, TValue}"/>
    /// </summary>
    public DisposableValuesCache() =>
        values = new();

    /// <summary>
    /// Instantiates a new instance of <see cref="DisposableValuesCache{TKey, TValue}"/>, specifying the time to live for values which have been disposed by all retrievers
    /// </summary>
    /// <param name="orphanTtl">The time to live for values which have been disposed by all retrievers; if retrieved again before expiration, termination is cancelled</param>
    public DisposableValuesCache(TimeSpan orphanTtl) :
        this() =>
        this.orphanTtl = orphanTtl;

    /// <summary>
    /// Instantiates a new instance of <see cref="DisposableValuesCache{TKey, TValue}"/> using the specified <paramref name="comparer"/>
    /// </summary>
    /// <param name="comparer">The equality comparison implementation to use when comparing keys</param>
    public DisposableValuesCache(IEqualityComparer<TKey> comparer) =>
        values = new(comparer);

    /// <summary>
    /// Instantiates a new instance of <see cref="DisposableValuesCache{TKey, TValue}"/> using the specified <paramref name="comparer"/>, specifying the time to live for values which have been disposed by all retrievers
    /// </summary>
    /// <param name="comparer">The equality comparison implementation to use when comparing keys</param>
    /// <param name="orphanTtl">The time to live for values which have been disposed by all retrievers; if retrieved again before expiration, termination is cancelled</param>
    public DisposableValuesCache(IEqualityComparer<TKey> comparer, TimeSpan orphanTtl) :
        this(comparer) =>
        this.orphanTtl = orphanTtl;

    readonly ReaderWriterLockSlim access = new();
    readonly TimeSpan? orphanTtl;
    readonly ConcurrentDictionary<TKey, TValue> values;

    /// <summary>
    /// Gets a value from the cache, generating it if necessary -- dispose of the value when done with it!
    /// </summary>
    /// <param name="key">The key of the value</param>
    public TValue this[TKey key]
    {
        get
        {
            TValue value;
            access.EnterReadLock();
            try
            {
                value = values.GetOrAdd(key, ValueFactory);
            }
            finally
            {
                access.ExitReadLock();
            }
            value.InitializeIfNew(this, key);
            return value;
        }
    }

    /// <summary>
    /// Gets the number of key-value pairs currently in the cache
    /// </summary>
    public int Count =>
        values.Count;

    static TValue ValueFactory(TKey key) =>
        new();

    /// <summary>
    /// Represents a value in the cache
    /// </summary>
    [SuppressMessage("Design", "CA1034: Nested types should not be visible", Justification = @"Ehh, not sure how to do this more elegantly, and the language supports it ¯\_(ツ)_/¯")]
    public abstract class Value :
        PropertyChangeNotifier,
        IAsyncDisposable,
        IDisposalStatus,
        IDisposable,
        INotifyDisposalOverridden,
        INotifyDisposed,
        INotifyDisposing
    {
        readonly object access = new();
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        DisposableValuesCache<TKey, TValue> cache;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        bool isDisposed;
        CancellationTokenSource? orphanTtlCts;
        int referenceCount;

        /// <inheritdoc/>
        public bool IsDisposed
        {
            get => isDisposed;
            private set => SetBackedProperty(ref isDisposed, in value, Disposable.IsDisposedPropertyChanging, Disposable.IsDisposedPropertyChanged);
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        /// <summary>
        /// Gets the key associated with this value
        /// </summary>
        protected TKey Key { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <inheritdoc/>
        public event EventHandler<DisposalNotificationEventArgs>? DisposalOverridden;
        /// <inheritdoc/>
        public event EventHandler<DisposalNotificationEventArgs>? Disposed;
        /// <inheritdoc/>
        public event EventHandler<DisposalNotificationEventArgs>? Disposing;

        /// <inheritdoc/>
        [SuppressMessage("Usage", "CA1816: Dispose methods should call SuppressFinalize", Justification = "Another method is doing the work")]
        public void Dispose()
        {
            if (cache.orphanTtl is { } orphanTtl && orphanTtl > TimeSpan.Zero)
            {
                CancellationToken token;
                lock (access)
                {
                    if (orphanTtlCts is not null)
                    {
                        orphanTtlCts.Cancel();
                        orphanTtlCts.Dispose();
                    }
                    orphanTtlCts = new CancellationTokenSource();
                    token = orphanTtlCts.Token;
                }
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(orphanTtl, token).ConfigureAwait(false);
                        lock (access)
                        {
                            try
                            {
                                orphanTtlCts?.Dispose();
                                orphanTtlCts = null;
                            }
                            catch (ObjectDisposedException)
                            {
                                return;
                            }
                        }
                        DisposalLogic();
                    }
                    catch (OperationCanceledException)
                    {
                        // we're still needed!
                    }
                });
            }
            else
                DisposalLogic();
        }

        /// <inheritdoc/>
        [SuppressMessage("Usage", "CA1816: Dispose methods should call SuppressFinalize", Justification = "Another method is doing the work")]
        public ValueTask DisposeAsync()
        {
            if (cache.orphanTtl is { } orphanTtl && orphanTtl > TimeSpan.Zero)
            {
                CancellationToken token;
                lock (access)
                {
                    if (orphanTtlCts is not null)
                    {
                        orphanTtlCts.Cancel();
                        orphanTtlCts.Dispose();
                    }
                    orphanTtlCts = new CancellationTokenSource();
                    token = orphanTtlCts.Token;
                }
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(orphanTtl, token).ConfigureAwait(false);
                        lock (access)
                        {
                            try
                            {
                                orphanTtlCts?.Dispose();
                                orphanTtlCts = null;
                            }
                            catch (ObjectDisposedException)
                            {
                                return;
                            }
                        }
                        DisposalLogic();
                    }
                    catch (OperationCanceledException)
                    {
                        // we're still needed!
                    }
                });
            }
            else
                DisposalLogic();
            return new ValueTask(Task.CompletedTask);
        }

        [SuppressMessage("Usage", "CA1816: Dispose methods should call SuppressFinalize", Justification = "This method is doing the work for the others")]
        void DisposalLogic()
        {
            var e = DisposalNotificationEventArgs.ByCallingDispose;
            Disposing?.Invoke(this, e);
            var isRemoving = false;
            cache.access.EnterWriteLock();
            try
            {
                isRemoving = --referenceCount == 0;
                if (isRemoving)
                    cache.values.TryRemove(Key, out _);
            }
            finally
            {
                cache.access.ExitWriteLock();
            }
            if (isRemoving)
            {
                OnTerminated();
                GC.SuppressFinalize(this);
                IsDisposed = true;
                Disposed?.Invoke(this, e);
            }
            else
                DisposalOverridden?.Invoke(this, e);
        }

        internal void InitializeIfNew(DisposableValuesCache<TKey, TValue> cache, TKey key)
        {
            lock (access)
            {
                if (orphanTtlCts is not null)
                {
                    orphanTtlCts.Cancel();
                    orphanTtlCts.Dispose();
                    orphanTtlCts = null;
                }
                if (++referenceCount == 1)
                {
                    this.cache = cache;
                    Key = key;
                    OnInitialized();
                }
            }
        }

        /// <summary>
        /// Invoked when the <see cref="Key"/> property has been set and the value is being initialized
        /// </summary>
        protected abstract void OnInitialized();

        /// <summary>
        /// Invoked when the value has been removed from the cache but before the finalizer has been suppressed
        /// </summary>
        protected abstract void OnTerminated();
    }
}
