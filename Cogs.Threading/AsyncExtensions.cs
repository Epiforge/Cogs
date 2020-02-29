using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Cogs.Threading
{
    /// <summary>
    /// Provides extensions for dealing with async utilities like <see cref="TaskCompletionSource{TResult}"/>
    /// </summary>
    public static class AsyncExtensions
    {
        #region TaskCompletionSource

        /// <summary>
        /// Invokes a void method and then <see cref="TaskCompletionSource{TResult}.SetResult(TResult)"/> with the default value of <typeparamref name="TResult"/> if it succeeds; otherwise invokes <see cref="TaskCompletionSource{TResult}.SetException(Exception)"/> with the exception thrown by the method
        /// </summary>
        /// <typeparam name="TResult">The generic type argument of the task completion source</typeparam>
        /// <param name="taskCompletionSource">The task completion source</param>
        /// <param name="action">The void method to invoke</param>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static void AttemptSetResult<TResult>(this TaskCompletionSource<TResult> taskCompletionSource, Action action)
        {
            try
            {
                action();
                taskCompletionSource.SetResult(default!);
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
            }
        }

        /// <summary>
        /// Invokes a method with a return value and then <see cref="TaskCompletionSource{TResult}.SetResult(TResult)"/> with what it returned; otherwise invokes <see cref="TaskCompletionSource{TResult}.SetException(Exception)"/> with the exception thrown by the method
        /// </summary>
        /// <typeparam name="TResult">The generic type argument of the task completion source</typeparam>
        /// <param name="taskCompletionSource">The task completion source</param>
        /// <param name="func">The method with a return value to invoke</param>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static void AttemptSetResult<TResult>(this TaskCompletionSource<TResult> taskCompletionSource, Func<TResult> func)
        {
            try
            {
                taskCompletionSource.SetResult(func());
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
            }
        }

        /// <summary>
        /// Invokes a void async method and then <see cref="TaskCompletionSource{TResult}.SetResult(TResult)"/> with the default value of <typeparamref name="TResult"/> if it succeeds; otherwise invokes <see cref="TaskCompletionSource{TResult}.SetException(Exception)"/> with the exception thrown by the method
        /// </summary>
        /// <typeparam name="TResult">The generic type argument of the task completion source</typeparam>
        /// <param name="taskCompletionSource">The task completion source</param>
        /// <param name="asyncAction">The void async method to invoke</param>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static async Task AttemptSetResultAsync<TResult>(this TaskCompletionSource<TResult> taskCompletionSource, Func<Task> asyncAction)
        {
            try
            {
                await asyncAction().ConfigureAwait(false);
                taskCompletionSource.SetResult(default!);
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
            }
        }

        /// <summary>
        /// Invokes an async method with a return value and then <see cref="TaskCompletionSource{TResult}.SetResult(TResult)"/> with what it returned; otherwise invokes <see cref="TaskCompletionSource{TResult}.SetException(Exception)"/> with the exception thrown by the method
        /// </summary>
        /// <typeparam name="TResult">The generic type argument of the task completion source</typeparam>
        /// <param name="taskCompletionSource">The task completion source</param>
        /// <param name="asyncFunc">The async method with a return value to invoke</param>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static async Task AttemptSetResultAsync<TResult>(this TaskCompletionSource<TResult> taskCompletionSource, Func<Task<TResult>> asyncFunc)
        {
            try
            {
                taskCompletionSource.SetResult(await asyncFunc().ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
            }
        }

        /// <summary>
        /// Invokes a void method and then <see cref="TaskCompletionSource{TResult}.TrySetResult(TResult)"/> with the default value of <typeparamref name="TResult"/> if it succeeds, returning the result; otherwise invokes <see cref="TaskCompletionSource{TResult}.TrySetException(Exception)"/> with the exception thrown by the method, returning the result
        /// </summary>
        /// <typeparam name="TResult">The generic type argument of the task completion source</typeparam>
        /// <param name="taskCompletionSource">The task completion source</param>
        /// <param name="action">The void method to invoke</param>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static bool AttemptTrySetResult<TResult>(this TaskCompletionSource<TResult?> taskCompletionSource, Action action) where TResult : class
        {
            try
            {
                action();
                return taskCompletionSource.TrySetResult(default);
            }
            catch (Exception ex)
            {
                return taskCompletionSource.TrySetException(ex);
            }
        }

        /// <summary>
        /// Invokes a method with a return value and then <see cref="TaskCompletionSource{TResult}.TrySetResult(TResult)"/> with what it returned, returning the result; otherwise invokes <see cref="TaskCompletionSource{TResult}.TrySetException(Exception)"/> with the exception thrown by the method, returning the result
        /// </summary>
        /// <typeparam name="TResult">The generic type argument of the task completion source</typeparam>
        /// <param name="taskCompletionSource">The task completion source</param>
        /// <param name="func">The method with a return value to invoke</param>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static bool AttemptTrySetResult<TResult>(this TaskCompletionSource<TResult> taskCompletionSource, Func<TResult> func)
        {
            try
            {
                return taskCompletionSource.TrySetResult(func());
            }
            catch (Exception ex)
            {
                return taskCompletionSource.TrySetException(ex);
            }
        }

        /// <summary>
        /// Invokes a void async method and then <see cref="TaskCompletionSource{TResult}.TrySetResult(TResult)"/> with the default value of <typeparamref name="TResult"/> if it succeeds, returning the result; otherwise invokes <see cref="TaskCompletionSource{TResult}.TrySetException(Exception)"/> with the exception thrown by the method, returning the result
        /// </summary>
        /// <typeparam name="TResult">The generic type argument of the task completion source</typeparam>
        /// <param name="taskCompletionSource">The task completion source</param>
        /// <param name="asyncAction">The void async method to invoke</param>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static async Task<bool> AttemptTrySetResultAsync<TResult>(this TaskCompletionSource<TResult?> taskCompletionSource, Func<Task> asyncAction) where TResult : class
        {
            try
            {
                await asyncAction().ConfigureAwait(false);
                return taskCompletionSource.TrySetResult(default);
            }
            catch (Exception ex)
            {
                return taskCompletionSource.TrySetException(ex);
            }
        }

        /// <summary>
        /// Invokes an async method with a return value and then <see cref="TaskCompletionSource{TResult}.TrySetResult(TResult)"/> with what it returned, returning the result; otherwise invokes <see cref="TaskCompletionSource{TResult}.TrySetException(Exception)"/> with the exception thrown by the method, returning the result
        /// </summary>
        /// <typeparam name="TResult">The generic type argument of the task completion source</typeparam>
        /// <param name="taskCompletionSource">The task completion source</param>
        /// <param name="asyncFunc">The async method with a return value to invoke</param>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static async Task<bool> AttemptTrySetResultAsync<TResult>(this TaskCompletionSource<TResult> taskCompletionSource, Func<Task<TResult>> asyncFunc)
        {
            try
            {
                return taskCompletionSource.TrySetResult(await asyncFunc().ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                return taskCompletionSource.TrySetException(ex);
            }
        }

        #endregion TaskCompletionSource
    }
}
