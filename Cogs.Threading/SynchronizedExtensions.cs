using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        static readonly ThreadLocal<Stack<SynchronizationContext?>> threadLocalSynchronizationContextStack = new ThreadLocal<Stack<SynchronizationContext?>>(ThreadLocalSynchronizationContextStackValueFactory);

        /// <summary>
        /// Executes the specified <paramref name="action"/> on the specified <paramref name="synchronizationContext"/>
        /// </summary>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/></param>
        /// <param name="action">The <see cref="Action"/></param>
        [SuppressMessage("Code Analysis", "CA1508: Avoid dead conditional code", Justification = "The analyzer is mistaken")]
        public static void Execute(this SynchronizationContext? synchronizationContext, Action action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            var thisSynchronizationContext = SynchronizationContext.Current;
            if (synchronizationContext is null || thisSynchronizationContext == synchronizationContext || threadLocalSynchronizationContextStack.Value.Contains(thisSynchronizationContext))
            {
                action();
                return;
            }
            var thisSynchronizationContextStack = threadLocalSynchronizationContextStack.Value.ToImmutableList();
            ExceptionDispatchInfo? edi = default;
            synchronizationContext.Send(state =>
            {
                foreach (var synchronizationContext in thisSynchronizationContextStack)
                    threadLocalSynchronizationContextStack.Value.Push(synchronizationContext);
                threadLocalSynchronizationContextStack.Value.Push(thisSynchronizationContext);
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    edi = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    for (var i = 0; i <= thisSynchronizationContextStack.Count; ++i)
                        threadLocalSynchronizationContextStack.Value.Pop();
                }
            }, null);
            edi?.Throw();
        }

        /// <summary>
        /// Executes the specified <paramref name="action"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/>
        /// </summary>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="action">The <see cref="Action"/></param>
        public static void Execute(this ISynchronized? synchronizable, Action action) => Execute(synchronizable?.SynchronizationContext, action);

        /// <summary>
        /// Executes the specified <paramref name="func"/> on the <paramref name="synchronizationContext"/> and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="func"/></typeparam>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/></param>
        /// <param name="func">The <see cref="Func{TResult}"/></param>
        /// <returns>The result of <paramref name="func"/></returns>
        [SuppressMessage("Code Analysis", "CA1508: Avoid dead conditional code", Justification = "The analyzer is mistaken")]
        public static TResult Execute<TResult>(this SynchronizationContext? synchronizationContext, Func<TResult> func)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));
            var thisSynchronizationContext = SynchronizationContext.Current;
            if (synchronizationContext is null || thisSynchronizationContext == synchronizationContext || threadLocalSynchronizationContextStack.Value.Contains(thisSynchronizationContext))
                return func();
            TResult result = default;
            var thisSynchronizationContextStack = threadLocalSynchronizationContextStack.Value.ToImmutableList();
            ExceptionDispatchInfo? edi = default;
            synchronizationContext.Send(state =>
            {
                foreach (var synchronizationContext in thisSynchronizationContextStack)
                    threadLocalSynchronizationContextStack.Value.Push(synchronizationContext);
                threadLocalSynchronizationContextStack.Value.Push(thisSynchronizationContext);
                try
                {
                    result = func();
                }
                catch (Exception ex)
                {
                    edi = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    for (var i = 0; i <= thisSynchronizationContextStack.Count; ++i)
                        threadLocalSynchronizationContextStack.Value.Pop();
                }
            }, null);
            edi?.Throw();
            return result!; // result cannot be unexpectedly null here because it only is if func threw, and if func threw, then edi did also
        }

        /// <summary>
        /// Executes the specified <paramref name="func"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/> and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="func"/></typeparam>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="func">The <see cref="Func{TResult}"/></param>
        /// <returns>The result of <paramref name="func"/></returns>
        public static TResult Execute<TResult>(this ISynchronized? synchronizable, Func<TResult> func) => Execute(synchronizable?.SynchronizationContext, func);

        /// <summary>
        /// Executes the specified <paramref name="action"/> on the specified <paramref name="synchronizationContext"/>
        /// </summary>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/></param>
        /// <param name="action">The <see cref="Action"/></param>
        public static Task ExecuteAsync(this SynchronizationContext? synchronizationContext, Action action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            var thisSynchronizationContext = SynchronizationContext.Current;
            if (synchronizationContext is null || thisSynchronizationContext == synchronizationContext || threadLocalSynchronizationContextStack.Value.Contains(thisSynchronizationContext))
            {
                action();
                return Task.CompletedTask;
            }
            var thisSynchronizationContextStack = threadLocalSynchronizationContextStack.Value.ToImmutableList();
            var completion = new TaskCompletionSource<object>();
            synchronizationContext.Post(state =>
            {
                foreach (var synchronizationContext in thisSynchronizationContextStack)
                    threadLocalSynchronizationContextStack.Value.Push(synchronizationContext);
                threadLocalSynchronizationContextStack.Value.Push(thisSynchronizationContext);
                completion.AttemptSetResult(action);
                for (var i = 0; i <= thisSynchronizationContextStack.Count; ++i)
                    threadLocalSynchronizationContextStack.Value.Pop();
            }, null);
            return completion.Task;
        }

        /// <summary>
        /// Executes the specified <paramref name="action"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/>
        /// </summary>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="action">The <see cref="Action"/></param>
        public static Task ExecuteAsync(this ISynchronized? synchronizable, Action action) => ExecuteAsync(synchronizable?.SynchronizationContext, action);

        /// <summary>
        /// Executes the specified <paramref name="func"/> on the <paramref name="synchronizationContext"/> and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="func"/></typeparam>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/></param>
        /// <param name="func">The <see cref="Func{TResult}"/></param>
        /// <returns>The result of <paramref name="func"/></returns>
        public static Task<TResult> ExecuteAsync<TResult>(this SynchronizationContext? synchronizationContext, Func<TResult> func)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));
            var thisSynchronizationContext = SynchronizationContext.Current;
            if (synchronizationContext is null || thisSynchronizationContext == synchronizationContext || threadLocalSynchronizationContextStack.Value.Contains(thisSynchronizationContext))
                return Task.FromResult(func());
            var thisSynchronizationContextStack = threadLocalSynchronizationContextStack.Value.ToImmutableList();
            var completion = new TaskCompletionSource<TResult>();
            synchronizationContext.Post(state =>
            {
                foreach (var synchronizationContext in thisSynchronizationContextStack)
                    threadLocalSynchronizationContextStack.Value.Push(synchronizationContext);
                threadLocalSynchronizationContextStack.Value.Push(thisSynchronizationContext);
                completion.AttemptSetResult(func);
                for (var i = 0; i <= thisSynchronizationContextStack.Count; ++i)
                    threadLocalSynchronizationContextStack.Value.Pop();
            }, null);
            return completion.Task;
        }

        /// <summary>
        /// Executes the specified <paramref name="func"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/> and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="func"/></typeparam>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="func">The <see cref="Func{TResult}"/></param>
        /// <returns>The result of <paramref name="func"/></returns>
        public static Task<TResult> ExecuteAsync<TResult>(this ISynchronized? synchronizable, Func<TResult> func) => ExecuteAsync(synchronizable?.SynchronizationContext, func);

        /// <summary>
        /// Executes the specified <paramref name="asyncAction"/> on the specified <paramref name="synchronizationContext"/>
        /// </summary>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/></param>
        /// <param name="asyncAction">The <see cref="Func{Task}"/></param>
        public static async Task ExecuteAsync(this SynchronizationContext? synchronizationContext, Func<Task> asyncAction)
        {
            if (asyncAction is null)
                throw new ArgumentNullException(nameof(asyncAction));
            var thisSynchronizationContext = SynchronizationContext.Current;
            if (synchronizationContext is null || thisSynchronizationContext == synchronizationContext || threadLocalSynchronizationContextStack.Value.Contains(thisSynchronizationContext))
            {
                await asyncAction().ConfigureAwait(false);
                return;
            }
            var thisSynchronizationContextStack = threadLocalSynchronizationContextStack.Value.ToImmutableList();
            var completion = new TaskCompletionSource<object>();
            synchronizationContext.Post(async state =>
            {
                foreach (var synchronizationContext in thisSynchronizationContextStack)
                    threadLocalSynchronizationContextStack.Value.Push(synchronizationContext);
                threadLocalSynchronizationContextStack.Value.Push(thisSynchronizationContext);
                await completion.AttemptSetResultAsync(asyncAction).ConfigureAwait(false);
                for (var i = 0; i <= thisSynchronizationContextStack.Count; ++i)
                    threadLocalSynchronizationContextStack.Value.Pop();
            }, null);
            await completion.Task.ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the specified <paramref name="asyncAction"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/>
        /// </summary>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="asyncAction">The <see cref="Func{Task}"/></param>
        public static Task ExecuteAsync(this ISynchronized? synchronizable, Func<Task> asyncAction) => ExecuteAsync(synchronizable?.SynchronizationContext, asyncAction);

        /// <summary>
        /// Executes the specified <paramref name="asyncFunc"/> on the specified <paramref name="synchronizationContext"/> and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="asyncFunc"/></typeparam>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/></param>
        /// <param name="asyncFunc">The <see cref="Func{Task}"/> that returns a value</param>
        /// <returns>The result of <paramref name="asyncFunc"/></returns>
        public static async Task<TResult> ExecuteAsync<TResult>(this SynchronizationContext? synchronizationContext, Func<Task<TResult>> asyncFunc)
        {
            if (asyncFunc is null)
                throw new ArgumentNullException(nameof(asyncFunc));
            var thisSynchronizationContext = SynchronizationContext.Current;
            if (synchronizationContext is null || thisSynchronizationContext == synchronizationContext || threadLocalSynchronizationContextStack.Value.Contains(thisSynchronizationContext))
                return await asyncFunc().ConfigureAwait(false);
            var thisSynchronizationContextStack = threadLocalSynchronizationContextStack.Value.ToImmutableList();
            var completion = new TaskCompletionSource<TResult>();
            synchronizationContext.Post(async state =>
            {
                foreach (var synchronizationContext in thisSynchronizationContextStack)
                    threadLocalSynchronizationContextStack.Value.Push(synchronizationContext);
                threadLocalSynchronizationContextStack.Value.Push(thisSynchronizationContext);
                await completion.AttemptSetResultAsync(asyncFunc).ConfigureAwait(false);
                for (var i = 0; i <= thisSynchronizationContextStack.Count; ++i)
                    threadLocalSynchronizationContextStack.Value.Pop();
            }, null);
            return await completion.Task.ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the specified <paramref name="asyncFunc"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/> and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="asyncFunc"/></typeparam>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="asyncFunc">The <see cref="Func{Task}"/> that returns a value</param>
        /// <returns>The result of <paramref name="asyncFunc"/></returns>
        public static Task<TResult> ExecuteAsync<TResult>(this ISynchronized? synchronizable, Func<Task<TResult>> asyncFunc) => ExecuteAsync(synchronizable?.SynchronizationContext, asyncFunc);

        /// <summary>
        /// Executes the specified <paramref name="action"/> on the specified <see cref="SynchronizationContext"/> (or <see cref="SynchronizationContext.Current"/> if that is <c>null</c>, or <see cref="Synchronization.DefaultSynchronizationContext"/> if both are <c>null</c>)
        /// </summary>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/></param>
        /// <param name="action">The <see cref="Action"/></param>
        public static void SequentialExecute(this SynchronizationContext? synchronizationContext, Action action) => Execute(synchronizationContext ?? SynchronizationContext.Current ?? Synchronization.DefaultSynchronizationContext, action);

        /// <summary>
        /// Executes the specified <paramref name="func"/> on the specified <see cref="SynchronizationContext"/> (or <see cref="SynchronizationContext.Current"/> if that is <c>null</c>, or <see cref="Synchronization.DefaultSynchronizationContext"/> if both are <c>null</c>) and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="func"/></typeparam>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/></param>
        /// <param name="func">The <see cref="Func{TResult}"/></param>
        /// <returns>The result of <paramref name="func"/></returns>
        public static TResult SequentialExecute<TResult>(this SynchronizationContext? synchronizationContext, Func<TResult> func) => Execute(synchronizationContext ?? SynchronizationContext.Current ?? Synchronization.DefaultSynchronizationContext, func);

        /// <summary>
        /// Executes the specified <paramref name="action"/> on the specified <see cref="SynchronizationContext"/> (or <see cref="SynchronizationContext.Current"/> if that is <c>null</c>, or <see cref="Synchronization.DefaultSynchronizationContext"/> if both are <c>null</c>)
        /// </summary>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/></param>
        /// <param name="action">The <see cref="Action"/></param>
        public static Task SequentialExecuteAsync(this SynchronizationContext? synchronizationContext, Action action) => ExecuteAsync(synchronizationContext ?? SynchronizationContext.Current ?? Synchronization.DefaultSynchronizationContext, action);

        /// <summary>
        /// Executes the specified <paramref name="func"/> on the specified <see cref="SynchronizationContext"/> (or <see cref="SynchronizationContext.Current"/> if that is <c>null</c>, or <see cref="Synchronization.DefaultSynchronizationContext"/> if both are <c>null</c>) and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="func"/></typeparam>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/></param>
        /// <param name="func">The <see cref="Func{TResult}"/></param>
        /// <returns>The result of <paramref name="func"/></returns>
        public static Task<TResult> SequentialExecuteAsync<TResult>(this SynchronizationContext? synchronizationContext, Func<TResult> func) => ExecuteAsync(synchronizationContext ?? SynchronizationContext.Current ?? Synchronization.DefaultSynchronizationContext, func);

        /// <summary>
        /// Executes the specified <paramref name="asyncAction"/> on the specified <see cref="SynchronizationContext"/> (or <see cref="SynchronizationContext.Current"/> if that is <c>null</c>, or <see cref="Synchronization.DefaultSynchronizationContext"/> if both are <c>null</c>)
        /// </summary>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/></param>
        /// <param name="asyncAction">The <see cref="Func{Task}"/></param>
        public static Task SequentialExecuteAsync(this SynchronizationContext? synchronizationContext, Func<Task> asyncAction) => ExecuteAsync(synchronizationContext ?? SynchronizationContext.Current ?? Synchronization.DefaultSynchronizationContext, asyncAction);

        /// <summary>
        /// Executes the specified <paramref name="asyncFunc"/> on the specified <see cref="SynchronizationContext"/> (or <see cref="SynchronizationContext.Current"/> if that is <c>null</c>, or <see cref="Synchronization.DefaultSynchronizationContext"/> if both are <c>null</c>) and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="asyncFunc"/></typeparam>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/></param>
        /// <param name="asyncFunc">The <see cref="Func{Task}"/> that returns a value</param>
        /// <returns>The result of <paramref name="asyncFunc"/></returns>
        public static Task<TResult> SequentialExecuteAsync<TResult>(this SynchronizationContext? synchronizationContext, Func<Task<TResult>> asyncFunc) => ExecuteAsync(synchronizationContext ?? SynchronizationContext.Current ?? Synchronization.DefaultSynchronizationContext, asyncFunc);

        /// <summary>
        /// Executes the specified <paramref name="action"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/> (or <see cref="SynchronizationContext.Current"/> if that is <c>null</c>, or <see cref="Synchronization.DefaultSynchronizationContext"/> if both are <c>null</c>)
        /// </summary>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="action">The <see cref="Action"/></param>
        public static void SequentialExecute(this ISynchronized? synchronizable, Action action) => SequentialExecute(synchronizable?.SynchronizationContext, action);

        /// <summary>
        /// Executes the specified <paramref name="func"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/> (or <see cref="SynchronizationContext.Current"/> if that is <c>null</c>, or <see cref="Synchronization.DefaultSynchronizationContext"/> if both are <c>null</c>) and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="func"/></typeparam>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="func">The <see cref="Func{TResult}"/></param>
        /// <returns>The result of <paramref name="func"/></returns>
        public static TResult SequentialExecute<TResult>(this ISynchronized? synchronizable, Func<TResult> func) => SequentialExecute(synchronizable?.SynchronizationContext, func);

        /// <summary>
        /// Executes the specified <paramref name="action"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/> (or <see cref="SynchronizationContext.Current"/> if that is <c>null</c>, or <see cref="Synchronization.DefaultSynchronizationContext"/> if both are <c>null</c>)
        /// </summary>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="action">The <see cref="Action"/></param>
        public static Task SequentialExecuteAsync(this ISynchronized? synchronizable, Action action) => SequentialExecuteAsync(synchronizable?.SynchronizationContext, action);

        /// <summary>
        /// Executes the specified <paramref name="func"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/> (or <see cref="SynchronizationContext.Current"/> if that is <c>null</c>, or <see cref="Synchronization.DefaultSynchronizationContext"/> if both are <c>null</c>) and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="func"/></typeparam>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="func">The <see cref="Func{TResult}"/></param>
        /// <returns>The result of <paramref name="func"/></returns>
        public static Task<TResult> SequentialExecuteAsync<TResult>(this ISynchronized? synchronizable, Func<TResult> func) => SequentialExecuteAsync(synchronizable?.SynchronizationContext, func);

        /// <summary>
        /// Executes the specified <paramref name="asyncAction"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/> (or <see cref="SynchronizationContext.Current"/> if that is <c>null</c>, or <see cref="Synchronization.DefaultSynchronizationContext"/> if both are <c>null</c>)
        /// </summary>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="asyncAction">The <see cref="Func{Task}"/></param>
        public static Task SequentialExecuteAsync(this ISynchronized? synchronizable, Func<Task> asyncAction) => SequentialExecuteAsync(synchronizable?.SynchronizationContext, asyncAction);

        /// <summary>
        /// Executes the specified <paramref name="asyncFunc"/> on the <see cref="ISynchronized.SynchronizationContext"/> of the specified <paramref name="synchronizable"/> (or <see cref="SynchronizationContext.Current"/> if that is <c>null</c>, or <see cref="Synchronization.DefaultSynchronizationContext"/> if both are <c>null</c>) and returns the result
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="asyncFunc"/></typeparam>
        /// <param name="synchronizable">The <see cref="ISynchronized"/></param>
        /// <param name="asyncFunc">The <see cref="Func{Task}"/> that returns a value</param>
        /// <returns>The result of <paramref name="asyncFunc"/></returns>
        public static Task<TResult> SequentialExecuteAsync<TResult>(this ISynchronized? synchronizable, Func<Task<TResult>> asyncFunc) => SequentialExecuteAsync(synchronizable?.SynchronizationContext, asyncFunc);

        static Stack<SynchronizationContext?> ThreadLocalSynchronizationContextStackValueFactory() => new Stack<SynchronizationContext?>();
    }
}
