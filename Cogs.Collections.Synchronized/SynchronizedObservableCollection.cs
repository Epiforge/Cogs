namespace Cogs.Collections.Synchronized;

/// <summary>
/// Represents a dynamic data collection the operations of which occur on a specific <see cref="System.Threading.SynchronizationContext"/> and that provides notifications when items get added, removed, or when the whole list is refreshed
/// </summary>
/// <typeparam name="T">The type of elements in the collection</typeparam>
public class SynchronizedObservableCollection<T> :
    ObservableCollection<T>,
    ISynchronized
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronizedObservableCollection{T}"/> class using <see cref="SynchronizationContext.Current"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>)
    /// </summary>
    public SynchronizedObservableCollection() :
        this(SynchronizationContext.Current ?? Synchronization.DefaultSynchronizationContext)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronizedObservableCollection{T}"/> class that contains elements copied from the specified collection and using <see cref="SynchronizationContext.Current"/> (or <see cref="Synchronization.DefaultSynchronizationContext"/> if that is <c>null</c>)
    /// </summary>
    /// <param name="collection">The collection from which the elements are copied</param>
    public SynchronizedObservableCollection(IEnumerable<T> collection) :
        this(SynchronizationContext.Current ?? Synchronization.DefaultSynchronizationContext, collection)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronizedObservableCollection{T}"/> class using the specified <see cref="System.Threading.SynchronizationContext"/>
    /// </summary>
    /// <param name="synchronizationContext">The <see cref="System.Threading.SynchronizationContext"/> on which to perform all operations</param>
    public SynchronizedObservableCollection(SynchronizationContext? synchronizationContext) :
        base() =>
        SynchronizationContext = synchronizationContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronizedObservableCollection{T}"/> class that contains elements copied from the specified collection and using the specified <see cref="System.Threading.SynchronizationContext"/>
    /// </summary>
    /// <param name="synchronizationContext">The <see cref="System.Threading.SynchronizationContext"/> on which to perform all operations</param>
    /// <param name="collection">The collection from which the elements are copied</param>
    public SynchronizedObservableCollection(SynchronizationContext? synchronizationContext, IEnumerable<T> collection) :
        base(collection) =>
        SynchronizationContext = synchronizationContext;

    /// <summary>
    /// Gets or sets the element at the specified index
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set</param>
    /// <returns>The element at the specified index</returns>
    public new T this[int index]
    {
        get => this.Execute(() => base[index]);
        set => this.Execute(() => base[index] = value);
    }

    /// <summary>
    /// Gets the number of elements actually contained in the <see cref="SynchronizedObservableCollection{T}"/>
    /// </summary>
    public Task<int> CountAsync =>
        this.ExecuteAsync(() => Count);

    /// <summary>
    /// Gets the <see cref="System.Threading.SynchronizationContext"/> on which this object's operations occur
    /// </summary>
    public SynchronizationContext? SynchronizationContext { get; }

    /// <summary>
    /// Adds an object to the end of the <see cref="SynchronizedObservableCollection{T}"/>
    /// </summary>
    /// <param name="item">The object to be added to the end of the <see cref="SynchronizedObservableCollection{T}"/></param>
    public Task AddAsync(T item) =>
        this.ExecuteAsync(() => Add(item));

    /// <summary>
    /// Removes all elements from the <see cref="SynchronizedObservableCollection{T}"/>
    /// </summary>
    public Task ClearAsync() =>
        this.ExecuteAsync(() => Clear());

    /// <summary>
    /// Removes all items from the collection
    /// </summary>
    protected override void ClearItems() =>
        this.Execute(() => base.ClearItems());

    /// <summary>
    /// Determines whether an element is in the <see cref="SynchronizedObservableCollection{T}"/>
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="SynchronizedObservableCollection{T}"/></param>
    /// <returns><c>true</c> if <paramref name="item"/> is found in the <see cref="SynchronizedObservableCollection{T}"/>; otherwise, <c>false</c></returns>
    public Task<bool> ContainsAsync(T item) =>
        this.ExecuteAsync(() => Contains(item));

    /// <summary>
    /// Copies the entire <see cref="SynchronizedObservableCollection{T}"/> to a compatible one-dimensional <see cref="Array"/>, starting at the specified index of the target array
    /// </summary>
    /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="SynchronizedObservableCollection{T}"/></param>
    /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins</param>
    public Task CopyToAsync(T[] array, int index) =>
        this.ExecuteAsync(() => CopyTo(array, index));

    /// <summary>
    /// Copies the entire <see cref="SynchronizedObservableCollection{T}"/> into a <see cref="IReadOnlyList{T}"/>
    /// </summary>
    public IReadOnlyList<T> GetAll() =>
        this.Execute(() => Items.ToImmutableArray());

    /// <summary>
    /// Copies the entire <see cref="SynchronizedObservableCollection{T}"/> into a <see cref="IReadOnlyList{T}"/>
    /// </summary>
    public Task<IReadOnlyList<T>> GetAllAsync() =>
        this.ExecuteAsync(() => (IReadOnlyList<T>)Items.ToImmutableArray());

    /// <summary>
    /// Gets the element at the specified index and removes it from the <see cref="SynchronizedObservableCollection{T}"/>
    /// </summary>
    /// <param name="index">The zero-based index of the element</param>
    /// <returns>The element at the specified index</returns>
    public virtual T GetAndRemoveAt(int index) =>
        this.Execute(() =>
        {
            var item = Items[index];
            RemoveAt(index);
            return item;
        });

    /// <summary>
    /// Gets the element at the specified index and removes it from the <see cref="SynchronizedObservableCollection{T}"/>
    /// </summary>
    /// <param name="index">The zero-based index of the element</param>
    /// <returns>The element at the specified index</returns>
    public Task<T> GetAndRemoveAtAsync(int index) =>
        this.ExecuteAsync(() => GetAndRemoveAt(index));

    /// <summary>
    /// Gets the element at the specified index
    /// </summary>
    /// <param name="index">The zero-based index of the element to get</param>
    /// <returns>The element at the specified index</returns>
    public Task<T> GetItemAsync(int index) =>
        this.ExecuteAsync(() => this[index]);

    /// <summary>
    /// Searches for the specified object and returns the zero-based index of the first occurrence within the entire <see cref="SynchronizedObservableCollection{T}"/>
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="SynchronizedObservableCollection{T}"/></param>
    /// <returns>The zero-based index of the first occurrence of <paramref name="item"/> within the entire <see cref="SynchronizedObservableCollection{T}"/>, if found; otherwise, -1</returns>
    public Task<int> IndexOfAsync(T item) =>
        this.ExecuteAsync(() => IndexOf(item));

    /// <summary>
    /// Inserts an element into the <see cref="SynchronizedObservableCollection{T}"/> at the specified index
    /// </summary>
    /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted</param>
    /// <param name="item">The object to insert</param>
    public Task InsertAsync(int index, T item) =>
        this.ExecuteAsync(() => Insert(index, item));

    /// <summary>
    /// Inserts an item into the collection at the specified index
    /// </summary>
    /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted</param>
    /// <param name="item">The object to insert</param>
    protected override void InsertItem(int index, T item) =>
        this.Execute(() => base.InsertItem(index, item));

    /// <summary>
    /// Moves the item at the specified index to a new location in the collection
    /// </summary>
    /// <param name="oldIndex">The zero-based index specifying the location of the item to be moved</param>
    /// <param name="newIndex">The zero-based index specifying the new location of the item</param>
    public Task MoveAsync(int oldIndex, int newIndex) =>
        this.ExecuteAsync(() => Move(oldIndex, newIndex));

    /// <summary>
    /// Moves the item at the specified index to a new location in the collection
    /// </summary>
    /// <param name="oldIndex">The zero-based index specifying the location of the item to be moved</param>
    /// <param name="newIndex">The zero-based index specifying the new location of the item</param>
    protected override void MoveItem(int oldIndex, int newIndex) =>
        this.Execute(() => base.MoveItem(oldIndex, newIndex));

    /// <summary>
    /// Removes the first occurrence of a specific object from the <see cref="SynchronizedObservableCollection{T}"/>
    /// </summary>
    /// <param name="item">The object to remove from the <see cref="SynchronizedObservableCollection{T}"/></param>
    /// <returns><c>true</c> if <paramref name="item"/> is successfully removed; otherwise, <c>false</c></returns>
    public Task<bool> RemoveAsync(T item) =>
        this.ExecuteAsync(() => Remove(item));

    /// <summary>
    /// Removes the element at the specified index of the <see cref="SynchronizedObservableCollection{T}"/>
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove</param>
    public Task RemoveAtAsync(int index) =>
        this.ExecuteAsync(() => RemoveAt(index));

    /// <summary>
    /// Removes the element at the specified index of the <see cref="SynchronizedObservableCollection{T}"/>
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove</param>
    protected override void RemoveItem(int index) =>
        this.Execute(() => base.RemoveItem(index));

    /// <summary>
    /// Replaces the item with the specified index with a specified item
    /// </summary>
    /// <param name="index">The index of the item to be replaced</param>
    /// <param name="item">The replacement item</param>
    /// <returns>The item that was replaced</returns>
    public T Replace(int index, T item) =>
        this.Execute(() =>
        {
            var replacedItem = Items[index];
            Items[index] = item;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, replacedItem, index));
            return replacedItem;
        });

    /// <summary>
    /// Replaces the item with the specified index with a specified item
    /// </summary>
    /// <param name="index">The index of the item to be replaced</param>
    /// <param name="item">The replacement item</param>
    /// <returns>The item that was replaced</returns>
    public Task<T> ReplaceAsync(int index, T item) =>
        this.ExecuteAsync(() => Replace(index, item));

    /// <summary>
    /// Replaces the element at the specified index
    /// </summary>
    /// <param name="index">The zero-based index of the element to replace</param>
    /// <param name="item">The new value for the element at the specified index</param>
    protected override void SetItem(int index, T item) =>
        this.Execute(() => base.SetItem(index, item));

    /// <summary>
    /// Replaces the element at the specified index
    /// </summary>
    /// <param name="index">The zero-based index of the element to replace</param>
    /// <param name="item">The new value for the element at the specified index</param>
    public Task SetItemAsync(int index, T item) =>
        this.ExecuteAsync(() => this[index] = item);
}
