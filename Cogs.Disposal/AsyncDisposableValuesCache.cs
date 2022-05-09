namespace Cogs.Disposal;

/// <summary>
/// Represents a cache of key-value pairs which, once disposed by all retrievers, are removed (values must implement <see cref="AsyncDisposableValuesCache{TKey, TValue}.Value"/>)
/// </summary>
/// <typeparam name="TKey">The type of the keys</typeparam>
/// <typeparam name="TValue">The type of the values</typeparam>
public class AsyncDisposableValuesCache<TKey, TValue>
    where TKey : notnull
    where TValue : AsyncDisposableValuesCache<TKey, TValue>.Value, new()
{
    /// <summary>
    /// Instantiates a new instance of <see cref="AsyncDisposableValuesCache{TKey, TValue}"/>
    /// </summary>
    public AsyncDisposableValuesCache() =>
        values = new();

    /// <summary>
    /// Instantiates a new instance of <see cref="AsyncDisposableValuesCache{TKey, TValue}"/>, specifying the time to live for values which have been disposed by all retrievers
    /// </summary>
    /// <param name="orphanTtl">The time to live for values which have been disposed by all retrievers; if retrieved again before expiration, termination is cancelled</param>
    public AsyncDisposableValuesCache(TimeSpan orphanTtl) :
        this() =>
        this.orphanTtl = orphanTtl;

    /// <summary>
    /// Instantiates a new instance of <see cref="AsyncDisposableValuesCache{TKey, TValue}"/> using the specified <paramref name="comparer"/>
    /// </summary>
    /// <param name="comparer">The equality comparison implementation to use when comparing keys</param>
    public AsyncDisposableValuesCache(IEqualityComparer<TKey> comparer) =>
        values = new(comparer);

    /// <summary>
    /// Instantiates a new instance of <see cref="AsyncDisposableValuesCache{TKey, TValue}"/> using the specified <paramref name="comparer"/>, specifying the time to live for values which have been disposed by all retrievers
    /// </summary>
    /// <param name="comparer">The equality comparison implementation to use when comparing keys</param>
    /// <param name="orphanTtl">The time to live for values which have been disposed by all retrievers; if retrieved again before expiration, termination is cancelled</param>
    public AsyncDisposableValuesCache(IEqualityComparer<TKey> comparer, TimeSpan orphanTtl) :
        this(comparer) =>
        this.orphanTtl = orphanTtl;

    readonly AsyncReaderWriterLock access = new();
    readonly TimeSpan? orphanTtl;
    readonly ConcurrentDictionary<TKey, TValue> values;

    /// <summary>
    /// Gets the number of key-value pairs currently in the cache
    /// </summary>
    public int Count =>
        values.Count;

    /// <summary>
    /// Gets a value from the cache, generating it if necessary -- dispose of the value when done with it!
    /// </summary>
    /// <param name="key">The key of the value</param>
    public Task<TValue> GetAsync(TKey key) =>
        GetAsync(key, CancellationToken.None);

    /// <summary>
    /// Gets a value from the cache, generating it if necessary -- dispose of the value when done with it!
    /// </summary>
    /// <param name="key">The key of the value</param>
    /// <param name="cancellationToken">The cancellation token used to cancel the retrieval</param>
    public async Task<TValue> GetAsync(TKey key, CancellationToken cancellationToken)
    {
        TValue value;
        using var grant = await access.ReaderLockAsync(cancellationToken);
        try
        {
            value = values.GetOrAdd(key, ValueFactory);
        }
        finally
        {
            grant.Dispose();
        }
        await value.InitializeIfNewAsync(this, key).ConfigureAwait(false);
        return value;
    }

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
        readonly AsyncLock access = new();
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        AsyncDisposableValuesCache<TKey, TValue> cache;
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
            if (cache is null)
                return;
            if (cache.orphanTtl is { } orphanTtl && orphanTtl > TimeSpan.Zero)
            {
                CancellationToken token;
                using (access.Lock())
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
                        using (await access.LockAsync().ConfigureAwait(false))
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
                        await DisposalLogicAsync().ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // we're still needed!
                    }
                });
            }
            else
                DisposalLogicAsync().AsTask().Wait();
        }

        /// <inheritdoc/>
        [SuppressMessage("Usage", "CA1816: Dispose methods should call SuppressFinalize", Justification = "Another method is doing the work")]
        public async ValueTask DisposeAsync()
        {
            if (cache is null)
                return;
            if (cache.orphanTtl is { } orphanTtl && orphanTtl > TimeSpan.Zero)
            {
                CancellationToken token;
                using (await access.LockAsync().ConfigureAwait(false))
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
                        using (await access.LockAsync().ConfigureAwait(false))
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
                        await DisposalLogicAsync().ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // we're still needed!
                    }
                });
            }
            else
                await DisposalLogicAsync().ConfigureAwait(false);
        }

        [SuppressMessage("Usage", "CA1816: Dispose methods should call SuppressFinalize", Justification = "This method is doing the work for the others")]
        async ValueTask DisposalLogicAsync()
        {
            var e = DisposalNotificationEventArgs.ByCallingDispose;
            Disposing?.Invoke(this, e);
            var isRemoving = false;
            using (await cache.access.WriterLockAsync().ConfigureAwait(false))
            {
                isRemoving = --referenceCount == 0;
                if (isRemoving)
                    cache.values.TryRemove(Key, out _);
            }
            if (isRemoving)
            {
                await OnTerminatedAsync().ConfigureAwait(false);
                GC.SuppressFinalize(this);
                IsDisposed = true;
                Disposed?.Invoke(this, e);
            }
            else
                DisposalOverridden?.Invoke(this, e);
        }

        internal async Task InitializeIfNewAsync(AsyncDisposableValuesCache<TKey, TValue> cache, TKey key)
        {
            using (await access.LockAsync().ConfigureAwait(false))
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
                    await OnInitializedAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Invoked when the <see cref="Key"/> property has been set and the value is being initialized
        /// </summary>
        protected abstract Task OnInitializedAsync();

        /// <summary>
        /// Invoked when the value has been removed from the cache but before the finalizer has been suppressed
        /// </summary>
        protected abstract ValueTask OnTerminatedAsync();
    }
}
