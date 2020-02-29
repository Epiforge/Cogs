using Cogs.Components;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Cogs.Disposal
{
    /// <summary>
    /// Provides an overridable mechanism for releasing unmanaged resources synchronously
    /// </summary>
    [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "This class is simplifying implementation of IDisposable for inheritors.")]
    public abstract class SyncDisposable : PropertyChangeNotifier, IDisposable
    {
        /// <summary>
        /// Finalizes this object
        /// </summary>
        ~SyncDisposable()
        {
            Dispose(false);
            IsDisposed = true;
            OnDisposed(new EventArgs());
        }

        readonly object disposalAccess = new object();
        bool isDisposed;

        /// <summary>
        /// Gets whether this object has been disposed
        /// </summary>
        public bool IsDisposed
        {
            get => isDisposed;
            private set => SetBackedProperty(ref isDisposed, in value);
        }

        /// <summary>
        /// Occurs when the object is disposed by a <see cref="Dispose()"/> call or the finalizer
        /// </summary>
        public event EventHandler? Disposed;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
        /// </summary>
        public void Dispose()
        {
            lock (disposalAccess)
                if (!IsDisposed && (IsDisposed = Dispose(true)))
                {
                    GC.SuppressFinalize(this);
                    OnDisposed(new EventArgs());
                }
        }

        /// <summary>
        /// Frees, releases, or resets unmanaged resources
        /// </summary>
        /// <param name="disposing">false if invoked by the finalizer because the object is being garbage collected; otherwise, true</param>
        /// <returns>true if disposal completed; otherwise, false</returns>
        protected abstract bool Dispose(bool disposing);

        /// <summary>
        /// Raises the <see cref="Disposed"/> event
        /// </summary>
		/// <param name="e">The arguments of the event</param>
        protected virtual void OnDisposed(EventArgs e) => Disposed?.Invoke(this, e);

        /// <summary>
        /// Ensure the object has not been disposed
        /// </summary>
		/// <exception cref="ObjectDisposedException">The object has already been disposed</exception>
		protected void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}
