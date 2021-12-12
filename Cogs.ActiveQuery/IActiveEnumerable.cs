namespace Cogs.ActiveQuery;

/// <summary>
/// Represents a read-only collection of elements that is the result of an active query
/// </summary>
/// <typeparam name="TElement">The type of the elements in the sequence</typeparam>
public interface IActiveEnumerable<out TElement> :
    IDisposable,
    IDisposalStatus,
    INotifyCollectionChanged,
    INotifyDisposalOverridden,
    INotifyDisposed,
    INotifyDisposing,
    INotifyElementFaultChanges,
    INotifyGenericCollectionChanged<TElement>,
    INotifyPropertyChanged,
    INotifyPropertyChanging,
    IReadOnlyList<TElement>,
    ISynchronized
{
}
