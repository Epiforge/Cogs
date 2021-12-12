namespace Cogs.ActiveQuery;

/// <summary>
/// Represents a group of elements that is itself an element in the results of a group by active query
/// </summary>
/// <typeparam name="TKey">The type of the values by which the source elements are being grouped</typeparam>
/// <typeparam name="TElement">The type of the source elements</typeparam>
public class ActiveGrouping<TKey, TElement> :
    ActiveEnumerable<TElement>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveGrouping{TKey, TElement}"/> class
    /// </summary>
    /// <param name="key">The key value that members of the group share in common</param>
    /// <param name="list">The list of members of the group</param>
    /// <param name="faultNotifier">The <see cref="INotifyElementFaultChanges"/> for the underlying data of the group's members</param>
    /// <param name="onDispose">The action to take when the <see cref="ActiveGrouping{TKey, TValue}"/> is disposed</param>
    public ActiveGrouping(TKey key, ObservableCollection<TElement> list, INotifyElementFaultChanges? faultNotifier = null, Action? onDispose = null) :
        base(list, faultNotifier, onDispose) =>
        Key = key;

    /// <summary>
    /// Gets the value shared by the source elements in this group
    /// </summary>
    public TKey Key { get; }
}
