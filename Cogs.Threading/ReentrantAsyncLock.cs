namespace Cogs.Threading;

/// <summary>
/// Creates a new async-compatible mutual exclusion lock that allows reentrance
/// </summary>
public sealed class ReentrantAsyncLock
{
    /// <summary>
    /// Creates an instance of <see cref="ReentrantAsyncLock"/>
    /// </summary>
    public ReentrantAsyncLock()
    {
        rootSemaphore = new(1);
        semaphore = new();
    }

    readonly SemaphoreSlim rootSemaphore;
    readonly AsyncLocal<SemaphoreSlim> semaphore;

    /// <summary>
    /// Execute <paramref name="action"/> synchronously, acquiring the lock synchronously if necessary (may block)
    /// </summary>
    /// <param name="action">The void method to execute</param>
    public void WithLock(Action action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));
        var lockSemaphore = semaphore.Value ?? rootSemaphore;
        lockSemaphore.Wait();
        using var methodSemaphore = new SemaphoreSlim(1);
        semaphore.Value = methodSemaphore;
        try
        {
            action();
        }
        finally
        {
            Debug.Assert(methodSemaphore == semaphore.Value);
            methodSemaphore.Wait();
            semaphore.Value = lockSemaphore;
            lockSemaphore.Release();
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
        var lockSemaphore = semaphore.Value ?? rootSemaphore;
        if (!lockSemaphore.Wait(0, CancellationToken.None))
            lockSemaphore.Wait(cancellationToken);
        using var methodSemaphore = new SemaphoreSlim(1);
        semaphore.Value = methodSemaphore;
        try
        {
            action();
        }
        finally
        {
            Debug.Assert(methodSemaphore == semaphore.Value);
            methodSemaphore.Wait(CancellationToken.None);
            semaphore.Value = lockSemaphore;
            lockSemaphore.Release();
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
        var lockSemaphore = semaphore.Value ?? rootSemaphore;
        lockSemaphore.Wait();
        using var methodSemaphore = new SemaphoreSlim(1);
        semaphore.Value = methodSemaphore;
        try
        {
            return func();
        }
        finally
        {
            Debug.Assert(methodSemaphore == semaphore.Value);
            methodSemaphore.Wait();
            semaphore.Value = lockSemaphore;
            lockSemaphore.Release();
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
        var lockSemaphore = semaphore.Value ?? rootSemaphore;
        if (!lockSemaphore.Wait(0, CancellationToken.None))
            lockSemaphore.Wait(cancellationToken);
        using var methodSemaphore = new SemaphoreSlim(1);
        semaphore.Value = methodSemaphore;
        try
        {
            return func();
        }
        finally
        {
            Debug.Assert(methodSemaphore == semaphore.Value);
            methodSemaphore.Wait(CancellationToken.None);
            semaphore.Value = lockSemaphore;
            lockSemaphore.Release();
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
        var lockSemaphore = semaphore.Value ?? rootSemaphore;
        await lockSemaphore.WaitAsync().ConfigureAwait(false);
        using var methodSemaphore = new SemaphoreSlim(1);
        semaphore.Value = methodSemaphore;
        try
        {
            action();
        }
        finally
        {
            Debug.Assert(methodSemaphore == semaphore.Value);
            await methodSemaphore.WaitAsync().ConfigureAwait(false);
            semaphore.Value = lockSemaphore;
            lockSemaphore.Release();
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
        var lockSemaphore = semaphore.Value ?? rootSemaphore;
        if (!await lockSemaphore.WaitAsync(0, CancellationToken.None).ConfigureAwait(false))
            await lockSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        using var methodSemaphore = new SemaphoreSlim(1);
        semaphore.Value = methodSemaphore;
        try
        {
            action();
        }
        finally
        {
            Debug.Assert(methodSemaphore == semaphore.Value);
            await methodSemaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            semaphore.Value = lockSemaphore;
            lockSemaphore.Release();
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
        var lockSemaphore = semaphore.Value ?? rootSemaphore;
        await lockSemaphore.WaitAsync().ConfigureAwait(false);
        using var methodSemaphore = new SemaphoreSlim(1);
        semaphore.Value = methodSemaphore;
        try
        {
            return func();
        }
        finally
        {
            Debug.Assert(methodSemaphore == semaphore.Value);
            await methodSemaphore.WaitAsync().ConfigureAwait(false);
            semaphore.Value = lockSemaphore;
            lockSemaphore.Release();
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
        var lockSemaphore = semaphore.Value ?? rootSemaphore;
        if (!await lockSemaphore.WaitAsync(0, CancellationToken.None).ConfigureAwait(false))
            await lockSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        using var methodSemaphore = new SemaphoreSlim(1);
        semaphore.Value = methodSemaphore;
        try
        {
            return func();
        }
        finally
        {
            Debug.Assert(methodSemaphore == semaphore.Value);
            await methodSemaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            semaphore.Value = lockSemaphore;
            lockSemaphore.Release();
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
        var lockSemaphore = semaphore.Value ?? rootSemaphore;
        await lockSemaphore.WaitAsync().ConfigureAwait(false);
        using var methodSemaphore = new SemaphoreSlim(1);
        semaphore.Value = methodSemaphore;
        try
        {
            await asyncAction().ConfigureAwait(false);
        }
        finally
        {
            Debug.Assert(methodSemaphore == semaphore.Value);
            await methodSemaphore.WaitAsync().ConfigureAwait(false);
            semaphore.Value = lockSemaphore;
            lockSemaphore.Release();
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
        var lockSemaphore = semaphore.Value ?? rootSemaphore;
        if (!await lockSemaphore.WaitAsync(0, CancellationToken.None).ConfigureAwait(false))
            await lockSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        using var methodSemaphore = new SemaphoreSlim(1);
        semaphore.Value = methodSemaphore;
        try
        {
            await asyncAction().ConfigureAwait(false);
        }
        finally
        {
            Debug.Assert(methodSemaphore == semaphore.Value);
            await methodSemaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            semaphore.Value = lockSemaphore;
            lockSemaphore.Release();
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
        var lockSemaphore = semaphore.Value ?? rootSemaphore;
        await lockSemaphore.WaitAsync().ConfigureAwait(false);
        using var methodSemaphore = new SemaphoreSlim(1);
        semaphore.Value = methodSemaphore;
        try
        {
            return await asyncFunc().ConfigureAwait(false);
        }
        finally
        {
            Debug.Assert(methodSemaphore == semaphore.Value);
            await methodSemaphore.WaitAsync().ConfigureAwait(false);
            semaphore.Value = lockSemaphore;
            lockSemaphore.Release();
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
        var lockSemaphore = semaphore.Value ?? rootSemaphore;
        if (!await lockSemaphore.WaitAsync(0, CancellationToken.None).ConfigureAwait(false))
            await lockSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        using var methodSemaphore = new SemaphoreSlim(1);
        semaphore.Value = methodSemaphore;
        try
        {
            return await asyncFunc().ConfigureAwait(false);
        }
        finally
        {
            Debug.Assert(methodSemaphore == semaphore.Value);
            await methodSemaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            semaphore.Value = lockSemaphore;
            lockSemaphore.Release();
        }
    }
}
