namespace Cogs.ActiveQuery;

/// <summary>
/// Represents a generic read-only collection of key-value pairs that is the result of an active query and that cannot be disposed by callers
/// </summary>
/// <typeparam name="TKey">The type of keys</typeparam>
/// <typeparam name="TValue">The type of values</typeparam>
public sealed class NondisposableActiveDictionary<TKey, TValue> :
    INondisposableActiveDictionary<TKey, TValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NondisposableActiveEnumerable{TElement}"/> class
    /// </summary>
    /// <param name="activeDictionary">The <see cref="INondisposableActiveDictionary{TKey, TValue}"/> upon which the <see cref="NondisposableActiveDictionary{TKey, TValue}"/> is based</param>
    public NondisposableActiveDictionary(IActiveDictionary<TKey, TValue> activeDictionary)
    {
        this.activeDictionary = activeDictionary;
        ((INotifyDictionaryChanged)this.activeDictionary).DictionaryChanged += ActiveDictionaryDictionaryChanged;
        this.activeDictionary.DisposalOverridden += ActiveDictionaryDisposalOverridden;
        this.activeDictionary.Disposed += ActiveDictionaryDisposed;
        this.activeDictionary.Disposing += ActiveDictionaryDisposing;
        this.activeDictionary.ElementFaultChanged += ActiveDictionaryElementFaultChanged;
        this.activeDictionary.ElementFaultChanging += ActiveDictionaryElementFaultChanging;
        this.activeDictionary.DictionaryChanged += ActiveDictionaryGenericDictionaryChanged;
        this.activeDictionary.PropertyChanged += ActiveDictionaryPropertyChanged;
        this.activeDictionary.PropertyChanging += ActiveDictionaryPropertyChanging;
    }

    readonly IActiveDictionary<TKey, TValue> activeDictionary;

    event EventHandler<NotifyDictionaryChangedEventArgs<object?, object?>>? INotifyDictionaryChanged.DictionaryChanged
    {
        add => DictionaryChangedBoxed += value;
        remove => DictionaryChangedBoxed -= value;
    }

    /// <summary>
    /// Gets the <see cref="IComparer{T}"/> in use by the <see cref="NondisposableActiveDictionary{TKey, TValue}"/> in order to order keys
    /// </summary>
    public IComparer<TKey>? Comparer =>
        activeDictionary.GetKeyComparer();

    /// <summary>
    /// Gets the number of elements in the collection
    /// </summary>
    public int Count =>
        activeDictionary.Count;

    /// <summary>
    /// Gets the <see cref="IEqualityComparer{T}"/> in use by the <see cref="NondisposableActiveDictionary{TKey, TValue}"/> in order to hash and to test keys for equality
    /// </summary>
    public IEqualityComparer<TKey>? EqualityComparer =>
        activeDictionary.GetKeyEqualityComparer();

    /// <summary>
    /// Gets the <see cref="ActiveQuery.IndexingStrategy"/> in use by the <see cref="NondisposableActiveDictionary{TKey, TValue}"/>
    /// </summary>
    public IndexingStrategy? IndexingStrategy =>
        activeDictionary.GetIndexingStrategy();

    /// <summary>
    /// Gets whether this object has been disposed
    /// </summary>
    public bool IsDisposed =>
        activeDictionary.IsDisposed;

    /// <summary>
    /// Gets an enumerable collection that contains the keys in the read-only dictionary
    /// </summary>
    public IEnumerable<TKey> Keys =>
        activeDictionary.Keys;

    /// <summary>
    /// Gets the exception that occured the most recent time the query updated
    /// </summary>
    public Exception? OperationFault =>
        activeDictionary.OperationFault;

    /// <summary>
    /// Gets the <see cref="System.Threading.SynchronizationContext"/> on which this object's operations occur
    /// </summary>
    public SynchronizationContext? SynchronizationContext =>
        activeDictionary.SynchronizationContext;

    /// <summary>
    /// Gets an enumerable collection that contains the values in the read-only dictionary
    /// </summary>
    public IEnumerable<TValue> Values =>
        activeDictionary.Values;

    /// <summary>
    /// Gets the element that has the specified key in the read-only dictionary
    /// </summary>
    /// <param name="key">The key to locate</param>
    /// <returns>The element that has the specified key in the read-only dictionary</returns>
    public TValue this[TKey key] =>
        activeDictionary[key];

    /// <summary>
    /// Occurs when the dictionary changes
    /// </summary>
    public event EventHandler<NotifyDictionaryChangedEventArgs<TKey, TValue>>? DictionaryChanged;

    event EventHandler<NotifyDictionaryChangedEventArgs<object?, object?>>? DictionaryChangedBoxed;

    /// <summary>
    /// Occurs when this object's disposal has been overridden
    /// </summary>
    public event EventHandler<DisposalNotificationEventArgs>? DisposalOverridden;

    /// <summary>
    /// Occurs when this object has been disposed
    /// </summary>
    public event EventHandler<DisposalNotificationEventArgs>? Disposed;

    /// <summary>
    /// Occurs when this object is being disposed
    /// </summary>
    public event EventHandler<DisposalNotificationEventArgs>? Disposing;

    /// <summary>
    /// Occurs when the fault for an element has changed
    /// </summary>
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanged;

    /// <summary>
    /// Occurs when the fault for an element is changing
    /// </summary>
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanging;

    /// <summary>
    /// Occurs when a property value changes
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Occurs when a property value is changing
    /// </summary>
    public event PropertyChangingEventHandler? PropertyChanging;

    /// <summary>
    /// Determines whether the read-only dictionary contains an element that has the specified key
    /// </summary>
    /// <param name="key">The key to locate</param>
    /// <returns><c>true</c> if the read-only dictionary contains an element that has the specified key; otherwise, <c>false</c></returns>
    public bool ContainsKey(TKey key) =>
        activeDictionary.ContainsKey(key);

    void ActiveDictionaryDictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<object?, object?> e) =>
        DictionaryChangedBoxed?.Invoke(this, e);

    void ActiveDictionaryDisposalOverridden(object sender, DisposalNotificationEventArgs e) =>
        DisposalOverridden?.Invoke(this, e);

    void ActiveDictionaryDisposed(object sender, DisposalNotificationEventArgs e)
    {
        Disposed?.Invoke(this, e);
        activeDictionary.ElementFaultChanged -= ActiveDictionaryElementFaultChanged;
        activeDictionary.ElementFaultChanging -= ActiveDictionaryElementFaultChanging;
        activeDictionary.DictionaryChanged -= ActiveDictionaryGenericDictionaryChanged;
        activeDictionary.PropertyChanged -= ActiveDictionaryPropertyChanged;
        activeDictionary.PropertyChanging -= ActiveDictionaryPropertyChanging;
    }

    void ActiveDictionaryDisposing(object sender, DisposalNotificationEventArgs e) =>
        Disposing?.Invoke(this, e);

    void ActiveDictionaryElementFaultChanged(object sender, ElementFaultChangeEventArgs e) =>
        ElementFaultChanged?.Invoke(this, e);

    void ActiveDictionaryElementFaultChanging(object sender, ElementFaultChangeEventArgs e) =>
        ElementFaultChanging?.Invoke(this, e);

    void ActiveDictionaryGenericDictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
        DictionaryChanged?.Invoke(this, e);

    void ActiveDictionaryPropertyChanged(object sender, PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void ActiveDictionaryPropertyChanging(object sender, PropertyChangingEventArgs e) =>
        PropertyChanging?.Invoke(this, e);

    /// <summary>
    /// Gets a list of all faulted elements
    /// </summary>
    /// <returns>The list</returns>
    public IReadOnlyList<(object? element, Exception? fault)> GetElementFaults() =>
        activeDictionary.GetElementFaults();

    /// <summary>
    /// Returns an enumerator that iterates through the collection
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection</returns>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() =>
        activeDictionary.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        ((IEnumerable)activeDictionary).GetEnumerator();

    /// <summary>
    /// Gets the value that is associated with the specified key
    /// </summary>
    /// <param name="key">The key to locate</param>
    /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter</param>
    /// <returns><c>true</c> if the <see cref="NondisposableActiveDictionary{TKey, TValue}"/> contains an element that has the specified key; otherwise, <c>false</c></returns>
    public bool TryGetValue(TKey key, out TValue value) =>
        activeDictionary.TryGetValue(key, out value);
}
