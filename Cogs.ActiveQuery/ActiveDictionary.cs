namespace Cogs.ActiveQuery;

/// <summary>
/// Represents a generic read-only collection of key-value pairs that is the result of an active query
/// </summary>
/// <typeparam name="TKey">The type of keys</typeparam>
/// <typeparam name="TValue">The type of values</typeparam>
public sealed class ActiveDictionary<TKey, TValue> :
    SyncDisposable,
    IActiveDictionary<TKey, TValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveDictionary{TKey, TValue}"/> class
    /// </summary>
    /// <param name="readOnlyDictionary">The read-only dictionary upon which the <see cref="ActiveDictionary{TKey, TValue}"/> is based</param>
    /// <param name="onDispose">The action to take when the <see cref="ActiveDictionary{TKey, TValue}"/> is disposed</param>
    public ActiveDictionary(IReadOnlyDictionary<TKey, TValue> readOnlyDictionary, Action? onDispose = null)
    {
        synchronized = readOnlyDictionary as ISynchronized;
        this.readOnlyDictionary = readOnlyDictionary is ActiveDictionary<TKey, TValue> activeDictionary ? activeDictionary.readOnlyDictionary : readOnlyDictionary;
        if (this.readOnlyDictionary is INotifyDictionaryChanged dictionaryNotifier)
            dictionaryNotifier.DictionaryChanged += DictionaryChangedHandler;
        if (this.readOnlyDictionary is INotifyDictionaryChanged<TKey, TValue> genericDictionaryNotifier)
            genericDictionaryNotifier.DictionaryChanged += GenericDictionaryChangedHandler;
        if (this.readOnlyDictionary is INotifyPropertyChanged propertyChangedNotifier)
            propertyChangedNotifier.PropertyChanged += PropertyChangedHandler;
        if (this.readOnlyDictionary is INotifyPropertyChanging propertyChangingNotifier)
            propertyChangingNotifier.PropertyChanging += PropertyChangingHandler;
        this.onDispose = onDispose;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveDictionary{TKey, TValue}"/> class
    /// </summary>
    /// <param name="readOnlyDictionary">The read-only dictionary upon which the <see cref="ActiveDictionary{TKey, TValue}"/> is based</param>
    /// <param name="faultNotifier">The <see cref="INotifyElementFaultChanges"/> for the underlying data of the <see cref="ActiveDictionary{TKey, TValue}"/></param>
    /// <param name="onDispose">The action to take when the <see cref="ActiveDictionary{TKey, TValue}"/> is disposed</param>
    public ActiveDictionary(IReadOnlyDictionary<TKey, TValue> readOnlyDictionary, INotifyElementFaultChanges? faultNotifier, Action? onDispose = null) :
        this(readOnlyDictionary, onDispose)
    {
        this.faultNotifier = faultNotifier ?? (readOnlyDictionary as INotifyElementFaultChanges);
        if (this.faultNotifier is { })
        {
            this.faultNotifier.ElementFaultChanged += FaultNotifierElementFaultChanged;
            this.faultNotifier.ElementFaultChanging += FaultNotifierElementFaultChanging;
        }
    }

    readonly INotifyElementFaultChanges? faultNotifier;
    readonly Action? onDispose;
    Exception? operationFault;
    readonly IReadOnlyDictionary<TKey, TValue> readOnlyDictionary;
    readonly ISynchronized? synchronized;

    event EventHandler<NotifyDictionaryChangedEventArgs<object?, object?>>? INotifyDictionaryChanged.DictionaryChanged
    {
        add =>
            DictionaryChangedBoxed += value;
        remove =>
            DictionaryChangedBoxed -= value;
    }

    /// <summary>
    /// Occurs when the dictionary changes
    /// </summary>
    public event EventHandler<NotifyDictionaryChangedEventArgs<TKey, TValue>>? DictionaryChanged;

    event EventHandler<NotifyDictionaryChangedEventArgs<object?, object?>>? DictionaryChangedBoxed;

    /// <summary>
    /// Occurs when the fault for an element has changed
    /// </summary>
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanged;

    /// <summary>
    /// Occurs when the fault for an element is changing
    /// </summary>
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanging;

    /// <summary>
    /// Determines whether the read-only dictionary contains an element that has the specified key
    /// </summary>
    /// <param name="key">The key to locate</param>
    /// <returns><c>true</c> if the read-only dictionary contains an element that has the specified key; otherwise, <c>false</c></returns>
    public bool ContainsKey(TKey key) => readOnlyDictionary.ContainsKey(key);

    void DictionaryChangedHandler(object sender, NotifyDictionaryChangedEventArgs<object?, object?> e) =>
        DictionaryChangedBoxed?.Invoke(this, e);

    void GenericDictionaryChangedHandler(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
        DictionaryChanged?.Invoke(this, e);

    /// <summary>
    /// Frees, releases, or resets unmanaged resources
    /// </summary>
    /// <param name="disposing"><c>false</c> if invoked by the finalizer because the object is being garbage collected; otherwise, <c>true</c></param>
    protected override bool Dispose(bool disposing)
    {
        onDispose?.Invoke();
        if (disposing)
        {
            if (readOnlyDictionary is INotifyDictionaryChanged<TKey, TValue> genericDictionaryNotifier)
                genericDictionaryNotifier.DictionaryChanged -= GenericDictionaryChangedHandler;
            if (readOnlyDictionary is INotifyPropertyChanged propertyChangedNotifier)
                propertyChangedNotifier.PropertyChanged -= PropertyChangedHandler;
            if (readOnlyDictionary is INotifyPropertyChanging propertyChangingNotifier)
                propertyChangingNotifier.PropertyChanging -= PropertyChangingHandler;
        }
        return true;
    }

    void FaultNotifierElementFaultChanged(object sender, ElementFaultChangeEventArgs e) =>
        ElementFaultChanged?.Invoke(this, e);

    void FaultNotifierElementFaultChanging(object sender, ElementFaultChangeEventArgs e) =>
        ElementFaultChanging?.Invoke(this, e);

    /// <summary>
    /// Gets a list of all faulted elements
    /// </summary>
    /// <returns>The list</returns>
    public IReadOnlyList<(object? element, Exception? fault)> GetElementFaults() =>
        faultNotifier?.GetElementFaults() ?? Enumerable.Empty<(object? element, Exception? fault)>().ToImmutableArray();

    /// <summary>
    /// Returns an enumerator that iterates through the collection
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection</returns>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() =>
        readOnlyDictionary.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        ((IEnumerable)readOnlyDictionary).GetEnumerator();

    void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Count))
            OnPropertyChanged(e);
    }

    void PropertyChangingHandler(object sender, PropertyChangingEventArgs e)
    {
        if (e.PropertyName == nameof(Count))
            OnPropertyChanging(e);
    }

    /// <summary>
    /// Gets the value that is associated with the specified key
    /// </summary>
    /// <param name="key">The key to locate</param>
    /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter</param>
    /// <returns><c>true</c> if the <see cref="ActiveDictionary{TKey, TValue}"/> contains an element that has the specified key; otherwise, <c>false</c></returns>
    public bool TryGetValue(TKey key, out TValue value) =>
        readOnlyDictionary.TryGetValue(key, out value);

    /// <summary>
    /// Gets the element that has the specified key in the read-only dictionary
    /// </summary>
    /// <param name="key">The key to locate</param>
    /// <returns>The element that has the specified key in the read-only dictionary</returns>
    public TValue this[TKey key] =>
        readOnlyDictionary[key];

    /// <summary>
    /// Gets the <see cref="IComparer{T}"/> in use by the <see cref="ActiveDictionary{TKey, TValue}"/> in order to order keys
    /// </summary>
    public IComparer<TKey>? Comparer =>
        readOnlyDictionary.GetKeyComparer();

    /// <summary>
    /// Gets the number of elements in the collection
    /// </summary>
    public int Count =>
        readOnlyDictionary.Count;

    /// <summary>
    /// Gets the <see cref="IEqualityComparer{T}"/> in use by the <see cref="ActiveDictionary{TKey, TValue}"/> in order to hash and to test keys for equality
    /// </summary>
    public IEqualityComparer<TKey>? EqualityComparer =>
        readOnlyDictionary.GetKeyEqualityComparer();

    /// <summary>
    /// Gets the <see cref="ActiveQuery.IndexingStrategy"/> in use by the <see cref="ActiveDictionary{TKey, TValue}"/>
    /// </summary>
    public IndexingStrategy? IndexingStrategy =>
        readOnlyDictionary.GetIndexingStrategy();

    /// <summary>
    /// Gets an enumerable collection that contains the keys in the read-only dictionary
    /// </summary>
    public IEnumerable<TKey> Keys =>
        readOnlyDictionary.Keys;

    /// <summary>
    /// Gets the exception that occured the most recent time the query updated
    /// </summary>
    public Exception? OperationFault
    {
        get => operationFault;
        internal set => SetBackedProperty(ref operationFault, in value);
    }

    /// <summary>
    /// Gets the <see cref="System.Threading.SynchronizationContext"/> on which this object's operations occur
    /// </summary>
    public SynchronizationContext? SynchronizationContext =>
        synchronized?.SynchronizationContext;

    /// <summary>
    /// Gets an enumerable collection that contains the values in the read-only dictionary
    /// </summary>
    public IEnumerable<TValue> Values =>
        readOnlyDictionary.Values;
}
