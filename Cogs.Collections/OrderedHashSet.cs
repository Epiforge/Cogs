namespace Cogs.Collections;

/// <summary>
/// Represents an ordered set of values
/// </summary>
/// <typeparam name="T">The type of elements in the ordered hash set</typeparam>
public sealed class OrderedHashSet<T> :
    ICollection<T>,
    IEnumerable,
    IEnumerable<T>,
    IReadOnlyCollection<T>,
    ISet<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedHashSet{T}"/> class that is empty and uses the default equality comparer for the set type
    /// </summary>
    public OrderedHashSet()
    {
        dict = new NullableKeyDictionary<T, LinkedListNode<T>>();
        list = new LinkedList<T>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedHashSet{T}"/> class that uses the default equality comparer for the set type, contains elements copied from the specified collection, and has sufficient capacity to accommodate the number of elements copied
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new set</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c></exception>
    public OrderedHashSet(IEnumerable<T> collection) :
        this()
    {
        foreach (var item in collection ?? throw new ArgumentNullException(nameof(collection)))
            Add(item);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedHashSet{T}"/> class that uses the specified equality comparer for the set type, contains elements copied from the specified collection, and has sufficient capacity to accommodate the number of elements copied
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new set</param>
    /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing values in the set, or <c>null</c> to use the default <see cref="EqualityComparer{T}"/> implementation for the set type</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c></exception>
    public OrderedHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer) :
        this(comparer)
    {
        foreach (var item in collection ?? throw new ArgumentNullException(nameof(collection)))
            Add(item);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedHashSet{T}"/> class that is empty and uses the specified equality comparer for the set type
    /// </summary>
    /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing values in the set, or <c>null</c> to use the default <see cref="EqualityComparer{T}"/> implementation for the set type</param>
    public OrderedHashSet(IEqualityComparer<T> comparer)
    {
        dict = new NullableKeyDictionary<T, LinkedListNode<T>>(comparer);
        list = new LinkedList<T>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedHashSet{T}"/> class that is empty, but has reserved space for capacity items and uses the default equality comparer for the set type
    /// </summary>
    /// <param name="capacity">The initial size of the <see cref="OrderedHashSet{T}"/></param>
    public OrderedHashSet(int capacity)
    {
        dict = new NullableKeyDictionary<T, LinkedListNode<T>>(capacity);
        list = new LinkedList<T>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedHashSet{T}"/> class that uses the specified equality comparer for the set type, and has sufficient capacity to accommodate capacity elements
    /// </summary>
    /// <param name="capacity">The initial size of the <see cref="OrderedHashSet{T}"/></param>
    /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing values in the set, or <c>null</c> to use the default <see cref="EqualityComparer{T}"/> implementation for the set type</param>
    public OrderedHashSet(int capacity, IEqualityComparer<T> comparer)
    {
        dict = new NullableKeyDictionary<T, LinkedListNode<T>>(capacity, comparer);
        list = new LinkedList<T>();
    }

    readonly NullableKeyDictionary<T, LinkedListNode<T>> dict;
    readonly LinkedList<T> list;

    /// <summary>
    /// Gets the number of elements that are contained in a set
    /// </summary>
    public int Count =>
        dict.Count;

    /// <summary>
    /// Gets the first element in the ordered set
    /// </summary>
    public T First =>
        dict.Count == 0 ? throw new InvalidOperationException("The ordered set contains no elements") : list.First.Value;

    bool ICollection<T>.IsReadOnly { get; } = false;

    /// <summary>
    /// Gets the last element in the ordered set
    /// </summary>
    public T Last =>
        dict.Count == 0 ? throw new InvalidOperationException("The ordered set contains no elements") : list.Last.Value;

    /// <summary>
    /// Adds the specified element to a set
    /// </summary>
    /// <param name="item">The element to add to the set</param>
    /// <returns><c>true</c> if the element is added to the <see cref="OrderedHashSet{T}"/> object; <c>false</c> if the element is already present</returns>
    public bool Add(T item) =>
        AddLast(item);

    /// <summary>
    /// Adds the specified element to the beginning of an ordered set
    /// </summary>
    /// <param name="item">The element to add to the set</param>
    /// <returns><c>true</c> if the element is added to the <see cref="OrderedHashSet{T}"/> object; <c>false</c> if the element is already present</returns>
    public bool AddFirst(T item)
    {
        if (dict.ContainsKey(item))
            return false;
        var node = list.AddFirst(item);
        dict.Add(item, node);
        return true;
    }

    /// <summary>
    /// Adds the specified element to the end of an ordered set
    /// </summary>
    /// <param name="item">The element to add to the set</param>
    /// <returns><c>true</c> if the element is added to the <see cref="OrderedHashSet{T}"/> object; <c>false</c> if the element is already present</returns>
    public bool AddLast(T item)
    {
        if (dict.ContainsKey(item))
            return false;
        var node = list.AddLast(item);
        dict.Add(item, node);
        return true;
    }

    void ICollection<T>.Add(T item) =>
        Add(item);

    /// <summary>
    /// Removes all elements from an <see cref="OrderedHashSet{T}"/> object
    /// </summary>
    public void Clear()
    {
        dict.Clear();
        list.Clear();
    }

    /// <summary>
    /// Determines whether an <see cref="OrderedHashSet{T}"/> object contains the specified element
    /// </summary>
    /// <param name="item">The element to locate in the <see cref="OrderedHashSet{T}"/> object</param>
    /// <returns><c>true</c> if the <see cref="OrderedHashSet{T}"/> object contains the specified element; otherwise, <c>false</c></returns>
    public bool Contains(T item) =>
        dict.ContainsKey(item);

    /// <summary>
    /// Copies the elements of an <see cref="OrderedHashSet{T}"/> object to an array
    /// </summary>
    /// <param name="array">The one-dimensional array that is the destination of the elements copied from the <see cref="OrderedHashSet{T}"/> object (the array must have zero-based indexing)</param>
    public void CopyTo(T[] array) =>
        list.CopyTo(array, 0);

    /// <summary>
    /// Copies the elements of an <see cref="OrderedHashSet{T}"/> object to an array, starting at the specified array index
    /// </summary>
    /// <param name="array">The one-dimensional array that is the destination of the elements copied from the <see cref="OrderedHashSet{T}"/> object (the array must have zero-based indexing)</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins</param>
    public void CopyTo(T[] array, int arrayIndex) =>
        list.CopyTo(array, arrayIndex);

    /// <summary>
    /// Copies the specified number of elements of an <see cref="OrderedHashSet{T}"/> object to an array, starting at the specified array index
    /// </summary>
    /// <param name="array">The one-dimensional array that is the destination of the elements copied from the <see cref="OrderedHashSet{T}"/> object (the array must have zero-based indexing)</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins</param>
    /// <param name="count">The number of elements to copy to array</param>
    public void CopyTo(T[] array, int arrayIndex, int count) =>
        list.Cast<T>().Take(count).ToImmutableArray().CopyTo(array, arrayIndex);

    /// <summary>
    /// Ensures that this hash set can hold the specified number of elements without growing
    /// </summary>
    /// <param name="capacity">The minimum capacity to ensure</param>
    /// <returns>The new capacity of this instance</returns>
    public int EnsureCapacity(int capacity) =>
        dict.EnsureCapacity(capacity);

    /// <summary>
    /// Removes all elements in the specified collection from the current <see cref="OrderedHashSet{T}"/> object
    /// </summary>
    /// <param name="other">The collection of items to remove from the <see cref="OrderedHashSet{T}"/> object</param>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null</exception>
    public void ExceptWith(IEnumerable<T> other)
    {
        foreach (var item in other ?? throw new ArgumentNullException(nameof(other)))
            Remove(item);
    }

    /// <summary>
    /// Returns an enumerator that iterates through an <see cref="OrderedHashSet{T}"/> object
    /// </summary>
    /// <returns>An <see cref="IEnumerator{T}"/> object for the <see cref="OrderedHashSet{T}"/> object</returns>
    public IEnumerator<T> GetEnumerator() =>
        list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    ISet<T> GetSet(IEnumerable<T> enumerable)
    {
        if (enumerable is not ISet<T> set)
            set = new HashSet<T>(enumerable, dict.Comparer);
        return set;
    }

    /// <summary>
    /// Modifies the current <see cref="OrderedHashSet{T}"/> object to contain only elements that are present in that object and in the specified collection
    /// </summary>
    /// <param name="other">The collection to compare to the current <see cref="OrderedHashSet{T}"/> object</param>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null</exception>
    public void IntersectWith(IEnumerable<T> other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));
        var itemsToRemove = new HashSet<T>(list, dict.Comparer);
        foreach (var item in other)
            itemsToRemove.Remove(item);
        foreach (var item in itemsToRemove)
            Remove(item);
    }

    /// <summary>
    /// Determines whether an <see cref="OrderedHashSet{T}"/> object is a proper subset of the specified collection
    /// </summary>
    /// <param name="other">The collection to compare to the current <see cref="OrderedHashSet{T}"/> object</param>
    /// <returns><c>true</c> if the <see cref="OrderedHashSet{T}"/> object is a proper subset of other; otherwise, <c>false</c></returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null</exception>
    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));
        var otherSet = GetSet(other);
        if (dict.Count >= otherSet.Count)
            return false;
        foreach (var item in dict.Keys)
            if (!otherSet.Contains(item))
                return false;
        return true;
    }

    /// <summary>
    /// Determines whether an <see cref="OrderedHashSet{T}"/> object is a proper superset of the specified collection
    /// </summary>
    /// <param name="other">The collection to compare to the current <see cref="OrderedHashSet{T}"/> object</param>
    /// <returns><c>true</c> if the <see cref="OrderedHashSet{T}"/> object is a proper superset of other; otherwise, <c>false</c></returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null</exception>
    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));
        var otherSet = GetSet(other);
        if (otherSet.Count >= dict.Count)
            return false;
        foreach (var item in otherSet)
            if (!dict.ContainsKey(item))
                return false;
        return true;
    }

    /// <summary>
    /// Determines whether an <see cref="OrderedHashSet{T}"/> object is a subset of the specified collection
    /// </summary>
    /// <param name="other">The collection to compare to the current <see cref="OrderedHashSet{T}"/> object</param>
    /// <returns><c>true</c> if the <see cref="OrderedHashSet{T}"/> object is a subset of other; otherwise, <c>false</c></returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null</exception>
    public bool IsSubsetOf(IEnumerable<T> other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));
        var otherSet = GetSet(other);
        if (dict.Count > otherSet.Count)
            return false;
        foreach (var item in dict.Keys)
            if (!otherSet.Contains(item))
                return false;
        return true;
    }

    /// <summary>
    /// Determines whether an <see cref="OrderedHashSet{T}"/> object is a superset of the specified collection
    /// </summary>
    /// <param name="other">The collection to compare to the current <see cref="OrderedHashSet{T}"/> object</param>
    /// <returns><c>true</c> if the <see cref="OrderedHashSet{T}"/> object is a superset of other; otherwise, <c>false</c></returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null</exception>
    public bool IsSupersetOf(IEnumerable<T> other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));
        var otherSet = GetSet(other);
        if (otherSet.Count > dict.Count)
            return false;
        foreach (var item in otherSet)
            if (!dict.ContainsKey(item))
                return false;
        return true;
    }

    /// <summary>
    /// Moves the specified element to first position in the ordered set
    /// </summary>
    /// <param name="item">The element to move in the set</param>
    /// <returns><c>true</c> if the element was moved to the first position in the <see cref="OrderedHashSet{T}"/> object; <c>false</c> if the element was not present</returns>
    public bool MoveToFirst(T item)
    {
        if (Remove(item))
        {
            AddFirst(item);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Moves the specified element to last position in the ordered set
    /// </summary>
    /// <param name="item">The element to move in the set</param>
    /// <returns><c>true</c> if the element was moved to the last position in the <see cref="OrderedHashSet{T}"/> object; <c>false</c> if the element was not present</returns>
    public bool MoveToLast(T item)
    {
        if (Remove(item))
        {
            Add(item);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Determines whether the current <see cref="OrderedHashSet{T}"/> object and a specified collection share common elements
    /// </summary>
    /// <param name="other">The collection to compare to the current <see cref="OrderedHashSet{T}"/> object</param>
    /// <returns><c>true</c> if the <see cref="OrderedHashSet{T}"/> object and other share at least one common element; otherwise, <c>false</c></returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null</exception>
    public bool Overlaps(IEnumerable<T> other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));
        foreach (var item in other)
            if (dict.ContainsKey(item))
                return true;
        return false;
    }

    /// <summary>
    /// Removes the specified element from an <see cref="OrderedHashSet{T}"/> object
    /// </summary>
    /// <param name="item">The element to remove</param>
    /// <returns><c>true</c> if the element is successfully found and removed; otherwise, <c>false</c> (this method returns <c>false</c> if <paramref name="item"/> is not found in the <see cref="OrderedHashSet{T}"/> object)</returns>
    public bool Remove(T item)
    {
        if (!dict.TryGetValue(item, out var node))
            return false;
        dict.Remove(item);
        list.Remove(node);
        return true;
    }

    /// <summary>
    /// Removes the first element in the ordered set
    /// </summary>
    /// <param name="item">The item that was removed</param>
    /// <returns><c>true</c> if an element was removed; otherwise, <c>false</c></returns>
    [SuppressMessage("Design", "CA1021: Avoid out parameters", Justification = "Since the set may contain null, we really don't have a choice here unless we resort to tuples.")]
    public bool RemoveFirst([MaybeNullWhen(false)] out T item)
    {
        if (dict.Count == 0)
        {
            item = default;
            return false;
        }
        var node = list.First;
        item = node.Value;
        dict.Remove(item);
        list.Remove(node);
        return true;
    }

    /// <summary>
    /// Removes the last element in the ordered set
    /// </summary>
    /// <param name="item">The item that was removed</param>
    /// <returns><c>true</c> if an element was removed; otherwise, <c>false</c></returns>
    [SuppressMessage("Design", "CA1021: Avoid out parameters", Justification = "Since the set may contain null, we really don't have a choice here unless we resort to tuples.")]
    public bool RemoveLast([MaybeNullWhen(false)] out T item)
    {
        if (dict.Count == 0)
        {
            item = default;
            return false;
        }
        var node = list.Last;
        item = node.Value;
        dict.Remove(item);
        list.Remove(node);
        return true;
    }

    /// <summary>
    /// Removes all elements that match the conditions defined by the specified predicate from an <see cref="OrderedHashSet{T}"/> collection
    /// </summary>
    /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the elements to remove</param>
    /// <returns>The number of elements that were removed from the <see cref="OrderedHashSet{T}"/> collection</returns>
    /// <exception cref="ArgumentNullException"><paramref name="match"/> is null</exception>
    public int RemoveWhere(Predicate<T> match)
    {
        if (match is null)
            throw new ArgumentNullException(nameof(match));
        var count = 0;
        foreach (var item in list.ToImmutableArray())
            if (match(item))
            {
                Remove(item);
                ++count;
            }
        return count;
    }

    /// <summary>
    /// Determines whether an <see cref="OrderedHashSet{T}"/> object and the specified collection contain the same elements
    /// </summary>
    /// <param name="other">The collection to compare to the current <see cref="OrderedHashSet{T}"/> object</param>
    /// <returns><c>true</c> if the <see cref="OrderedHashSet{T}"/> object is equal to other; otherwise, <c>false</c></returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null</exception>
    public bool SetEquals(IEnumerable<T> other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));
        var otherSet = GetSet(other);
        if (list.Count != otherSet.Count)
            return false;
        foreach (var item in otherSet)
            if (!dict.ContainsKey(item))
                return false;
        return true;
    }

    /// <summary>
    /// Modifies the current <see cref="OrderedHashSet{T}"/> object to contain only elements that are present either in that object or in the specified collection, but not both
    /// </summary>
    /// <param name="other">The collection to compare to the current <see cref="OrderedHashSet{T}"/> object</param>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null</exception>
    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));
        foreach (var item in GetSet(other))
        {
            if (dict.ContainsKey(item))
                Remove(item);
            else
                Add(item);
        }
    }

    /// <summary>
    /// Sets the capacity of an <see cref="OrderedHashSet{T}"/> object to the actual number of elements it contains, rounded up to a nearby, implementation-specific value
    /// </summary>
    public void TrimExcess() =>
        dict.TrimExcess();

    /// <summary>
    /// Searches the set for a given value and returns the equal value it finds, if any
    /// </summary>
    /// <param name="equalValue">The value to search for</param>
    /// <param name="actualValue">The value from the set that the search found, or the default value of <typeparamref name="T"/> when the search yielded no match</param>
    /// <returns>A value indicating whether the search was successful</returns>
    public bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue)
    {
        if (dict.TryGetValue(equalValue, out var node))
        {
            actualValue = node.Value;
            return true;
        }
        actualValue = default;
        return false;
    }

    /// <summary>
    /// Modifies the current <see cref="OrderedHashSet{T}"/> object to contain all elements that are present in itself, the specified collection, or both
    /// </summary>
    /// <param name="other">The collection to compare to the current <see cref="OrderedHashSet{T}"/> object</param>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null</exception>
    public void UnionWith(IEnumerable<T> other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));
        foreach (var item in other)
            Add(item);
    }
}
