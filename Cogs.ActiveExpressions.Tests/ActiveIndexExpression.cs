namespace Cogs.ActiveExpressions.Tests;

[TestClass]
public class ActiveIndexExpression
{
    #region TestRangeObservableCollection

    /// <summary>
    /// Represents a dynamic data collection that supports bulk operations and provides notifications when items get added, removed, or when the whole list is refreshed
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    public class SynchronizedRangeObservableCollection<T> :
        SynchronizedObservableCollection<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedRangeObservableCollection{T}"/> class using <see cref="SynchronizationContext.Current"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>)
        /// </summary>
        public SynchronizedRangeObservableCollection() :
            base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedRangeObservableCollection{T}"/> class using <see cref="SynchronizationContext.Current"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>)
        /// </summary>
        /// <param name="raiseCollectionChangedEventsForIndividualElements">Whether to raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon by range methods</param>
        public SynchronizedRangeObservableCollection(bool raiseCollectionChangedEventsForIndividualElements) :
            base() =>
            RaiseCollectionChangedEventsForIndividualElements = raiseCollectionChangedEventsForIndividualElements;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedRangeObservableCollection{T}"/> class that contains elements copied from the specified collection and using <see cref="SynchronizationContext.Current"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>)
        /// </summary>
        /// <param name="collection">The collection from which the elements are copied</param>
        public SynchronizedRangeObservableCollection(IEnumerable<T> collection) :
            base(collection)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedRangeObservableCollection{T}"/> class that contains elements copied from the specified collection and using <see cref="SynchronizationContext.Current"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>)
        /// </summary>
        /// <param name="collection">The collection from which the elements are copied</param>
        /// <param name="raiseCollectionChangedEventsForIndividualElements">Whether to raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon by range methods</param>
        public SynchronizedRangeObservableCollection(IEnumerable<T> collection, bool raiseCollectionChangedEventsForIndividualElements) :
            base(collection) =>
            RaiseCollectionChangedEventsForIndividualElements = raiseCollectionChangedEventsForIndividualElements;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedRangeObservableCollection{T}"/> class using the specified <see cref="System.Threading.SynchronizationContext"/>
        /// </summary>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform all operations</param>
        public SynchronizedRangeObservableCollection(SynchronizationContext? synchronizationContext) :
            base(synchronizationContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedRangeObservableCollection{T}"/> class using the specified <see cref="System.Threading.SynchronizationContext"/>
        /// </summary>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform all operations</param>
        /// <param name="raiseCollectionChangedEventsForIndividualElements">Whether to raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon by range methods</param>
        public SynchronizedRangeObservableCollection(SynchronizationContext? synchronizationContext, bool raiseCollectionChangedEventsForIndividualElements) :
            base(synchronizationContext) =>
            RaiseCollectionChangedEventsForIndividualElements = raiseCollectionChangedEventsForIndividualElements;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedRangeObservableCollection{T}"/> class that contains elements copied from the specified collection and using the specified <see cref="System.Threading.SynchronizationContext"/>
        /// </summary>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform all operations</param>
        /// <param name="collection">The collection from which the elements are copied</param>
        public SynchronizedRangeObservableCollection(SynchronizationContext? synchronizationContext, IEnumerable<T> collection) :
            base(synchronizationContext, collection)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedRangeObservableCollection{T}"/> class that contains elements copied from the specified collection and using the specified <see cref="System.Threading.SynchronizationContext"/>
        /// </summary>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform all operations</param>
        /// <param name="collection">The collection from which the elements are copied</param>
        /// <param name="raiseCollectionChangedEventsForIndividualElements">Whether to raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon by range methods</param>
        public SynchronizedRangeObservableCollection(SynchronizationContext? synchronizationContext, IEnumerable<T> collection, bool raiseCollectionChangedEventsForIndividualElements) :
            base(synchronizationContext, collection) =>
            RaiseCollectionChangedEventsForIndividualElements = raiseCollectionChangedEventsForIndividualElements;

        /// <summary>
        /// Gets whether this <see cref="SynchronizedRangeObservableCollection{T}"/> will raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon by range methods
        /// </summary>
        public bool RaiseCollectionChangedEventsForIndividualElements { get; }

        /// <summary>
        /// Adds objects to the end of the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="items">The objects to be added to the end of the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        public void AddRange(IEnumerable<T> items) =>
            this.Execute(() => InsertRange(Items.Count, items));

        /// <summary>
        /// Adds objects to the end of the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="items">The objects to be added to the end of the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        public void AddRange(IList<T> items) =>
            AddRange((IEnumerable<T>)items);

        /// <summary>
        /// Adds objects to the end of the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="items">The objects to be added to the end of the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        public Task AddRangeAsync(IEnumerable<T> items) =>
            this.ExecuteAsync(() => InsertRangeAsync(Items.Count, items));

        /// <summary>
        /// Adds objects to the end of the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="items">The objects to be added to the end of the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        public Task AddRangeAsync(IList<T> items) =>
            AddRangeAsync((IEnumerable<T>)items);

        /// <summary>
        /// Removes all object from the <see cref="SynchronizedRangeObservableCollection{T}"/> that satisfy the <paramref name="predicate"/>
        /// </summary>
        /// <param name="predicate">A predicate used to determine whether to remove an object from the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        /// <returns>The items that were removed</returns>
        public IReadOnlyList<T> GetAndRemoveAll(Func<T, bool> predicate) =>
            this.Execute(() =>
            {
                var removed = new List<T>();
                for (var i = 0; i < Items.Count;)
                {
                    if (predicate(Items[i]))
                        removed.Add(GetAndRemoveAt(i));
                    else
                        ++i;
                }
                return removed.ToImmutableArray();
            });

        /// <summary>
        /// Removes all object from the <see cref="SynchronizedRangeObservableCollection{T}"/> that satisfy the <paramref name="predicate"/>
        /// </summary>
        /// <param name="predicate">A predicate used to determine whether to remove an object from the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        /// <returns>The items that were removed</returns>
        public Task<IReadOnlyList<T>> GetAndRemoveAllAsync(Func<T, bool> predicate) =>
            this.ExecuteAsync(() => GetAndRemoveAll(predicate));

        /// <summary>
        /// Removes all object from the <see cref="SynchronizedRangeObservableCollection{T}"/> that satisfy the <paramref name="asyncPredicate"/>
        /// </summary>
        /// <param name="asyncPredicate">An asynchronous predicate used to determine whether to remove an object from the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        /// <returns>The items that were removed</returns>
        public Task<IReadOnlyList<T>> GetAndRemoveAllAsync(Func<T, Task<bool>> asyncPredicate) =>
            this.ExecuteAsync(async () =>
            {
                var removed = new List<T>();
                for (var i = 0; i < Items.Count;)
                {
                    if (await asyncPredicate(Items[i]).ConfigureAwait(false))
                        removed.Add(GetAndRemoveAt(i));
                    else
                        ++i;
                }
                return (IReadOnlyList<T>)removed.ToImmutableArray();
            });

        /// <summary>
        /// Gets the element at the specified index and removes it from the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="index">The zero-based index of the element</param>
        /// <returns>The element at the specified index</returns>
        public override T GetAndRemoveAt(int index) =>
            this.Execute(() => base.GetAndRemoveAt(index));

        /// <summary>
        /// Gets the elements in the range starting at the specified index and of the specified length
        /// </summary>
        /// <param name="index">The index of the element at the start of the range</param>
        /// <param name="count">The number of elements in the range</param>
        /// <returns>The elements in the range</returns>
        public IReadOnlyList<T> GetRange(int index, int count) =>
            this.Execute(() =>
            {
                var result = new List<T>();
                for (int i = index, ii = index + count; i < ii; ++i)
                    result.Add(this[i]);
                return result.ToImmutableArray();
            });

        /// <summary>
        /// Gets the elements in the range starting at the specified index and of the specified length
        /// </summary>
        /// <param name="index">The index of the element at the start of the range</param>
        /// <param name="count">The number of elements in the range</param>
        /// <returns>The elements in the range</returns>
        public Task<IReadOnlyList<T>> GetRangeAsync(int index, int count) =>
            this.ExecuteAsync(() => GetRange(index, count));

        /// <summary>
        /// Inserts elements into the <see cref="SynchronizedRangeObservableCollection{T}"/> at the specified index
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="items"/> should be inserted</param>
        /// <param name="items">The objects to insert</param>
        public void InsertRange(int index, IEnumerable<T> items) =>
            this.Execute(() =>
            {
                var originalIndex = index;
                --index;
                if (RaiseCollectionChangedEventsForIndividualElements)
                    foreach (var item in items)
                        InsertItem(++index, item);
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
                        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
                    }
                }
            });

        /// <summary>
        /// Inserts elements into the <see cref="SynchronizedRangeObservableCollection{T}"/> at the specified index
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="items"/> should be inserted</param>
        /// <param name="items">The objects to insert</param>
        public void InsertRange(int index, IList<T> items) =>
            InsertRange(index, (IEnumerable<T>)items);

        /// <summary>
        /// Inserts elements into the <see cref="SynchronizedRangeObservableCollection{T}"/> at the specified index
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="items"/> should be inserted</param>
        /// <param name="items">The objects to insert</param>
        public Task InsertRangeAsync(int index, IEnumerable<T> items) =>
            this.ExecuteAsync(() => InsertRange(index, items));

        /// <summary>
        /// Inserts elements into the <see cref="SynchronizedRangeObservableCollection{T}"/> at the specified index
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="items"/> should be inserted</param>
        /// <param name="items">The objects to insert</param>
        public Task InsertRangeAsync(int index, IList<T> items) =>
            InsertRangeAsync(index, (IEnumerable<T>)items);

        /// <summary>
        /// Moves the items at the specified index to a new location in the collection
        /// </summary>
        /// <param name="oldStartIndex">The zero-based index specifying the location of the items to be moved</param>
        /// <param name="newStartIndex">The zero-based index specifying the new location of the items</param>
        /// <param name="count">The number of items to move</param>
        public void MoveRange(int oldStartIndex, int newStartIndex, int count) =>
            this.Execute(() =>
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
                        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
                    }
                }
            });

        /// <summary>
        /// Moves the items at the specified index to a new location in the collection
        /// </summary>
        /// <param name="oldStartIndex">The zero-based index specifying the location of the items to be moved</param>
        /// <param name="newStartIndex">The zero-based index specifying the new location of the items</param>
        /// <param name="count">The number of items to move</param>
        public Task MoveRangeAsync(int oldStartIndex, int newStartIndex, int count) =>
            this.ExecuteAsync(() => MoveRange(oldStartIndex, newStartIndex, count));

        /// <summary>
        /// Removes all object from the <see cref="SynchronizedRangeObservableCollection{T}"/> that satisfy the <paramref name="predicate"/>
        /// </summary>
        /// <param name="predicate">A predicate used to determine whether to remove an object from the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        /// <returns>The number of items that were removed</returns>
        public int RemoveAll(Func<T, bool> predicate) =>
            this.Execute(() => GetAndRemoveAll(predicate).Count);

        /// <summary>
        /// Removes all object from the <see cref="SynchronizedRangeObservableCollection{T}"/> that satisfy the <paramref name="predicate"/>
        /// </summary>
        /// <param name="predicate">A predicate used to determine whether to remove an object from the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        /// <returns>The number of items that were removed</returns>
        public Task<int> RemoveAllAsync(Func<T, bool> predicate) =>
            this.ExecuteAsync(() => RemoveAll(predicate));

        /// <summary>
        /// Removes all object from the <see cref="SynchronizedRangeObservableCollection{T}"/> that satisfy the <paramref name="asyncPredicate"/>
        /// </summary>
        /// <param name="asyncPredicate">An asynchronous predicate used to determine whether to remove an object from the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        /// <returns>The number of items that were removed</returns>
        public Task<int> RemoveAllAsync(Func<T, Task<bool>> asyncPredicate) =>
            this.ExecuteAsync(async () => (await GetAndRemoveAllAsync(asyncPredicate).ConfigureAwait(false)).Count);

        /// <summary>
        /// Removes the specified items from the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="items">The items to be removed</param>
        /// <returns>The number of items that were removed</returns>
        public void RemoveRange(IEnumerable<T> items) =>
            this.Execute(() =>
            {
                foreach (var item in items)
                {
                    var index = Items.IndexOf(item);
                    if (index >= 0)
                    {
                        Items.RemoveAt(index);
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
                        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
                    }
                }
            });

        /// <summary>
        /// Removes the specified items from the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="items">The items to be removed</param>
        /// <returns>The number of items that were removed</returns>
        public void RemoveRange(IList<T> items) =>
            RemoveRange((IEnumerable<T>)items);

        /// <summary>
        /// Removes the specified range of items from the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="index">The index of the first item in the range</param>
        /// <param name="count">The number of items in the range</param>
        public void RemoveRange(int index, int count) =>
            this.Execute(() =>
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
                        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
                    }
                }
            });

        /// <summary>
        /// Removes the specified items from the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="items">The items to be removed</param>
        /// <returns>The number of items that were removed</returns>
        public Task RemoveRangeAsync(IEnumerable<T> items) =>
            this.ExecuteAsync(() => RemoveRange(items));

        /// <summary>
        /// Removes the specified items from the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="items">The items to be removed</param>
        /// <returns>The number of items that were removed</returns>
        public Task RemoveRangeAsync(IList<T> items) =>
            this.ExecuteAsync(() => RemoveRange(items));

        /// <summary>
        /// Removes the specified range of items from the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="index">The index of the first item in the range</param>
        /// <param name="count">The number of items in the range</param>
        public Task RemoveRangeAsync(int index, int count) =>
            this.ExecuteAsync(() => RemoveRange(index, count));

        /// <summary>
        /// Replace all items in the <see cref="SynchronizedRangeObservableCollection{T}"/> with the items in the specified collection
        /// </summary>
        /// <param name="items">The collection of replacement items</param>
        public void ReplaceAll(IEnumerable<T> items)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));
            this.Execute(() =>
            {
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
                        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
                }
            });
        }

        /// <summary>
        /// Replace all items in the <see cref="SynchronizedRangeObservableCollection{T}"/> with the items in the specified collection
        /// </summary>
        /// <param name="items">The collection of replacement items</param>
        public void ReplaceAll(IList<T> items) =>
            ReplaceAll((IEnumerable<T>)items);

        /// <summary>
        /// Replace all items in the <see cref="SynchronizedRangeObservableCollection{T}"/> with the items in the specified collection
        /// </summary>
        /// <param name="items">The collection of replacement items</param>
        public Task ReplaceAllAsync(IEnumerable<T> items) =>
            this.ExecuteAsync(() => ReplaceAll(items));

        /// <summary>
        /// Replace all items in the <see cref="SynchronizedRangeObservableCollection{T}"/> with the items in the specified collection
        /// </summary>
        /// <param name="items">The collection of replacement items</param>
        public Task ReplaceAllAsync(IList<T> items) =>
            this.ExecuteAsync(() => ReplaceAll(items));

        /// <summary>
        /// Replaces the specified range of items from the <see cref="SynchronizedRangeObservableCollection{T}"/> with the items in the specified collection
        /// </summary>
        /// <param name="index">The index of the first item in the range</param>
        /// <param name="count">The number of items in the range</param>
        /// <param name="collection">The collection of replacement items</param>
        /// <returns>The items that were replaced</returns>
        public IReadOnlyList<T> ReplaceRange(int index, int count, IEnumerable<T>? collection = null) =>
            this.Execute(() =>
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
                        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
                    return oldItems.ToImmutableArray();
                }
            });

        /// <summary>
        /// Replaces the specified range of items from the <see cref="SynchronizedRangeObservableCollection{T}"/> with the items in the specified list
        /// </summary>
        /// <param name="index">The index of the first item in the range</param>
        /// <param name="count">The number of items in the range</param>
        /// <param name="list">The list of replacement items</param>
        /// <returns>The items that were replaced</returns>
        public IReadOnlyList<T> ReplaceRange(int index, int count, IList<T> list) =>
            ReplaceRange(index, count, (IEnumerable<T>)list);

        /// <summary>
        /// Replaces the specified range of items from the <see cref="SynchronizedRangeObservableCollection{T}"/> with the items in the specified collection
        /// </summary>
        /// <param name="index">The index of the first item in the range</param>
        /// <param name="count">The number of items in the range</param>
        /// <param name="collection">The collection of replacement items</param>
        /// <returns>The items that were replaced</returns>
        public Task<IReadOnlyList<T>> ReplaceRangeAsync(int index, int count, IEnumerable<T>? collection = null) =>
            this.ExecuteAsync(() => ReplaceRange(index, count, collection));

        /// <summary>
        /// Replaces the specified range of items from the <see cref="SynchronizedRangeObservableCollection{T}"/> with the items in the specified list
        /// </summary>
        /// <param name="index">The index of the first item in the range</param>
        /// <param name="count">The number of items in the range</param>
        /// <param name="list">The list of replacement items</param>
        /// <returns>The items that were replaced</returns>
        public Task<IReadOnlyList<T>> ReplaceRangeAsync(int index, int count, IList<T> list) =>
            this.ExecuteAsync(() => ReplaceRange(index, count, list));

        /// <summary>
        /// Resets the <see cref="SynchronizedRangeObservableCollection{T}"/> with the specified collection of items
        /// </summary>
        /// <param name="newCollection">The collection of items</param>
        public void Reset(IEnumerable<T> newCollection) =>
            this.Execute(() =>
            {
                var previousCount = Items.Count;
                Items.Clear();
                foreach (var element in newCollection)
                    Items.Add(element);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                if (previousCount != Items.Count)
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            });

        /// <summary>
        /// Resets the <see cref="SynchronizedRangeObservableCollection{T}"/> with the specified collection of items
        /// </summary>
        /// <param name="newCollection">The collection of items</param>
        public Task ResetAsync(IEnumerable<T> newCollection) =>
            this.ExecuteAsync(() => Reset(newCollection));
    }

    class TestRangeObservableCollection<T> :
        SynchronizedRangeObservableCollection<T>
    {
        public TestRangeObservableCollection() : base()
        {
        }

        public TestRangeObservableCollection(IEnumerable<T> collection) : base(collection)
        {
        }

        public void ChangeElementAndOnlyNotifyProperty(int index, T value)
        {
            Items[index] = value;
            OnPropertyChanged(new PropertyChangedEventArgs("Item"));
        }
    }

    #endregion TestRangeObservableCollection

    [TestMethod]
    public void ArgumentChanges()
    {
        var reversedNumbersList = Enumerable.Range(1, 10).Reverse().ToImmutableList();
        var john = TestPerson.CreateJohn();
        var values = new BlockingCollection<int>();
        using (var expr = ActiveExpression.Create((p1, p2) => p1[p2.Name!.Length], reversedNumbersList, john))
        {
            void propertyChanged(object? sender, PropertyChangedEventArgs e) => values.Add(expr.Value);
            expr.PropertyChanged += propertyChanged;
            values.Add(expr.Value);
            john.Name = "J";
            john.Name = "Joh";
            john.Name = string.Empty;
            john.Name = "Johnny";
            john.Name = "John";
            expr.PropertyChanged -= propertyChanged;
        }
        Assert.IsTrue(new int[] { 6, 9, 7, 10, 4, 6 }.SequenceEqual(values));
    }

    [TestMethod]
    public void ArgumentFaultPropagation()
    {
        var numbers = new ObservableCollection<int>(Enumerable.Range(0, 10));
        var john = TestPerson.CreateJohn();
        using var expr = ActiveExpression.Create((p1, p2) => p1[p2.Name!.Length], numbers, john);
        Assert.IsNull(expr.Fault);
        john.Name = null;
        Assert.IsNotNull(expr.Fault);
        john.Name = "John";
        Assert.IsNull(expr.Fault);
    }

    [TestMethod]
    public void CollectionChanges()
    {
        var numbers = new SynchronizedRangeObservableCollection<int>(Enumerable.Range(1, 10));
        var values = new BlockingCollection<int>();
        using (var expr = ActiveExpression.Create(p1 => p1[5], numbers))
        {
            void propertyChanged(object? sender, PropertyChangedEventArgs e) => values.Add(expr.Value);
            expr.PropertyChanged += propertyChanged;
            values.Add(expr.Value);
            numbers.Add(11);
            numbers.Insert(0, 0);
            numbers.Remove(11);
            numbers.Remove(0);
            numbers[4] = 50;
            numbers[4] = 5;
            numbers[5] = 60;
            numbers[5] = 6;
            numbers[6] = 70;
            numbers[6] = 7;
            numbers.Move(0, 1);
            numbers.Move(0, 1);
            numbers.MoveRange(0, 5, 5);
            numbers.MoveRange(0, 5, 5);
            numbers.MoveRange(5, 0, 5);
            numbers.MoveRange(5, 0, 5);
            numbers.Reset(numbers.Select(i => i * 10).ToImmutableArray());
            expr.PropertyChanged -= propertyChanged;
        }
        Assert.IsTrue(new int[] { 6, 5, 6, 60, 6, 1, 6, 1, 6, 60 }.SequenceEqual(values));
    }

    [TestMethod]
    public void ConsistentHashCode()
    {
        int hashCode1, hashCode2;
        var john = TestPerson.CreateJohn();
        var men = new List<TestPerson> { john, null! };
        using (var expr = ActiveExpression.Create(p1 => p1[0], men))
            hashCode1 = expr.GetHashCode();
        using (var expr = ActiveExpression.Create(p1 => p1[0], men))
            hashCode2 = expr.GetHashCode();
        Assert.IsTrue(hashCode1 == hashCode2);
    }

    [TestMethod]
    public void DictionaryChanges()
    {
        var perfectNumbers = new ObservableDictionary<int, int>(Enumerable.Range(1, 10).ToDictionary(i => i, i => i * i));
        var values = new BlockingCollection<int>();
        using (var expr = ActiveExpression.Create(p1 => p1[5], perfectNumbers))
        {
            void propertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(ActiveExpression<object?>.Value))
                    values.Add(expr.Value);
            }

            expr.PropertyChanged += propertyChanged;
            values.Add(expr.Value);
            perfectNumbers.Add(11, 11 * 11);
            perfectNumbers.AddRange(Enumerable.Range(12, 3).ToDictionary(i => i, i => i * i));
            perfectNumbers.Remove(11);
            perfectNumbers.RemoveRange(Enumerable.Range(12, 3));
            perfectNumbers.Remove(5);
            perfectNumbers.Add(5, 30);
            perfectNumbers[5] = 25;
            perfectNumbers.RemoveRange(Enumerable.Range(4, 3));
            perfectNumbers.AddRange(Enumerable.Range(4, 3).ToDictionary(i => i, i => i * i));
            perfectNumbers.Clear();
            expr.PropertyChanged -= propertyChanged;
        }
        Assert.IsTrue(new int[] { 25, 0, 30, 25, 0, 25, 0 }.SequenceEqual(values));
    }

    [TestMethod]
    public void Equality()
    {
        var john = TestPerson.CreateJohn();
        var men = new List<TestPerson> { john, null! };
        var emily = TestPerson.CreateEmily();
        var women = new List<TestPerson> { emily, null! };
        using var expr1 = ActiveExpression.Create(p1 => p1[0], men);
        using var expr2 = ActiveExpression.Create(p1 => p1[0], men);
        using var expr3 = ActiveExpression.Create(p1 => p1[1], men);
        using var expr4 = ActiveExpression.Create(p1 => p1[0], women);
        Assert.IsTrue(expr1 == expr2);
        Assert.IsFalse(expr1 == expr3);
        Assert.IsFalse(expr1 == expr4);
    }

    [TestMethod]
    public void Equals()
    {
        var john = TestPerson.CreateJohn();
        var men = new List<TestPerson> { john, null! };
        var emily = TestPerson.CreateEmily();
        var women = new List<TestPerson> { emily, null! };
        using var expr1 = ActiveExpression.Create(p1 => p1[0], men);
        using var expr2 = ActiveExpression.Create(p1 => p1[0], men);
        using var expr3 = ActiveExpression.Create(p1 => p1[1], men);
        using var expr4 = ActiveExpression.Create(p1 => p1[0], women);
        Assert.IsTrue(expr1.Equals(expr2));
        Assert.IsFalse(expr1.Equals(expr3));
        Assert.IsFalse(expr1.Equals(expr4));
    }

    [TestMethod]
    public void Inequality()
    {
        var john = TestPerson.CreateJohn();
        var men = new List<TestPerson> { john, null! };
        var emily = TestPerson.CreateEmily();
        var women = new List<TestPerson> { emily, null! };
        using var expr1 = ActiveExpression.Create(p1 => p1[0], men);
        using var expr2 = ActiveExpression.Create(p1 => p1[0], men);
        using var expr3 = ActiveExpression.Create(p1 => p1[1], men);
        using var expr4 = ActiveExpression.Create(p1 => p1[0], women);
        Assert.IsFalse(expr1 != expr2);
        Assert.IsTrue(expr1 != expr3);
        Assert.IsTrue(expr1 != expr4);
    }

    [TestMethod]
    public void ManualCreation()
    {
        var people = new List<TestPerson>() { TestPerson.CreateEmily() };
        using var expr = ActiveExpression.Create(Expression.Lambda<Func<string>>(Expression.MakeMemberAccess(Expression.MakeIndex(Expression.Constant(people), typeof(List<TestPerson>).GetProperties().First(p => p.GetIndexParameters().Length > 0), new Expression[] { Expression.Constant(0) }), typeof(TestPerson).GetProperty(nameof(TestPerson.Name))!)));
        Assert.IsNull(expr.Fault);
        Assert.AreEqual("Emily", expr.Value);
    }

    [TestMethod]
    public void ObjectChanges()
    {
        var john = TestPerson.CreateJohn();
        var men = new ObservableCollection<TestPerson> { john };
        var emily = TestPerson.CreateEmily();
        var women = new ObservableCollection<TestPerson> { emily };
        using var expr = ActiveExpression.Create((p1, p2) => (p1.Count > 0 ? p1 : p2)[0], men, women);
        Assert.AreSame(john, expr.Value);
        men.Clear();
        Assert.AreSame(emily, expr.Value);
    }

    [TestMethod]
    public void ObjectFaultPropagation()
    {
        var numbers = new ObservableCollection<int>(Enumerable.Range(0, 10));
        var otherNumbers = new ObservableCollection<int>(Enumerable.Range(0, 10));
        var john = TestPerson.CreateJohn();
        using var expr = ActiveExpression.Create((p1, p2, p3) => (p3.Name!.Length == 0 ? p1 : p2)[0], numbers, otherNumbers, john);
        Assert.IsNull(expr.Fault);
        john.Name = null;
        Assert.IsNotNull(expr.Fault);
        john.Name = "John";
        Assert.IsNull(expr.Fault);
    }

    [TestMethod]
    public void ObjectValueChanges()
    {
        var numbers = new TestRangeObservableCollection<int>(Enumerable.Range(0, 10));
        using var expr = ActiveExpression.Create(p1 => p1[0], numbers);
        Assert.AreEqual(expr.Value, 0);
        numbers.ChangeElementAndOnlyNotifyProperty(0, 100);
        Assert.AreEqual(expr.Value, 100);
    }

    [TestMethod]
    public void StringConversion()
    {
        var emily = TestPerson.CreateEmily();
        emily.Name = "X";
        var people = new ObservableCollection<TestPerson> { emily };
        using var expr = ActiveExpression.Create(p1 => p1[0].Name!.Length + 1, people);
        Assert.AreEqual($"({{C}} /* {people} */[{{C}} /* 0 */] /* {{X}} */.Name /* \"X\" */.Length /* 1 */ + {{C}} /* 1 */) /* 2 */", expr.ToString());
    }

    [TestMethod]
    public async Task ValueAsyncDisposalAsync()
    {
        var john = AsyncDisposableTestPerson.CreateJohn();
        var emily = AsyncDisposableTestPerson.CreateEmily();
        var people = new ObservableCollection<AsyncDisposableTestPerson> { john };
        var options = new ActiveExpressionOptions();
        options.AddExpressionValueDisposal(() => new ObservableCollection<AsyncDisposableTestPerson>()[0]);
        var disposedTcs = new TaskCompletionSource<object?>();
        using (var ae = ActiveExpression.Create(p => p[0], people, options))
        {
            Assert.AreSame(john, ae.Value);
            Assert.IsFalse(john.IsDisposed);
            john.Disposed += (sender, e) => disposedTcs.SetResult(null);
            people[0] = emily;
            await Task.WhenAny(disposedTcs.Task, Task.Delay(TimeSpan.FromSeconds(1)));
            Assert.AreSame(emily, ae.Value);
            Assert.IsTrue(john.IsDisposed);
            disposedTcs = new TaskCompletionSource<object?>();
            emily.Disposed += (sender, e) => disposedTcs.SetResult(null);
        }
        await Task.WhenAny(disposedTcs.Task, Task.Delay(TimeSpan.FromSeconds(1)));
        Assert.IsTrue(emily.IsDisposed);
    }

    [TestMethod]
    public void ValueDisposal()
    {
        var john = SyncDisposableTestPerson.CreateJohn();
        var emily = SyncDisposableTestPerson.CreateEmily();
        var people = new ObservableCollection<SyncDisposableTestPerson> { john };
        var options = new ActiveExpressionOptions();
        options.AddExpressionValueDisposal(() => new ObservableCollection<SyncDisposableTestPerson>()[0]);
        using (var ae = ActiveExpression.Create(p => p[0], people, options))
        {
            Assert.AreSame(john, ae.Value);
            Assert.IsFalse(john.IsDisposed);
            people[0] = emily;
            Assert.AreSame(emily, ae.Value);
            Assert.IsTrue(john.IsDisposed);
        }
        Assert.IsTrue(emily.IsDisposed);
    }
}
