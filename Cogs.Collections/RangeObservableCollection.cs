namespace Cogs.Collections;

/// <summary>
/// Represents a dynamic data collection that supports bulk operations and provides notifications when items get added, removed, or when the whole list is refreshed
/// </summary>
/// <typeparam name="T">The type of elements in the collection</typeparam>
public class RangeObservableCollection<T> : ObservableCollection<T>, INotifyGenericCollectionChanged<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RangeObservableCollection{T}"/>
    /// </summary>
    public RangeObservableCollection() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeObservableCollection{T}"/>
    /// </summary>
    /// <param name="raiseCollectionChangedEventsForIndividualElements">Whether to raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon by range methods</param>
    public RangeObservableCollection(bool raiseCollectionChangedEventsForIndividualElements) : base() =>
        RaiseCollectionChangedEventsForIndividualElements = raiseCollectionChangedEventsForIndividualElements;

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeObservableCollection{T}"/> class that contains elements copied from the specified collection
    /// </summary>
    /// <param name="collection">The collection from which the elements are copied</param>
    public RangeObservableCollection(IEnumerable<T> collection) : base(collection)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeObservableCollection{T}"/> class that contains elements copied from the specified collection
    /// </summary>
    /// <param name="collection">The collection from which the elements are copied</param>
    /// <param name="raiseCollectionChangedEventsForIndividualElements">Whether to raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon by range methods</param>
    public RangeObservableCollection(IEnumerable<T> collection, bool raiseCollectionChangedEventsForIndividualElements) : base(collection) =>
        RaiseCollectionChangedEventsForIndividualElements = raiseCollectionChangedEventsForIndividualElements;

    /// <summary>
    /// Gets whether this <see cref="RangeObservableCollection{T}"/> will raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon by range methods
    /// </summary>
    public bool RaiseCollectionChangedEventsForIndividualElements { get; }

    /// <summary>
    /// Occurs when the collection changes
    /// </summary>
    public event NotifyGenericCollectionChangedEventHandler<T>? GenericCollectionChanged;

    /// <summary>
    /// Adds objects to the end of the <see cref="RangeObservableCollection{T}"/>
    /// </summary>
    /// <param name="items">The objects to be added to the end of the <see cref="RangeObservableCollection{T}"/></param>
    public void AddRange(IEnumerable<T> items) => InsertRange(Items.Count, items);

    /// <summary>
    /// Adds objects to the end of the <see cref="RangeObservableCollection{T}"/>
    /// </summary>
    /// <param name="items">The objects to be added to the end of the <see cref="RangeObservableCollection{T}"/></param>
    public void AddRange(IList<T> items) => AddRange((IEnumerable<T>)items);

    /// <summary>
    /// Removes all object from the <see cref="RangeObservableCollection{T}"/> that satisfy the <paramref name="predicate"/>
    /// </summary>
    /// <param name="predicate">A predicate used to determine whether to remove an object from the <see cref="RangeObservableCollection{T}"/></param>
    /// <returns>The items that were removed</returns>
    public IReadOnlyList<T> GetAndRemoveAll(Func<T, bool> predicate)
    {
        if (predicate is null)
            throw new ArgumentNullException(nameof(predicate));
        var removed = new List<T>();
        for (var i = 0; i < Items.Count;)
        {
            if (predicate(Items[i]))
                removed.Add(GetAndRemoveAt(i));
            else
                ++i;
        }
        return removed.ToImmutableArray();
    }

    /// <summary>
    /// Gets the element at the specified index and removes it from the <see cref="RangeObservableCollection{T}"/>
    /// </summary>
    /// <param name="index">The zero-based index of the element</param>
    /// <returns>The element at the specified index</returns>
    public virtual T GetAndRemoveAt(int index)
    {
        var item = Items[index];
        RemoveAt(index);
        return item;
    }

    /// <summary>
    /// Gets the elements in the range starting at the specified index and of the specified length
    /// </summary>
    /// <param name="index">The index of the element at the start of the range</param>
    /// <param name="count">The number of elements in the range</param>
    /// <returns>The elements in the range</returns>
    public IReadOnlyList<T> GetRange(int index, int count)
    {
        var result = new List<T>();
        for (int i = index, ii = index + count; i < ii; ++i)
            result.Add(this[i]);
        return result.ToImmutableArray();
    }

    /// <summary>
    /// Inserts elements into the <see cref="RangeObservableCollection{T}"/> at the specified index
    /// </summary>
    /// <param name="index">The zero-based index at which <paramref name="items"/> should be inserted</param>
    /// <param name="items">The objects to insert</param>
    public void InsertRange(int index, IEnumerable<T> items)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));
        var originalIndex = index;
        --index;
        if (RaiseCollectionChangedEventsForIndividualElements)
        {
            foreach (var item in items)
                InsertItem(++index, item);
        }
        else
        {
            var list = new List<T>();
            foreach (var item in items)
            {
                Items.Insert(++index, item);
                list.Add(item);
            }
            if (list.Count > 0)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, list, originalIndex));
                NotifyCountChanged();
            }
        }
    }

    /// <summary>
    /// Inserts elements into the <see cref="RangeObservableCollection{T}"/> at the specified index
    /// </summary>
    /// <param name="index">The zero-based index at which <paramref name="items"/> should be inserted</param>
    /// <param name="items">The objects to insert</param>
    public void InsertRange(int index, IList<T> items) => InsertRange(index, (IEnumerable<T>)items);

    /// <summary>
    /// Moves the items at the specified index to a new location in the collection
    /// </summary>
    /// <param name="oldStartIndex">The zero-based index specifying the location of the items to be moved</param>
    /// <param name="newStartIndex">The zero-based index specifying the new location of the items</param>
    /// <param name="count">The number of items to move</param>
    public void MoveRange(int oldStartIndex, int newStartIndex, int count)
    {
        if (oldStartIndex != newStartIndex && count > 0)
        {
            var extractionIndex = oldStartIndex;
            var insertionIndex = newStartIndex - 1;
            if (RaiseCollectionChangedEventsForIndividualElements)
                for (var i = 0; i < count; ++i)
                {
                    Move(extractionIndex, ++insertionIndex);
                    if (oldStartIndex > newStartIndex)
                        ++extractionIndex;
                }
            else
            {
                var movedItems = new List<T>();
                for (var i = 0; i < count; ++i)
                {
                    var item = Items[extractionIndex];
                    Items.RemoveAt(extractionIndex);
                    movedItems.Add(item);
                }
                foreach (var item in movedItems)
                    Items.Insert(++insertionIndex, item);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, movedItems, newStartIndex, oldStartIndex));
                NotifyCountChanged();
            }
        }
    }

    void NotifyCountChanged() => OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));

    /// <summary>
    /// Raises the <see cref="INotifyCollectionChanged.CollectionChanged"/> event with the provided arguments
    /// </summary>
    /// <param name="e">Arguments of the event being raised</param>
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);
        OnGenericCollectionChanged(NotifyGenericCollectionChangedEventArgs<T>.FromNotifyCollectionChangedEventArgs(e));
    }

    /// <summary>
    /// Raises the <see cref="INotifyGenericCollectionChanged{T}.GenericCollectionChanged"/> event with the provided arguments
    /// </summary>
    /// <param name="e">Arguments of the event being raised</param>
    protected virtual void OnGenericCollectionChanged(NotifyGenericCollectionChangedEventArgs<T> e) => GenericCollectionChanged?.Invoke(this, e);

    /// <summary>
    /// Removes all object from the <see cref="RangeObservableCollection{T}"/> that satisfy the <paramref name="predicate"/>
    /// </summary>
    /// <param name="predicate">A predicate used to determine whether to remove an object from the <see cref="RangeObservableCollection{T}"/></param>
    /// <returns>The number of items that were removed</returns>
    public int RemoveAll(Func<T, bool> predicate) => GetAndRemoveAll(predicate).Count;

    /// <summary>
    /// Removes the specified items from the <see cref="RangeObservableCollection{T}"/>
    /// </summary>
    /// <param name="items">The items to be removed</param>
    /// <returns>The number of items that were removed</returns>
    public void RemoveRange(IEnumerable<T> items)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));
        foreach (var item in items)
        {
            var index = Items.IndexOf(item);
            if (index >= 0)
            {
                Items.RemoveAt(index);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
                NotifyCountChanged();
            }
        }
    }

    /// <summary>
    /// Removes the specified items from the <see cref="RangeObservableCollection{T}"/>
    /// </summary>
    /// <param name="items">The items to be removed</param>
    /// <returns>The number of items that were removed</returns>
    public void RemoveRange(IList<T> items) => RemoveRange((IEnumerable<T>)items);

    /// <summary>
    /// Removes the specified range of items from the <see cref="RangeObservableCollection{T}"/>
    /// </summary>
    /// <param name="index">The index of the first item in the range</param>
    /// <param name="count">The number of items in the range</param>
    public void RemoveRange(int index, int count)
    {
        if (count > 0)
        {
            if (RaiseCollectionChangedEventsForIndividualElements)
                for (var i = 0; i < count; ++i)
                    RemoveAt(index);
            else
            {
                var removedItems = new T[count];
                for (var removalIndex = 0; removalIndex < count; ++removalIndex)
                {
                    removedItems[removalIndex] = Items[index];
                    Items.RemoveAt(index);
                }
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems, index));
                NotifyCountChanged();
            }
        }
    }

    /// <summary>
    /// Replace all items in the <see cref="RangeObservableCollection{T}"/> with the items in the specified collection
    /// </summary>
    /// <param name="items">The collection of replacement items</param>
    public void ReplaceAll(IEnumerable<T> items)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));
        if (RaiseCollectionChangedEventsForIndividualElements)
        {
            Clear();
            AddRange(items);
        }
        else
        {
            var oldItems = new T[Items.Count];
            Items.CopyTo(oldItems, 0);
            Items.Clear();
            var list = new List<T>();
            foreach (var element in items)
            {
                Items.Add(element);
                list.Add(element);
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, list, oldItems, 0));
            if (oldItems.Length != list.Count)
                NotifyCountChanged();
        }
    }

    /// <summary>
    /// Replace all items in the <see cref="RangeObservableCollection{T}"/> with the items in the specified collection
    /// </summary>
    /// <param name="items">The collection of replacement items</param>
    public void ReplaceAll(IList<T> items) => ReplaceAll((IEnumerable<T>)items);

    /// <summary>
    /// Replaces the specified range of items from the <see cref="RangeObservableCollection{T}"/> with the items in the specified collection
    /// </summary>
    /// <param name="index">The index of the first item in the range</param>
    /// <param name="count">The number of items in the range</param>
    /// <param name="collection">The collection of replacement items</param>
    /// <returns>The items that were replaced</returns>
    public IReadOnlyList<T> ReplaceRange(int index, int count, IEnumerable<T>? collection = null)
    {
        if (RaiseCollectionChangedEventsForIndividualElements)
        {
            var oldItems = GetRange(index, count);
            RemoveRange(index, count);
            if (collection is not null)
                InsertRange(index, collection);
            return oldItems;
        }
        else
        {
            var originalIndex = index;
            var oldItems = new T[count];
            for (var i = 0; i < count; ++i)
            {
                oldItems[i] = Items[index];
                Items.RemoveAt(index);
            }
            var list = new List<T>();
            index -= 1;
            if (collection is not null)
                foreach (var element in collection)
                {
                    Items.Insert(++index, element);
                    list.Add(element);
                }
            if (list.Count > 0)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, list, oldItems, originalIndex));
            else
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems, originalIndex));
            if (oldItems.Length != list.Count)
                NotifyCountChanged();
            return oldItems.ToImmutableArray();
        }
    }

    /// <summary>
    /// Replaces the specified range of items from the <see cref="RangeObservableCollection{T}"/> with the items in the specified list
    /// </summary>
    /// <param name="index">The index of the first item in the range</param>
    /// <param name="count">The number of items in the range</param>
    /// <param name="list">The list of replacement items</param>
    /// <returns>The items that were replaced</returns>
    public IReadOnlyList<T> ReplaceRange(int index, int count, IList<T> list) => ReplaceRange(index, count, (IEnumerable<T>)list);

    /// <summary>
    /// Resets the <see cref="RangeObservableCollection{T}"/> with the specified collection of items
    /// </summary>
    /// <param name="newCollection">The collection of items</param>
    public void Reset(IEnumerable<T> newCollection)
    {
        if (newCollection is null)
            throw new ArgumentNullException(nameof(newCollection));
        var previousCount = Items.Count;
        Items.Clear();
        foreach (var element in newCollection)
            Items.Add(element);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        if (previousCount != Items.Count)
            NotifyCountChanged();
    }
}
