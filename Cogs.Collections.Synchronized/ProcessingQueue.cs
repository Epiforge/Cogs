using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Cogs.Collections.Synchronized
{
    /// <summary>
    /// A queue that will perform an action on each item enqueued in serial
    /// </summary>
    /// <typeparam name="T">The type of items in the queue</typeparam>
    public class ProcessingQueue<T> : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessingQueue{T}"/> class
        /// </summary>
        /// <param name="action">The action to perform on each item</param>
        public ProcessingQueue(Action<T> action)
        {
            this.action = action;
            queueCancellationTokenSource = new CancellationTokenSource();
            queue = new BufferBlock<T>(new DataflowBlockOptions { CancellationToken = queueCancellationTokenSource.Token });
            Task.Run(ProcessQueueAsync);
        }

        /// <summary>
        /// Finalizes this object
        /// </summary>
        ~ProcessingQueue() => Dispose(false);

        readonly Action<T> action;
        readonly CancellationTokenSource queueCancellationTokenSource;
        readonly BufferBlock<T> queue;

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
            if (disposing)
                queueCancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Adds an item to the queue to be processed and immediately returns control to the caller
        /// </summary>
        /// <param name="item">The item to be processed</param>
        /// <returns><c>true</c> if the item was added; otherwise, <c>false</c></returns>
        public bool Enqueue(T item) => queue.Post(item);

        /// <summary>
        /// Raises the <see cref="UnhandledException"/> event
        /// </summary>
        /// <param name="e">The event data</param>
        protected virtual void OnUnhandledException(ProcessingQueueUnhandledExceptionEventArgs<T> e) => UnhandledException?.Invoke(this, e);

        /// <summary>
        /// Creates event data for the <see cref="UnhandledException"/> event and calls <see cref="OnUnhandledException(ProcessingQueueUnhandledExceptionEventArgs{T})"/>
        /// </summary>
        /// <param name="item">The item the processing of which threw the unhandled exception</param>
        /// <param name="exception">The unhandled exception that was thrown by the processing queue action</param>
        protected void OnUnhandledException(T item, Exception exception) => OnUnhandledException(new ProcessingQueueUnhandledExceptionEventArgs<T>(item, exception));

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
            }
        }
    }
}
