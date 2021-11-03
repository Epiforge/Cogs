namespace Cogs.Collections;

/// <summary>
/// Read-only wrapper around an <see cref="IReadOnlyDictionary{TKey, TValue}"/>
/// </summary>
/// <typeparam name="TKey">The type of keys in the read-only dictionary</typeparam>
/// <typeparam name="TValue">The type of values in the read-only dictionary</typeparam>
public class ReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyDictionary{TKey, TValue}"/> class
    /// </summary>
    /// <param name="readOnlyDictionary">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> around which to wrap</param>
    public ReadOnlyDictionary(IReadOnlyDictionary<TKey, TValue> readOnlyDictionary) => this.readOnlyDictionary = readOnlyDictionary;

    readonly IReadOnlyDictionary<TKey, TValue> readOnlyDictionary;

    /// <summary>
    /// Determines whether the read-only dictionary contains an element that has the specified key
    /// </summary>
    /// <param name="key">The key to locate</param>
    /// <returns><c>true</c> if the dictionary contains the key; otherwise, <c>false</c></returns>
    public bool ContainsKey(TKey key) => readOnlyDictionary.ContainsKey(key);

    IEnumerator IEnumerable.GetEnumerator() => readOnlyDictionary.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the read-only dictionary
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the dictionary</returns>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => readOnlyDictionary.GetEnumerator();

    /// <summary>
    /// Gets the value that is associated with the specified key
    /// </summary>
    /// <param name="key">The key to locate</param>
    /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter</param>
    /// <returns><c>true</c> if the read-only dictionary contains the key; otherwise, <c>false</c></returns>
    public bool TryGetValue(TKey key, out TValue value) => readOnlyDictionary.TryGetValue(key, out value);

    /// <summary>
    /// Gets the element that has the specified key in the read-only dictionary
    /// </summary>
    /// <param name="key">The key to locate</param>
    /// <returns>The element that has the specified key in the read-only dictionary</returns>
    public TValue this[TKey key] => readOnlyDictionary[key];

    /// <summary>
    /// Gets the number of elements in the read-only dictionary
    /// </summary>
    public int Count => readOnlyDictionary.Count;

    /// <summary>
    /// Gets an enumerable collection that contains the keys in the read-only dictionary
    /// </summary>
    public IEnumerable<TKey> Keys => readOnlyDictionary.Keys;

    /// <summary>
    /// Gets an enumerable collection that contains the values in the read-only dictionary
    /// </summary>
    public IEnumerable<TValue> Values => readOnlyDictionary.Values;
}
