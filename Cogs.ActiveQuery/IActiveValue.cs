namespace Cogs.ActiveQuery;

/// <summary>
/// Represents the scalar result of an active query
/// </summary>
/// <typeparam name="TValue">The type of the scalar result</typeparam>
public interface IActiveValue<out TValue> : IDisposable, IDisposalStatus, INotifyDisposed, INotifyDisposing, INotifyElementFaultChanges, INotifyPropertyChanged, INotifyPropertyChanging
{
    /// <summary>
    /// Gets the exception that occured the most recent time the query updated
    /// </summary>
    Exception? OperationFault { get; }

    /// <summary>
    /// Gets the value from the most recent time the query updated
    /// </summary>
    TValue Value { get; }
}
