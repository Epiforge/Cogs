namespace Cogs.Collections;

/// <summary>
/// Represents a generic collection of key/value pairs that supports bulk operations
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary</typeparam>
public interface IRangeDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
{
    /// <summary>
    /// Adds elements with the provided keys and values to the <see cref="IRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="keyValuePairs">The key-value pairs to add</param>
    void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs);

    /// <summary>
    /// Adds elements with the provided keys and values to the <see cref="IRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="keyValuePairs">The key-value pairs to add</param>
    void AddRange(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs);

    /// <summary>
    /// Determines whether the <see cref="IRangeDictionary{TKey, TValue}"/> contains an element with the specified key
    /// </summary>
    /// <param name="key">The key to locate in the <see cref="IRangeDictionary{TKey, TValue}"/></param>
    /// <returns><c>true</c> if the <see cref="IRangeDictionary{TKey, TValue}"/> contains an element with the key; otherwise, <c>false</c></returns>
    new bool ContainsKey(TKey key);

    /// <summary>
    /// Gets the elements with the specified keys
    /// </summary>
    /// <param name="keys">The keys of the elements to get</param>
    /// <returns>The elements with the specified keys</returns>
    IReadOnlyList<KeyValuePair<TKey, TValue>> GetRange(IEnumerable<TKey> keys);

    /// <summary>
    /// Removes any elements that satisfy the specified predicate from the <see cref="IRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="predicate">A predicate that returns <c>true</c> when passed the key and value of an element to be removed</param>
    /// <returns>The key-value pairs of the elements that were removed</returns>
    IReadOnlyList<KeyValuePair<TKey, TValue>> RemoveAll(Func<TKey, TValue, bool> predicate);

    /// <summary>
    /// Removes the elements with any of the specified keys from the <see cref="IRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="keys">The keys of the elements to remove</param>
    /// <returns>The keys of the elements that were found and removed</returns>
    IReadOnlyList<TKey> RemoveRange(IEnumerable<TKey> keys);

    /// <summary>
    /// Replaces elements in the <see cref="IRangeDictionary{TKey, TValue}"/> with specified elements
    /// </summary>
    /// <param name="keyValuePairs">The replacement key-value pairs</param>
    void ReplaceRange(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs);

    /// <summary>
    /// Removes the elements with any of the specified keys from and then adds elements with the provided keys and values to the <see cref="IRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="removeKeys">The keys of the elements to remove</param>
    /// <param name="newKeyValuePairs">The key-value pairs to add</param>
    /// <returns>The keys of the elements that were found and removed</returns>
    IReadOnlyList<TKey> ReplaceRange(IEnumerable<TKey> removeKeys, IEnumerable<KeyValuePair<TKey, TValue>> newKeyValuePairs);

    /// <summary>
    /// Reinitializes the hash table or binary search tree used internally by the <see cref="IRangeDictionary{TKey, TValue}"/>, removing all elements
    /// </summary>
    void Reset();

    /// <summary>
    /// Reinitializes the hash table or binary search tree used internally by the <see cref="IRangeDictionary{TKey, TValue}"/> with the elements from the specified dictionary
    /// </summary>
    /// <param name="dictionary">The dictionary from which to retrieve the initial elements</param>
    void Reset(IDictionary<TKey, TValue> dictionary);

    /// <summary>
    /// Gets the value associated with the specified key
    /// </summary>
    /// <param name="key">The key the value of which to get</param>
    /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter</param>
    /// <returns><c>true</c> if the object that implements <see cref="IRangeDictionary{TKey, TValue}"/> contains an element with the specified key; otherwise, <c>false</c></returns>
    new bool TryGetValue(TKey key, out TValue value);

    /// <summary>
    /// Gets or sets the element with the specified key
    /// </summary>
    /// <param name="key">The key of the element to get or set</param>
    /// <returns>The element with the specified key</returns>
    new TValue this[TKey key] { get; set; }

    /// <summary>
    /// Gets an <see cref="ICollection{T}"/> containing the keys of the <see cref="IRangeDictionary{TKey, TValue}"/>
    /// </summary>
    new IEnumerable<TKey> Keys { get; }

    /// <summary>
    /// Gets an <see cref="ICollection{T}"/> containing the values in the <see cref="IRangeDictionary{TKey, TValue}"/>
    /// </summary>
    new IEnumerable<TValue> Values { get; }
}
