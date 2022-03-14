namespace Cogs.Collections;

/// <summary>
/// Represents a collection of key/value pairs that are sorted on the key in which a key may be <c>null</c>
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
[SuppressMessage("Code Analysis", "CA1033: Interface methods should be callable by child types")]
public class NullableKeySortedDictionary<TKey, TValue> :
    IDictionary<TKey, TValue>,
    IReadOnlyDictionary<TKey, TValue>,
    ISortKeys<TKey>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullableKeySortedDictionary{TKey, TValue}"/> class that is empty and uses the default <see cref="IComparer{T}"/> implementation for the key type
    /// </summary>
    public NullableKeySortedDictionary() =>
        dict = new SortedDictionary<TKey, TValue>();

    /// <summary>
    /// Initializes a new instance of the <see cref="NullableKeySortedDictionary{TKey, TValue}"/> class that contains elements copied from the specified <see cref="IDictionary{TKey, TValue}"/> and uses the default <see cref="IComparer{T}"/> implementation for the key type
    /// </summary>
    /// <param name="dictionary">The <see cref="IDictionary{TKey, TValue}"/> whose elements are copied to the new <see cref="ObservableSortedDictionary{TKey, TValue}"/></param>
    public NullableKeySortedDictionary(IDictionary<TKey, TValue> dictionary) :
        this()
    {
        if (dictionary is null)
            throw new ArgumentNullException(nameof(dictionary));
        AddRange(dictionary);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NullableKeySortedDictionary{TKey, TValue}"/> class that is empty and uses the specified <see cref="IComparer{T}"/> implementation to compare keys
    /// </summary>
    /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing keys, or <c>null</c> to use the default <see cref="Comparer{T}"/> for the type of the key</param>
    public NullableKeySortedDictionary(IComparer<TKey> comparer) =>
        dict = new SortedDictionary<TKey, TValue>(comparer);

    /// <summary>
    /// Initializes a new instance of the <see cref="NullableKeySortedDictionary{TKey, TValue}"/> class that contains elements copied from the specified <see cref="IDictionary{TKey, TValue}"/> and uses the specified <see cref="IComparer{T}"/> implementation to compare keys
    /// </summary>
    /// <param name="dictionary">The <see cref="IDictionary{TKey, TValue}"/> whose elements are copied to the new <see cref="NullableKeySortedDictionary{TKey, TValue}"/></param>
    /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing keys, or <c>null</c> to use the default <see cref="Comparer{T}"/> for the type of the key</param>
    public NullableKeySortedDictionary(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer) :
        this(comparer)
    {
        if (dictionary is null)
            throw new ArgumentNullException(nameof(dictionary));
        AddRange(dictionary);
    }

    readonly SortedDictionary<TKey, TValue> dict;
    bool hasNullKeyedValue = false;
    TValue nullKeyedValue = default!;

    /// <summary>
    /// Gets the <see cref="IComparer{TKey}"/> used to order the elements of the <see cref="IDictionary{TKey, TValue}"/>
    /// </summary>
    public IComparer<TKey> Comparer =>
        dict.Comparer;

    /// <summary>
    /// Gets the number of elements in the collection
    /// </summary>
    public int Count =>
        dict.Count;

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly =>
        ((IDictionary<TKey, TValue>)dict).IsReadOnly;

    /// <summary>
    /// Gets an <see cref="ICollection{T}"/> containing the keys of the <see cref="IDictionary{TKey, TValue}"/>
    /// </summary>
    public ICollection<TKey> Keys =>
        (hasNullKeyedValue ? new TKey[] { default! } : Enumerable.Empty<TKey>()).Concat(((IDictionary<TKey, TValue>)dict).Keys).ToImmutableArray();

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys =>
        Keys;

    /// <summary>
    /// Gets an <see cref="ICollection{T}"/> containing the values in the <see cref="IDictionary{TKey, TValue}"/>
    /// </summary>
    public ICollection<TValue> Values =>
        (hasNullKeyedValue ? new TValue[] { nullKeyedValue } : Enumerable.Empty<TValue>()).Concat(((IDictionary<TKey, TValue>)dict).Values).ToImmutableArray();

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values =>
        Values;

    /// <summary>
    /// Gets or sets the value associated with the specified key
    /// </summary>
    /// <param name="key">The key of the value to get or set</param>
    /// <returns>The value associated with the specified key</returns>
    public TValue this[TKey key]
    {
        get => key is null ? hasNullKeyedValue ? nullKeyedValue : throw new KeyNotFoundException() : dict[key];

        set
        {
            if (key is null)
            {
                if (hasNullKeyedValue)
                    nullKeyedValue = value;
                throw new KeyNotFoundException();
            }
            else
                dict[key] = value;
        }
    }

    /// <summary>
    /// Adds the specified key and value to the dictionary
    /// </summary>
    /// <param name="key">The key of the element to add</param>
    /// <param name="value">The value of the element to add</param>
    public void Add(TKey key, TValue value)
    {
        if (key is null)
        {
            if (hasNullKeyedValue)
                throw new ArgumentException("A value with a null key has already been added");
            nullKeyedValue = value;
            hasNullKeyedValue = true;
        }
        else
            dict.Add(key, value);
    }

    void AddRange(IDictionary<TKey, TValue> dictionary)
    {
        foreach (var kv in dictionary)
            Add(kv.Key, kv.Value);
    }

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <summary>
    /// Removes all keys and values from the <see cref="IDictionary{TKey, TValue}"/>
    /// </summary>
    public void Clear()
    {
        hasNullKeyedValue = false;
        nullKeyedValue = default!;
        dict.Clear();
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => item.Key is null ? hasNullKeyedValue && EqualityComparer<TValue>.Default.Equals(nullKeyedValue, item.Value) : ((IDictionary<TKey, TValue>)dict).Contains(item);

    /// <summary>
    /// Determines whether the <see cref="IDictionary{TKey, TValue}"/> contains an element with the specified key
    /// </summary>
    /// <param name="key">The key to locate in the <see cref="IDictionary{TKey, TValue}"/></param>
    /// <returns><c>true</c> if the <see cref="IDictionary{TKey, TValue}"/> contains an element with the key; otherwise, <c>false</c></returns>
    public bool ContainsKey(TKey key) =>
        key is null ? hasNullKeyedValue : dict.ContainsKey(key);

    /// <summary>
    /// Determines whether the <see cref="IDictionary{TKey, TValue}"/> contains a specific value
    /// </summary>
    /// <param name="value">The value to locate in the <see cref="IDictionary{TKey, TValue}"/></param>
    /// <returns><c>true</c> if the <see cref="IDictionary{TKey, TValue}"/> contains an element with the value; otherwise, <c>false</c></returns>
    public bool ContainsValue(TValue value) =>
        dict.ContainsValue(value) || hasNullKeyedValue && EqualityComparer<TValue>.Default.Equals(nullKeyedValue, value);

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (hasNullKeyedValue)
            array[0] = new KeyValuePair<TKey, TValue>(default!, nullKeyedValue);
        dict.CopyTo(array, arrayIndex + (hasNullKeyedValue ? 1 : 0));
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() =>
        (hasNullKeyedValue ? new KeyValuePair<TKey, TValue>[] { new KeyValuePair<TKey, TValue>(default!, nullKeyedValue) } : Enumerable.Empty<KeyValuePair<TKey, TValue>>()).Concat(dict).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        ((IEnumerable<KeyValuePair<TKey, TValue>>)this).GetEnumerator();

    /// <summary>
    /// Removes the element with the specified key from the <see cref="IDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="key">The key of the element to remove</param>
    /// <returns><c>true</c> if the element is successfully removed; otherwise, <c>false</c> (this method also returns <c>false</c> if key was not found in the original <see cref="IDictionary{TKey, TValue}"/>)</returns>
    public bool Remove(TKey key)
    {
        if (key is null)
        {
            if (hasNullKeyedValue)
            {
                nullKeyedValue = default!;
                hasNullKeyedValue = false;
                return true;
            }
            return false;
        }
        return dict.Remove(key);
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
    {
        if (item.Key is null)
        {
            if (hasNullKeyedValue && EqualityComparer<TValue>.Default.Equals(nullKeyedValue, item.Value))
            {
                nullKeyedValue = default!;
                hasNullKeyedValue = false;
                return true;
            }
            return false;
        }
        return ((IDictionary<TKey, TValue>)dict).Remove(item);
    }

    /// <summary>
    /// Gets the value associated with the specified key
    /// </summary>
    /// <param name="key">The key the value of which to get</param>
    /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter</param>
    /// <returns><c>true</c> if the object that implements <see cref="IDictionary{TKey, TValue}"/> contains an element with the specified key; otherwise, <c>false</c></returns>
    public bool TryGetValue(TKey key, out TValue value)
    {
        if (key is null)
        {
            if (hasNullKeyedValue)
            {
                value = nullKeyedValue;
                return true;
            }
            value = default!;
            return false;
        }
        return dict.TryGetValue(key, out value);
    }
}
