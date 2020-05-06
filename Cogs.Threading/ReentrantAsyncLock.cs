using Nito.AsyncEx;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cogs.Threading
{
    /// <summary>
    /// A mutual exclusion lock that is compatible with async and is reentrant
    /// </summary>
    public class ReentrantAsyncLock
    {
        /// <summary>
        /// Creates an instance of <see cref="ReentrantAsyncLock"/>
        /// </summary>
        public ReentrantAsyncLock()
        {
            accessSource = new AsyncLock();
            acquiredAccess = new AsyncLocal<IDisposable?>();
        }

        readonly AsyncLock accessSource;
        readonly AsyncLocal<IDisposable?> acquiredAccess;

        /// <summary>
        /// Execute <paramref name="action"/> synchronously, acquiring the lock synchronously if necessary (may block)
        /// </summary>
        /// <param name="action">The void method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the action will be executed only if the lock is already acquired or can be acquired immediately</param>
        public void WithLock(Action action, CancellationToken cancellationToken = default)
        {
            bool lockAcquiredHere;
            if (lockAcquiredHere = acquiredAccess.Value is null)
                acquiredAccess.Value = accessSource.Lock(cancellationToken);
            try
            {
                action();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value!.Dispose();
                    acquiredAccess.Value = null;
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="func"/> synchronously and return its return value, acquiring the lock synchronously if necessary (may block)
        /// </summary>
        /// <param name="func">The method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the method will be executed only if the lock is already acquired or can be acquired immediately</param>
        public T WithLock<T>(Func<T> func, CancellationToken cancellationToken = default)
        {
            bool lockAcquiredHere;
            if (lockAcquiredHere = acquiredAccess.Value is null)
                acquiredAccess.Value = accessSource.Lock(cancellationToken);
            try
            {
                return func();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value!.Dispose();
                    acquiredAccess.Value = null;
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="action"/> synchronously, acquiring the lock asynchronously if necessary
        /// </summary>
        /// <param name="action">The void action to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the action will be executed only if the lock is already acquired or can be acquired immediately</param>
        public async Task WithLockAsync(Action action, CancellationToken cancellationToken = default)
        {
            bool lockAcquiredHere;
            if (lockAcquiredHere = acquiredAccess.Value is null)
                acquiredAccess.Value = await accessSource.LockAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                action();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value!.Dispose();
                    acquiredAccess.Value = null;
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="func"/> synchronously and return its return value, acquiring the lock asynchronously if necessary
        /// </summary>
        /// <param name="func">The method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the method will be executed only if the lock is already acquired or can be acquired immediately</param>
        public async Task<T> WithLockAsync<T>(Func<T> func, CancellationToken cancellationToken = default)
        {
            bool lockAcquiredHere;
            if (lockAcquiredHere = acquiredAccess.Value is null)
                acquiredAccess.Value = await accessSource.LockAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return func();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value!.Dispose();
                    acquiredAccess.Value = null;
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="asyncAction"/> asynchronously, acquiring the lock asynchronously if necessary
        /// </summary>
        /// <param name="asyncAction">The asynchronous void method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the action will be executed only if the lock is already acquired or can be acquired immediately</param>
        public async Task WithLockAsync(Func<Task> asyncAction, CancellationToken cancellationToken = default)
        {
            bool lockAcquiredHere;
            if (lockAcquiredHere = acquiredAccess.Value is null)
                acquiredAccess.Value = await accessSource.LockAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await asyncAction().ConfigureAwait(false);
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value!.Dispose();
                    acquiredAccess.Value = null;
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="asyncFunc"/> asynchronously and return its return value, acquiring the lock asynchronously if necessary
        /// </summary>
        /// <param name="asyncFunc">The asynchronous method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the method will be executed only if the lock is already acquired or can be acquired immediately</param>
        public async Task<T> WithLockAsync<T>(Func<Task<T>> asyncFunc, CancellationToken cancellationToken = default)
        {
            bool lockAcquiredHere;
            if (lockAcquiredHere = acquiredAccess.Value is null)
                acquiredAccess.Value = await accessSource.LockAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await asyncFunc().ConfigureAwait(false);
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value!.Dispose();
                    acquiredAccess.Value = null;
                }
            }
        }
    }
}
