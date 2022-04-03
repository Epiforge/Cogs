namespace Cogs.Collections;

/// <summary>
/// Read-only wrapper around an <see cref="ObservableConcurrentDictionary{TKey, TValue}"/>
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
[SuppressMessage("Code Analysis", "CA1033: Interface methods should be callable by child types")]
public class ReadOnlyObservableConcurrentDictionary<TKey, TValue> :
    PropertyChangeNotifier,
    ICollection,
    ICollection<KeyValuePair<TKey, TValue>>,
    IEnumerable,
    IEnumerable<KeyValuePair<TKey, TValue>>,
    IDictionary,
    IDictionary<TKey, TValue>,
    IHashKeys<TKey>,
    INotifyCollectionChanged,
    INotifyDictionaryChanged,
    INotifyDictionaryChanged<TKey, TValue>,
    IReadOnlyCollection<KeyValuePair<TKey, TValue>>,
    IReadOnlyDictionary<TKey, TValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyObservableConcurrentDictionary{TKey, TValue}"/> class
    /// </summary>
    /// <param name="observableConcurrentDictionary">The <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> around which to wrap</param>
    public ReadOnlyObservableConcurrentDictionary(ObservableConcurrentDictionary<TKey, TValue> observableConcurrentDictionary)
    {
        ocd = observableConcurrentDictionary;
        ocd.CollectionChanged += ObservableConcurrentDictionaryCollectionChanged;
        ((INotifyDictionaryChanged)ocd).DictionaryChanged += ObservableConcurrentDictionaryDictionaryChanged;
        ocd.DictionaryChanged += ObservableConcurrentDictionaryDictionaryChanged;
        ocd.PropertyChanged += ObservableConcurrentDictionaryPropertyChanged;
        ocd.PropertyChanging += ObservableConcurrentDictionaryPropertyChanging;
    }

    readonly ObservableConcurrentDictionary<TKey, TValue> ocd;

    /// <summary>
    /// Gets the value associated with the specified key
    /// </summary>
    /// <param name="key">The key of the value to get or set</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c></exception>
    /// <exception cref="KeyNotFoundException">The property is retrieved and <paramref name="key"/> does not exist in the collection</exception>
    public TValue this[TKey key] =>
        ocd[key];

    object IDictionary.this[object key]
    {
        get => ((IDictionary)ocd)[key];
        set => throw new NotSupportedException();
    }

    TValue IDictionary<TKey, TValue>.this[TKey key]
    {
        get => ocd[key];
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets the equality comparison implementation used when comparing keys
    /// </summary>
    public IEqualityComparer<TKey> Comparer =>
        ocd.Comparer;

    /// <summary>
    /// Gets the number of key/value pairs contained in the <see cref="ReadOnlyObservableConcurrentDictionary{TKey, TValue}"/>
    /// </summary>
    public int Count =>
        ocd.Count;

    /// <summary>
    /// Gets a value that indicates whether the <see cref="ReadOnlyObservableConcurrentDictionary{TKey, TValue}"/> is empty
    /// </summary>
    public bool IsEmpty =>
        ocd.IsEmpty;

    bool IDictionary.IsFixedSize =>
        ((IDictionary)ocd).IsFixedSize;

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly =>
        ((ICollection<KeyValuePair<TKey, TValue>>)ocd).IsReadOnly;

    bool IDictionary.IsReadOnly =>
        ((IDictionary)ocd).IsReadOnly;

    bool ICollection.IsSynchronized =>
        ((ICollection)ocd).IsSynchronized;

    /// <summary>
    /// Gets a collection containing the keys in the <see cref="ReadOnlyObservableConcurrentDictionary{TKey, TValue}"/>
    /// </summary>
    public ICollection<TKey> Keys =>
        ocd.Keys;

    ICollection IDictionary.Keys =>
        ((IDictionary)ocd).Keys;

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys =>
        ((IReadOnlyDictionary<TKey, TValue>)ocd).Keys;

    object ICollection.SyncRoot =>
        ((ICollection)ocd).SyncRoot;

    /// <summary>
    /// Gets a collection that contains the values in the <see cref="ReadOnlyObservableConcurrentDictionary{TKey, TValue}"/>
    /// </summary>
    public ICollection<TValue> Values =>
        ocd.Values;

    ICollection IDictionary.Values =>
        ((IDictionary)ocd).Values;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values =>
        ((IReadOnlyDictionary<TKey, TValue>)ocd).Values;

    /// <summary>
    /// Occurs when the collection changes
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Occurs when the dictionary changes
    /// </summary>
    public event EventHandler<NotifyDictionaryChangedEventArgs<TKey, TValue>>? DictionaryChanged;

    event EventHandler<NotifyDictionaryChangedEventArgs<object?, object?>>? INotifyDictionaryChanged.DictionaryChanged
    {
        add => DictionaryChangedBoxed += value;
        remove => DictionaryChangedBoxed -= value;
    }

    event EventHandler<NotifyDictionaryChangedEventArgs<object?, object?>>? DictionaryChangedBoxed;

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) =>
        throw new NotSupportedException();

    void IDictionary<TKey, TValue>.Add(TKey key, TValue value) =>
        throw new NotSupportedException();

    void IDictionary.Add(object key, object value) =>
        throw new NotSupportedException();

    void ICollection<KeyValuePair<TKey, TValue>>.Clear() =>
        throw new NotSupportedException();

    void IDictionary.Clear() =>
        throw new NotSupportedException();

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) =>
        ((ICollection<KeyValuePair<TKey, TValue>>)ocd).Contains(item);

    bool IDictionary.Contains(object key) => ((IDictionary)ocd).Contains(key);

    /// <summary>
    /// Determines whether the <see cref="ReadOnlyObservableConcurrentDictionary{TKey, TValue}"/> contains the specified key
    /// </summary>
    /// <param name="key">The key to locate in the <see cref="ReadOnlyObservableConcurrentDictionary{TKey, TValue}"/></param>
    /// <returns><c>true</c> if the <see cref="ReadOnlyObservableConcurrentDictionary{TKey, TValue}"/> contains an element with the specified key; otherwise, <c>false</c></returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c></exception>
    public bool ContainsKey(TKey key) =>
        ocd.ContainsKey(key);

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<TKey, TValue>>)ocd).CopyTo(array, arrayIndex);

    void ICollection.CopyTo(Array array, int index) =>
        ((ICollection)ocd).CopyTo(array, index);

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="ReadOnlyObservableConcurrentDictionary{TKey, TValue}"/>
    /// </summary>
    /// <returns>An enumerator for the <see cref="ReadOnlyObservableConcurrentDictionary{TKey, TValue}"/></returns>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() =>
        ocd.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        ((IEnumerable)ocd).GetEnumerator();

    IDictionaryEnumerator IDictionary.GetEnumerator() =>
        ((IDictionary)ocd).GetEnumerator();

    void ObservableConcurrentDictionaryCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
        OnCollectionChanged(e);

    void ObservableConcurrentDictionaryDictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<object?, object?> e) =>
        OnDictionaryChangedBoxed(e);

    void ObservableConcurrentDictionaryDictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
        OnDictionaryChanged(e);

    void ObservableConcurrentDictionaryPropertyChanged(object sender, PropertyChangedEventArgs e) =>
        OnPropertyChanged(e);

    void ObservableConcurrentDictionaryPropertyChanging(object sender, PropertyChangingEventArgs e) =>
        OnPropertyChanging(e);

    /// <summary>
    /// Raises the <see cref="INotifyCollectionChanged.CollectionChanged"/> event
    /// </summary>
    /// <param name="e">The event arguments</param>
    protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e) =>
        CollectionChanged?.Invoke(this, e);

    /// <summary>
    /// Raises the <see cref="INotifyDictionaryChanged{TKey, TValue}.DictionaryChanged"/> event
    /// </summary>
    /// <param name="e">The event arguments</param>
    protected void OnDictionaryChanged(NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
        DictionaryChanged?.Invoke(this, e);

    /// <summary>
    /// Raises the <see cref="INotifyDictionaryChanged.DictionaryChanged"/> event
    /// </summary>
    /// <param name="e">The event arguments</param>
    protected void OnDictionaryChangedBoxed(NotifyDictionaryChangedEventArgs<object?, object?> e) =>
        DictionaryChangedBoxed?.Invoke(this, e);

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) =>
        throw new NotSupportedException();

    void IDictionary.Remove(object key) =>
        throw new NotSupportedException();

    bool IDictionary<TKey, TValue>.Remove(TKey key) =>
        throw new NotSupportedException();

    /// <summary>
    /// Copies the key and value pairs stored in the <see cref="ReadOnlyObservableConcurrentDictionary{TKey, TValue}"/> to a new array
    /// </summary>
    /// <returns>A new array containing a snapshot of key and value pairs copied from the <see cref="ReadOnlyObservableConcurrentDictionary{TKey, TValue}"/></returns>
    public KeyValuePair<TKey, TValue>[] ToArray() =>
        ocd.ToArray();

    /// <summary>
    /// Attempts to get the value associated with the specified key from the <see cref="ReadOnlyObservableConcurrentDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="key">The key of the value to get</param>
    /// <param name="value">When this method returns, contains the object from the <see cref="ReadOnlyObservableConcurrentDictionary{TKey, TValue}"/> that has the specified key, or the default value of the type if the operation failed</param>
    /// <returns><c>true</c> if the key was found in the <see cref="ReadOnlyObservableConcurrentDictionary{TKey, TValue}"/>; otherwise, <c>false</c></returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c></exception>
    public bool TryGetValue(TKey key, out TValue value) =>
        ocd.TryGetValue(key, out value);
}
