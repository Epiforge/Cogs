using System;
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
        /// Gets the value passed to the results of <see cref="TaskCompletionSource{TResult}"/> against which void methods are invoked
        /// </summary>
        public static object AttemptSetResultDefaultObject { get; } = new object();

        /// <summary>
        /// Invokes a void method and then <see cref="TaskCompletionSource{TResult}.SetResult(TResult)"/> with the value of <see cref="AttemptSetResultDefaultObject"/> if it succeeds; otherwise invokes <see cref="TaskCompletionSource{TResult}.SetException(Exception)"/> with the exception thrown by the method
        /// </summary>
        /// <param name="taskCompletionSource">The task completion source</param>
        /// <param name="action">The void method to invoke</param>
        public static void AttemptSetResult(this TaskCompletionSource<object> taskCompletionSource, Action action) => AttemptSetResult(taskCompletionSource, action, AttemptSetResultDefaultObject);

        /// <summary>
        /// Invokes a void method and then <see cref="TaskCompletionSource{TResult}.SetResult(TResult)"/> with the default value of <typeparamref name="TResult"/> if it succeeds; otherwise invokes <see cref="TaskCompletionSource{TResult}.SetException(Exception)"/> with the exception thrown by the method
        /// </summary>
        /// <typeparam name="TResult">The generic type argument of the task completion source</typeparam>
        /// <param name="taskCompletionSource">The task completion source</param>
        /// <param name="action">The void method to invoke</param>
        [Obsolete("This method sets the result of the task completion source with a default value, which may be null, despite the fact that produces task may not indicate the possibility of a null value; use the AttemptSetResult<TResult>(TaskCompletionSource<TResult>, Action, TResult) overload instead")]
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
        /// Invokes a void method and then <see cref="TaskCompletionSource{TResult}.SetResult(TResult)"/> with the default value of <typeparamref name="TResult"/> if it succeeds; otherwise invokes <see cref="TaskCompletionSource{TResult}.SetException(Exception)"/> with the exception thrown by the method
        /// </summary>
        /// <typeparam name="TResult">The generic type argument of the task completion source</typeparam>
        /// <param name="taskCompletionSource">The task completion source</param>
        /// <param name="action">The void method to invoke</param>
        /// <param name="result">The result to set for <paramref name="taskCompletionSource"/></param>
        public static void AttemptSetResult<TResult>(this TaskCompletionSource<TResult> taskCompletionSource, Action action, TResult result)
        {
            try
            {
                action();
                taskCompletionSource.SetResult(result);
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
        /// Invokes a void async method and then <see cref="TaskCompletionSource{TResult}.SetResult(TResult)"/> with the value of <see cref="AttemptSetResultDefaultObject"/> if it succeeds; otherwise invokes <see cref="TaskCompletionSource{TResult}.SetException(Exception)"/> with the exception thrown by the method
        /// </summary>
        /// <param name="taskCompletionSource">The task completion source</param>
        /// <param name="asyncAction">The void async method to invoke</param>
        public static Task AttemptSetResultAsync(this TaskCompletionSource<object> taskCompletionSource, Func<Task> asyncAction) => AttemptSetResultAsync(taskCompletionSource, asyncAction, AttemptSetResultDefaultObject);

        /// <summary>
        /// Invokes a void async method and then <see cref="TaskCompletionSource{TResult}.SetResult(TResult)"/> with the default value of <typeparamref name="TResult"/> if it succeeds; otherwise invokes <see cref="TaskCompletionSource{TResult}.SetException(Exception)"/> with the exception thrown by the method
        /// </summary>
        /// <typeparam name="TResult">The generic type argument of the task completion source</typeparam>
        /// <param name="taskCompletionSource">The task completion source</param>
        /// <param name="asyncAction">The void async method to invoke</param>
        [Obsolete("This method sets the result of the task completion source with a default value, which may be null, despite the fact that produces task may not indicate the possibility of a null value; use the AttemptSetResult<TResult>(TaskCompletionSource<TResult>, Action, TResult) overload instead")]
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
        /// Invokes a void async method and then <see cref="TaskCompletionSource{TResult}.SetResult(TResult)"/> with the default value of <typeparamref name="TResult"/> if it succeeds; otherwise invokes <see cref="TaskCompletionSource{TResult}.SetException(Exception)"/> with the exception thrown by the method
        /// </summary>
        /// <typeparam name="TResult">The generic type argument of the task completion source</typeparam>
        /// <param name="taskCompletionSource">The task completion source</param>
        /// <param name="asyncAction">The void async method to invoke</param>
        /// <param name="result">The result to set for <paramref name="taskCompletionSource"/></param>
        public static async Task AttemptSetResultAsync<TResult>(this TaskCompletionSource<TResult> taskCompletionSource, Func<Task> asyncAction, TResult result)
        {
            try
            {
                await asyncAction().ConfigureAwait(false);
                taskCompletionSource.SetResult(result);
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
