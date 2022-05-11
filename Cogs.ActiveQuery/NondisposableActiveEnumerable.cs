namespace Cogs.ActiveQuery;

/// <summary>
/// Represents a read-only collection of elements that is the result of an active query and that cannot be disposed by callers
/// </summary>
/// <typeparam name="TElement">The type of the elements in the sequence</typeparam>
public class NondisposableActiveEnumerable<TElement> :
    INondisposableActiveEnumerable<TElement>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NondisposableActiveEnumerable{TElement}"/> class
    /// </summary>
    /// <param name="activeEnumerable">The <see cref="IActiveEnumerable{TElement}"/> upon which the <see cref="NondisposableActiveEnumerable{TElement}"/> is based</param>
    public NondisposableActiveEnumerable(IActiveEnumerable<TElement> activeEnumerable)
    {
        this.activeEnumerable = activeEnumerable;
        this.activeEnumerable.CollectionChanged += ActiveEnumerableCollectionChanged;
        this.activeEnumerable.DisposalOverridden += ActiveEnumerableDisposalOverridden;
        this.activeEnumerable.Disposed += ActiveEnumerableDisposed;
        this.activeEnumerable.Disposing += ActiveEnumerableDisposing;
        this.activeEnumerable.ElementFaultChanged += ActiveEnumerableElementFaultChanged;
        this.activeEnumerable.ElementFaultChanging += ActiveEnumerableElementFaultChanging;
        this.activeEnumerable.PropertyChanged += ActiveEnumerablePropertyChanged;
        this.activeEnumerable.PropertyChanging += ActiveEnumerablePropertyChanging;
    }

    readonly IActiveEnumerable<TElement> activeEnumerable;

    /// <summary>
    /// Gets the element at the specified index in the read-only list
    /// </summary>
    /// <param name="index">The zero-based index of the element to get</param>
    /// <returns>The element at the specified index in the read-only list</returns>
    public TElement this[int index] =>
        activeEnumerable[index];

    object? IList.this[int index]
    {
        get => this[index];
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets the number of elements in the collection
    /// </summary>
    public int Count =>
        activeEnumerable.Count;

    /// <summary>
    /// Gets whether this object has been disposed
    /// </summary>
    public bool IsDisposed =>
        activeEnumerable.IsDisposed;

    bool IList.IsFixedSize { get; } = false;

    bool IList.IsReadOnly { get; } = true;

    bool ICollection.IsSynchronized { get; } = false;

    /// <summary>
    /// Gets the <see cref="System.Threading.SynchronizationContext"/> on which this object's operations occur
    /// </summary>
    public SynchronizationContext? SynchronizationContext =>
        activeEnumerable.SynchronizationContext;

    object? ICollection.SyncRoot { get; } = null;

    /// <summary>
    /// Occurs when the collection changes
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

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

    void ActiveEnumerableCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
        CollectionChanged?.Invoke(this, e);

    void ActiveEnumerableDisposalOverridden(object sender, DisposalNotificationEventArgs e) =>
        DisposalOverridden?.Invoke(this, e);

    void ActiveEnumerableDisposed(object sender, DisposalNotificationEventArgs e)
    {
        Disposed?.Invoke(this, e);
        activeEnumerable.CollectionChanged -= ActiveEnumerableCollectionChanged;
        activeEnumerable.ElementFaultChanged -= ActiveEnumerableElementFaultChanged;
        activeEnumerable.ElementFaultChanging -= ActiveEnumerableElementFaultChanging;
        activeEnumerable.PropertyChanged -= ActiveEnumerablePropertyChanged;
        activeEnumerable.PropertyChanging -= ActiveEnumerablePropertyChanging;
    }

    void ActiveEnumerableDisposing(object sender, DisposalNotificationEventArgs e) =>
        Disposing?.Invoke(this, e);

    void ActiveEnumerableElementFaultChanged(object sender, ElementFaultChangeEventArgs e) =>
        ElementFaultChanged?.Invoke(this, e);

    void ActiveEnumerableElementFaultChanging(object sender, ElementFaultChangeEventArgs e) =>
        ElementFaultChanging?.Invoke(this, e);

    void ActiveEnumerablePropertyChanged(object sender, PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void ActiveEnumerablePropertyChanging(object sender, PropertyChangingEventArgs e) =>
        PropertyChanging?.Invoke(this, e);

    int IList.Add(object value) =>
        throw new NotSupportedException();

    void IList.Clear() =>
        throw new NotSupportedException();

    [SuppressMessage("Design", "CA1033: Interface methods should be callable by child types")]
    bool IList.Contains(object? value) =>
        this.Execute(() => value is TElement element && this.Contains(element));

    [SuppressMessage("Design", "CA1033: Interface methods should be callable by child types")]
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
    /// Gets a list of all faulted elements
    /// </summary>
    /// <returns>The list</returns>
    public IReadOnlyList<(object? element, Exception? fault)> GetElementFaults() =>
        activeEnumerable.GetElementFaults();

    IEnumerator IEnumerable.GetEnumerator() =>
        activeEnumerable.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the collection
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection</returns>
    public IEnumerator<TElement> GetEnumerator() =>
        activeEnumerable.GetEnumerator();

    [SuppressMessage("Design", "CA1033: Interface methods should be callable by child types")]
    int IList.IndexOf(object value) =>
        this.Execute(() => value is TElement element ? this.IndexOf(element) : -1);

    void IList.Insert(int index, object value) =>
        throw new NotSupportedException();

    void IList.Remove(object value) =>
        throw new NotSupportedException();

    void IList.RemoveAt(int index) =>
        throw new NotSupportedException();
}
