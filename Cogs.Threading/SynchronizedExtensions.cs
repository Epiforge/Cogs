using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Cogs.Threading
{
    /// <summary>
    /// Provides extensions for executing operations with instances of <see cref="SynchronizationContext"/> and <see cref="ISynchronized"/>
    /// </summary>
    public static class SynchronizedExtensions
    {
        /// <summary>
        /// Executes the specified <paramref name="action"/> on the specified <paramref name="synchronizationContext"/>
        /// </summary>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/></param>
        /// <param name="action">The <see cref="Action"/></param>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static void Execute(this SynchronizationContext synchronizationContext, Action action)
        {
            if (synchronizationContext == null || SynchronizationContext.Current == synchronizationContext)
            {
                action();
                return;
            }
            ExceptionDispatchInfo? edi = default;
            synchronizationContext.Send(state =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    edi = ExceptionDispatchInfo.Capture(ex);
                }
            }, null);
            edi?.Throw();
        }

        /// <summary>
        /// Executes the specified <paramref name="action"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/>
        /// </summary>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="action">The <see cref="Action"/></param>
        public static void Execute(this ISynchronized synchronizable, Action action) => Execute(synchronizable.SynchronizationContext, action);

        /// <summary>
        /// Executes the specified <paramref name="func"/> on the <paramref name="synchronizationContext"/> and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="func"/></typeparam>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/></param>
        /// <param name="func">The <see cref="Func{TResult}"/></param>
        /// <returns>The result of <paramref name="func"/></returns>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        [return: MaybeNull]
        public static TResult Execute<TResult>(this SynchronizationContext synchronizationContext, Func<TResult> func)
        {
            if (synchronizationContext == null || SynchronizationContext.Current == synchronizationContext)
                return func();
            TResult result = default!;
            ExceptionDispatchInfo? edi = default;
            synchronizationContext.Send(state =>
            {
                try
                {
                    result = func();
                }
                catch (Exception ex)
                {
                    edi = ExceptionDispatchInfo.Capture(ex);
                }
            }, null);
            edi?.Throw();
            return result;
        }

        /// <summary>
        /// Executes the specified <paramref name="func"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/> and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="func"/></typeparam>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="func">The <see cref="Func{TResult}"/></param>
        /// <returns>The result of <paramref name="func"/></returns>
        [return: MaybeNull]
        public static TResult Execute<TResult>(this ISynchronized synchronizable, Func<TResult> func) => Execute(synchronizable.SynchronizationContext, func);

        /// <summary>
        /// Executes the specified <paramref name="action"/> on the specified <paramref name="synchronizationContext"/>
        /// </summary>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/></param>
        /// <param name="action">The <see cref="Action"/></param>
        public static Task ExecuteAsync(this SynchronizationContext synchronizationContext, Action action)
        {
            if (synchronizationContext == null || SynchronizationContext.Current == synchronizationContext)
            {
                action();
                return Task.CompletedTask;
            }
            var completion = new TaskCompletionSource<object?>();
            synchronizationContext.Post(state => completion.AttemptSetResult(action), null);
            return completion.Task;
        }

        /// <summary>
        /// Executes the specified <paramref name="action"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/>
        /// </summary>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="action">The <see cref="Action"/></param>
        public static Task ExecuteAsync(this ISynchronized synchronizable, Action action) => ExecuteAsync(synchronizable.SynchronizationContext, action);

        /// <summary>
        /// Executes the specified <paramref name="func"/> on the <paramref name="synchronizationContext"/> and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="func"/></typeparam>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/></param>
        /// <param name="func">The <see cref="Func{TResult}"/></param>
        /// <returns>The result of <paramref name="func"/></returns>
        public static Task<TResult> ExecuteAsync<TResult>(this SynchronizationContext synchronizationContext, Func<TResult> func)
        {
            if (synchronizationContext == null || SynchronizationContext.Current == synchronizationContext)
                return Task.FromResult(func());
            var completion = new TaskCompletionSource<TResult>();
            synchronizationContext.Post(state => completion.AttemptSetResult(func), null);
            return completion.Task;
        }

        /// <summary>
        /// Executes the specified <paramref name="func"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/> and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="func"/></typeparam>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="func">The <see cref="Func{TResult}"/></param>
        /// <returns>The result of <paramref name="func"/></returns>
        public static Task<TResult> ExecuteAsync<TResult>(this ISynchronized synchronizable, Func<TResult> func) => ExecuteAsync(synchronizable.SynchronizationContext, func);

        /// <summary>
        /// Executes the specified <paramref name="asyncAction"/> on the specified <paramref name="synchronizationContext"/>
        /// </summary>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/></param>
        /// <param name="asyncAction">The <see cref="Func{Task}"/></param>
        public static async Task ExecuteAsync(this SynchronizationContext synchronizationContext, Func<Task> asyncAction)
        {
            if (synchronizationContext == null || SynchronizationContext.Current == synchronizationContext)
            {
                await asyncAction().ConfigureAwait(false);
                return;
            }
            var completion = new TaskCompletionSource<object?>();
            synchronizationContext.Post(async state => await completion.AttemptSetResultAsync(asyncAction).ConfigureAwait(false), null);
            await completion.Task.ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the specified <paramref name="asyncAction"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/>
        /// </summary>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="asyncAction">The <see cref="Func{Task}"/></param>
        public static Task ExecuteAsync(this ISynchronized synchronizable, Func<Task> asyncAction) => ExecuteAsync(synchronizable.SynchronizationContext, asyncAction);

        /// <summary>
        /// Executes the specified <paramref name="asyncFunc"/> on the specified <paramref name="synchronizationContext"/> and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="asyncFunc"/></typeparam>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/></param>
        /// <param name="asyncFunc">The <see cref="Func{Task}"/> that returns a value</param>
        /// <returns>The result of <paramref name="asyncFunc"/></returns>
        public static async Task<TResult> ExecuteAsync<TResult>(this SynchronizationContext synchronizationContext, Func<Task<TResult>> asyncFunc)
        {
            if (synchronizationContext == null || SynchronizationContext.Current == synchronizationContext)
                return await asyncFunc().ConfigureAwait(false);
            var completion = new TaskCompletionSource<TResult>();
            synchronizationContext.Post(async state => await completion.AttemptSetResultAsync(asyncFunc).ConfigureAwait(false), null);
            return await completion.Task.ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the specified <paramref name="asyncFunc"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/> and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="asyncFunc"/></typeparam>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="asyncFunc">The <see cref="Func{Task}"/> that returns a value</param>
        /// <returns>The result of <paramref name="asyncFunc"/></returns>
        public static Task<TResult> ExecuteAsync<TResult>(this ISynchronized synchronizable, Func<Task<TResult>> asyncFunc) => ExecuteAsync(synchronizable.SynchronizationContext, asyncFunc);

        /// <summary>
        /// Executes the specified <paramref name="action"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>)
        /// </summary>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="action">The <see cref="Action"/></param>
        public static void SequentialExecute(this ISynchronized synchronizable, Action action) => Execute(synchronizable?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext, action);

        /// <summary>
        /// Executes the specified <paramref name="func"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>) and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="func"/></typeparam>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="func">The <see cref="Func{TResult}"/></param>
        /// <returns>The result of <paramref name="func"/></returns>
        [return: MaybeNull]
        public static TResult SequentialExecute<TResult>(this ISynchronized synchronizable, Func<TResult> func) => Execute(synchronizable?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext, func);

        /// <summary>
        /// Executes the specified <paramref name="action"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>)
        /// </summary>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="action">The <see cref="Action"/></param>
        public static Task SequentialExecuteAsync(this ISynchronized synchronizable, Action action) => ExecuteAsync(synchronizable?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext, action);

        /// <summary>
        /// Executes the specified <paramref name="func"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>) and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="func"/></typeparam>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="func">The <see cref="Func{TResult}"/></param>
        /// <returns>The result of <paramref name="func"/></returns>
        public static Task<TResult> SequentialExecuteAsync<TResult>(this ISynchronized synchronizable, Func<TResult> func) => ExecuteAsync(synchronizable?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext, func);

        /// <summary>
        /// Executes the specified <paramref name="asyncAction"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>)
        /// </summary>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="asyncAction">The <see cref="Func{Task}"/></param>
        public static Task SequentialExecuteAsync(this ISynchronized synchronizable, Func<Task> asyncAction) => ExecuteAsync(synchronizable?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext, asyncAction);

        /// <summary>
        /// Executes the specified <paramref name="asyncFunc"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>) and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="asyncFunc"/></typeparam>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="asyncFunc">The <see cref="Func{Task}"/> that returns a value</param>
        /// <returns>The result of <paramref name="asyncFunc"/></returns>
        public static Task<TResult> SequentialExecuteAsync<TResult>(this ISynchronized synchronizable, Func<Task<TResult>> asyncFunc) => ExecuteAsync(synchronizable?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext, asyncFunc);
    }
}
