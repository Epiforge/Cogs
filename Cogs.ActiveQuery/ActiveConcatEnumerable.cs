namespace Cogs.ActiveQuery;

/// <summary>
/// Represents a read-only collection of elements that is the result of calling <see cref="ActiveEnumerableExtensions.ActiveConcat{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/> or <see cref="ActiveEnumerableExtensions.ActiveConcat{TSource}(IEnumerable{TSource}, IEnumerable{TSource}, SynchronizationContext)"/>
/// </summary>
/// <typeparam name="TElement">The type of the elements in the sequence</typeparam>
public sealed class ActiveConcatEnumerable<TElement> :
    SyncDisposable,
    IActiveEnumerable<TElement>
{
    internal ActiveConcatEnumerable(IEnumerable<TElement> first, IEnumerable<TElement> second) :
        this(first, second, first is ISynchronized syncedFirst && second is ISynchronized syncedSecond ? (syncedFirst.SynchronizationContext != syncedSecond.SynchronizationContext ? throw new InvalidOperationException($"{nameof(first)} and {nameof(second)} are both synchronizable but using different synchronization contexts; select a different overload of {nameof(ActiveEnumerableExtensions.ActiveConcat)} to specify the synchronization context to use") : syncedFirst.SynchronizationContext ?? syncedSecond.SynchronizationContext) : null)
    {
    }

    internal ActiveConcatEnumerable(IEnumerable<TElement> first, IEnumerable<TElement> second, SynchronizationContext? synchronizationContext)
    {
        this.first = first;
        firstCount = this.first.Count();
        this.second = second;
        SynchronizationContext = synchronizationContext;
        firstCount = this.Execute(() => first.Count());
        if (first is INotifyCollectionChanged firstCollectionNotifier)
            firstCollectionNotifier.CollectionChanged += FirstCollectionChanged;
        if (first is INotifyElementFaultChanges firstFaultNotifier)
        {
            firstFaultNotifier.ElementFaultChanged += FaultNotifierElementFaultChanged;
            firstFaultNotifier.ElementFaultChanging += FaultNotifierElementFaultChanging;
        }
        if (second is INotifyCollectionChanged secondCollectionNotifier)
            secondCollectionNotifier.CollectionChanged += SecondCollectionChanged;
        if (second is INotifyElementFaultChanges secondFaultNotifier)
        {
            secondFaultNotifier.ElementFaultChanged += FaultNotifierElementFaultChanged;
            secondFaultNotifier.ElementFaultChanging += FaultNotifierElementFaultChanging;
        }
    }

    readonly IEnumerable<TElement> first;
    int firstCount;
    readonly IEnumerable<TElement> second;

    /// <summary>
    /// Gets the element at the specified index in the read-only list
    /// </summary>
    /// <param name="index">The zero-based index of the element to get</param>
    /// <returns>The element at the specified index in the read-only list</returns>
    public TElement this[int index] =>
        this.Execute(() => index >= firstCount ? second.ElementAt(index - firstCount) : first.ElementAt(index));

    object? IList.this[int index]
    {
        get => this[index];
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets the number of elements in the collection
    /// </summary>
    public int Count =>
        this.Execute(() => firstCount + second.Count());

    bool IList.IsFixedSize { get; } = false;

    bool IList.IsReadOnly { get; } = true;

    bool ICollection.IsSynchronized { get; } = false;

    /// <summary>
    /// Gets the <see cref="System.Threading.SynchronizationContext"/> on which this object's operations occur
    /// </summary>
    public SynchronizationContext? SynchronizationContext { get; }

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

    bool IList.Contains(object? value) =>
        this.Execute(() =>
        {
            if (value is TElement element)
            {
                var comparer = EqualityComparer<TElement>.Default;
                foreach (var item in this)
                    if (comparer.Equals(item, element))
                        return true;
            }
            return false;
        });

    void ICollection.CopyTo(Array array, int index) =>
        this.Execute(() =>
        {
            --index;
            foreach (var item in this)
            {
                if (++index >= array.Length)
                    break;
                array.SetValue(item, index);
            }
        });

    /// <summary>
    /// Frees, releases, or resets unmanaged resources
    /// </summary>
    /// <param name="disposing"><c>false</c> if invoked by the finalizer because the object is being garbage collected; otherwise, <c>true</c></param>
    protected override bool Dispose(bool disposing)
    {
        if (disposing)
        {
            if (first is INotifyCollectionChanged firstCollectionNotifier)
                firstCollectionNotifier.CollectionChanged -= FirstCollectionChanged;
            if (first is INotifyElementFaultChanges firstFaultNotifier)
            {
                firstFaultNotifier.ElementFaultChanged -= FaultNotifierElementFaultChanged;
                firstFaultNotifier.ElementFaultChanging -= FaultNotifierElementFaultChanging;
            }
            if (second is INotifyCollectionChanged secondCollectionNotifier)
                secondCollectionNotifier.CollectionChanged -= SecondCollectionChanged;
            if (second is INotifyElementFaultChanges secondFaultNotifier)
            {
                secondFaultNotifier.ElementFaultChanged -= FaultNotifierElementFaultChanged;
                secondFaultNotifier.ElementFaultChanging -= FaultNotifierElementFaultChanging;
            }
        }
        return true;
    }

    void FaultNotifierElementFaultChanged(object sender, ElementFaultChangeEventArgs e) =>
        OnElementFaultChanged(e);

    void FaultNotifierElementFaultChanging(object sender, ElementFaultChangeEventArgs e) =>
        OnElementFaultChanging(e);

    void FirstCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
            firstCount = first.Count();
        else
            firstCount += (e.NewItems?.Count ?? 0) - (e.OldItems?.Count ?? 0);
        OnCollectionChanged(e);
    }

    /// <summary>
    /// Gets a list of all faulted elements
    /// </summary>
    /// <returns>The list</returns>
    public IReadOnlyList<(object? element, Exception? fault)> GetElementFaults() =>
        this.Execute(() => ((first as INotifyElementFaultChanges)?.GetElementFaults() ?? Enumerable.Empty<(object? element, Exception? fault)>()).Concat((second as INotifyElementFaultChanges)?.GetElementFaults() ?? Enumerable.Empty<(object? element, Exception? fault)>()).ToImmutableArray());

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the collection
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection</returns>
    public IEnumerator<TElement> GetEnumerator() =>
        this.Execute(() => first.Concat(second).GetEnumerator());

    int IList.IndexOf(object value) =>
        this.Execute(() =>
        {
            if (value is TElement element)
            {
                var index = -1;
                var comparer = EqualityComparer<TElement>.Default;
                foreach (var item in this)
                {
                    ++index;
                    if (comparer.Equals(element, item))
                        return index;
                }
            }
            return -1;
        });

    void IList.Insert(int index, object value) =>
        throw new NotSupportedException();

    void OnCollectionChanged(NotifyCollectionChangedEventArgs e) =>
        CollectionChanged?.Invoke(this, e);

    void OnElementFaultChanged(ElementFaultChangeEventArgs e) =>
        ElementFaultChanged?.Invoke(this, e);

    void OnElementFaultChanging(ElementFaultChangeEventArgs e) =>
        ElementFaultChanging?.Invoke(this, e);

    void IList.Remove(object value) =>
        throw new NotSupportedException();

    void IList.RemoveAt(int index) =>
        throw new NotSupportedException();

    void SecondCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        e = e.Action switch
        {
            NotifyCollectionChangedAction.Add => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItems, firstCount + e.NewStartingIndex),
            NotifyCollectionChangedAction.Move => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, e.NewItems, firstCount + e.NewStartingIndex, firstCount + e.OldStartingIndex),
            NotifyCollectionChangedAction.Remove => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, e.OldItems, firstCount + e.OldStartingIndex),
            NotifyCollectionChangedAction.Replace => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, e.NewItems, e.OldItems, firstCount + e.NewStartingIndex),
            NotifyCollectionChangedAction.Reset => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset),
            _ => throw new NotSupportedException(),
        };
        OnCollectionChanged(e);
    }
}
