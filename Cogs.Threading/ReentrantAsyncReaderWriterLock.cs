using Nito.AsyncEx;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cogs.Threading
{
    /// <summary>
    /// Creates a new async-compatible reader/writer lock that allows reentrance but not escalation
    /// </summary>
    public class ReentrantAsyncReaderWriterLock
    {
        /// <summary>
        /// Creates an instance of <see cref="ReentrantAsyncReaderWriterLock"/>
        /// </summary>
        public ReentrantAsyncReaderWriterLock()
        {
            accessSource = new AsyncReaderWriterLock();
            acquiredAccess = new AsyncLocal<(IDisposable? token, bool isWriter)>();
        }

        readonly AsyncReaderWriterLock accessSource;
        readonly AsyncLocal<(IDisposable? token, bool isWriter)> acquiredAccess;

        /// <summary>
        /// Execute <paramref name="action"/> synchronously, acquiring the lock synchronously as a reader if necessary (may block)
        /// </summary>
        /// <param name="action">The void method to execute</param>
        public void WithReaderLock(Action action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            var (token, _) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = accessSource.ReaderLock();
                acquiredAccess.Value = (token, false);
            }
            try
            {
                action();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="action"/> synchronously, acquiring the lock synchronously as a reader if necessary (may block)
        /// </summary>
        /// <param name="action">The void method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the action will be executed only if the lock is already acquired or can be acquired immediately</param>
        public void WithReaderLock(Action action, CancellationToken cancellationToken)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            var (token, _) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = accessSource.ReaderLock(cancellationToken);
                acquiredAccess.Value = (token, false);
            }
            try
            {
                action();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="func"/> synchronously and return its return value, acquiring the lock synchronously as a reader if necessary (may block)
        /// </summary>
        /// <param name="func">The method to execute</param>
        public T WithReaderLock<T>(Func<T> func)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));
            var (token, _) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = accessSource.ReaderLock();
                acquiredAccess.Value = (token, false);
            }
            try
            {
                return func();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="func"/> synchronously and return its return value, acquiring the lock synchronously as a reader if necessary (may block)
        /// </summary>
        /// <param name="func">The method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the method will be executed only if the lock is already acquired or can be acquired immediately</param>
        public T WithReaderLock<T>(Func<T> func, CancellationToken cancellationToken)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));
            var (token, _) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = accessSource.ReaderLock(cancellationToken);
                acquiredAccess.Value = (token, false);
            }
            try
            {
                return func();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="action"/> synchronously, acquiring the lock asynchronously as a reader if necessary
        /// </summary>
        /// <param name="action">The void action to execute</param>
        public async Task WithReaderLockAsync(Action action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            var (token, _) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = await accessSource.ReaderLockAsync().ConfigureAwait(false);
                acquiredAccess.Value = (token, false);
            }
            try
            {
                action();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="action"/> synchronously, acquiring the lock asynchronously as a reader if necessary
        /// </summary>
        /// <param name="action">The void action to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the action will be executed only if the lock is already acquired or can be acquired immediately</param>
        public async Task WithReaderLockAsync(Action action, CancellationToken cancellationToken)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            var (token, _) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = await accessSource.ReaderLockAsync(cancellationToken).ConfigureAwait(false);
                acquiredAccess.Value = (token, false);
            }
            try
            {
                action();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="func"/> synchronously and return its return value, acquiring the lock asynchronously as a reader if necessary
        /// </summary>
        /// <param name="func">The method to execute</param>
        public async Task<T> WithReaderLockAsync<T>(Func<T> func)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));
            var (token, _) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = await accessSource.ReaderLockAsync().ConfigureAwait(false);
                acquiredAccess.Value = (token, false);
            }
            try
            {
                return func();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="func"/> synchronously and return its return value, acquiring the lock asynchronously as a reader if necessary
        /// </summary>
        /// <param name="func">The method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the method will be executed only if the lock is already acquired or can be acquired immediately</param>
        public async Task<T> WithReaderLockAsync<T>(Func<T> func, CancellationToken cancellationToken)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));
            var (token, _) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = await accessSource.ReaderLockAsync(cancellationToken).ConfigureAwait(false);
                acquiredAccess.Value = (token, false);
            }
            try
            {
                return func();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="asyncAction"/> asynchronously, acquiring the lock asynchronously as a reader if necessary
        /// </summary>
        /// <param name="asyncAction">The asynchronous void method to execute</param>
        public async Task WithReaderLockAsync(Func<Task> asyncAction)
        {
            if (asyncAction is null)
                throw new ArgumentNullException(nameof(asyncAction));
            var (token, _) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = await accessSource.ReaderLockAsync().ConfigureAwait(false);
                acquiredAccess.Value = (token, false);
            }
            try
            {
                await asyncAction().ConfigureAwait(false);
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="asyncAction"/> asynchronously, acquiring the lock asynchronously as a reader if necessary
        /// </summary>
        /// <param name="asyncAction">The asynchronous void method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the method will be executed only if the lock is already acquired or can be acquired immediately</param>
        public async Task WithReaderLockAsync(Func<Task> asyncAction, CancellationToken cancellationToken)
        {
            if (asyncAction is null)
                throw new ArgumentNullException(nameof(asyncAction));
            var (token, _) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = await accessSource.ReaderLockAsync(cancellationToken).ConfigureAwait(false);
                acquiredAccess.Value = (token, false);
            }
            try
            {
                await asyncAction().ConfigureAwait(false);
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="asyncFunc"/> asynchronously and return its return value, acquiring the lock asynchronously as a reader if necessary
        /// </summary>
        /// <param name="asyncFunc">The asynchronous method to execute</param>
        public async Task<T> WithReaderLockAsync<T>(Func<Task<T>> asyncFunc)
        {
            if (asyncFunc is null)
                throw new ArgumentNullException(nameof(asyncFunc));
            var (token, _) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = await accessSource.ReaderLockAsync().ConfigureAwait(false);
                acquiredAccess.Value = (token, false);
            }
            try
            {
                return await asyncFunc().ConfigureAwait(false);
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="asyncFunc"/> asynchronously and return its return value, acquiring the lock asynchronously as a reader if necessary
        /// </summary>
        /// <param name="asyncFunc">The asynchronous method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the method will be executed only if the lock is already acquired or can be acquired immediately</param>
        public async Task<T> WithReaderLockAsync<T>(Func<Task<T>> asyncFunc, CancellationToken cancellationToken)
        {
            if (asyncFunc is null)
                throw new ArgumentNullException(nameof(asyncFunc));
            var (token, _) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = await accessSource.ReaderLockAsync(cancellationToken).ConfigureAwait(false);
                acquiredAccess.Value = (token, false);
            }
            try
            {
                return await asyncFunc().ConfigureAwait(false);
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="action"/> synchronously, acquiring the lock synchronously as a writer if necessary (may block)
        /// </summary>
        /// <param name="action">The void method to execute</param>
        /// <exception cref="LockEscalationException">This flow has already acquired this lock as a reader</exception>
        public void WithWriterLock(Action action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            var (token, isWriting) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = accessSource.ReaderLock();
                acquiredAccess.Value = (token, true);
            }
            else if (!isWriting)
                throw new LockEscalationException();
            try
            {
                action();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="action"/> synchronously, acquiring the lock synchronously as a writer if necessary (may block)
        /// </summary>
        /// <param name="action">The void method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the action will be executed only if the lock is already acquired or can be acquired immediately</param>
        /// <exception cref="LockEscalationException">This flow has already acquired this lock as a reader</exception>
        public void WithWriterLock(Action action, CancellationToken cancellationToken)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            var (token, isWriting) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = accessSource.ReaderLock(cancellationToken);
                acquiredAccess.Value = (token, true);
            }
            else if (!isWriting)
                throw new LockEscalationException();
            try
            {
                action();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="func"/> synchronously and return its return value, acquiring the lock synchronously as a writer if necessary (may block)
        /// </summary>
        /// <param name="func">The method to execute</param>
        /// <exception cref="LockEscalationException">This flow has already acquired this lock as a reader</exception>
        public T WithWriterLock<T>(Func<T> func)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));
            var (token, isWriting) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = accessSource.ReaderLock();
                acquiredAccess.Value = (token, true);
            }
            else if (!isWriting)
                throw new LockEscalationException();
            try
            {
                return func();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="func"/> synchronously and return its return value, acquiring the lock synchronously as a writer if necessary (may block)
        /// </summary>
        /// <param name="func">The method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the method will be executed only if the lock is already acquired or can be acquired immediately</param>
        /// <exception cref="LockEscalationException">This flow has already acquired this lock as a reader</exception>
        public T WithWriterLock<T>(Func<T> func, CancellationToken cancellationToken)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));
            var (token, isWriting) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = accessSource.ReaderLock(cancellationToken);
                acquiredAccess.Value = (token, true);
            }
            else if (!isWriting)
                throw new LockEscalationException();
            try
            {
                return func();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="action"/> synchronously, acquiring the lock asynchronously as a writer if necessary
        /// </summary>
        /// <param name="action">The void action to execute</param>
        /// <exception cref="LockEscalationException">This flow has already acquired this lock as a reader</exception>
        public async Task WithWriterLockAsync(Action action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            var (token, isWriting) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = await accessSource.ReaderLockAsync().ConfigureAwait(false);
                acquiredAccess.Value = (token, true);
            }
            else if (!isWriting)
                throw new LockEscalationException();
            try
            {
                action();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="action"/> synchronously, acquiring the lock asynchronously as a writer if necessary
        /// </summary>
        /// <param name="action">The void action to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the action will be executed only if the lock is already acquired or can be acquired immediately</param>
        /// <exception cref="LockEscalationException">This flow has already acquired this lock as a reader</exception>
        public async Task WithWriterLockAsync(Action action, CancellationToken cancellationToken)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            var (token, isWriting) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = await accessSource.ReaderLockAsync(cancellationToken).ConfigureAwait(false);
                acquiredAccess.Value = (token, true);
            }
            else if (!isWriting)
                throw new LockEscalationException();
            try
            {
                action();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="func"/> synchronously and return its return value, acquiring the lock asynchronously as a writer if necessary
        /// </summary>
        /// <param name="func">The method to execute</param>
        /// <exception cref="LockEscalationException">This flow has already acquired this lock as a reader</exception>
        public async Task<T> WithWriterLockAsync<T>(Func<T> func)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));
            var (token, isWriting) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = await accessSource.ReaderLockAsync().ConfigureAwait(false);
                acquiredAccess.Value = (token, true);
            }
            else if (!isWriting)
                throw new LockEscalationException();
            try
            {
                return func();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="func"/> synchronously and return its return value, acquiring the lock asynchronously as a writer if necessary
        /// </summary>
        /// <param name="func">The method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the method will be executed only if the lock is already acquired or can be acquired immediately</param>
        /// <exception cref="LockEscalationException">This flow has already acquired this lock as a reader</exception>
        public async Task<T> WithWriterLockAsync<T>(Func<T> func, CancellationToken cancellationToken)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));
            var (token, isWriting) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = await accessSource.ReaderLockAsync(cancellationToken).ConfigureAwait(false);
                acquiredAccess.Value = (token, true);
            }
            else if (!isWriting)
                throw new LockEscalationException();
            try
            {
                return func();
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="asyncAction"/> asynchronously, acquiring the lock asynchronously as a writer if necessary
        /// </summary>
        /// <param name="asyncAction">The asynchronous void method to execute</param>
        /// <exception cref="LockEscalationException">This flow has already acquired this lock as a reader</exception>
        public async Task WithWriterLockAsync(Func<Task> asyncAction)
        {
            if (asyncAction is null)
                throw new ArgumentNullException(nameof(asyncAction));
            var (token, isWriting) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = await accessSource.ReaderLockAsync().ConfigureAwait(false);
                acquiredAccess.Value = (token, true);
            }
            else if (!isWriting)
                throw new LockEscalationException();
            try
            {
                await asyncAction().ConfigureAwait(false);
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="asyncAction"/> asynchronously, acquiring the lock asynchronously as a writer if necessary
        /// </summary>
        /// <param name="asyncAction">The asynchronous void method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the method will be executed only if the lock is already acquired or can be acquired immediately</param>
        /// <exception cref="LockEscalationException">This flow has already acquired this lock as a reader</exception>
        public async Task WithWriterLockAsync(Func<Task> asyncAction, CancellationToken cancellationToken)
        {
            if (asyncAction is null)
                throw new ArgumentNullException(nameof(asyncAction));
            var (token, isWriting) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = await accessSource.ReaderLockAsync(cancellationToken).ConfigureAwait(false);
                acquiredAccess.Value = (token, true);
            }
            else if (!isWriting)
                throw new LockEscalationException();
            try
            {
                await asyncAction().ConfigureAwait(false);
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="asyncFunc"/> asynchronously and return its return value, acquiring the lock asynchronously as a writer if necessary
        /// </summary>
        /// <param name="asyncFunc">The asynchronous method to execute</param>
        /// <exception cref="LockEscalationException">This flow has already acquired this lock as a reader</exception>
        public async Task<T> WithWriterLockAsync<T>(Func<Task<T>> asyncFunc)
        {
            if (asyncFunc is null)
                throw new ArgumentNullException(nameof(asyncFunc));
            var (token, isWriting) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = await accessSource.ReaderLockAsync().ConfigureAwait(false);
                acquiredAccess.Value = (token, true);
            }
            else if (!isWriting)
                throw new LockEscalationException();
            try
            {
                return await asyncFunc().ConfigureAwait(false);
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="asyncFunc"/> asynchronously and return its return value, acquiring the lock asynchronously as a writer if necessary
        /// </summary>
        /// <param name="asyncFunc">The asynchronous method to execute</param>
        /// <param name="cancellationToken">If cancellation is requested, the method will be executed only if the lock is already acquired or can be acquired immediately</param>
        /// <exception cref="LockEscalationException">This flow has already acquired this lock as a reader</exception>
        public async Task<T> WithWriterLockAsync<T>(Func<Task<T>> asyncFunc, CancellationToken cancellationToken)
        {
            if (asyncFunc is null)
                throw new ArgumentNullException(nameof(asyncFunc));
            var (token, isWriting) = acquiredAccess.Value;
            bool lockAcquiredHere;
            if (lockAcquiredHere = token is null)
            {
                token = await accessSource.ReaderLockAsync(cancellationToken).ConfigureAwait(false);
                acquiredAccess.Value = (token, true);
            }
            else if (!isWriting)
                throw new LockEscalationException();
            try
            {
                return await asyncFunc().ConfigureAwait(false);
            }
            finally
            {
                if (lockAcquiredHere)
                {
                    acquiredAccess.Value = default;
                    token!.Dispose();
                }
            }
        }
    }
}
