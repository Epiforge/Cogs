namespace Cogs.ActiveQuery;

/// <summary>
/// Represents a read-only collection of elements that is the result of an active query and that cannot be disposed by callers
/// </summary>
/// <typeparam name="TElement">The type of the elements in the sequence</typeparam>
public interface INondisposableActiveEnumerable<out TElement> :
    IDisposalStatus,
    IList,
    INotifyCollectionChanged,
    INotifyDisposed,
    INotifyDisposing,
    INotifyElementFaultChanges,
    INotifyPropertyChanged,
    INotifyPropertyChanging,
    IReadOnlyList<TElement>,
    ISynchronized
{
}
