using System;

namespace Cogs.Disposal
{
    /// <summary>
    /// Notifies clients that the object is being disposed
    /// </summary>
    public interface INotifyDisposing
    {
        /// <summary>
        /// Occurs when this object is being disposed
        /// </summary>
        event EventHandler<DisposalNotificationEventArgs>? Disposing;
    }
}
