using System;

namespace Cogs.Disposal
{
    /// <summary>
    /// Represents the arguments for the <see cref="INotifyDisposalOverridden.DisposalOverridden"/>, <see cref="INotifyDisposed.Disposed"/>, and <see cref="INotifyDisposing.Disposing"/> events
    /// </summary>
    public class DisposalNotificationEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DisposalNotificationEventArgs"/> class
        /// </summary>
        /// <param name="isFinalizer"><c>true</c> if the object is being disposed by the finalizer; otherwise, <c>false</c></param>
        public DisposalNotificationEventArgs(bool isFinalizer) => IsFinalizer = isFinalizer;

        /// <summary>
        /// Gets whether the object is being disposed by the finalizer
        /// </summary>
        public bool IsFinalizer { get; }
    }
}
