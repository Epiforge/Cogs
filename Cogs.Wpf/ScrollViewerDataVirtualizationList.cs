using Cogs.Disposal;

namespace Cogs.Wpf;

/// <summary>
/// Wraps a list of items such that when a control's <see cref="ItemsControl.ItemsSource"/> is bound items are loaded and unloaded as they become visible in a <see cref="ScrollViewer"/>
/// </summary>
public class ScrollViewerDataVirtualizationList :
    SyncDisposable,
    IList,
    INotifyCollectionChanged
{
    internal ScrollViewerDataVirtualizationList(ScrollViewer scrollViewer, IList list, int additionalItems)
    {
        this.scrollViewer = scrollViewer;
        this.list = list;
        this.additionalItems = additionalItems;
        loadedItems = new();
        scrollViewer.ScrollChanged += ScrollViewerScrollChanged;
        if (list is INotifyCollectionChanged collectionChangedNotifyingList)
            collectionChangedNotifyingList.CollectionChanged += ListCollectionChanged;
        if (list is INotifyPropertyChanged propertyChangedNotifyingList)
            propertyChangedNotifyingList.PropertyChanged += ListPropertyChanged;
        AdjustLoadedItems();
        adjustmentTimer = new(ReadjustmentTimerTick);
    }

    int additionalItems;
    bool belayAdjustments;
    readonly IList list;
    readonly HashSet<IDataVirtualizationItem> loadedItems;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "This field will be disposed by the base class, the analyzer just doesn't see that.")]
    readonly Timer adjustmentTimer;
    readonly ScrollViewer scrollViewer;

    /// <inheritdoc/>
    public int Count =>
        list.Count;

    /// <inheritdoc/>
    public bool IsFixedSize =>
        list.IsFixedSize;

    /// <inheritdoc/>
    public bool IsReadOnly { get; } = true;

    /// <inheritdoc/>
    public bool IsSynchronized =>
        list.IsSynchronized;

    /// <inheritdoc/>
    public object SyncRoot =>
        list.SyncRoot;

    /// <inheritdoc/>
    public object? this[int index]
    {
        get => list[index];
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets/sets the number of additional items to load before and after the visible items
    /// </summary>
    public int AdditionalItems
    {
        get => additionalItems;
        set
        {
            if (SetBackedProperty(ref additionalItems, in value))
                AdjustLoadedItems();
        }
    }

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    int IList.Add(object? value) =>
        throw new NotSupportedException();

    void AdjustLoadedItems()
    {
        if (belayAdjustments)
            return;
        var visibleItems = Enumerable
            .Range((int)scrollViewer.VerticalOffset - additionalItems, (int)Math.Ceiling(scrollViewer.ViewportHeight) + 1 + additionalItems)
            .Select(i => i >= 0 && i < list.Count ? list[i] : null)
            .OfType<IDataVirtualizationItem>()
            .ToImmutableArray();
        var itemsToLoad = visibleItems.Except(loadedItems).ToImmutableArray();
        var itemsToUnload = loadedItems.Except(visibleItems).ToImmutableArray();
        foreach (var item in itemsToLoad)
        {
            item.Load();
            loadedItems.Add(item);
        }
        foreach (var item in itemsToUnload)
        {
            loadedItems.Remove(item);
            item.Unload();
        }
    }

    void IList.Clear() =>
        throw new NotSupportedException();

    /// <inheritdoc/>
    public bool Contains(object? value) =>
        list.Contains(value);

    /// <inheritdoc/>
    public void CopyTo(Array array, int index) =>
        list.CopyTo(array, index);

    /// <inheritdoc/>
    protected override bool Dispose(bool disposing)
    {
        if (disposing)
        {
            scrollViewer.ScrollChanged -= ScrollViewerScrollChanged;
            if (list is INotifyCollectionChanged collectionChangedNotifyingList)
                collectionChangedNotifyingList.CollectionChanged -= ListCollectionChanged;
            if (list is INotifyPropertyChanged propertyChangedNotifyingList)
                propertyChangedNotifyingList.PropertyChanged -= ListPropertyChanged;
            adjustmentTimer.Dispose();
        }
        return true;
    }

    /// <inheritdoc/>
    public IEnumerator GetEnumerator() =>
        list.GetEnumerator();

    /// <inheritdoc/>
    public int IndexOf(object? value) =>
        list.IndexOf(value);

    void IList.Insert(int index, object? value) =>
        throw new NotSupportedException();

    void ListCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnCollectionChanged(e);
        belayAdjustments = true;
        adjustmentTimer.Change(TimeSpan.FromMilliseconds(250), Timeout.InfiniteTimeSpan);
    }

    void ListPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(list.Count))
            OnPropertyChanged(e);
    }

    /// <summary>
    /// Raises the <see cref="INotifyCollectionChanged.CollectionChanged"/> event
    /// </summary>
    /// <param name="e">The event's data</param>
    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) =>
        CollectionChanged?.Invoke(this, e);

    void ReadjustmentTimerTick(object? state)
    {
        belayAdjustments = false;
        if (!scrollViewer.Dispatcher.CheckAccess())
            scrollViewer.Dispatcher.Invoke(() => AdjustLoadedItems());
        else
            AdjustLoadedItems();
    }

    void IList.Remove(object? value) =>
        throw new NotSupportedException();

    void IList.RemoveAt(int index) =>
        throw new NotSupportedException();

    void ScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e) =>
        AdjustLoadedItems();
}
