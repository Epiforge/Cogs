namespace Cogs.Collections.Synchronized;

/// <summary>
/// A queue that will perform an action on each item enqueued in serial
/// </summary>
/// <typeparam name="T">The type of items in the queue</typeparam>
public class ProcessingQueue<T> :
    IDisposable,
    IWaitUntilIdle
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessingQueue{T}"/> class
    /// </summary>
    /// <param name="action">The action to perform on each item</param>
    public ProcessingQueue(Action<T> action)
    {
        this.action = action;
        count = 0;
        countChanged = new AsyncManualResetEvent();
        queueCancellationTokenSource = new CancellationTokenSource();
        queue = new BufferBlock<T>(new DataflowBlockOptions
        {
            CancellationToken = queueCancellationTokenSource.Token
        });
        Task.Run(ProcessQueueAsync);
    }

    /// <summary>
    /// Finalizes this object
    /// </summary>
    ~ProcessingQueue() =>
        Dispose(false);

    readonly Action<T> action;
    long count;
    readonly AsyncManualResetEvent countChanged;
    readonly CancellationTokenSource queueCancellationTokenSource;
    readonly BufferBlock<T> queue;

    /// <summary>
    /// Gets the number of items currently in the queue or being processed
    /// </summary>
    public long Count =>
        Interlocked.Read(ref count);

    /// <summary>
    /// Gets whether the queue has been disposed, and is no longer accepting or processing items
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Occurs when the action throws an unhandled exception
    /// </summary>
    public event EventHandler<ProcessingQueueUnhandledExceptionEventArgs<T>>? UnhandledException;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Frees, releases, or resets unmanaged resources
    /// </summary>
    /// <param name="disposing"><c>false</c> if invoked by the finalizer because the object is being garbage collected; otherwise, <c>true</c></param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !IsDisposed)
        {
            queueCancellationTokenSource.Cancel();
            queueCancellationTokenSource.Dispose();
            Interlocked.Exchange(ref count, 0);
            countChanged.Set();
            countChanged.Reset();
            IsDisposed = true;
        }
    }

    /// <summary>
    /// Adds an item to the queue to be processed and immediately returns control to the caller
    /// </summary>
    /// <param name="item">The item to be processed</param>
    /// <returns><c>true</c> if the item was added; otherwise, <c>false</c></returns>
    public bool Enqueue(T item)
    {
        if (queue.Post(item))
        {
            Interlocked.Increment(ref count);
            countChanged.Set();
            countChanged.Reset();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Raises the <see cref="UnhandledException"/> event
    /// </summary>
    /// <param name="e">The event data</param>
    protected virtual void OnUnhandledException(ProcessingQueueUnhandledExceptionEventArgs<T> e) =>
        UnhandledException?.Invoke(this, e);

    /// <summary>
    /// Creates event data for the <see cref="UnhandledException"/> event and calls <see cref="OnUnhandledException(ProcessingQueueUnhandledExceptionEventArgs{T})"/>
    /// </summary>
    /// <param name="item">The item the processing of which threw the unhandled exception</param>
    /// <param name="exception">The unhandled exception that was thrown by the processing queue action</param>
    protected void OnUnhandledException(T item, Exception exception) =>
        OnUnhandledException(new ProcessingQueueUnhandledExceptionEventArgs<T>(item, exception));

    async Task ProcessQueueAsync()
    {
        while (true)
        {
            var item = await queue.ReceiveAsync().ConfigureAwait(false);
            try
            {
                action(item);
            }
            catch (Exception ex)
            {
                try
                {
                    OnUnhandledException(item, ex);
                }
                catch
                {
                    // seriously?
                }
            }
            finally
            {
                if (Interlocked.Decrement(ref count) < 0)
                    Interlocked.Exchange(ref count, 0);
                countChanged.Set();
                countChanged.Reset();
            }
        }
    }

    /// <summary>
    /// Synchronously waits until the queue has finished processing all items that have been enqueued and is now idle (this method may block the calling thread)
    /// </summary>
    public void WaitUntilIdle()
    {
        while (true)
        {
            if (Interlocked.Read(ref count) == 0)
                return;
            countChanged.Wait();
        }
    }

    /// <summary>
    /// Synchronously waits until the queue has finished processing all items that have been enqueued and is now idle (this method may block the calling thread)
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to cancel the wait (if this token is already cancelled, this method will first check whether the queue has finished processing)</param>
    public void WaitUntilIdle(CancellationToken cancellationToken)
    {
        while (true)
        {
            if (Interlocked.Read(ref count) == 0)
                return;
            countChanged.Wait(cancellationToken);
        }
    }

    /// <summary>
    /// Asynchronously waits until the queue has finished processing all items that have been enqueued and is now idle
    /// </summary>
    public async Task WaitUntilIdleAsync()
    {
        while (true)
        {
            if (Interlocked.Read(ref count) == 0)
                return;
            await countChanged.WaitAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously waits until the queue has finished processing all items that have been enqueued and is now idle
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to cancel the wait (if this token is already cancelled, this method will first check whether the queue has finished processing)</param>
    public async Task WaitUntilIdleAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            if (Interlocked.Read(ref count) == 0)
                return;
            await countChanged.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
