namespace Cogs.Threading;

/// <summary>
/// Represents an object that supports callers waiting until it is idle
/// </summary>
public interface IWaitUntilIdle
{
    /// <summary>
    /// Synchronously waits until the object is now idle (this method may block the calling thread)
    /// </summary>
    void WaitUntilIdle();

    /// <summary>
    /// Synchronously waits until the object is now idle (this method may block the calling thread)
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to cancel the wait (if this token is already cancelled, this method will first check whether the object is idle)</param>
    void WaitUntilIdle(CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously waits until the object is now idle
    /// </summary>
    Task WaitUntilIdleAsync();

    /// <summary>
    /// Asynchronously waits until the object is now idle
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to cancel the wait (if this token is already cancelled, this method will first check whether the object is idle)</param>
    Task WaitUntilIdleAsync(CancellationToken cancellationToken);
}
