namespace Cogs.Collections;

/// <summary>
/// Represents a generic collection of key/value pairs that supports bulk operations and notifies listeners of dynamic changes, such as when an item is added and removed or the whole list is cleared
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary</typeparam>
public interface IObservableRangeDictionary<TKey, TValue> :
    INotifyCollectionChanged,
    INotifyGenericCollectionChanged<KeyValuePair<TKey, TValue>>,
    INotifyDictionaryChanged,
    INotifyDictionaryChanged<TKey, TValue>,
    IRangeDictionary<TKey, TValue>
{
}
