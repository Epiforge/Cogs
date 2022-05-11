namespace Cogs.ActiveQuery;

/// <summary>
/// Represents a read-only collection of elements that is the result of an active query
/// </summary>
/// <typeparam name="TElement">The type of the elements in the sequence</typeparam>
public interface IActiveEnumerable<out TElement> :
    IDisposable,
    IDisposalStatus,
    IList,
    INotifyCollectionChanged,
    INotifyDisposalOverridden,
    INotifyDisposed,
    INotifyDisposing,
    INotifyElementFaultChanges,
    INotifyPropertyChanged,
    INotifyPropertyChanging,
    IReadOnlyList<TElement>,
    ISynchronized
{
    /// <summary>
    /// Gets the element at the specified index in the read-only list
    /// </summary>
    /// <param name="index">The zero-based index of the element to get</param>
    new TElement this[int index] { get; }

    /// <summary>
    /// Gets the number of elements in the collection
    /// </summary>
    new int Count { get; }
}
