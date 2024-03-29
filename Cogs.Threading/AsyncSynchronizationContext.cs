namespace Cogs.Threading;

/// <summary>
/// Provides a synchronization context for the Task Parallel Library
/// </summary>
public sealed class AsyncSynchronizationContext :
    SynchronizationContext,
    IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncSynchronizationContext"/> class
    /// </summary>
    public AsyncSynchronizationContext() :
        this(true)
    {
    }

    internal AsyncSynchronizationContext(bool allowDisposal)
    {
        queuedCallbacksCancellationTokenSource = new CancellationTokenSource();
        queuedCallbacks = new BufferBlock<(SendOrPostCallback callback, object? state, ManualResetEventSlim? signal, Exception? exception)>(new DataflowBlockOptions { CancellationToken = queuedCallbacksCancellationTokenSource.Token });
        this.allowDisposal = allowDisposal;
        Task.Run(ProcessCallbacks);
    }

    /// <summary>
    /// Finalizes this object
    /// </summary>
    ~AsyncSynchronizationContext() =>
        Dispose(false);

    readonly bool allowDisposal;
    readonly BufferBlock<(SendOrPostCallback callback, object? state, ManualResetEventSlim? signal, Exception? exception)> queuedCallbacks;
    readonly CancellationTokenSource queuedCallbacksCancellationTokenSource;

    /// <inheritdoc/>
    public override SynchronizationContext CreateCopy() =>
        this;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!allowDisposal)
            throw new InvalidOperationException();
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Frees, releases, or resets unmanaged resources
    /// </summary>
    /// <param name="disposing"><c>false</c> if invoked by the finalizer because the object is being garbage collected; otherwise, <c>true</c></param>
    void Dispose(bool disposing)
    {
        if (disposing)
        {
            queuedCallbacksCancellationTokenSource.Cancel();
            queuedCallbacksCancellationTokenSource.Dispose();
        }
    }

    /// <inheritdoc/>
    public override void Post(SendOrPostCallback d, object state) =>
        queuedCallbacks.Post((d ?? throw new ArgumentNullException(nameof(d)), state, null, null));

    async Task ProcessCallbacks()
    {
        while (true)
        {
            var csse = await queuedCallbacks.ReceiveAsync().ConfigureAwait(false);
            var currentContext = Current;
            SetSynchronizationContext(this);
            var (callback, state, signal, _) = csse;
            try
            {
                callback(state);
            }
            catch (Exception ex)
            {
                csse.exception = ex;
            }
            if (signal is not null)
                signal.Set();
            SetSynchronizationContext(currentContext);
        }
    }

    /// <inheritdoc/>
    [SuppressMessage("Code Analysis", "CA1508: Avoid dead conditional code", Justification = "The analyzer is mistaken")]
    public override void Send(SendOrPostCallback d, object state)
    {
        using var signal = new ManualResetEventSlim(false);
        var csse = (callback: d ?? throw new ArgumentNullException(nameof(d)), state, signal, exception: (Exception?)null);
        queuedCallbacks.Post(csse);
        signal.Wait();
        if (csse.exception is not null)
            ExceptionDispatchInfo.Capture(csse.exception).Throw();
    }
}
