using Cogs.Components;
using Nito.AsyncEx;
using System;
using System.Threading.Tasks;

namespace Cogs.Disposal
{
    /// <summary>
    /// Provides an overridable mechanism for releasing unmanaged resources asynchronously or synchronously
    /// </summary>
    public abstract class Disposable : PropertyChangeNotifier, IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Finalizes this object
        /// </summary>
        ~Disposable()
        {
            Dispose(false);
            IsDisposed = true;
            OnDisposed(new EventArgs());
        }

        readonly AsyncLock disposalAccess = new AsyncLock();
        bool isDisposed;

        /// <summary>
        /// Occurs when the object is disposed by a <see cref="Dispose()"/> call, a <see cref="DisposeAsync()"/> call, or the finalizer
        /// </summary>
        public event EventHandler? Disposed;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
        /// </summary>
        public void Dispose()
        {
            using (disposalAccess.Lock())
                if (!isDisposed && (IsDisposed = Dispose(true)))
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
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            using (await disposalAccess.LockAsync().ConfigureAwait(false))
                if (!isDisposed && (IsDisposed = await DisposeAsync(true).ConfigureAwait(false)))
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
        protected abstract ValueTask<bool> DisposeAsync(bool disposing);

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
            if (isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// Gets whether this object has been disposed
        /// </summary>
		public bool IsDisposed
        {
            get => isDisposed;
            private set => SetBackedProperty(ref isDisposed, in value);
        }
    }
}
