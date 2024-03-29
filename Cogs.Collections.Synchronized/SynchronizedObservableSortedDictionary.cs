namespace Cogs.Collections.Synchronized;

/// <summary>
/// Represents a collection of key/value pairs the operations of which occur on a specific <see cref="System.Threading.SynchronizationContext"/>, that are sorted on the key, that supports bulk operations and notifies listeners of dynamic changes, such as when an item is added and removed or the whole list is cleared
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
public sealed class SynchronizedObservableSortedDictionary<TKey, TValue> :
    ObservableSortedDictionary<TKey, TValue>,
    ISynchronizedObservableRangeDictionary<TKey, TValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/> class that is empty, uses the default <see cref="IComparer{T}"/> implementation for the key type, and using <see cref="SynchronizationContext.Current"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>)
    /// </summary>
    public SynchronizedObservableSortedDictionary() :
        this(SynchronizationContext.Current ?? Synchronization.DefaultSynchronizationContext)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/> class that is empty, uses the specified <see cref="IComparer{T}"/> implementation to compare keys, and using <see cref="SynchronizationContext.Current"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>)
    /// </summary>
    /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing keys, or <c>null</c> to use the default <see cref="Comparer{T}"/> for the type of the key</param>
    public SynchronizedObservableSortedDictionary(IComparer<TKey> comparer) :
        this(SynchronizationContext.Current ?? Synchronization.DefaultSynchronizationContext, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/> class that contains elements copied from the specified <see cref="IDictionary{TKey, TValue}"/>, uses the default <see cref="IComparer{T}"/> implementation for the key type, and using <see cref="SynchronizationContext.Current"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>)
    /// </summary>
    /// <param name="dictionary">The <see cref="IDictionary{TKey, TValue}"/> whose elements are copied to the new <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/></param>
    public SynchronizedObservableSortedDictionary(IDictionary<TKey, TValue> dictionary) :
        this(SynchronizationContext.Current ?? Synchronization.DefaultSynchronizationContext, dictionary)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/> class that contains elements copied from the specified <see cref="IDictionary{TKey, TValue}"/>, uses the specified <see cref="IComparer{T}"/> implementation to compare keys, and using <see cref="SynchronizationContext.Current"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>)
    /// </summary>
    /// <param name="dictionary">The <see cref="IDictionary{TKey, TValue}"/> whose elements are copied to the new <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/></param>
    /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing keys, or <c>null</c> to use the default <see cref="Comparer{T}"/> for the type of the key</param>
    public SynchronizedObservableSortedDictionary(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer) :
        this(SynchronizationContext.Current ?? Synchronization.DefaultSynchronizationContext, dictionary, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/> class that is empty, uses the default <see cref="IComparer{T}"/> implementation for the key type, and using the specified <see cref="System.Threading.SynchronizationContext"/>
    /// </summary>
    /// <param name="synchronizationContext">The <see cref="System.Threading.SynchronizationContext"/> on which to perform all operations</param>
    public SynchronizedObservableSortedDictionary(SynchronizationContext? synchronizationContext) :
        base() =>
        SynchronizationContext = synchronizationContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/> class that is empty, uses the specified <see cref="IComparer{T}"/> implementation to compare keys, and using the specified <see cref="System.Threading.SynchronizationContext"/>
    /// </summary>
    /// <param name="synchronizationContext">The <see cref="System.Threading.SynchronizationContext"/> on which to perform all operations</param>
    /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing keys, or <c>null</c> to use the default <see cref="Comparer{T}"/> for the type of the key</param>
    public SynchronizedObservableSortedDictionary(SynchronizationContext? synchronizationContext, IComparer<TKey> comparer) :
        base(comparer) =>
        SynchronizationContext = synchronizationContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/> class that contains elements copied from the specified <see cref="IDictionary{TKey, TValue}"/>, uses the default <see cref="IComparer{T}"/> implementation for the key type, and using the specified <see cref="System.Threading.SynchronizationContext"/>
    /// </summary>
    /// <param name="synchronizationContext">The <see cref="System.Threading.SynchronizationContext"/> on which to perform all operations</param>
    /// <param name="dictionary">The <see cref="IDictionary{TKey, TValue}"/> whose elements are copied to the new <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/></param>
    public SynchronizedObservableSortedDictionary(SynchronizationContext? synchronizationContext, IDictionary<TKey, TValue> dictionary) :
        base(dictionary) =>
        SynchronizationContext = synchronizationContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/> class that contains elements copied from the specified <see cref="IDictionary{TKey, TValue}"/>, uses the specified <see cref="IComparer{T}"/> implementation to compare keys, and using the specified <see cref="System.Threading.SynchronizationContext"/>
    /// </summary>
    /// <param name="synchronizationContext">The <see cref="System.Threading.SynchronizationContext"/> on which to perform all operations</param>
    /// <param name="dictionary">The <see cref="IDictionary{TKey, TValue}"/> whose elements are copied to the new <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/></param>
    /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing keys, or <c>null</c> to use the default <see cref="Comparer{T}"/> for the type of the key</param>
    public SynchronizedObservableSortedDictionary(SynchronizationContext? synchronizationContext, IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer) :
        base(dictionary, comparer) =>
        SynchronizationContext = synchronizationContext;

    /// <summary>
    /// Gets or sets the value associated with the specified key
    /// </summary>
    /// <param name="key">The key of the value to get or set</param>
    /// <returns>The value associated with the specified key</returns>
    public override TValue this[TKey key]
    {
        get => this.Execute(() => base[key]);
        set => this.Execute(() => base[key] = value);
    }

    /// <summary>
    /// Gets the <see cref="IComparer{T}"/> used to order the elements of the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/>
    /// </summary>
    public override IComparer<TKey> Comparer =>
        this.Execute(() => base.Comparer)!;

    /// <summary>
    /// Gets the <see cref="IComparer{T}"/> used to order the elements of the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/>
    /// </summary>
    public Task<IComparer<TKey>> ComparerAsync =>
        this.ExecuteAsync(() => base.Comparer);

    /// <summary>
    /// Gets the number of elements in the collection
    /// </summary>
    public override int Count =>
        this.Execute(() => base.Count);

    /// <summary>
    /// Gets the number of key-value pairs contained in the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    public Task<int> CountAsync =>
        this.ExecuteAsync(() => base.Count);

    /// <summary>
    /// Gets a value that indicates whether the <see cref="IDictionary"/> is read-only
    /// </summary>
    protected override bool DictionaryIsReadOnly =>
        this.Execute(() => base.DictionaryIsReadOnly);

    /// <summary>
    /// Gets a value indicating whether the <see cref="ICollection{T}"/> is read-only
    /// </summary>
    protected override bool GenericCollectionIsReadOnly =>
        this.Execute(() => base.GenericCollectionIsReadOnly);

    /// <summary>
    /// Gets a value that indicates whether access to the <see cref="ICollection"/> is synchronized (thread safe)
    /// </summary>
    protected override bool IsCollectionSynchronized =>
        this.Execute(() => base.IsCollectionSynchronized);

    /// <summary>
    /// Gets a value that indicates whether the <see cref="IDictionary"/> has a fixed size
    /// </summary>
    protected override bool IsFixedSize =>
        this.Execute(() => base.IsFixedSize);

    /// <summary>
    /// Gets an <see cref="ICollection{T}"/> containing the keys of the <see cref="IDictionary{TKey, TValue}"/>
    /// </summary>
    public override SortedDictionary<TKey, TValue>.KeyCollection Keys =>
        this.Execute(() => base.Keys)!;

    /// <summary>
    /// Gets an <see cref="ICollection"/> containing the keys of the <see cref="IDictionary"/>
    /// </summary>
    protected override ICollection KeysCollection =>
        this.Execute(() => base.KeysCollection)!;

    /// <summary>
    /// Gets an <see cref="ICollection{T}"/> containing the keys of the <see cref="IDictionary{TKey, TValue}"/>
    /// </summary>
    protected override ICollection<TKey> KeysGenericCollection =>
        this.Execute(() => base.KeysGenericCollection)!;

    /// <summary>
    /// Gets a collection containing the keys of the <see cref="IReadOnlyDictionary{TKey, TValue}"/>
    /// </summary>
    protected override IEnumerable<TKey> KeysGenericEnumerable =>
        this.Execute(() => base.KeysGenericEnumerable)!;

    /// <summary>
    /// Gets the <see cref="System.Threading.SynchronizationContext"/> on which this object's operations occur
    /// </summary>
    public SynchronizationContext? SynchronizationContext { get; }

    /// <summary>
    /// Gets an object that can be used to synchronize access to the <see cref="ICollection"/>
    /// </summary>
    protected override object SyncRoot =>
        this.Execute(() => base.SyncRoot)!;

    /// <summary>
    /// Gets an <see cref="ICollection{T}"/> containing the values in the <see cref="IDictionary{TKey, TValue}"/>
    /// </summary>
    public override SortedDictionary<TKey, TValue>.ValueCollection Values =>
        this.Execute(() => base.Values)!;

    /// <summary>
    /// Gets an <see cref="ICollection"/> containing the values in the <see cref="IDictionary"/>
    /// </summary>
    protected override ICollection ValuesCollection =>
        this.Execute(() => base.ValuesCollection)!;

    /// <summary>
    /// Gets an <see cref="ICollection{T}"/> containing the values in the <see cref="IDictionary{TKey, TValue}"/>
    /// </summary>
    protected override ICollection<TValue> ValuesGenericCollection =>
        this.Execute(() => base.ValuesGenericCollection)!;

    /// <summary>
    /// Gets a collection containing the values in the <see cref="ObservableDictionary{TKey, TValue}"/>
    /// </summary>
    protected override IEnumerable<TValue> ValuesGenericEnumerable =>
        this.Execute(() => base.ValuesGenericEnumerable)!;

    /// <summary>
    /// Adds the specified key and value to the dictionary
    /// </summary>
    /// <param name="key">The key of the element to add</param>
    /// <param name="value">The value of the element to add</param>
    public override void Add(TKey key, TValue value) =>
        this.Execute(() => base.Add(key, value));

    /// <summary>
    /// Adds an element with the provided key and value to the <see cref="IDictionary"/> object
    /// </summary>
    /// <param name="key">The object to use as the key of the element to add</param>
    /// <param name="value">The object to use as the value of the element to add</param>
    protected override void Add(object key, object value) =>
        this.Execute(() => base.Add(key, value));

    /// <summary>
    /// Adds an item to the <see cref="ICollection{T}"/>
    /// </summary>
    /// <param name="item">The object to add to the <see cref="ICollection{T}"/></param>
    protected override void Add(KeyValuePair<TKey, TValue> item) =>
        this.Execute(() => base.Add(item));

    /// <summary>
    /// Adds an element with the provided key and value to the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="key">The object to use as the key of the element to add</param>
    /// <param name="value">The object to use as the value of the element to add</param>
    public Task AddAsync(TKey key, TValue value) =>
        this.ExecuteAsync(() => base.Add(key, value));

    /// <summary>
    /// Adds elements with the provided keys and values to the <see cref="IRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="keyValuePairs">The key-value pairs to add</param>
    public override void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs) =>
        this.Execute(() => base.AddRange(keyValuePairs));

    /// <summary>
    /// Adds elements with the provided keys and values to the <see cref="IRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="keyValuePairs">The key-value pairs to add</param>
    public override void AddRange(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs) =>
        this.Execute(() => base.AddRange(keyValuePairs));

    /// <summary>
    /// Adds elements with the provided keys and values to the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="keyValuePairs">The key-value pairs to add</param>
    public Task AddRangeAsync(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs) =>
        this.ExecuteAsync(() => base.AddRange(keyValuePairs));

    /// <summary>
    /// Adds elements with the provided keys and values to the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="keyValuePairs">The key-value pairs to add</param>
    public Task AddRangeAsync(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs) =>
        this.ExecuteAsync(() => base.AddRange(keyValuePairs));

    /// <summary>
    /// Removes all elements from the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/>
    /// </summary>
    public override void Clear() =>
        this.Execute(() => base.Clear());

    /// <summary>
    /// Removes all keys and values from the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    public Task ClearAsync() =>
        this.ExecuteAsync(() => base.Clear());

    /// <summary>
    /// Determines whether the <see cref="IDictionary"/> contains the specified key
    /// </summary>
    /// <param name="key">The key to locate in the <see cref="IDictionary"/></param>
    /// <returns><c>true</c> if the <see cref="IDictionary"/> contains an element with the specified key; otherwise, <c>false</c></returns>
    protected override bool Contains(object key) =>
        this.Execute(() => base.Contains(key));

    /// <summary>
    /// Determines whether the <see cref="ICollection{T}"/> contains a specific value
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="ICollection{T}"/></param>
    /// <returns><c>true</c> if <paramref name="item"/> is found in the <see cref="ICollection{T}"/>; otherwise, <c>false</c></returns>
    protected override bool Contains(KeyValuePair<TKey, TValue> item) =>
        this.Execute(() => base.Contains(item));

    /// <summary>
    /// Determines whether the <see cref="IDictionary{TKey, TValue}"/> contains an element with the specified key
    /// </summary>
    /// <param name="key">The key to locate in the <see cref="IDictionary{TKey, TValue}"/></param>
    /// <returns><c>true</c> if the <see cref="IDictionary{TKey, TValue}"/> contains an element with the key; otherwise, <c>false</c></returns>
    public override bool ContainsKey(TKey key) =>
        this.Execute(() => base.ContainsKey(key));

    /// <summary>
    /// Determines whether the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/> contains an element with the specified key
    /// </summary>
    /// <param name="key">The key to locate in the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/></param>
    /// <returns><c>true</c> if the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/> contains an element with the key; otherwise, <c>false</c></returns>
    public Task<bool> ContainsKeyAsync(TKey key) =>
        this.ExecuteAsync(() => base.ContainsKey(key));

    /// <summary>
    /// Determines whether the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/> contains an element with the specified value
    /// </summary>
    /// <param name="value">The value to locate in the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/></param>
    /// <returns><c>true</c> if the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/> contains an element with the specified value; otherwise, <c>false</c></returns>
    public override bool ContainsValue(TValue value) =>
        this.Execute(() => base.ContainsValue(value));

    /// <summary>
    /// Determines whether the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/> contains an element with the specified value
    /// </summary>
    /// <param name="value">THe value to locate in the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/></param>
    /// <returns><c>true</c> if the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/> contains an element with the value; otherwise, <c>false</c></returns>
    public Task<bool> ContainsValueAsync(TValue value) =>
        this.ExecuteAsync(() => base.ContainsValue(value));

    /// <summary>
    /// Copies the elements of the <see cref="ICollection"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index
    /// </summary>
    /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ICollection"/> (the <see cref="Array"/> must have zero-based indexing)</param>
    /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins</param>
    protected override void CopyTo(Array array, int index) =>
        this.Execute(() => base.CopyTo(array, index));

    /// <summary>
    /// Copies the elements of the <see cref="ICollection{T}"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index
    /// </summary>
    /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ICollection{T}"/> (the <see cref="Array"/> must have zero-based indexing)</param>
    /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins</param>
    public override void CopyTo(KeyValuePair<TKey, TValue>[] array, int index) =>
        this.Execute(() => base.CopyTo(array, index));

    /// <summary>
    /// Copies all keys in the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/> into a <see cref="IReadOnlyList{T}"/>
    /// </summary>
    public IReadOnlyList<TKey> GetAllKeys() =>
        this.Execute(() => Keys.ToImmutableArray());

    /// <summary>
    /// Copies all keys in the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/> into a <see cref="IReadOnlyList{T}"/>
    /// </summary>
    public Task<IReadOnlyList<TKey>> GetAllKeysAsync() =>
        this.ExecuteAsync(() => (IReadOnlyList<TKey>)Keys.ToImmutableArray());

    /// <summary>
    /// Returns an <see cref="IDictionaryEnumerator"/> object for the <see cref="IDictionary"/> object
    /// </summary>
    /// <returns>An <see cref="IDictionaryEnumerator"/> object for the <see cref="IDictionary"/> object</returns>
    protected override IDictionaryEnumerator GetDictionaryEnumerator() =>
        this.Execute(() => base.GetDictionaryEnumerator())!;

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/>
    /// </summary>
    /// <returns>A <see cref="SortedDictionary{TKey, TValue}.Enumerator"/> for the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/></returns>
    public override SortedDictionary<TKey, TValue>.Enumerator GetEnumerator() =>
        this.Execute(() => base.GetEnumerator());

    /// <summary>
    /// Returns an enumerator that iterates through the collection
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection</returns>
    protected override IEnumerator<KeyValuePair<TKey, TValue>> GetKeyValuePairEnumerator() =>
        this.Execute(() => base.GetKeyValuePairEnumerator())!;

    /// <summary>
    /// Returns an enumerator that iterates through a collection
    /// </summary>
    /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection</returns>
    protected override IEnumerator GetNonGenericEnumerator() =>
        this.Execute(() => base.GetNonGenericEnumerator())!;

    /// <summary>
    /// Gets the elements with the specified keys
    /// </summary>
    /// <param name="keys">The keys of the elements to get</param>
    /// <returns>The elements with the specified keys</returns>
    public override IReadOnlyList<KeyValuePair<TKey, TValue>> GetRange(IEnumerable<TKey> keys) =>
        this.Execute(() => base.GetRange(keys))!;

    /// <summary>
    /// Gets the elements with the specified keys
    /// </summary>
    /// <param name="keys">The keys of the elements to get</param>
    /// <returns>The elements with the specified keys</returns>
    public Task<IReadOnlyList<KeyValuePair<TKey, TValue>>> GetRangeAsync(IEnumerable<TKey> keys) =>
        this.ExecuteAsync(() => base.GetRange(keys));

    /// <summary>
    /// Gets the element with the specified key
    /// </summary>
    /// <param name="key">The key of the element to get</param>
    /// <returns>The element with the specified key, or <c>null</c> if the key does not exist</returns>
    protected override object GetValue(object key) =>
        this.Execute(() => base.GetValue(key))!;

    /// <summary>
    /// Gets the element with the specified key
    /// </summary>
    /// <param name="key">The key of the element to get</param>
    /// <returns>The element with the specified key</returns>
    public Task<TValue> GetValueAsync(TKey key) =>
        this.ExecuteAsync(() => base[key]);

    /// <summary>
    /// Removes the value with the specified key from the <see cref="ObservableDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="key">The key of the element to remove</param>
    /// <returns><c>true</c> if the element is successfully found and removed; otherwise, <c>false</c> (this method returns false if key is not found in the <see cref="ObservableDictionary{TKey, TValue}"/>)</returns>
    public override bool Remove(TKey key) =>
        this.Execute(() => base.Remove(key));

    /// <summary>
    /// Removes the element with the specified key from the <see cref="IDictionary"/> object
    /// </summary>
    /// <param name="key">The key of the element to remove</param>
    protected override void Remove(object key) =>
        this.Execute(() => base.Remove(key));

    /// <summary>
    /// Removes the first occurrence of a specific object from the <see cref="ICollection{T}"/>
    /// </summary>
    /// <param name="item">The object to remove from the <see cref="ICollection{T}"/></param>
    /// <returns><c>true</c> if item was successfully removed from the <see cref="ICollection{T}"/>; otherwise, <c>false</c></returns>
    protected override bool Remove(KeyValuePair<TKey, TValue> item) =>
        this.Execute(() => base.Remove(item));

    /// <summary>
    /// Removes any elements that satisfy the specified predicate from the <see cref="IRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="predicate">A predicate that returns <c>true</c> when passed the key and value of an element to be removed</param>
    /// <returns>The key-value pairs of the elements that were removed</returns>
    public override IReadOnlyList<KeyValuePair<TKey, TValue>> RemoveAll(Func<TKey, TValue, bool> predicate) =>
        this.Execute(() => base.RemoveAll(predicate))!;

    /// <summary>
    /// Removes any elements that satisfy the specified predicate from the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="predicate">A predicate that returns <c>true</c> when passed the key and value of an element to be removed</param>
    /// <returns>The key-value pairs of the elements that were removed</returns>
    public Task<IReadOnlyList<KeyValuePair<TKey, TValue>>> RemoveAllAsync(Func<TKey, TValue, bool> predicate) =>
        this.ExecuteAsync(() => base.RemoveAll(predicate));

    /// <summary>
    /// Removes any elements that satisfy the specified predicate from the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="asyncPredicate">An asynchronous predicate that returns <c>true</c> when passed the key and value of an element to be removed</param>
    /// <returns>The key-value pairs of the elements that were removed</returns>
    public Task<IReadOnlyList<KeyValuePair<TKey, TValue>>> RemoveAllAsync(Func<TKey, TValue, Task<bool>> asyncPredicate) =>
        this.ExecuteAsync(async () =>
        {
            var removing = new List<KeyValuePair<TKey, TValue>>();
            foreach (var kv in this.ToList())
                if (await asyncPredicate(kv.Key, kv.Value).ConfigureAwait(false))
                    removing.Add(kv);
            var removedKeys = new HashSet<TKey>(RemoveRange(removing.Select(kv => kv.Key)));
            return (IReadOnlyList<KeyValuePair<TKey, TValue>>)removing.Where(kv => removedKeys.Contains(kv.Key)).ToImmutableArray();
        });

    /// <summary>
    /// Removes the element with the specified key from the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="key">The key of the element to remove</param>
    /// <returns><c>true</c> if the element is successfully removed; otherwise, <c>false</c></returns>
    public Task<bool> RemoveAsync(TKey key) =>
        this.ExecuteAsync(() => base.Remove(key));

    /// <summary>
    /// Removes the elements with any of the specified keys from the <see cref="IRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="keys">The keys of the elements to remove</param>
    /// <returns>The keys of the elements that were found and removed</returns>
    public override IReadOnlyList<TKey> RemoveRange(IEnumerable<TKey> keys) =>
        this.Execute(() => base.RemoveRange(keys))!;

    /// <summary>
    /// Removes the elements with any of the specified keys from the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="keys">The keys of the elements to remove</param>
    /// <returns>The keys of the elements that were found and removed</returns>
    public Task<IReadOnlyList<TKey>> RemoveRangeAsync(IEnumerable<TKey> keys) =>
        this.ExecuteAsync(() => base.RemoveRange(keys));

    /// <summary>
    /// Replaces elements in the <see cref="IRangeDictionary{TKey, TValue}"/> with specified elements
    /// </summary>
    /// <param name="keyValuePairs">The replacement key-value pairs</param>
    public override void ReplaceRange(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs) =>
        this.Execute(() => base.ReplaceRange(keyValuePairs));

    /// <summary>
    /// Removes the elements with any of the specified keys from and then adds elements with the provided keys and values to the <see cref="IRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="removeKeys">The keys of the elements to remove</param>
    /// <param name="newKeyValuePairs">The key-value pairs to add</param>
    /// <returns>The keys of the elements that were found and removed</returns>
    public override IReadOnlyList<TKey> ReplaceRange(IEnumerable<TKey> removeKeys, IEnumerable<KeyValuePair<TKey, TValue>> newKeyValuePairs) =>
        this.Execute(() => base.ReplaceRange(removeKeys, newKeyValuePairs))!;

    /// <summary>
    /// Replaces elements in the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/> with specified elements
    /// </summary>
    /// <param name="keyValuePairs">The replacement key-value pairs</param>
    public Task ReplaceRangeAsync(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs) =>
        this.ExecuteAsync(() => base.ReplaceRange(keyValuePairs));

    /// <summary>
    /// Removes the elements with any of the specified keys from and then adds elements with the provided keys and values to the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="removeKeys">The keys of the elements to remove</param>
    /// <param name="newKeyValuePairs">The key-value pairs to add</param>
    /// <returns>The keys of the elements that were found and removed</returns>
    public Task<IReadOnlyList<TKey>> ReplaceRangeAsync(IEnumerable<TKey> removeKeys, IEnumerable<KeyValuePair<TKey, TValue>> newKeyValuePairs) =>
        this.ExecuteAsync(() => base.ReplaceRange(removeKeys, newKeyValuePairs));

    /// <summary>
    /// Reinitializes the binary search tree used internally by the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/>, removing all elements
    /// </summary>
    public override void Reset() =>
        this.Execute(() => base.Reset());

    /// <summary>
    /// Reinitializes the binary search tree used internally by the <see cref="SynchronizedObservableSortedDictionary{TKey, TValue}"/> with the elements from the specified dictionary
    /// </summary>
    /// <param name="dictionary">The dictionary from which to retrieve the initial elements</param>
    public override void Reset(IDictionary<TKey, TValue> dictionary) =>
        this.Execute(() => base.Reset(dictionary));

    /// <summary>
    /// Reinitializes the hash table or binary search tree used internally by the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>, removing all elements
    /// </summary>
    public Task ResetAsync() =>
        this.ExecuteAsync(() => base.Reset());

    /// <summary>
    /// Reinitializes the hash table or binary search tree used internally by the <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/> with the elements from the specified dictionary
    /// </summary>
    /// <param name="dictionary">The dictionary from which to retrieve the initial elements</param>
    public Task ResetAsync(IDictionary<TKey, TValue> dictionary) =>
        this.ExecuteAsync(() => base.Reset(dictionary));

    /// <summary>
    /// Sets the element with the specified key
    /// </summary>
    /// <param name="key">The key of the element to set</param>
    /// <param name="value">The new value for the element</param>
    protected override void SetValue(object key, object value) =>
        this.Execute(() => base.SetValue(key, value));

    /// <summary>
    /// Sets the element with the specified key
    /// </summary>
    /// <param name="key">The key of the element to set</param>
    /// <param name="value">The element with the specified key</param>
    public Task SetValueAsync(TKey key, TValue value) =>
        this.ExecuteAsync(() => base[key] = value);

    /// <summary>
    /// Gets the value associated with the specified key
    /// </summary>
    /// <param name="key">The key the value of which to get</param>
    /// <returns><c>true</c> if the object that implements <see cref="IDictionary{TKey, TValue}"/> contains an element with the specified key and the value that was found; otherwise, <c>false</c> and the default value of <typeparamref name="TValue"/></returns>
    protected override (bool valueRetrieved, TValue value) TryGetValue(TKey key) =>
        this.Execute(() => base.TryGetValue(key));

    /// <summary>
    /// Gets the value associated with the specified key
    /// </summary>
    /// <param name="key">The key the value of which to get</param>
    /// <returns><c>true</c> if the object that implements <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/> contains an element with the specified key and the value that was retrieved; otherwise, <c>false</c> and the default <typeparamref name="TValue"/></returns>
    public Task<(bool valueRetrieved, TValue value)> TryGetValueAsync(TKey key) =>
        this.ExecuteAsync(() => base.TryGetValue(key));
}
