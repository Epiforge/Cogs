namespace Cogs.ActiveQuery;

sealed class ActiveSelectManyEnumerable<TSource, TResult> :
    SyncDisposable,
    IActiveEnumerable<TResult>
{
    ActiveSelectManyEnumerable(IEnumerable<TSource> source, Expression<Func<TSource, IEnumerable<TResult>>> selector, ActiveExpressionOptions? selectorOptions, bool parallel)
    {
        this.source = source;
        this.selector = selector;
        this.selectorOptions = selectorOptions;
        this.parallel = parallel;
        access = new();
        SynchronizationContext = (this.source as ISynchronized)?.SynchronizationContext;
        (activeSelectQuery, count, enumerableInstances) = this.Execute(() =>
        {
            lock (access)
            {
                var activeSelectQuery = this.source.ActiveSelect(this.selector, this.selectorOptions, this.parallel);
                var count = 0;
                var enumerableInstances = new Dictionary<IEnumerable<TResult>, int>();
                for (int i = 0, ii = activeSelectQuery.Count; i < ii; ++i)
                {
                    var enumerable = activeSelectQuery[i];
                    if (enumerable is not null)
                    {
                        count += enumerable.Count();
                        if (enumerableInstances.TryGetValue(enumerable, out var instancesOfEnumerable))
                            enumerableInstances[enumerable] = instancesOfEnumerable + 1;
                        else
                        {
                            enumerableInstances.Add(enumerable, 1);
                            if (enumerable is INotifyCollectionChanged collectionChangedNotifier)
                                collectionChangedNotifier.CollectionChanged += EnumerableChanged;
                        }
                    }
                }
                activeSelectQuery.CollectionChanged += ActiveSelectQueryCollectionChanged;
                activeSelectQuery.ElementFaultChanged += ActiveSelectQueryElementFaultChanged;
                activeSelectQuery.ElementFaultChanging += ActiveSelectQueryElementFaultChanging;
                return (activeSelectQuery, count, enumerableInstances);
            }
        });
    }

    readonly object access;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "This field will be disposed by the base class, the analyzer just doesn't see that.")]
    IActiveEnumerable<IEnumerable<TResult>?>? activeSelectQuery;
    int count;
    Dictionary<IEnumerable<TResult>, int>? enumerableInstances;
    readonly bool parallel;
    readonly Expression<Func<TSource, IEnumerable<TResult>>> selector;
    readonly ActiveExpressionOptions? selectorOptions;
    readonly IEnumerable<TSource> source;

    public TResult this[int index] => throw new NotImplementedException();

    object? IList.this[int index]
    {
        get => this[index];
        set => throw new NotSupportedException();
    }

    public int Count
    {
        get => count;
        private set => SetBackedProperty(ref count, in value);
    }

    bool IList.IsFixedSize { get; } = false;

    bool IList.IsReadOnly { get; } = true;

    bool ICollection.IsSynchronized { get; } = false;

    public SynchronizationContext? SynchronizationContext { get; private set; }

    object? ICollection.SyncRoot { get; } = null;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanged;
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanging;

    [SuppressMessage("Code Analysis", "CA1502: Avoid excessive complexity")]
    void ActiveSelectQueryCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        lock (access!)
        {
            NotifyCollectionChangedEventArgs? eventArgs = null;
            var newCount = 0;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Remove:
                    {
                        var reducedNewStartingIndex = 0;
                        var newItems = new List<TResult>();
                        if (e.NewItems is not null && e.NewStartingIndex >= 0)
                        {
                            reducedNewStartingIndex = GetReducedStartingIndexUnderLock(e.NewStartingIndex);
                            for (int i = 0, ii = e.NewItems.Count; i < ii; ++i)
                                if (e.NewItems[i] is IEnumerable<TResult> newEnumerable)
                                {
                                    newItems.AddRange(newEnumerable);
                                    if (enumerableInstances!.TryGetValue(newEnumerable, out var instancesOfEnumerable))
                                        enumerableInstances[newEnumerable] = instancesOfEnumerable + 1;
                                    else
                                    {
                                        enumerableInstances.Add(newEnumerable, 1);
                                        if (newEnumerable is INotifyCollectionChanged collectionChangedNotifier)
                                            collectionChangedNotifier.CollectionChanged += EnumerableChanged;
                                    }
                                }
                        }
                        var reducedOldStartingIndex = 0;
                        var oldItems = new List<TResult>();
                        if (e.OldItems is not null && e.OldStartingIndex >= 0)
                        {
                            reducedOldStartingIndex = GetReducedStartingIndexUnderLock(e.OldStartingIndex);
                            if (e.OldStartingIndex > e.NewStartingIndex)
                                reducedOldStartingIndex += newItems.Count;
                            for (int i = 0, ii = e.OldItems.Count; i < ii; ++i)
                                if (e.OldItems[i] is IEnumerable<TResult> oldEnumerable)
                                {
                                    oldItems.AddRange(oldEnumerable);
                                    var instancesOfEnumerable = enumerableInstances![oldEnumerable];
                                    if (instancesOfEnumerable == 1)
                                    {
                                        enumerableInstances.Remove(oldEnumerable);
                                        if (oldEnumerable is INotifyCollectionChanged collectionChangedNotifier)
                                            collectionChangedNotifier.CollectionChanged -= EnumerableChanged;
                                    }
                                    else
                                        enumerableInstances[oldEnumerable] = instancesOfEnumerable - 1;
                                }
                        }
                        if (oldItems.Count > 0)
                        {
                            if (newItems.Count > 0)
                                eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems.ToImmutableArray(), oldItems.ToImmutableArray(), reducedOldStartingIndex);
                            else
                                eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems.ToImmutableArray(), reducedOldStartingIndex);
                        }
                        else if (newItems.Count > 0)
                            eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems.ToImmutableArray(), reducedNewStartingIndex);
                        newCount = count - oldItems.Count + newItems.Count;
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldItems.Count > 0 && e.OldStartingIndex != e.NewStartingIndex)
                    {
                        var reducedNewStartingIndex = GetReducedStartingIndexUnderLock(e.NewStartingIndex);
                        var reducedOldStartingIndex = GetReducedStartingIndexUnderLock(e.OldStartingIndex);
                        var movedItems = e.OldItems.Cast<IEnumerable<TResult>>().SelectMany(enumerable => enumerable ?? Enumerable.Empty<TResult>()).ToImmutableArray();
                        if (e.OldStartingIndex > e.NewStartingIndex)
                            reducedOldStartingIndex += movedItems.Length;
                        eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, movedItems, reducedNewStartingIndex, reducedOldStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var enumerable in enumerableInstances!.Keys)
                    {
                        if (enumerable is INotifyCollectionChanged collectionChangedNotifier)
                            collectionChangedNotifier.CollectionChanged -= EnumerableChanged;
                    }
                    activeSelectQuery!.CollectionChanged -= ActiveSelectQueryCollectionChanged;
                    activeSelectQuery.ElementFaultChanged -= ActiveSelectQueryElementFaultChanged;
                    activeSelectQuery.ElementFaultChanging -= ActiveSelectQueryElementFaultChanging;
                    activeSelectQuery.Dispose();
                    activeSelectQuery = source.ActiveSelect(selector, selectorOptions, parallel);
                    enumerableInstances = new Dictionary<IEnumerable<TResult>, int>();
                    for (int i = 0, ii = activeSelectQuery.Count; i < ii; ++i)
                    {
                        var enumerable = activeSelectQuery[i];
                        if (enumerable is not null)
                        {
                            newCount += enumerable.Count();
                            if (enumerableInstances.TryGetValue(enumerable, out var instancesOfEnumerable))
                                enumerableInstances[enumerable] = instancesOfEnumerable + 1;
                            else
                            {
                                enumerableInstances.Add(enumerable, 1);
                                if (enumerable is INotifyCollectionChanged collectionChangedNotifier)
                                    collectionChangedNotifier.CollectionChanged += EnumerableChanged;
                            }
                        }
                    }
                    activeSelectQuery.CollectionChanged += ActiveSelectQueryCollectionChanged;
                    activeSelectQuery.ElementFaultChanged += ActiveSelectQueryElementFaultChanged;
                    activeSelectQuery.ElementFaultChanging += ActiveSelectQueryElementFaultChanging;
                    eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                    break;
                default:
                    throw new NotSupportedException($"Unknown collection changed action {e.Action}");
            }
            if (eventArgs is not null)
            {
                if (eventArgs.Action != NotifyCollectionChangedAction.Move)
                    Count = newCount;
                CollectionChanged?.Invoke(this, eventArgs);
            }
        }
    }

    void ActiveSelectQueryElementFaultChanged(object sender, ElementFaultChangeEventArgs e) =>
        ElementFaultChanged?.Invoke(this, e);

    void ActiveSelectQueryElementFaultChanging(object sender, ElementFaultChangeEventArgs e) =>
        ElementFaultChanging?.Invoke(this, e);

    int IList.Add(object value) =>
        throw new NotSupportedException();

    void IList.Clear() =>
        throw new NotSupportedException();

    bool IList.Contains(object? value) =>
        this.Execute(() =>
        {
            lock (access!)
            {
                if (activeSelectQuery is not null && value is TResult result)
                {
                    var comparer = EqualityComparer<TResult>.Default;
                    for (int i = 0, ii = activeSelectQuery.Count; i < ii; ++i)
                        if (activeSelectQuery[i] is { } activeSelectQueryEnumerable)
                            foreach (var activeSelectQueryEnumerableResult in activeSelectQueryEnumerable)
                                if (comparer.Equals(activeSelectQueryEnumerableResult, result))
                                    return true;
                }
                return false;
            }
        });

    void ICollection.CopyTo(Array array, int index) =>
        this.Execute(() =>
        {
            lock (access!)
            {
                if (activeSelectQuery is null)
                    return;
                var arrayIndex = -1;
                for (int i = 0, ii = activeSelectQuery.Count; i < ii; ++i)
                    if (activeSelectQuery[i] is { } activeSelectQueryEnumerable)
                        foreach (var activeSelectQueryEnumerableResult in activeSelectQueryEnumerable)
                        {
                            ++arrayIndex;
                            if (arrayIndex >= array.Length)
                                break;
                            array.SetValue(activeSelectQueryEnumerableResult, arrayIndex);
                        }
            }
        });

    protected override bool Dispose(bool disposing)
    {
        if (disposing)
            this.Execute(() =>
            {
                lock (access!)
                {
                    foreach (var enumerable in enumerableInstances!.Keys)
                    {
                        if (enumerable is INotifyCollectionChanged collectionChangedNotifier)
                            collectionChangedNotifier.CollectionChanged -= EnumerableChanged;
                    }
                    activeSelectQuery!.CollectionChanged -= ActiveSelectQueryCollectionChanged;
                    activeSelectQuery.ElementFaultChanged -= ActiveSelectQueryElementFaultChanged;
                    activeSelectQuery.ElementFaultChanging -= ActiveSelectQueryElementFaultChanging;
                    activeSelectQuery.Dispose();
                }
            });
        return true;
    }

    void EnumerableChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is IEnumerable<TResult> enumerable)
            lock (access!)
            {
                if (enumerableInstances!.TryGetValue(enumerable, out var instances))
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                        CollectionChanged?.Invoke(this, e);
                    else
                    {
                        var newCount = count + ((e.NewItems?.Count ?? 0) - (e.OldItems?.Count ?? 0) * instances);
                        var reducedCount = enumerable.Count();
                        var reducedIndex = 0;
                        for (int i = 0, ii = activeSelectQuery!.Count; i < ii && instances > 0; ++i)
                        {
                            var activeSelectQueryEnumerable = activeSelectQuery[i];
                            if (activeSelectQueryEnumerable == enumerable)
                            {
                                --instances;
                                CollectionChanged?.Invoke(this, e.Action switch
                                {
                                    NotifyCollectionChangedAction.Add => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItems, reducedIndex + e.NewStartingIndex),
                                    NotifyCollectionChangedAction.Move => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, e.NewItems ?? e.OldItems, reducedIndex + e.NewStartingIndex, reducedIndex + e.OldStartingIndex),
                                    NotifyCollectionChangedAction.Remove => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, e.OldItems, reducedIndex + e.OldStartingIndex),
                                    NotifyCollectionChangedAction.Replace => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, e.NewItems, e.OldItems, reducedIndex + e.OldStartingIndex),
                                    _ => throw new NotSupportedException()
                                });
                                reducedIndex += reducedCount;
                            }
                            else
                                reducedIndex += activeSelectQueryEnumerable.Count();
                        }
                        Count = newCount;
                    }
                }
            }
    }

    public IReadOnlyList<(object? element, Exception? fault)> GetElementFaults() =>
        this.Execute(() =>
        {
            lock (access!)
                return activeSelectQuery!.GetElementFaults();
        });

    public IEnumerator<TResult> GetEnumerator() =>
        this.Execute(() => GetEnumeratorInContext());

    IEnumerator <TResult> GetEnumeratorInContext()
    {
        lock (access!)
            foreach (var activeSelectQueryEnumerable in activeSelectQuery!)
                if (activeSelectQueryEnumerable is not null)
                    foreach (var result in activeSelectQueryEnumerable)
                        yield return result;
    }

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    int IList.IndexOf(object value) =>
        this.Execute(() =>
        {
            lock (access!)
            {
                if (activeSelectQuery is not null && value is TResult result)
                {
                    var comparer = EqualityComparer<TResult>.Default;
                    var index = -1;
                    for (int i = 0, ii = activeSelectQuery.Count; i < ii; ++i)
                        if (activeSelectQuery[i] is { } activeSelectQueryEnumerable)
                            foreach (var activeSelectQueryEnumerableResult in activeSelectQueryEnumerable)
                            {
                                ++index;
                                if (comparer.Equals(activeSelectQueryEnumerableResult, result))
                                    return index;
                            }
                }
                return -1;
            }
        });

    void IList.Insert(int index, object value) =>
        throw new NotSupportedException();

    int GetReducedStartingIndexUnderLock(int mapIndex)
    {
        if (mapIndex < 0 || mapIndex >= activeSelectQuery!.Count)
            return -1;
        var reducedIndex = 0;
        for (int i = 0, ii = activeSelectQuery.Count; i < ii; ++i)
        {
            if (i == mapIndex)
                return reducedIndex;
            reducedIndex += activeSelectQuery[i]?.Count() ?? 0;
        }
        return -1;
    }

    void IList.Remove(object value) =>
        throw new NotSupportedException();

    void IList.RemoveAt(int index) =>
        throw new NotSupportedException();

    internal static IActiveEnumerable<TResult> Get(IEnumerable<TSource> source, Expression<Func<TSource, IEnumerable<TResult>>> selector, ActiveExpressionOptions? selectorOptions, bool parallel)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (selector is null)
            throw new ArgumentNullException(nameof(selector));
        return new ActiveSelectManyEnumerable<TSource, TResult>(source, selector, selectorOptions, parallel);
    }
}
