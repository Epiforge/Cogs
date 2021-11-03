namespace Cogs.Threading;

/// <summary>
/// Provides general utilities for synchronization
/// </summary>
public static class Synchronization
{
    static readonly AsyncSynchronizationContext asyncSynchronizationContext = new(false);

    /// <summary>
    /// Gets the synchronization context that is used by methods in this library that require one when <see cref="SynchronizationContext.Current"/> is <c>null</c>
    /// </summary>
    public static SynchronizationContext DefaultSynchronizationContext => asyncSynchronizationContext;
}
