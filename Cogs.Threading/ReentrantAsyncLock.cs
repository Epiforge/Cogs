using Nito.AsyncEx;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cogs.Threading
{
    /// <summary>
    /// Creates a new async-compatible mutual exclusion lock that allows reentrance
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
        public void WithLock(Action action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            bool lockAcquiredHere;
            if (lockAcquiredHere = acquiredAccess.Value is null)
                acquiredAccess.Value = accessSource.Lock();
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
        /// Execute <paramref name="action"/> synchronously, acquiring the lock synchronously if necessary (may block)
        /// </summary>
        /// <param name="action">The void method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the action will be executed only if the lock is already acquired or can be acquired immediately</param>
        public void WithLock(Action action, CancellationToken cancellationToken)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
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
        public T WithLock<T>(Func<T> func)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));
            bool lockAcquiredHere;
            if (lockAcquiredHere = acquiredAccess.Value is null)
                acquiredAccess.Value = accessSource.Lock();
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
        /// Execute <paramref name="func"/> synchronously and return its return value, acquiring the lock synchronously if necessary (may block)
        /// </summary>
        /// <param name="func">The method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the method will be executed only if the lock is already acquired or can be acquired immediately</param>
        public T WithLock<T>(Func<T> func, CancellationToken cancellationToken)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));
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
        public async Task WithLockAsync(Action action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            bool lockAcquiredHere;
            if (lockAcquiredHere = acquiredAccess.Value is null)
                acquiredAccess.Value = await accessSource.LockAsync().ConfigureAwait(false);
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
        /// Execute <paramref name="action"/> synchronously, acquiring the lock asynchronously if necessary
        /// </summary>
        /// <param name="action">The void action to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the action will be executed only if the lock is already acquired or can be acquired immediately</param>
        public async Task WithLockAsync(Action action, CancellationToken cancellationToken)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
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
        public async Task<T> WithLockAsync<T>(Func<T> func)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));
            bool lockAcquiredHere;
            if (lockAcquiredHere = acquiredAccess.Value is null)
                acquiredAccess.Value = await accessSource.LockAsync().ConfigureAwait(false);
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
        /// Execute <paramref name="func"/> synchronously and return its return value, acquiring the lock asynchronously if necessary
        /// </summary>
        /// <param name="func">The method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the method will be executed only if the lock is already acquired or can be acquired immediately</param>
        public async Task<T> WithLockAsync<T>(Func<T> func, CancellationToken cancellationToken)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));
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
        public async Task WithLockAsync(Func<Task> asyncAction)
        {
            if (asyncAction is null)
                throw new ArgumentNullException(nameof(asyncAction));
            bool lockAcquiredHere;
            if (lockAcquiredHere = acquiredAccess.Value is null)
                acquiredAccess.Value = await accessSource.LockAsync().ConfigureAwait(false);
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
        /// Execute <paramref name="asyncAction"/> asynchronously, acquiring the lock asynchronously if necessary
        /// </summary>
        /// <param name="asyncAction">The asynchronous void method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the action will be executed only if the lock is already acquired or can be acquired immediately</param>
        public async Task WithLockAsync(Func<Task> asyncAction, CancellationToken cancellationToken)
        {
            if (asyncAction is null)
                throw new ArgumentNullException(nameof(asyncAction));
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
        public async Task<T> WithLockAsync<T>(Func<Task<T>> asyncFunc)
        {
            if (asyncFunc is null)
                throw new ArgumentNullException(nameof(asyncFunc));
            bool lockAcquiredHere;
            if (lockAcquiredHere = acquiredAccess.Value is null)
                acquiredAccess.Value = await accessSource.LockAsync().ConfigureAwait(false);
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

        /// <summary>
        /// Execute <paramref name="asyncFunc"/> asynchronously and return its return value, acquiring the lock asynchronously if necessary
        /// </summary>
        /// <param name="asyncFunc">The asynchronous method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the method will be executed only if the lock is already acquired or can be acquired immediately</param>
        public async Task<T> WithLockAsync<T>(Func<Task<T>> asyncFunc, CancellationToken cancellationToken)
        {
            if (asyncFunc is null)
                throw new ArgumentNullException(nameof(asyncFunc));
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
