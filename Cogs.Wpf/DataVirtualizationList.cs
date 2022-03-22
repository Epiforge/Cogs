using Cogs.Collections;
using Cogs.Disposal;

namespace Cogs.Wpf;

/// <summary>
/// Wraps a list of items such that when a control's <see cref="ItemsControl.ItemsSource"/> is bound items are loaded and unloaded as necessary to minimize memory/resource usage
/// </summary>
/// <typeparam name="T">The type of items in the list</typeparam>
public class DataVirtualizationList<T> :
    SyncDisposable,
    IList,
    INotifyCollectionChanged
{
    internal DataVirtualizationList(IReadOnlyList<T> list)
    {
        loaded = new OrderedHashSet<IDataVirtualizationItem>();
        this.list = list;
        if (list is INotifyCollectionChanged collectionChangedNotifyingList)
            collectionChangedNotifyingList.CollectionChanged += ListCollectionChanged;
        if (list is INotifyPropertyChanged propertyChangedNotifyingList)
            propertyChangedNotifyingList.PropertyChanged += ListPropertyChanged;
    }

    int lastIndex = -1;
    readonly IReadOnlyList<T> list;
    readonly OrderedHashSet<IDataVirtualizationItem> loaded;
    int loadCapacity = 1;

    /// <summary>
    /// Gets the element at the specified index in the list
    /// </summary>
    /// <param name="index">The zero-based index of the element to get</param>
    /// <returns>The element at the specified index in the list</returns>
    /// <exception cref="NotImplementedException">The setter was used</exception>
    public object? this[int index]
    {
        get
        {
            Load(index);
            lastIndex = index;
            return list[index];
        }
        set => throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the number of elements in the collection
    /// </summary>
    public int Count =>
        list.Count;

    internal Func<int>? LoadCapacitySelector { get; set; }

    bool IList.IsFixedSize { get; } = false;

    bool IList.IsReadOnly { get; } = true;

    bool ICollection.IsSynchronized { get; } = false;

    object ICollection.SyncRoot { get; } = new object();

    /// <summary>
    /// Occurs when the collection changes
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    int IList.Add(object? value) =>
        throw new NotSupportedException();

    void IList.Clear() =>
        throw new NotSupportedException();

    /// <summary>
    /// Determines whether the list contains a specific value
    /// </summary>
    /// <param name="value">The object to locate in the list</param>
    /// <returns><c>true</c> if <paramref name="value"/> is found in the list; otherwise, <c>false</c></returns>
    public bool Contains(object? value) =>
        value is T typedValue && list.Contains(typedValue);

    void ICollection.CopyTo(Array array, int index) =>
        throw new NotSupportedException();

    /// <summary>
    /// Frees, releases, or resets unmanaged resources
    /// </summary>
    /// <param name="disposing"><c>false</c> if invoked by the finalizer because the object is being garbage collected; otherwise, <c>true</c></param>
    /// <returns><c>true</c> if disposal completed; otherwise, <c>false</c></returns>
    protected override bool Dispose(bool disposing)
    {
        if (disposing)
        {
            if (list is INotifyCollectionChanged collectionChangedNotifyingList)
                collectionChangedNotifyingList.CollectionChanged -= ListCollectionChanged;
            foreach (var item in loaded)
                item.Unload();
        }
        return true;
    }

    /// <summary>
    /// Returns an enumerator that iterates through a collection
    /// </summary>
    /// <returns></returns>
    public IEnumerator GetEnumerator()
    {
        for (var i = 0; i < Count; ++i)
            yield return this[i];
    }

    [SuppressMessage("Design", "CA1033: Interface methods should be callable by child types", Justification = "This is deliberately incorrect to cheese WPF")]
    int IList.IndexOf(object? value) =>
        -1;

    void IList.Insert(int index, object? value) =>
        throw new NotSupportedException();

    void ListCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshLoadCapacity();
        if (lastIndex >= 0)
        {
            if (e.OldItems is { } oldItems)
                foreach (var item in oldItems)
                    if (item is T typedItem)
                        Unload(typedItem);
            var index = lastIndex;
            if (index >= list.Count)
                index = list.Count - 1;
            if (e.NewItems is { } newItems && (Math.Abs(index - e.NewStartingIndex) < loadCapacity / 2 || Math.Abs(index - (e.NewStartingIndex + newItems.Count)) < loadCapacity / 2))
                foreach (var item in newItems)
                    if (item is T typedItem)
                        Load(typedItem);
        }
        OnCollectionChanged(e);
    }

    void ListPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(list.Count))
            OnPropertyChanged(e);
    }

    void Load(T item)
    {
        if (item is IDataVirtualizationItem virtualizedItem)
        {
            if (loaded.AddFirst(virtualizedItem))
            {
                virtualizedItem.Load();
                TrimByLoadCapacity();
            }
            else
                loaded.MoveToFirst(virtualizedItem);
        }
    }

    void Load(int index) =>
        Load(list[index]);

    /// <summary>
    /// Raises the <see cref="INotifyCollectionChanged.CollectionChanged"/> event with the provided arguments
    /// </summary>
    /// <param name="e">Arguments of the event being raised</param>
    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) =>
        CollectionChanged?.Invoke(this, e);

    internal void RefreshLoadCapacity()
    {
        if (LoadCapacitySelector is { } selector)
            SetLoadCapacity(selector());
    }

    internal void SetLoadCapacity(int value)
    {
        loadCapacity = value;
        TrimByLoadCapacity();
    }

    void TrimByLoadCapacity()
    {
        if (loaded is not null)
            while (loaded.Count > loadCapacity)
                Unload((T)loaded.Last);
    }

    void IList.Remove(object? value) =>
        throw new NotSupportedException();

    void IList.RemoveAt(int index) =>
        throw new NotSupportedException();

    void Unload(T item)
    {
        if (item is IDataVirtualizationItem virtualizedItem && loaded.Remove(virtualizedItem))
            virtualizedItem.Unload();
    }
}
