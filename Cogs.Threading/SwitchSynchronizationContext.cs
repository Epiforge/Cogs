namespace Cogs.Threading;

/// <summary>
/// Provides a synchronization context that can be switched on and off
/// </summary>
public sealed class SwitchSynchronizationContext :
    SynchronizationContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="SwitchSynchronizationContext"/>
    /// </summary>
    /// <param name="basis">An instance of <see cref="SynchronizationContext"/> upon which operations will be executed when the <see cref="UsingBasis"/> property is <c>true</c></param>
    public SwitchSynchronizationContext(SynchronizationContext basis) =>
        this.basis = basis;

    readonly SynchronizationContext basis;

    /// <summary>
    /// Gets/sets whether or not the basis <see cref="SynchronizationContext"/> will be used for methods invoked on this instance
    /// </summary>
    public bool UsingBasis { get; set; } = true;

    /// <inheritdoc/>
    public override SynchronizationContext CreateCopy() =>
        this;

    /// <inheritdoc/>
    public override void OperationCompleted()
    {
        if (UsingBasis)
            basis.OperationCompleted();
        else
            base.OperationCompleted();
    }

    /// <inheritdoc/>
    public override void OperationStarted()
    {
        if (UsingBasis)
            basis.OperationStarted();
        else
            base.OperationStarted();
    }

    /// <inheritdoc/>
    public override void Post(SendOrPostCallback d, object state)
    {
        if (UsingBasis)
            basis.Post(d, state);
        else
            Synchronization.DefaultSynchronizationContext.Post(d, state);
    }

    /// <inheritdoc/>
    public override void Send(SendOrPostCallback d, object state)
    {
        if (UsingBasis)
            basis.Send(d, state);
        else
            base.Send(d, state);
    }

    /// <inheritdoc/>
    public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
    {
        if (UsingBasis)
            return basis.Wait(waitHandles, waitAll, millisecondsTimeout);
        else
            return base.Wait(waitHandles, waitAll, millisecondsTimeout);
    }
}
