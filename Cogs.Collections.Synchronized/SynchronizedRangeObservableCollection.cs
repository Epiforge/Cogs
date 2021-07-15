using Cogs.Threading;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Cogs.Collections.Synchronized
{
    /// <summary>
    /// Represents a dynamic data collection that supports bulk operations and provides notifications when items get added, removed, or when the whole list is refreshed
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    public class SynchronizedRangeObservableCollection<T> : SynchronizedObservableCollection<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedRangeObservableCollection{T}"/> class using <see cref="SynchronizationContext.Current"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>)
        /// </summary>
        public SynchronizedRangeObservableCollection() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedRangeObservableCollection{T}"/> class using <see cref="SynchronizationContext.Current"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>)
        /// </summary>
        /// <param name="raiseCollectionChangedEventsForIndividualElements">Whether to raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon by range methods</param>
        public SynchronizedRangeObservableCollection(bool raiseCollectionChangedEventsForIndividualElements) : base() => RaiseCollectionChangedEventsForIndividualElements = raiseCollectionChangedEventsForIndividualElements;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedRangeObservableCollection{T}"/> class that contains elements copied from the specified collection and using <see cref="SynchronizationContext.Current"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>)
        /// </summary>
        /// <param name="collection">The collection from which the elements are copied</param>
        public SynchronizedRangeObservableCollection(IEnumerable<T> collection) : base(collection)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedRangeObservableCollection{T}"/> class that contains elements copied from the specified collection and using <see cref="SynchronizationContext.Current"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>)
        /// </summary>
        /// <param name="collection">The collection from which the elements are copied</param>
        /// <param name="raiseCollectionChangedEventsForIndividualElements">Whether to raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon by range methods</param>
        public SynchronizedRangeObservableCollection(IEnumerable<T> collection, bool raiseCollectionChangedEventsForIndividualElements) : base(collection) => RaiseCollectionChangedEventsForIndividualElements = raiseCollectionChangedEventsForIndividualElements;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedRangeObservableCollection{T}"/> class using the specified <see cref="System.Threading.SynchronizationContext"/>
        /// </summary>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform all operations</param>
        public SynchronizedRangeObservableCollection(SynchronizationContext? synchronizationContext) : base(synchronizationContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedRangeObservableCollection{T}"/> class using the specified <see cref="System.Threading.SynchronizationContext"/>
        /// </summary>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform all operations</param>
        /// <param name="raiseCollectionChangedEventsForIndividualElements">Whether to raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon by range methods</param>
        public SynchronizedRangeObservableCollection(SynchronizationContext? synchronizationContext, bool raiseCollectionChangedEventsForIndividualElements) : base(synchronizationContext) => RaiseCollectionChangedEventsForIndividualElements = raiseCollectionChangedEventsForIndividualElements;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedRangeObservableCollection{T}"/> class that contains elements copied from the specified collection and using the specified <see cref="System.Threading.SynchronizationContext"/>
        /// </summary>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform all operations</param>
        /// <param name="collection">The collection from which the elements are copied</param>
        public SynchronizedRangeObservableCollection(SynchronizationContext? synchronizationContext, IEnumerable<T> collection) : base(synchronizationContext, collection)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedRangeObservableCollection{T}"/> class that contains elements copied from the specified collection and using the specified <see cref="System.Threading.SynchronizationContext"/>
        /// </summary>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform all operations</param>
        /// <param name="collection">The collection from which the elements are copied</param>
        /// <param name="raiseCollectionChangedEventsForIndividualElements">Whether to raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon by range methods</param>
        public SynchronizedRangeObservableCollection(SynchronizationContext? synchronizationContext, IEnumerable<T> collection, bool raiseCollectionChangedEventsForIndividualElements) : base(synchronizationContext, collection) => RaiseCollectionChangedEventsForIndividualElements = raiseCollectionChangedEventsForIndividualElements;

        /// <summary>
        /// Gets whether this <see cref="SynchronizedRangeObservableCollection{T}"/> will raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon by range methods
        /// </summary>
        public bool RaiseCollectionChangedEventsForIndividualElements { get; }

        /// <summary>
        /// Adds objects to the end of the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="items">The objects to be added to the end of the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        public void AddRange(IEnumerable<T> items) => InsertRange(Items.Count, items);

        /// <summary>
        /// Adds objects to the end of the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="items">The objects to be added to the end of the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        public void AddRange(IList<T> items) => AddRange((IEnumerable<T>)items);

        /// <summary>
        /// Adds objects to the end of the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="items">The objects to be added to the end of the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        public Task AddRangeAsync(IEnumerable<T> items) => InsertRangeAsync(Items.Count, items);

        /// <summary>
        /// Adds objects to the end of the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="items">The objects to be added to the end of the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        public Task AddRangeAsync(IList<T> items) => AddRangeAsync((IEnumerable<T>)items);

        /// <summary>
        /// Removes all object from the <see cref="SynchronizedRangeObservableCollection{T}"/> that satisfy the <paramref name="predicate"/>
        /// </summary>
        /// <param name="predicate">A predicate used to determine whether to remove an object from the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        /// <returns>The items that were removed</returns>
        public IReadOnlyList<T> GetAndRemoveAll(Func<T, bool> predicate) => this.Execute(() =>
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
        public Task<IReadOnlyList<T>> GetAndRemoveAllAsync(Func<T, bool> predicate) => this.ExecuteAsync(() => GetAndRemoveAll(predicate));

        /// <summary>
        /// Removes all object from the <see cref="SynchronizedRangeObservableCollection{T}"/> that satisfy the <paramref name="asyncPredicate"/>
        /// </summary>
        /// <param name="asyncPredicate">An asynchronous predicate used to determine whether to remove an object from the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        /// <returns>The items that were removed</returns>
        public Task<IReadOnlyList<T>> GetAndRemoveAllAsync(Func<T, Task<bool>> asyncPredicate) => this.ExecuteAsync(async () =>
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
        public override T GetAndRemoveAt(int index) => this.Execute(() => base.GetAndRemoveAt(index));

        /// <summary>
        /// Gets the elements in the range starting at the specified index and of the specified length
        /// </summary>
        /// <param name="index">The index of the element at the start of the range</param>
        /// <param name="count">The number of elements in the range</param>
        /// <returns>The elements in the range</returns>
        public IReadOnlyList<T> GetRange(int index, int count) => this.Execute(() =>
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
        public Task<IReadOnlyList<T>> GetRangeAsync(int index, int count) => this.ExecuteAsync(() => GetRange(index, count));

        /// <summary>
        /// Inserts elements into the <see cref="SynchronizedRangeObservableCollection{T}"/> at the specified index
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="items"/> should be inserted</param>
        /// <param name="items">The objects to insert</param>
        public void InsertRange(int index, IEnumerable<T> items) => this.Execute(() =>
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
        public void InsertRange(int index, IList<T> items) => InsertRange(index, (IEnumerable<T>)items);

        /// <summary>
        /// Inserts elements into the <see cref="SynchronizedRangeObservableCollection{T}"/> at the specified index
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="items"/> should be inserted</param>
        /// <param name="items">The objects to insert</param>
        public Task InsertRangeAsync(int index, IEnumerable<T> items) => this.ExecuteAsync(() => InsertRange(index, items));

        /// <summary>
        /// Inserts elements into the <see cref="SynchronizedRangeObservableCollection{T}"/> at the specified index
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="items"/> should be inserted</param>
        /// <param name="items">The objects to insert</param>
        public Task InsertRangeAsync(int index, IList<T> items) => InsertRangeAsync(index, (IEnumerable<T>)items);

        /// <summary>
        /// Moves the items at the specified index to a new location in the collection
        /// </summary>
        /// <param name="oldStartIndex">The zero-based index specifying the location of the items to be moved</param>
        /// <param name="newStartIndex">The zero-based index specifying the new location of the items</param>
        /// <param name="count">The number of items to move</param>
        public void MoveRange(int oldStartIndex, int newStartIndex, int count) => this.Execute(() =>
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
        public Task MoveRangeAsync(int oldStartIndex, int newStartIndex, int count) => this.ExecuteAsync(() => MoveRange(oldStartIndex, newStartIndex, count));

        /// <summary>
        /// Removes all object from the <see cref="SynchronizedRangeObservableCollection{T}"/> that satisfy the <paramref name="predicate"/>
        /// </summary>
        /// <param name="predicate">A predicate used to determine whether to remove an object from the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        /// <returns>The number of items that were removed</returns>
        public int RemoveAll(Func<T, bool> predicate) => this.Execute(() => GetAndRemoveAll(predicate).Count);

        /// <summary>
        /// Removes all object from the <see cref="SynchronizedRangeObservableCollection{T}"/> that satisfy the <paramref name="predicate"/>
        /// </summary>
        /// <param name="predicate">A predicate used to determine whether to remove an object from the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        /// <returns>The number of items that were removed</returns>
        public Task<int> RemoveAllAsync(Func<T, bool> predicate) => this.ExecuteAsync(() => RemoveAll(predicate));

        /// <summary>
        /// Removes all object from the <see cref="SynchronizedRangeObservableCollection{T}"/> that satisfy the <paramref name="asyncPredicate"/>
        /// </summary>
        /// <param name="asyncPredicate">An asynchronous predicate used to determine whether to remove an object from the <see cref="SynchronizedRangeObservableCollection{T}"/></param>
        /// <returns>The number of items that were removed</returns>
        public Task<int> RemoveAllAsync(Func<T, Task<bool>> asyncPredicate) => this.ExecuteAsync(async () => (await GetAndRemoveAllAsync(asyncPredicate).ConfigureAwait(false)).Count);

        /// <summary>
        /// Removes the specified items from the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="items">The items to be removed</param>
        /// <returns>The number of items that were removed</returns>
        public void RemoveRange(IEnumerable<T> items) => this.Execute(() =>
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
        public void RemoveRange(IList<T> items) => RemoveRange((IEnumerable<T>)items);

        /// <summary>
        /// Removes the specified range of items from the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="index">The index of the first item in the range</param>
        /// <param name="count">The number of items in the range</param>
        public void RemoveRange(int index, int count) => this.Execute(() =>
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
        public Task RemoveRangeAsync(IEnumerable<T> items) => this.ExecuteAsync(() => RemoveRange(items));

        /// <summary>
        /// Removes the specified items from the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="items">The items to be removed</param>
        /// <returns>The number of items that were removed</returns>
        public Task RemoveRangeAsync(IList<T> items) => this.ExecuteAsync(() => RemoveRange(items));

        /// <summary>
        /// Removes the specified range of items from the <see cref="SynchronizedRangeObservableCollection{T}"/>
        /// </summary>
        /// <param name="index">The index of the first item in the range</param>
        /// <param name="count">The number of items in the range</param>
        public Task RemoveRangeAsync(int index, int count) => this.ExecuteAsync(() => RemoveRange(index, count));

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
        public void ReplaceAll(IList<T> items) => ReplaceAll((IEnumerable<T>)items);

        /// <summary>
        /// Replace all items in the <see cref="SynchronizedRangeObservableCollection{T}"/> with the items in the specified collection
        /// </summary>
        /// <param name="items">The collection of replacement items</param>
        public Task ReplaceAllAsync(IEnumerable<T> items) => this.ExecuteAsync(() => ReplaceAll(items));

        /// <summary>
        /// Replace all items in the <see cref="SynchronizedRangeObservableCollection{T}"/> with the items in the specified collection
        /// </summary>
        /// <param name="items">The collection of replacement items</param>
        public Task ReplaceAllAsync(IList<T> items) => this.ExecuteAsync(() => ReplaceAll(items));

        /// <summary>
        /// Replaces the specified range of items from the <see cref="SynchronizedRangeObservableCollection{T}"/> with the items in the specified collection
        /// </summary>
        /// <param name="index">The index of the first item in the range</param>
        /// <param name="count">The number of items in the range</param>
        /// <param name="collection">The collection of replacement items</param>
        /// <returns>The items that were replaced</returns>
        public IReadOnlyList<T> ReplaceRange(int index, int count, IEnumerable<T>? collection = null) => this.Execute(() =>
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
        public IReadOnlyList<T> ReplaceRange(int index, int count, IList<T> list) => ReplaceRange(index, count, (IEnumerable<T>)list);

        /// <summary>
        /// Replaces the specified range of items from the <see cref="SynchronizedRangeObservableCollection{T}"/> with the items in the specified collection
        /// </summary>
        /// <param name="index">The index of the first item in the range</param>
        /// <param name="count">The number of items in the range</param>
        /// <param name="collection">The collection of replacement items</param>
        /// <returns>The items that were replaced</returns>
        public Task<IReadOnlyList<T>> ReplaceRangeAsync(int index, int count, IEnumerable<T>? collection = null) => this.ExecuteAsync(() => ReplaceRange(index, count, collection));

        /// <summary>
        /// Replaces the specified range of items from the <see cref="SynchronizedRangeObservableCollection{T}"/> with the items in the specified list
        /// </summary>
        /// <param name="index">The index of the first item in the range</param>
        /// <param name="count">The number of items in the range</param>
        /// <param name="list">The list of replacement items</param>
        /// <returns>The items that were replaced</returns>
        public Task<IReadOnlyList<T>> ReplaceRangeAsync(int index, int count, IList<T> list) => this.ExecuteAsync(() => ReplaceRange(index, count, list));

        /// <summary>
        /// Resets the <see cref="SynchronizedRangeObservableCollection{T}"/> with the specified collection of items
        /// </summary>
        /// <param name="newCollection">The collection of items</param>
        public void Reset(IEnumerable<T> newCollection) => this.Execute(() =>
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
        public Task ResetAsync(IEnumerable<T> newCollection) => this.ExecuteAsync(() => Reset(newCollection));
    }
}
