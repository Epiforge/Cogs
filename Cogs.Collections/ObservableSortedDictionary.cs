namespace Cogs.Collections;

/// <summary>
/// Represents a collection of key/value pairs that are sorted on the key, that supports bulk operations and notifies listeners of dynamic changes, such as when an item is added and removed or the whole list is cleared
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
[SuppressMessage("Code Analysis", "CA1033: Interface methods should be callable by child types")]
public class ObservableSortedDictionary<TKey, TValue> :
    PropertyChangeNotifier,
    ICollection,
    ICollection<KeyValuePair<TKey, TValue>>,
    IDictionary,
    IDictionary<TKey, TValue>,
    IEnumerable,
    IEnumerable<KeyValuePair<TKey, TValue>>,
    IObservableRangeDictionary<TKey, TValue>,
    IReadOnlyCollection<KeyValuePair<TKey, TValue>>,
    IReadOnlyDictionary<TKey, TValue>,
    ISortKeys<TKey>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableSortedDictionary{TKey, TValue}"/> class that is empty and uses the default <see cref="IComparer{T}"/> implementation for the key type
    /// </summary>
    public ObservableSortedDictionary()
    {
        gsd = new SortedDictionary<TKey, TValue>();
        ci = gsd;
        gci = gsd;
        di = gsd;
        gdi = gsd;
        ei = gsd;
        gei = gsd;
        grodi = gsd;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableSortedDictionary{TKey, TValue}"/> class that is empty and uses the specified <see cref="IComparer{T}"/> implementation to compare keys
    /// </summary>
    /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing keys, or <c>null</c> to use the default <see cref="Comparer{T}"/> for the type of the key</param>
    public ObservableSortedDictionary(IComparer<TKey> comparer)
    {
        gsd = new SortedDictionary<TKey, TValue>(comparer);
        ci = gsd;
        gci = gsd;
        di = gsd;
        gdi = gsd;
        ei = gsd;
        gei = gsd;
        grodi = gsd;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableSortedDictionary{TKey, TValue}"/> class that contains elements copied from the specified <see cref="IDictionary{TKey, TValue}"/> and uses the default <see cref="IComparer{T}"/> implementation for the key type
    /// </summary>
    /// <param name="dictionary">The <see cref="IDictionary{TKey, TValue}"/> whose elements are copied to the new <see cref="ObservableSortedDictionary{TKey, TValue}"/></param>
    public ObservableSortedDictionary(IDictionary<TKey, TValue> dictionary)
    {
        gsd = new SortedDictionary<TKey, TValue>(dictionary);
        ci = gsd;
        gci = gsd;
        di = gsd;
        gdi = gsd;
        ei = gsd;
        gei = gsd;
        grodi = gsd;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableSortedDictionary{TKey, TValue}"/> class that contains elements copied from the specified <see cref="IDictionary{TKey, TValue}"/> and uses the specified <see cref="IComparer{T}"/> implementation to compare keys
    /// </summary>
    /// <param name="dictionary">The <see cref="IDictionary{TKey, TValue}"/> whose elements are copied to the new <see cref="ObservableSortedDictionary{TKey, TValue}"/></param>
    /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing keys, or <c>null</c> to use the default <see cref="Comparer{T}"/> for the type of the key</param>
    public ObservableSortedDictionary(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer)
    {
        gsd = new SortedDictionary<TKey, TValue>(dictionary, comparer);
        ci = gsd;
        gci = gsd;
        di = gsd;
        gdi = gsd;
        ei = gsd;
        gei = gsd;
        grodi = gsd;
    }

    SortedDictionary<TKey, TValue> gsd;
    ICollection ci;
    ICollection<KeyValuePair<TKey, TValue>> gci;
    IDictionary di;
    IDictionary<TKey, TValue> gdi;
    IEnumerable ei;
    IEnumerable<KeyValuePair<TKey, TValue>> gei;
    IReadOnlyDictionary<TKey, TValue> grodi;

    /// <summary>
    /// Occurs when the collection changes
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Occurs when the dictionary changes
    /// </summary>
    public event EventHandler<NotifyDictionaryChangedEventArgs<TKey, TValue>>? DictionaryChanged;

    event EventHandler<NotifyDictionaryChangedEventArgs<object?, object?>>? DictionaryChangedBoxed;

    /// <summary>
    /// Occurs when the collection changes
    /// </summary>
    public event NotifyGenericCollectionChangedEventHandler<KeyValuePair<TKey, TValue>>? GenericCollectionChanged;

    /// <summary>
    /// Adds the specified key and value to the dictionary
    /// </summary>
    /// <param name="key">The key of the element to add</param>
    /// <param name="value">The value of the element to add</param>
    public virtual void Add(TKey key, TValue value)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));
        if (!gsd.ContainsKey(key))
            NotifyCountChanging();
        gsd.Add(key, value);
        OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, key, value));
        NotifyCountChanged();
    }

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) =>
        Add(item);

    void IDictionary.Add(object key, object value) =>
        Add(key, value);

    /// <summary>
    /// Adds an element with the provided key and value to the <see cref="IDictionary"/> object
    /// </summary>
    /// <param name="key">The object to use as the key of the element to add</param>
    /// <param name="value">The object to use as the value of the element to add</param>
    protected virtual void Add(object key, object value)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));
        if (key is TKey typedKey && !gsd.ContainsKey(typedKey))
            NotifyCountChanging();
        di.Add(key, value);
        OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, (TKey)key, (TValue)value));
        NotifyCountChanged();
    }

    /// <summary>
    /// Adds an item to the <see cref="ICollection{T}"/>
    /// </summary>
    /// <param name="item">The object to add to the <see cref="ICollection{T}"/></param>
    [SuppressMessage("Usage", "CA2208: Instantiate argument exceptions correctly")]
    protected virtual void Add(KeyValuePair<TKey, TValue> item)
    {
        if (item.Key is null)
            throw new ArgumentNullException("key");
        if (!gsd.ContainsKey(item.Key))
            NotifyCountChanging();
        gci.Add(item);
        OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, item));
        NotifyCountChanged();
    }

    /// <summary>
    /// Adds elements with the provided keys and values to the <see cref="IRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="keyValuePairs">The key-value pairs to add</param>
    public virtual void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs) =>
        AddRange(keyValuePairs.ToImmutableArray());

    /// <summary>
    /// Adds elements with the provided keys and values to the <see cref="IRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="keyValuePairs">The key-value pairs to add</param>
    public virtual void AddRange(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs)
    {
        if (keyValuePairs is null)
            throw new ArgumentNullException(nameof(keyValuePairs));
        if (keyValuePairs.Any(kvp => kvp.Key is null || gsd.ContainsKey(kvp.Key)))
            throw new ArgumentException("One of the keys was null or already found in the dictionary", nameof(keyValuePairs));
        if (keyValuePairs.Count > 0)
        {
            NotifyCountChanging();
            foreach (var keyValuePair in keyValuePairs)
                gsd.Add(keyValuePair.Key, keyValuePair.Value);
            OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, keyValuePairs));
            NotifyCountChanged();
        }
    }

    void CastAndNotifyReset()
    {
        ci = gsd;
        gci = gsd;
        di = gsd;
        gdi = gsd;
        ei = gsd;
        gei = gsd;
        grodi = gsd;
        OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Reset));
    }

    /// <summary>
    /// Removes all elements from the <see cref="ObservableSortedDictionary{TKey, TValue}"/>
    /// </summary>
    public virtual void Clear()
    {
        if (Count > 0)
        {
            NotifyCountChanging();
            gsd.Clear();
            OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Reset));
            NotifyCountChanged();
        }
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) =>
        Contains(item);

    bool IDictionary.Contains(object key) =>
        Contains(key);

    /// <summary>
    /// Determines whether the <see cref="IDictionary"/> contains the specified key
    /// </summary>
    /// <param name="key">The key to locate in the <see cref="IDictionary"/></param>
    /// <returns><c>true</c> if the <see cref="IDictionary"/> contains an element with the specified key; otherwise, <c>false</c></returns>
    protected virtual bool Contains(object key) =>
        di.Contains(key);

    /// <summary>
    /// Determines whether the <see cref="ICollection{T}"/> contains a specific value
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="ICollection{T}"/></param>
    /// <returns><c>true</c> if <paramref name="item"/> is found in the <see cref="ICollection{T}"/>; otherwise, <c>false</c></returns>
    protected virtual bool Contains(KeyValuePair<TKey, TValue> item) =>
        gci.Contains(item);

    /// <summary>
    /// Determines whether the <see cref="IDictionary{TKey, TValue}"/> contains an element with the specified key
    /// </summary>
    /// <param name="key">The key to locate in the <see cref="IDictionary{TKey, TValue}"/></param>
    /// <returns><c>true</c> if the <see cref="IDictionary{TKey, TValue}"/> contains an element with the key; otherwise, <c>false</c></returns>
    public virtual bool ContainsKey(TKey key) =>
        gsd.ContainsKey(key);

    /// <summary>
    /// Determines whether the <see cref="ObservableSortedDictionary{TKey, TValue}"/> contains an element with the specified value
    /// </summary>
    /// <param name="value">The value to locate in the <see cref="ObservableSortedDictionary{TKey, TValue}"/></param>
    /// <returns><c>true</c> if the <see cref="ObservableSortedDictionary{TKey, TValue}"/> contains an element with the specified value; otherwise, <c>false</c></returns>
    public virtual bool ContainsValue(TValue value) =>
        gsd.ContainsValue(value);

    void ICollection.CopyTo(Array array, int index) =>
        CopyTo(array, index);

    /// <summary>
    /// Copies the elements of the <see cref="ICollection"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index
    /// </summary>
    /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ICollection"/> (the <see cref="Array"/> must have zero-based indexing)</param>
    /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins</param>
    protected virtual void CopyTo(Array array, int index) =>
        ci.CopyTo(array, index);

    /// <summary>
    /// Copies the elements of the <see cref="ICollection{T}"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index
    /// </summary>
    /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ICollection{T}"/> (the <see cref="Array"/> must have zero-based indexing)</param>
    /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins</param>
    public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int index) =>
        gsd.CopyTo(array, index);

    /// <summary>
    /// Returns an <see cref="IDictionaryEnumerator"/> object for the <see cref="IDictionary"/> object
    /// </summary>
    /// <returns>An <see cref="IDictionaryEnumerator"/> object for the <see cref="IDictionary"/> object</returns>
    protected virtual IDictionaryEnumerator GetDictionaryEnumerator() =>
        di.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="ObservableSortedDictionary{TKey, TValue}"/>
    /// </summary>
    /// <returns>A <see cref="SortedDictionary{TKey, TValue}.Enumerator"/> for the <see cref="ObservableSortedDictionary{TKey, TValue}"/></returns>
    public virtual SortedDictionary<TKey, TValue>.Enumerator GetEnumerator() =>
        gsd.GetEnumerator();

    IDictionaryEnumerator IDictionary.GetEnumerator() =>
        GetDictionaryEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        GetNonGenericEnumerator();

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() =>
        GetKeyValuePairEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the collection
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection</returns>
    protected virtual IEnumerator<KeyValuePair<TKey, TValue>> GetKeyValuePairEnumerator() =>
        gei.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through a collection
    /// </summary>
    /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection</returns>
    protected virtual IEnumerator GetNonGenericEnumerator() =>
        ei.GetEnumerator();

    /// <summary>
    /// Gets the elements with the specified keys
    /// </summary>
    /// <param name="keys">The keys of the elements to get</param>
    /// <returns>The elements with the specified keys</returns>
    public virtual IReadOnlyList<KeyValuePair<TKey, TValue>> GetRange(IEnumerable<TKey> keys) =>
        keys.Select(key => new KeyValuePair<TKey, TValue>(key, this[key])).ToImmutableArray();

    /// <summary>
    /// Gets the element with the specified key
    /// </summary>
    /// <param name="key">The key of the element to get</param>
    /// <returns>The element with the specified key, or <c>null</c> if the key does not exist</returns>
    protected virtual object GetValue(object key) =>
        di[key];

    /// <summary>
    /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event for the <see cref="Count"/> property
    /// </summary>
    protected void NotifyCountChanged() =>
        OnPropertyChanged(nameof(Count));

    /// <summary>
    /// Raises the <see cref="INotifyPropertyChanging.PropertyChanging"/> event for the <see cref="Count"/> property
    /// </summary>
    protected void NotifyCountChanging() =>
        OnPropertyChanging(nameof(Count));

    /// <summary>
    /// Calls <see cref="OnDictionaryChanged(NotifyDictionaryChangedEventArgs{TKey, TValue})"/> and also calls <see cref="OnCollectionChanged(NotifyCollectionChangedEventArgs)"/>, <see cref="OnDictionaryChangedBoxed(NotifyDictionaryChangedEventArgs{object, object})"/>, and <see cref="OnGenericCollectionChanged(NotifyGenericCollectionChangedEventArgs{KeyValuePair{TKey, TValue}})"/> when applicable
    /// </summary>
    /// <param name="e">The event arguments for <see cref="INotifyDictionaryChanged{TKey, TValue}.DictionaryChanged"/></param>
    protected virtual void OnChanged(NotifyDictionaryChangedEventArgs<TKey, TValue> e)
    {
        if (e is null)
            throw new ArgumentNullException(nameof(e));
        if (CollectionChanged != null)
            switch (e.Action)
            {
                case NotifyDictionaryChangedAction.Add:
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItems));
                    break;
                case NotifyDictionaryChangedAction.Remove:
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, e.OldItems));
                    break;
                case NotifyDictionaryChangedAction.Replace:
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, e.NewItems, e.OldItems));
                    break;
                case NotifyDictionaryChangedAction.Reset:
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    break;
                default:
                    throw new NotSupportedException();
            }
        if (GenericCollectionChanged != null)
            switch (e.Action)
            {
                case NotifyDictionaryChangedAction.Add:
                    OnGenericCollectionChanged(new NotifyGenericCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>(NotifyCollectionChangedAction.Add, e.NewItems));
                    break;
                case NotifyDictionaryChangedAction.Remove:
                    OnGenericCollectionChanged(new NotifyGenericCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>(NotifyCollectionChangedAction.Remove, e.OldItems));
                    break;
                case NotifyDictionaryChangedAction.Replace:
                    OnGenericCollectionChanged(new NotifyGenericCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>(NotifyCollectionChangedAction.Replace, e.NewItems, e.OldItems));
                    break;
                case NotifyDictionaryChangedAction.Reset:
                    OnGenericCollectionChanged(new NotifyGenericCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>(NotifyCollectionChangedAction.Reset));
                    break;
                default:
                    throw new NotSupportedException();
            }
        if (DictionaryChangedBoxed != null)
            switch (e.Action)
            {
                case NotifyDictionaryChangedAction.Add:
                    OnDictionaryChangedBoxed(new NotifyDictionaryChangedEventArgs<object?, object?>(NotifyDictionaryChangedAction.Add, e.NewItems.Select(kv => new KeyValuePair<object?, object?>(kv.Key, kv.Value))));
                    break;
                case NotifyDictionaryChangedAction.Remove:
                    OnDictionaryChangedBoxed(new NotifyDictionaryChangedEventArgs<object?, object?>(NotifyDictionaryChangedAction.Remove, e.OldItems.Select(kv => new KeyValuePair<object?, object?>(kv.Key, kv.Value))));
                    break;
                case NotifyDictionaryChangedAction.Replace:
                    OnDictionaryChangedBoxed(new NotifyDictionaryChangedEventArgs<object?, object?>(NotifyDictionaryChangedAction.Replace, e.NewItems.Select(kv => new KeyValuePair<object?, object?>(kv.Key, kv.Value)), e.OldItems.Select(kv => new KeyValuePair<object?, object?>(kv.Key, kv.Value))));
                    break;
                case NotifyDictionaryChangedAction.Reset:
                    OnDictionaryChangedBoxed(new NotifyDictionaryChangedEventArgs<object?, object?>(NotifyDictionaryChangedAction.Reset));
                    break;
            }
        OnDictionaryChanged(e);
    }

    /// <summary>
    /// Raises the <see cref="INotifyCollectionChanged.CollectionChanged"/> event
    /// </summary>
    /// <param name="e">The event arguments</param>
    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) =>
        CollectionChanged?.Invoke(this, e);

    /// <summary>
    /// Raises the <see cref="INotifyDictionaryChanged{TKey, TValue}.DictionaryChanged"/> event
    /// </summary>
    /// <param name="e">The event arguments</param>
    protected virtual void OnDictionaryChanged(NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
        DictionaryChanged?.Invoke(this, e);

    /// <summary>
    /// Raises the <see cref="INotifyDictionaryChanged.DictionaryChanged"/> event
    /// </summary>
    /// <param name="e">The event arguments</param>
    protected virtual void OnDictionaryChangedBoxed(NotifyDictionaryChangedEventArgs<object?, object?> e) =>
        DictionaryChangedBoxed?.Invoke(this, e);

    /// <summary>
    /// Raises the <see cref="INotifyGenericCollectionChanged{T}.GenericCollectionChanged"/> event
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnGenericCollectionChanged(NotifyGenericCollectionChangedEventArgs<KeyValuePair<TKey, TValue>> e) =>
        GenericCollectionChanged?.Invoke(this, e);

    /// <summary>
    /// Removes the value with the specified key from the <see cref="ObservableDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="key">The key of the element to remove</param>
    /// <returns><c>true</c> if the element is successfully found and removed; otherwise, <c>false</c> (this method returns false if key is not found in the <see cref="ObservableDictionary{TKey, TValue}"/>)</returns>
    public virtual bool Remove(TKey key)
    {
        if (gsd.TryGetValue(key, out var value))
        {
            NotifyCountChanging();
            gsd.Remove(key);
            OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Remove, key, value));
            NotifyCountChanged();
            return true;
        }
        return false;
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) =>
        Remove(item);

    void IDictionary.Remove(object key) =>
        Remove(key);

    /// <summary>
    /// Removes the element with the specified key from the <see cref="IDictionary"/> object
    /// </summary>
    /// <param name="key">The key of the element to remove</param>
    protected virtual void Remove(object key)
    {
        if (di.Contains(key))
        {
            var value = di[key];
            NotifyCountChanging();
            Remove(key);
            OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Remove, (TKey)key, (TValue)value));
            NotifyCountChanged();
        }
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the <see cref="ICollection{T}"/>
    /// </summary>
    /// <param name="item">The object to remove from the <see cref="ICollection{T}"/></param>
    /// <returns><c>true</c> if item was successfully removed from the <see cref="ICollection{T}"/>; otherwise, <c>false</c></returns>
    protected virtual bool Remove(KeyValuePair<TKey, TValue> item)
    {
        if (gci.Contains(item))
        {
            NotifyCountChanging();
            gci.Remove(item);
            OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Remove, item));
            NotifyCountChanged();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes any elements that satisfy the specified predicate from the <see cref="IRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="predicate">A predicate that returns <c>true</c> when passed the key and value of an element to be removed</param>
    /// <returns>The key-value pairs of the elements that were removed</returns>
    public virtual IReadOnlyList<KeyValuePair<TKey, TValue>> RemoveAll(Func<TKey, TValue, bool> predicate)
    {
        if (predicate is null)
            throw new ArgumentNullException(nameof(predicate));
        var removed = new List<KeyValuePair<TKey, TValue>>();
        foreach (var kv in gsd.ToList())
            if (predicate(kv.Key, kv.Value))
            {
                if (removed.Count == 0)
                    NotifyCountChanging();
                gsd.Remove(kv.Key);
                removed.Add(kv);
            }
        if (removed.Count > 0)
        {
            OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Remove, removed));
            NotifyCountChanged();
        }
        return removed.ToImmutableArray();
    }

    /// <summary>
    /// Removes the elements with any of the specified keys from the <see cref="IRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="keys">The keys of the elements to remove</param>
    /// <returns>The keys of the elements that were found and removed</returns>
    public virtual IReadOnlyList<TKey> RemoveRange(IEnumerable<TKey> keys)
    {
        if (keys is null)
            throw new ArgumentNullException(nameof(keys));
        var removingKeyValuePairs = new List<KeyValuePair<TKey, TValue>>();
        foreach (var key in keys)
            if (gsd.TryGetValue(key, out var value))
                removingKeyValuePairs.Add(new KeyValuePair<TKey, TValue>(key, value));
        var removedKeys = new List<TKey>();
        if (removingKeyValuePairs.Any())
        {
            NotifyCountChanging();
            foreach (var removingKeyValuePair in removingKeyValuePairs)
            {
                gsd.Remove(removingKeyValuePair.Key);
                removedKeys.Add(removingKeyValuePair.Key);
            }
            OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Remove, removingKeyValuePairs));
            NotifyCountChanged();
        }
        return removedKeys.ToImmutableArray();
    }

    /// <summary>
    /// Replaces elements in the <see cref="IRangeDictionary{TKey, TValue}"/> with specified elements
    /// </summary>
    /// <param name="keyValuePairs">The replacement key-value pairs</param>
    public virtual void ReplaceRange(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
    {
        if (keyValuePairs is null)
            throw new ArgumentNullException(nameof(keyValuePairs));
        if (keyValuePairs.Any(kvp => !gsd.ContainsKey(kvp.Key)))
            throw new ArgumentException("One of the keys was not found in the dictionary", nameof(keyValuePairs));
        var oldItems = GetRange(keyValuePairs.Select(kv => kv.Key));
        foreach (var keyValuePair in keyValuePairs)
            gsd[keyValuePair.Key] = keyValuePair.Value;
        OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Replace, keyValuePairs, oldItems));
    }

    /// <summary>
    /// Removes the elements with any of the specified keys from and then adds elements with the provided keys and values to the <see cref="IRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="removeKeys">The keys of the elements to remove</param>
    /// <param name="newKeyValuePairs">The key-value pairs to add</param>
    /// <returns>The keys of the elements that were found and removed</returns>
    public virtual IReadOnlyList<TKey> ReplaceRange(IEnumerable<TKey> removeKeys, IEnumerable<KeyValuePair<TKey, TValue>> newKeyValuePairs)
    {
        if (newKeyValuePairs is null)
            throw new ArgumentNullException(nameof(newKeyValuePairs));
        var removingKeys = removeKeys.ToImmutableHashSet();
        if (newKeyValuePairs.Where(kvp => !removingKeys.Contains(kvp.Key)).Any(kvp => kvp.Key is null || gsd.ContainsKey(kvp.Key)))
            throw new ArgumentException("One of the new keys was null or already found in the dictionary", nameof(newKeyValuePairs));
        var removingKeyValuePairs = new List<KeyValuePair<TKey, TValue>>();
        foreach (var key in removingKeys)
            if (gsd.TryGetValue(key, out var value))
                removingKeyValuePairs.Add(new KeyValuePair<TKey, TValue>(key, value));
        var countChanging = removingKeyValuePairs.Count != newKeyValuePairs.Count();
        if (countChanging)
            NotifyCountChanging();
        var removedKeys = new List<TKey>();
        foreach (var removingKeyValuePair in removingKeyValuePairs)
        {
            gsd.Remove(removingKeyValuePair.Key);
            removedKeys.Add(removingKeyValuePair.Key);
        }
        foreach (var keyValuePair in newKeyValuePairs)
            gsd.Add(keyValuePair.Key, keyValuePair.Value);
        OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Replace, newKeyValuePairs, removingKeyValuePairs));
        if (countChanging)
            NotifyCountChanged();
        return removedKeys.ToImmutableArray();
    }

    /// <summary>
    /// Reinitializes the binary search tree used internally by the <see cref="ObservableSortedDictionary{TKey, TValue}"/>, removing all elements
    /// </summary>
    public virtual void Reset()
    {
        var countChanging = gsd.Count > 0;
        if (countChanging)
            NotifyCountChanging();
        gsd = new SortedDictionary<TKey, TValue>(gsd.Comparer);
        CastAndNotifyReset();
        if (countChanging)
            NotifyCountChanged();
    }

    /// <summary>
    /// Reinitializes the binary search tree used internally by the <see cref="ObservableSortedDictionary{TKey, TValue}"/> with the elements from the specified dictionary
    /// </summary>
    /// <param name="dictionary">The dictionary from which to retrieve the initial elements</param>
    public virtual void Reset(IDictionary<TKey, TValue> dictionary)
    {
        var countChanging = gsd.Count > 0;
        if (countChanging)
            NotifyCountChanging();
        gsd = new SortedDictionary<TKey, TValue>(dictionary, gsd.Comparer);
        CastAndNotifyReset();
        if (countChanging)
            NotifyCountChanged();
    }

    /// <summary>
    /// Sets the element with the specified key
    /// </summary>
    /// <param name="key">The key of the element to set</param>
    /// <param name="value">The new value for the element</param>
    protected virtual void SetValue(object key, object value)
    {
        var oldValue = GetValue(key);
        di[key] = value;
        OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Replace, (TKey)key, (TValue)value, (TValue)oldValue));
    }

    /// <summary>
    /// Gets the value associated with the specified key
    /// </summary>
    /// <param name="key">The key the value of which to get</param>
    /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter</param>
    /// <returns><c>true</c> if the object that implements <see cref="IDictionary{TKey, TValue}"/> contains an element with the specified key; otherwise, <c>false</c></returns>
    public bool TryGetValue(TKey key, out TValue value)
    {
        bool valueRetrieved;
        (valueRetrieved, value) = TryGetValue(key);
        return valueRetrieved;
    }

    /// <summary>
    /// Gets the value associated with the specified key
    /// </summary>
    /// <param name="key">The key the value of which to get</param>
    /// <returns><c>true</c> if the object that implements <see cref="IDictionary{TKey, TValue}"/> contains an element with the specified key and the value that was found; otherwise, <c>false</c> and the default value of <typeparamref name="TValue"/></returns>
    protected virtual (bool valueRetrieved, TValue value) TryGetValue(TKey key)
    {
        var valueRetrieved = gsd.TryGetValue(key, out var value);
        return (valueRetrieved, value);
    }

    event EventHandler<NotifyDictionaryChangedEventArgs<object?, object?>>? INotifyDictionaryChanged.DictionaryChanged
    {
        add => DictionaryChangedBoxed += value;
        remove => DictionaryChangedBoxed -= value;
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key
    /// </summary>
    /// <param name="key">The key of the value to get or set</param>
    /// <returns>The value associated with the specified key</returns>
    public virtual TValue this[TKey key]
    {
        get => gsd[key];
        set
        {
            var oldValue = gsd[key];
            gsd[key] = value;
            OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Replace, key, value, oldValue));
        }
    }

    object IDictionary.this[object key]
    {
        get => GetValue(key);
        set => SetValue(key, value);
    }

    /// <summary>
    /// Gets the <see cref="IComparer{T}"/> used to order the elements of the <see cref="ObservableSortedDictionary{TKey, TValue}"/>
    /// </summary>
    public virtual IComparer<TKey> Comparer =>
        gsd.Comparer;

    /// <summary>
    /// Gets the number of elements in the collection
    /// </summary>
    public virtual int Count =>
        gsd.Count;

    /// <summary>
    /// Gets a value that indicates whether the <see cref="IDictionary"/> is read-only
    /// </summary>
    protected virtual bool DictionaryIsReadOnly =>
        di.IsReadOnly;

    /// <summary>
    /// Gets a value indicating whether the <see cref="ICollection{T}"/> is read-only
    /// </summary>
    protected virtual bool GenericCollectionIsReadOnly =>
        gci.IsReadOnly;

    /// <summary>
    /// Gets a value that indicates whether access to the <see cref="ICollection"/> is synchronized (thread safe)
    /// </summary>
    protected virtual bool IsCollectionSynchronized =>
        ci.IsSynchronized;

    bool IDictionary.IsFixedSize =>
        IsFixedSize;

    /// <summary>
    /// Gets a value that indicates whether the <see cref="IDictionary"/> has a fixed size
    /// </summary>
    protected virtual bool IsFixedSize =>
        di.IsFixedSize;

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly =>
        GenericCollectionIsReadOnly;

    bool IDictionary.IsReadOnly =>
        DictionaryIsReadOnly;

    bool ICollection.IsSynchronized =>
        IsCollectionSynchronized;

    /// <summary>
    /// Gets an <see cref="ICollection{T}"/> containing the keys of the <see cref="IDictionary{TKey, TValue}"/>
    /// </summary>
    public virtual SortedDictionary<TKey, TValue>.KeyCollection Keys =>
        gsd.Keys;

    ICollection IDictionary.Keys =>
        KeysCollection;

    ICollection<TKey> IDictionary<TKey, TValue>.Keys =>
        KeysGenericCollection;

    IEnumerable<TKey> IRangeDictionary<TKey, TValue>.Keys =>
        KeysGenericEnumerable;

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys =>
        KeysGenericEnumerable;

    /// <summary>
    /// Gets an <see cref="ICollection"/> containing the keys of the <see cref="IDictionary"/>
    /// </summary>
    protected virtual ICollection KeysCollection =>
        di.Keys;

    /// <summary>
    /// Gets an <see cref="ICollection{T}"/> containing the keys of the <see cref="IDictionary{TKey, TValue}"/>
    /// </summary>
    protected virtual ICollection<TKey> KeysGenericCollection =>
        gdi.Keys;

    /// <summary>
    /// Gets a collection containing the keys of the <see cref="IReadOnlyDictionary{TKey, TValue}"/>
    /// </summary>
    protected virtual IEnumerable<TKey> KeysGenericEnumerable =>
        grodi.Keys;

    object ICollection.SyncRoot =>
        SyncRoot;

    /// <summary>
    /// Gets an object that can be used to synchronize access to the <see cref="ICollection"/>
    /// </summary>
    protected virtual object SyncRoot =>
        ci.SyncRoot;

    /// <summary>
    /// Gets an <see cref="ICollection{T}"/> containing the values in the <see cref="IDictionary{TKey, TValue}"/>
    /// </summary>
    public virtual SortedDictionary<TKey, TValue>.ValueCollection Values =>
        gsd.Values;

    ICollection IDictionary.Values =>
        ValuesCollection;

    ICollection<TValue> IDictionary<TKey, TValue>.Values =>
        ValuesGenericCollection;

    IEnumerable<TValue> IRangeDictionary<TKey, TValue>.Values =>
        ValuesGenericEnumerable;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values =>
        ValuesGenericEnumerable;

    /// <summary>
    /// Gets an <see cref="ICollection"/> containing the values in the <see cref="IDictionary"/>
    /// </summary>
    protected virtual ICollection ValuesCollection =>
        di.Values;

    /// <summary>
    /// Gets an <see cref="ICollection{T}"/> containing the values in the <see cref="IDictionary{TKey, TValue}"/>
    /// </summary>
    protected virtual ICollection<TValue> ValuesGenericCollection =>
        gdi.Values;

    /// <summary>
    /// Gets a collection containing the values in the <see cref="ObservableDictionary{TKey, TValue}"/>
    /// </summary>
    protected virtual IEnumerable<TValue> ValuesGenericEnumerable =>
        grodi.Values;
}
