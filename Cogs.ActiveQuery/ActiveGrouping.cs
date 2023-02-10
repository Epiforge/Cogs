namespace Cogs.ActiveQuery;

/// <summary>
/// Represents a group of elements that is itself an element in the results of a group by active query
/// </summary>
/// <typeparam name="TKey">The type of the values by which the source elements are being grouped</typeparam>
/// <typeparam name="TElement">The type of the source elements</typeparam>
sealed class ActiveGrouping<TKey, TElement> :
    SyncDisposable,
    IActiveGrouping<TKey, TElement>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveGrouping{TKey, TElement}"/> class
    /// </summary>
    /// <param name="key">The key value that members of the group share in common</param>
    /// <param name="list">The list of members of the group</param>
    /// <param name="faultNotifier">The <see cref="INotifyElementFaultChanges"/> for the underlying data of the group's members</param>
    /// <param name="onDispose">The action to take when the <see cref="ActiveGrouping{TKey, TValue}"/> is disposed</param>
    public ActiveGrouping(TKey key, ObservableCollection<TElement> list, INotifyElementFaultChanges? faultNotifier = null, Action? onDispose = null)
    {
        synchronized = list as ISynchronized;
        this.faultNotifier = faultNotifier ?? (list as INotifyElementFaultChanges);
        if (this.faultNotifier is { })
        {
            this.faultNotifier.ElementFaultChanged += FaultNotifierElementFaultChanged;
            this.faultNotifier.ElementFaultChanging += FaultNotifierElementFaultChanging;
        }
        readOnlyList = list;
        if (readOnlyList is INotifyCollectionChanged collectionNotifier)
            collectionNotifier.CollectionChanged += CollectionChangedHandler;
        this.onDispose = onDispose;
        Key = key;
    }

    readonly INotifyElementFaultChanges? faultNotifier;
    readonly Action? onDispose;
    readonly IReadOnlyList<TElement> readOnlyList;
    readonly ISynchronized? synchronized;

    /// <summary>
    /// Gets the element at the specified index in the read-only list
    /// </summary>
    /// <param name="index">The zero-based index of the element to get</param>
    /// <returns>The element at the specified index in the read-only list</returns>
    public TElement this[int index] =>
        readOnlyList[index];

    object? IList.this[int index]
    {
        get => this[index];
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets the number of elements in the collection
    /// </summary>
    public int Count =>
        readOnlyList.Count;

    /// <summary>
    /// Gets the value shared by the source elements in this group
    /// </summary>
    public TKey Key { get; }

    bool IList.IsFixedSize { get; } = false;

    bool IList.IsReadOnly { get; } = true;

    bool ICollection.IsSynchronized { get; } = false;

    /// <summary>
    /// Gets the <see cref="System.Threading.SynchronizationContext"/> on which this object's operations occur
    /// </summary>
    public SynchronizationContext? SynchronizationContext =>
        synchronized?.SynchronizationContext;

    object? ICollection.SyncRoot { get; } = null;

    /// <summary>
    /// Occurs when the collection changes
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Occurs when the fault for an element has changed
    /// </summary>
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanged;

    /// <summary>
    /// Occurs when the fault for an element is changing
    /// </summary>
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanging;

    int IList.Add(object value) =>
        throw new NotSupportedException();

    void IList.Clear() =>
        throw new NotSupportedException();

    void CollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e) =>
        CollectionChanged?.Invoke(this, e);

    bool IList.Contains(object? value) =>
        this.Execute(() =>
        {
            if (value is TElement element)
            {
                var comparer = EqualityComparer<TElement>.Default;
                for (int i = 0, ii = readOnlyList.Count; i < ii; ++i)
                    if (comparer.Equals(readOnlyList[i], element))
                        return true;
            }
            return false;
        });

    void ICollection.CopyTo(Array array, int index) =>
        this.Execute(() =>
        {
            for (int i = 0, ii = readOnlyList.Count; i < ii; ++i)
            {
                if (index + i >= array.Length)
                    break;
                array.SetValue(readOnlyList[i], index + i);
            }
        });

    public override void Dispose() =>
        throw new InvalidOperationException("Active groupings cannot be disposed by callers; dispose the parent ActiveGroupBy query instead");

    /// <summary>
    /// Frees, releases, or resets unmanaged resources
    /// </summary>
    /// <param name="disposing"><c>false</c> if invoked by the finalizer because the object is being garbage collected; otherwise, <c>true</c></param>
    protected override bool Dispose(bool disposing)
    {
        if (disposing)
        {
            onDispose?.Invoke();
            if (faultNotifier is { })
            {
                faultNotifier.ElementFaultChanged -= FaultNotifierElementFaultChanged;
                faultNotifier.ElementFaultChanging -= FaultNotifierElementFaultChanging;
            }
            if (readOnlyList is INotifyCollectionChanged collectionNotifier)
                collectionNotifier.CollectionChanged -= CollectionChangedHandler;
        }
        return true;
    }

    void IActiveGrouping<TKey, TElement>.DisposeInternal() =>
        base.Dispose();

    void FaultNotifierElementFaultChanged(object sender, ElementFaultChangeEventArgs e) =>
        ElementFaultChanged?.Invoke(this, e);

    void FaultNotifierElementFaultChanging(object sender, ElementFaultChangeEventArgs e) =>
        ElementFaultChanging?.Invoke(this, e);

    IEnumerator IEnumerable.GetEnumerator() =>
        readOnlyList.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the collection
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection</returns>
    public IEnumerator<TElement> GetEnumerator() =>
        readOnlyList.GetEnumerator();

    /// <summary>
    /// Gets a list of all faulted elements
    /// </summary>
    /// <returns>The list</returns>
    public IReadOnlyList<(object? element, Exception? fault)> GetElementFaults() =>
        faultNotifier?.GetElementFaults() ?? Enumerable.Empty<(object? element, Exception? fault)>().ToImmutableArray();

    int IList.IndexOf(object value) =>
        this.Execute(() =>
        {
            if (value is TElement element)
            {
                var comparer = EqualityComparer<TElement>.Default;
                for (int i = 0, ii = readOnlyList.Count; i < ii; ++i)
                    if (comparer.Equals(readOnlyList[i], element))
                        return i;
            }
            return -1;
        });

    void IList.Insert(int index, object value) =>
        throw new NotSupportedException();

    void IList.Remove(object value) =>
        throw new NotSupportedException();

    void IList.RemoveAt(int index) =>
        throw new NotSupportedException();
}
