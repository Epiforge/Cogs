using System;

namespace Cogs.Collections.Synchronized
{
    /// <summary>
    /// Provides data for the <see cref="ProcessingQueue{T}.UnhandledException"/> event
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ProcessingQueueUnhandledExceptionEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessingQueueUnhandledExceptionEventArgs{T}"/> class
        /// </summary>
        /// <param name="item">The item the processing of which threw the unhandled exception</param>
        /// <param name="exception">The unhandled exception that was thrown by the processing queue action</param>
        public ProcessingQueueUnhandledExceptionEventArgs(T item, Exception exception)
        {
            Item = item;
            Exception = exception;
        }

        /// <summary>
        /// Gets the unhandled exception that was thrown by the processing queue action
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the item the processing of which threw the unhandled exception
        /// </summary>
        public T Item { get; }
    }
}
