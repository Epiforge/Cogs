namespace Cogs.Collections.Synchronized;

/// <summary>
/// Represents a generic collection of key/value pairs the operations of which occur on a specific <see cref="SynchronizationContext"/>, that supports bulk operations and notifies listeners of dynamic changes, such as when an item is added and removed or the whole list is cleared
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary</typeparam>
public interface ISynchronizedObservableRangeDictionary<TKey, TValue> : IObservableRangeDictionary<TKey, TValue>, ISynchronized
{
    /// <summary>
    /// Adds an element with the provided key and value to the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="key">The object to use as the key of the element to add</param>
    /// <param name="value">The object to use as the value of the element to add</param>
    Task AddAsync(TKey key, TValue value);

    /// <summary>
    /// Adds elements with the provided keys and values to the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="keyValuePairs">The key-value pairs to add</param>
    Task AddRangeAsync(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs);

    /// <summary>
    /// Adds elements with the provided keys and values to the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="keyValuePairs">The key-value pairs to add</param>
    Task AddRangeAsync(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs);

    /// <summary>
    /// Removes all keys and values from the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Determines whether the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/> contains an element with the specified key
    /// </summary>
    /// <param name="key">The key to locate in the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/></param>
    /// <returns><c>true</c> if the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/> contains an element with the key; otherwise, <c>false</c></returns>
    Task<bool> ContainsKeyAsync(TKey key);

    /// <summary>
    /// Determines whether the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/> contains an element with the specified value
    /// </summary>
    /// <param name="value">The value to locate in the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/></param>
    /// <returns><c>true</c> if the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/> contains an element with the value; otherwise, <c>false</c></returns>
    Task<bool> ContainsValueAsync(TValue value);

    /// <summary>
    /// Copies all keys in the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/> into a <see cref="IReadOnlyList{T}"/>
    /// </summary>
    IReadOnlyList<TKey> GetAllKeys();

    /// <summary>
    /// Copies all keys in the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/> into a <see cref="IReadOnlyList{T}"/>
    /// </summary>
    Task<IReadOnlyList<TKey>> GetAllKeysAsync();

    /// <summary>
    /// Gets the elements with the specified keys
    /// </summary>
    /// <param name="keys">The keys of the elements to get</param>
    /// <returns>The elements with the specified keys</returns>
    Task<IReadOnlyList<KeyValuePair<TKey, TValue>>> GetRangeAsync(IEnumerable<TKey> keys);

    /// <summary>
    /// Gets the element with the specified key
    /// </summary>
    /// <param name="key">The key of the element to get</param>
    /// <returns>The element with the specified key</returns>
    Task<TValue> GetValueAsync(TKey key);

    /// <summary>
    /// Removes any elements that satisfy the specified predicate from the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="predicate">A predicate that returns <c>true</c> when passed the key and value of an element to be removed</param>
    /// <returns>The key-value pairs of the elements that were removed</returns>
    Task<IReadOnlyList<KeyValuePair<TKey, TValue>>> RemoveAllAsync(Func<TKey, TValue, bool> predicate);

    /// <summary>
    /// Removes the element with the specified key from the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="key">The key of the element to remove</param>
    /// <returns><c>true</c> if the element is successfully removed; otherwise, <c>false</c></returns>
    Task<bool> RemoveAsync(TKey key);

    /// <summary>
    /// Removes the elements with any of the specified keys from the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="keys">The keys of the elements to remove</param>
    /// <returns>The keys of the elements that were found and removed</returns>
    Task<IReadOnlyList<TKey>> RemoveRangeAsync(IEnumerable<TKey> keys);

    /// <summary>
    /// Replaces elements in the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/> with specified elements
    /// </summary>
    /// <param name="keyValuePairs">The replacement key-value pairs</param>
    Task ReplaceRangeAsync(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs);

    /// <summary>
    /// Removes the elements with any of the specified keys from and then adds elements with the provided keys and values to the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="removeKeys">The keys of the elements to remove</param>
    /// <param name="newKeyValuePairs">The key-value pairs to add</param>
    /// <returns>The keys of the elements that were found and removed</returns>
    Task<IReadOnlyList<TKey>> ReplaceRangeAsync(IEnumerable<TKey> removeKeys, IEnumerable<KeyValuePair<TKey, TValue>> newKeyValuePairs);

    /// <summary>
    /// Reinitializes the hash table or binary search tree used internally by the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>, removing all elements
    /// </summary>
    Task ResetAsync();

    /// <summary>
    /// Reinitializes the hash table or binary search tree used internally by the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/> with the elements from the specified dictionary
    /// </summary>
    /// <param name="dictionary">The dictionary from which to retrieve the initial elements</param>
    Task ResetAsync(IDictionary<TKey, TValue> dictionary);

    /// <summary>
    /// Sets the element with the specified key
    /// </summary>
    /// <param name="key">The key of the element to set</param>
    /// <param name="value">The element with the specified key</param>
    Task SetValueAsync(TKey key, TValue value);

    /// <summary>
    /// Gets the value associated with the specified key
    /// </summary>
    /// <param name="key">The key the value of which to get</param>
    /// <returns><c>true</c> if the object that implements <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/> contains an element with the specified key and the value that was retrieved; otherwise, <c>false</c> and the default <typeparamref name="TValue"/></returns>
    Task<(bool valueRetrieved, TValue value)> TryGetValueAsync(TKey key);

    /// <summary>
    /// Gets the number of key-value pairs contained in the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    Task<int> CountAsync { get; }
}
