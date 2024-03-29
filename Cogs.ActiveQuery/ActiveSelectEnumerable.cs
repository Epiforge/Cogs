namespace Cogs.ActiveQuery;

sealed class ActiveSelectEnumerable<TResult> :
    SyncDisposable,
    IActiveEnumerable<TResult>,
    IObserveActiveExpressions<TResult>
{
    ActiveSelectEnumerable(IEnumerable source, Expression<Func<object?, TResult>> selector, ActiveExpressionOptions? selectorOptions, bool parallel)
    {
        access = new();
        this.source = source;
        this.selector = selector;
        this.selectorOptions = selectorOptions;
        this.parallel = parallel;
        SynchronizationContext = (this.source as ISynchronized)?.SynchronizationContext;
        (activeExpressions, activeExpressionCounts, count) = this.Execute(() =>
        {
            lock (access)
            {
                List<IActiveExpression<object?, TResult>> activeExpressions;
                if (this.parallel)
                    activeExpressions = this.source.Cast<object?>().DataflowSelectAsync(element => (IActiveExpression<object?, TResult>)ActiveExpression.Create(this.selector, element, this.selectorOptions)).Result.ToList();
                else if (this.source is IList sourceList)
                {
                    activeExpressions = new List<IActiveExpression<object?, TResult>>(sourceList.Count);
                    for (int i = 0, ii = sourceList.Count; i < ii; ++i)
                        activeExpressions.Add(ActiveExpression.Create(this.selector, sourceList[i], this.selectorOptions));
                }
                else
                {
                    activeExpressions = new List<IActiveExpression<object?, TResult>>();
                    foreach (var element in this.source)
                        activeExpressions.Add(ActiveExpression.Create(this.selector, element, this.selectorOptions));
                }
                var activeExpressionCounts = new Dictionary<IActiveExpression<object?, TResult>, int>();
                for (int i = 0, ii = activeExpressions.Count; i < ii; ++i)
                {
                    var activeExpression = activeExpressions[i];
                    if (activeExpressionCounts!.TryGetValue(activeExpression, out var existingCount))
                        activeExpressionCounts[activeExpression] = existingCount + 1;
                    else
                    {
                        activeExpressionCounts.Add(activeExpression, 1);
                        activeExpression.AddActiveExpressionOserver(this);
                    }
                }
                if (this.source is INotifyCollectionChanged collectionChangeNotifier)
                    collectionChangeNotifier.CollectionChanged += SourceChanged;
                return (activeExpressions, activeExpressionCounts, activeExpressions.Count);
            }
        });
    }

    readonly object access;
    Dictionary<IActiveExpression<object?, TResult>, int>? activeExpressionCounts;
    List<IActiveExpression<object?, TResult>>? activeExpressions;
    int count;
    readonly bool parallel;
    readonly Expression<Func<object?, TResult>> selector;
    readonly ActiveExpressionOptions? selectorOptions;
    readonly IEnumerable source;

    public TResult this[int index] =>
        this.Execute(() =>
        {
            lock (access)
                return activeExpressions![index].Value!;
        });

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
#pragma warning disable CS0067
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanging;
#pragma warning restore CS0067

    void IObserveActiveExpressions<TResult>.ActiveExpressionChanged(IObservableActiveExpression<TResult> activeExpression, TResult? oldValue, TResult? newValue, Exception? oldFault, Exception? newFault)
    {
        if (activeExpression is IActiveExpression<object?, TResult> typedActiveExpression)
            this.Execute(() =>
            {
                lock (access)
                {
                    if (!ReferenceEquals(oldFault, newFault))
                        ElementFaultChanged?.Invoke(this, new ElementFaultChangeEventArgs(typedActiveExpression.Arg, newFault, activeExpressionCounts![typedActiveExpression]));
                    if (!EqualityComparer<TResult>.Default.Equals(oldValue!, newValue!))
                        for (int i = 0, ii = activeExpressions!.Count; i < ii; ++i)
                        {
                            var iActiveExpression = activeExpressions[i];
                            if (iActiveExpression == typedActiveExpression)
                                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newValue, oldValue, i));
                        }
                }
            });
    }

    int IList.Add(object value) =>
        throw new NotSupportedException();

    void IList.Clear() =>
        throw new NotSupportedException();

    bool IList.Contains(object? value) =>
        this.Execute(() =>
        {
            if (activeExpressions is not null && value is TResult result)
            {
                var comparer = EqualityComparer<TResult>.Default;
                for (int i = 0, ii = activeExpressions.Count; i < ii; ++i)
                    if (comparer.Equals(activeExpressions[i].Value!, result))
                        return true;
            }
            return false;
        });

    void ICollection.CopyTo(Array array, int index) =>
        this.Execute(() =>
        {
            if (activeExpressions is null)
                return;
            for (int i = 0, ii = activeExpressions.Count; i < ii; ++i)
            {
                if (index + i >= array.Length)
                    break;
                array.SetValue(activeExpressions[i].Value, index + i);
            }
        });

    protected override bool Dispose(bool disposing)
    {
        if (disposing)
            this.Execute(() =>
            {
                lock (access)
                {
                    foreach (var activeExpression in activeExpressionCounts!.Keys)
                    {
                        activeExpression.RemoveActiveExpressionObserver(this);
                        for (int i = 0, ii = activeExpressionCounts[activeExpression]; i < ii; ++i)
                            activeExpression.Dispose();
                    }
                    if (source is INotifyCollectionChanged collectionChangeNotifier)
                        collectionChangeNotifier.CollectionChanged -= SourceChanged;
                }
            });
        return true;
    }

    public IReadOnlyList<(object? element, Exception? fault)> GetElementFaults() =>
        this.Execute(() =>
        {
            lock (access)
            {
                var result = new List<(object? element, Exception? fault)>();
                foreach (var activeExpression in activeExpressionCounts!.Keys)
                    if (activeExpression.Fault is { } fault)
                        result.Add((activeExpression.Arg, fault));
                return result.AsReadOnly();
            }
        });

    public IEnumerator<TResult> GetEnumerator() =>
        this.Execute(() => GetEnumeratorInContext());

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    IEnumerator<TResult> GetEnumeratorInContext()
    {
        lock (access)
            foreach (var activeExpression in activeExpressions!)
                yield return activeExpression.Value!;
    }

    int IList.IndexOf(object value) =>
        this.Execute(() =>
        {
            if (activeExpressions is not null && value is TResult result)
            {
                var comparer = EqualityComparer<TResult>.Default;
                for (int i = 0, ii = activeExpressions.Count; i < ii; ++i)
                    if (comparer.Equals(activeExpressions[i].Value!, result))
                        return i;
            }
            return -1;
        });

    void IList.Insert(int index, object value) =>
        throw new NotSupportedException();

    void IList.Remove(object value) =>
        throw new NotSupportedException();

    void IList.RemoveAt(int index) =>
        throw new NotSupportedException();

    (int newCount, NotifyCollectionChangedEventArgs eventArgs) ResetUnderLock()
    {
        foreach (var activeExpression in activeExpressionCounts!.Keys)
        {
            activeExpression.RemoveActiveExpressionObserver(this);
            for (int i = 0, ii = activeExpressionCounts[activeExpression]; i < ii; ++i)
                activeExpression.Dispose();
        }

        if (parallel)
            activeExpressions = source.Cast<object?>().DataflowSelectAsync(element => (IActiveExpression<object?, TResult>)ActiveExpression.Create(selector, element, selectorOptions)).Result.ToList();
        else if (source is IList sourceList)
        {
            activeExpressions = new List<IActiveExpression<object?, TResult>>(sourceList.Count);
            for (int i = 0, ii = sourceList.Count; i < ii; ++i)
                activeExpressions.Add(ActiveExpression.Create(selector, sourceList[i], selectorOptions));
        }
        else
        {
            activeExpressions = new List<IActiveExpression<object?, TResult>>();
            foreach (var element in source)
                activeExpressions.Add(ActiveExpression.Create(selector, element, selectorOptions));
        }
        activeExpressionCounts = new Dictionary<IActiveExpression<object?, TResult>, int>();
        for (int i = 0, ii = activeExpressions.Count; i < ii; ++i)
        {
            var activeExpression = activeExpressions[i];
            if (activeExpressionCounts!.TryGetValue(activeExpression, out var existingCount))
                activeExpressionCounts[activeExpression] = existingCount + 1;
            else
            {
                activeExpressionCounts.Add(activeExpression, 1);
                activeExpression.AddActiveExpressionOserver(this);
            }
        }
        return (activeExpressions.Count, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity", Justification = @"Splitting this up into more methods is ¯\_(ツ)_/¯")]
    void SourceChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        lock (access)
        {
            NotifyCollectionChangedEventArgs? eventArgs = null;
            var newCount = 0;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Remove:
                    var oldItems = new List<TResult>();
                    if (e.OldItems is not null && e.OldStartingIndex >= 0)
                    {
                        List<IActiveExpression<object?, TResult>>? removedActiveExpressions = null;
                        try
                        {
                            removedActiveExpressions = activeExpressions!.GetRange(e.OldStartingIndex, e.OldItems.Count);
                        }
                        catch (ArgumentException)
                        {
                            (newCount, eventArgs) = ResetUnderLock();
                            break;
                        }
                        activeExpressions.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                        for (int i = 0, ii = removedActiveExpressions.Count; i < ii; ++i)
                        {
                            var removedActiveExpression = removedActiveExpressions[i];
                            oldItems.Add(removedActiveExpression.Value!);
                            var existingCount = activeExpressionCounts![removedActiveExpression];
                            if (existingCount == 1)
                            {
                                removedActiveExpression.RemoveActiveExpressionObserver(this);
                                activeExpressionCounts.Remove(removedActiveExpression);
                            }
                            else
                                activeExpressionCounts[removedActiveExpression] = existingCount - 1;
                            removedActiveExpression.Dispose();
                        }
                    }
                    var newItems = new List<TResult>();
                    if (e.NewItems is not null && e.NewStartingIndex >= 0)
                    {
                        var addedActiveExpressions = new List<IActiveExpression<object?, TResult>>();
                        for (int i = 0, ii = e.NewItems.Count; i < ii; ++i)
                        {
                            var element = e.NewItems[i];
                            var addedActiveExpression = ActiveExpression.Create(selector, element, selectorOptions);
                            newItems.Add(addedActiveExpression.Value!);
                            addedActiveExpressions.Add(addedActiveExpression);
                            if (activeExpressionCounts!.TryGetValue(addedActiveExpression, out var existingCount))
                                activeExpressionCounts[addedActiveExpression] = existingCount + 1;
                            else
                            {
                                activeExpressionCounts.Add(addedActiveExpression, 1);
                                addedActiveExpression.AddActiveExpressionOserver(this);
                            }
                        }
                        activeExpressions!.InsertRange(e.NewStartingIndex, addedActiveExpressions);
                    }
                    if (oldItems.Count > 0)
                    {
                        if (newItems.Count > 0)
                            eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems.ToImmutableArray(), oldItems.ToImmutableArray(), e.OldStartingIndex);
                        else
                            eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems.ToImmutableArray(), e.OldStartingIndex);
                    }
                    else if (newItems.Count > 0)
                        eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems.ToImmutableArray(), e.NewStartingIndex);
                    newCount = count - oldItems.Count + newItems.Count;
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldItems.Count > 0 && e.OldStartingIndex != e.NewStartingIndex)
                    {
                        List<IActiveExpression<object?, TResult>>? movedActiveExpressions = null;
                        try
                        {
                            movedActiveExpressions = activeExpressions!.GetRange(e.OldStartingIndex, e.OldItems.Count);
                        }
                        catch (ArgumentException)
                        {
                            (newCount, eventArgs) = ResetUnderLock();
                            break;
                        }
                        activeExpressions.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                        activeExpressions.InsertRange(e.NewStartingIndex, movedActiveExpressions);
                        eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, movedActiveExpressions.Select(ae => ae.Value).ToImmutableArray(), e.NewStartingIndex, e.OldStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    (newCount, eventArgs) = ResetUnderLock();
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

    internal static IActiveEnumerable<TResult> Get(IEnumerable source, Expression<Func<object?, TResult>> selector, ActiveExpressionOptions? selectorOptions, bool parallel)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (selector is null)
            throw new ArgumentNullException(nameof(selector));
        return new ActiveSelectEnumerable<TResult>(source, selector, selectorOptions, parallel);
    }

    class EqualityComparer :
        IEqualityComparer<(IEnumerable source, Expression<Func<object?, TResult>> selector, ActiveExpressionOptions? selectorOptions, bool parallel)>
    {
        public bool Equals((IEnumerable source, Expression<Func<object?, TResult>> selector, ActiveExpressionOptions? selectorOptions, bool parallel) x, (IEnumerable source, Expression<Func<object?, TResult>> selector, ActiveExpressionOptions? selectorOptions, bool parallel) y) =>
            ReferenceEquals(x.source, y.source) && ExpressionEqualityComparer.Default.Equals(x.selector, y.selector) && (x.selectorOptions is null && y.selectorOptions is null || x.selectorOptions is not null && y.selectorOptions is not null && x.selectorOptions.Equals(y.selectorOptions));

        public int GetHashCode((IEnumerable source, Expression<Func<object?, TResult>> selector, ActiveExpressionOptions? selectorOptions, bool parallel) obj) =>
            HashCode.Combine(obj.source, ExpressionEqualityComparer.Default.GetHashCode(obj.selector), obj.selectorOptions);
    }
}
sealed class ActiveSelectEnumerable<TSource, TResult> :
    SyncDisposable,
    IActiveEnumerable<TResult>,
    IObserveActiveExpressions<TResult>
{
    ActiveSelectEnumerable(IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions, bool parallel)
    {
        access = new();
        this.source = source;
        this.selector = selector;
        this.selectorOptions = selectorOptions;
        this.parallel = parallel;
        SynchronizationContext = (this.source as ISynchronized)?.SynchronizationContext;
        this.Execute(() =>
        {
            lock (access)
            {
                ConcurrentQueue<NotifyCollectionChangedEventArgs>? pendingChanges = null;
                void enqueuePendingChange(object? sender, NotifyCollectionChangedEventArgs e) =>
                    pendingChanges?.Enqueue(e);

                ImmutableArray<TSource> sourceCopy;
                while (true)
                {
                    try
                    {
                        sourceCopy = this.source.ToImmutableArray();
                        break;
                    }
                    catch (InvalidOperationException)
                    {
                        continue;
                    }
                }

                if (this.source is INotifyCollectionChanged collectionChangeNotifier)
                    collectionChangeNotifier.CollectionChanged += enqueuePendingChange;

                if (this.parallel)
                    activeExpressions = sourceCopy.DataflowSelectAsync(element => (IActiveExpression<TSource, TResult>)ActiveExpression.Create(this.selector, element, this.selectorOptions)).Result.ToList();
                else
                {
                    activeExpressions = new List<IActiveExpression<TSource, TResult>>(sourceCopy.Length);
                    for (int i = 0, ii = sourceCopy.Length; i < ii; ++i)
                        activeExpressions.Add(ActiveExpression.Create(this.selector, sourceCopy[i], this.selectorOptions));
                }

                activeExpressionCounts = new Dictionary<IActiveExpression<TSource, TResult>, int>();
                for (int i = 0, ii = activeExpressions.Count; i < ii; ++i)
                {
                    var activeExpression = activeExpressions[i];
                    if (activeExpressionCounts!.TryGetValue(activeExpression, out var existingCount))
                        activeExpressionCounts[activeExpression] = existingCount + 1;
                    else
                    {
                        activeExpressionCounts.Add(activeExpression, 1);
                        activeExpression.AddActiveExpressionOserver(this);
                    }
                }
                count = activeExpressions.Count;

                while (pendingChanges?.TryDequeue(out var pendingChange) ?? false)
                    SourceChanged(this.source, pendingChange);

                if (this.source is INotifyCollectionChanged collectionChangeNotifier2)
                {
                    collectionChangeNotifier2.CollectionChanged -= enqueuePendingChange;
                    collectionChangeNotifier2.CollectionChanged += SourceChanged;
                }
            }
        });
    }

    readonly object access;
    Dictionary<IActiveExpression<TSource, TResult>, int>? activeExpressionCounts;
    List<IActiveExpression<TSource, TResult>>? activeExpressions;
    int count;
    readonly bool parallel;
    readonly Expression<Func<TSource, TResult>> selector;
    readonly ActiveExpressionOptions? selectorOptions;
    readonly IEnumerable<TSource> source;

    public TResult this[int index] =>
        this.Execute(() =>
        {
            lock (access)
                return activeExpressions![index].Value!;
        });

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
#pragma warning disable CS0067
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanging;
#pragma warning restore CS0067

    void IObserveActiveExpressions<TResult>.ActiveExpressionChanged(IObservableActiveExpression<TResult> activeExpression, TResult? oldValue, TResult? newValue, Exception? oldFault, Exception? newFault)
    {
        if (activeExpression is IActiveExpression<TSource, TResult> typedActiveExpression)
            this.Execute(() =>
            {
                lock (access)
                {
                    if (!ReferenceEquals(oldFault, newFault))
                        ElementFaultChanged?.Invoke(this, new ElementFaultChangeEventArgs(typedActiveExpression.Arg, newFault, activeExpressionCounts![typedActiveExpression]));
                    if (!EqualityComparer<TResult>.Default.Equals(oldValue!, newValue!))
                        for (int i = 0, ii = activeExpressions!.Count; i < ii; ++i)
                        {
                            var iActiveExpression = activeExpressions[i];
                            if (iActiveExpression == typedActiveExpression)
                                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newValue, oldValue, i));
                        }
                }
            });
    }

    int IList.Add(object value) =>
        throw new NotSupportedException();

    void IList.Clear() =>
        throw new NotSupportedException();

    bool IList.Contains(object? value) =>
        this.Execute(() =>
        {
            lock (access)
            {
                if (activeExpressions is not null && value is TResult result)
                {
                    var comparer = EqualityComparer<TResult>.Default;
                    for (int i = 0, ii = activeExpressions.Count; i < ii; ++i)
                        if (comparer.Equals(activeExpressions[i].Value!, result))
                            return true;
                }
                return false;
            }
        });

    void ICollection.CopyTo(Array array, int index) =>
        this.Execute(() =>
        {
            lock (access)
            {
                if (activeExpressions is null)
                    return;
                for (int i = 0, ii = activeExpressions.Count; i < ii; ++i)
                {
                    if (index + i >= array.Length)
                        break;
                    array.SetValue(activeExpressions[i].Value, index + i);
                }
            }
        });

    protected override bool Dispose(bool disposing)
    {
        if (disposing)
            this.Execute(() =>
            {
                lock (access)
                {
                    foreach (var activeExpression in activeExpressionCounts!.Keys)
                    {
                        activeExpression.RemoveActiveExpressionObserver(this);
                        for (int i = 0, ii = activeExpressionCounts[activeExpression]; i < ii; ++i)
                            activeExpression.Dispose();
                    }
                    if (source is INotifyCollectionChanged collectionChangeNotifier)
                        collectionChangeNotifier.CollectionChanged -= SourceChanged;
                }
            });
        return true;
    }

    public IReadOnlyList<(object? element, Exception? fault)> GetElementFaults() =>
        this.Execute(() =>
        {
            lock (access)
            {
                var result = new List<(object? element, Exception? fault)>();
                foreach (var activeExpression in activeExpressionCounts!.Keys)
                    if (activeExpression.Fault is { } fault)
                        result.Add((activeExpression.Arg, fault));
                return result.AsReadOnly();
            }
        });

    public IEnumerator<TResult> GetEnumerator() =>
        this.Execute(() => GetEnumeratorInContext());

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    IEnumerator<TResult> GetEnumeratorInContext()
    {
        lock (access)
            foreach (var activeExpression in activeExpressions!)
                yield return activeExpression.Value!;
    }

    int IList.IndexOf(object value) =>
        this.Execute(() =>
        {
            lock (access)
            {
                if (activeExpressions is not null && value is TResult result)
                {
                    var comparer = EqualityComparer<TResult>.Default;
                    for (int i = 0, ii = activeExpressions.Count; i < ii; ++i)
                        if (comparer.Equals(activeExpressions[i].Value!, result))
                            return i;
                }
                return -1;
            }
        });

    void IList.Insert(int index, object value) =>
        throw new NotSupportedException();

    void IList.Remove(object value) =>
        throw new NotSupportedException();

    void IList.RemoveAt(int index) =>
        throw new NotSupportedException();

    (int newCount, NotifyCollectionChangedEventArgs eventArgs) ResetUnderLock()
    {
        foreach (var activeExpression in activeExpressionCounts!.Keys)
        {
            activeExpression.RemoveActiveExpressionObserver(this);
            for (int i = 0, ii = activeExpressionCounts[activeExpression]; i < ii; ++i)
                activeExpression.Dispose();
        }

        if (parallel)
            activeExpressions = source.DataflowSelectAsync(element => (IActiveExpression<TSource, TResult>)ActiveExpression.Create(selector, element, selectorOptions)).Result.ToList();
        else if (source is IList<TSource> sourceList)
        {
            activeExpressions = new List<IActiveExpression<TSource, TResult>>(sourceList.Count);
            for (int i = 0, ii = sourceList.Count; i < ii; ++i)
                activeExpressions.Add(ActiveExpression.Create(selector, sourceList[i], selectorOptions));
        }
        else
        {
            activeExpressions = new List<IActiveExpression<TSource, TResult>>();
            foreach (var element in source)
                activeExpressions.Add(ActiveExpression.Create(selector, element, selectorOptions));
        }
        activeExpressionCounts = new Dictionary<IActiveExpression<TSource, TResult>, int>();
        for (int i = 0, ii = activeExpressions.Count; i < ii; ++i)
        {
            var activeExpression = activeExpressions[i];
            if (activeExpressionCounts!.TryGetValue(activeExpression, out var existingCount))
                activeExpressionCounts[activeExpression] = existingCount + 1;
            else
            {
                activeExpressionCounts.Add(activeExpression, 1);
                activeExpression.AddActiveExpressionOserver(this);
            }
        }
        return (activeExpressions.Count, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity", Justification = @"Splitting this up into more methods is ¯\_(ツ)_/¯")]
    void SourceChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        lock (access)
        {
            NotifyCollectionChangedEventArgs? eventArgs = null;
            var newCount = 0;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Remove:
                    var oldItems = new List<TResult>();
                    if (e.OldItems is not null && e.OldStartingIndex >= 0)
                    {
                        List<IActiveExpression<TSource, TResult>>? removedActiveExpressions = null;
                        try
                        {
                            removedActiveExpressions = activeExpressions!.GetRange(e.OldStartingIndex, e.OldItems.Count);
                        }
                        catch (ArgumentException)
                        {
                            (newCount, eventArgs) = ResetUnderLock();
                            break;
                        }
                        activeExpressions.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                        for (int i = 0, ii = removedActiveExpressions.Count; i < ii; ++i)
                        {
                            var removedActiveExpression = removedActiveExpressions[i];
                            oldItems.Add(removedActiveExpression.Value!);
                            var existingCount = activeExpressionCounts![removedActiveExpression];
                            if (existingCount == 1)
                            {
                                removedActiveExpression.RemoveActiveExpressionObserver(this);
                                activeExpressionCounts.Remove(removedActiveExpression);
                            }
                            else
                                activeExpressionCounts[removedActiveExpression] = existingCount - 1;
                            removedActiveExpression.Dispose();
                        }
                    }
                    var newItems = new List<TResult>();
                    if (e.NewItems is not null && e.NewStartingIndex >= 0)
                    {
                        var addedActiveExpressions = new List<IActiveExpression<TSource, TResult>>();
                        for (int i = 0, ii = e.NewItems.Count; i < ii; ++i)
                        {
                            var element = (TSource)e.NewItems[i];
                            var addedActiveExpression = ActiveExpression.Create(selector, element, selectorOptions);
                            newItems.Add(addedActiveExpression.Value!);
                            addedActiveExpressions.Add(addedActiveExpression);
                            if (activeExpressionCounts!.TryGetValue(addedActiveExpression, out var existingCount))
                                activeExpressionCounts[addedActiveExpression] = existingCount + 1;
                            else
                            {
                                activeExpressionCounts.Add(addedActiveExpression, 1);
                                addedActiveExpression.AddActiveExpressionOserver(this);
                            }
                        }
                        activeExpressions!.InsertRange(e.NewStartingIndex, addedActiveExpressions);
                    }
                    if (oldItems.Count > 0)
                    {
                        if (newItems.Count > 0)
                            eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems.ToImmutableArray(), oldItems.ToImmutableArray(), e.OldStartingIndex);
                        else
                            eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems.ToImmutableArray(), e.OldStartingIndex);
                    }
                    else if (newItems.Count > 0)
                        eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems.ToImmutableArray(), e.NewStartingIndex);
                    newCount = count - oldItems.Count + newItems.Count;
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldItems.Count > 0 && e.OldStartingIndex != e.NewStartingIndex)
                    {
                        List<IActiveExpression<TSource, TResult>>? movedActiveExpressions = null;
                        try
                        {
                            movedActiveExpressions = activeExpressions!.GetRange(e.OldStartingIndex, e.OldItems.Count);
                        }
                        catch (ArgumentException)
                        {
                            (newCount, eventArgs) = ResetUnderLock();
                            break;
                        }
                        activeExpressions.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                        activeExpressions.InsertRange(e.NewStartingIndex, movedActiveExpressions);
                        eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, movedActiveExpressions.Select(ae => ae.Value).ToImmutableArray(), e.NewStartingIndex, e.OldStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    (newCount, eventArgs) = ResetUnderLock();
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

    internal static IActiveEnumerable<TResult> Get(IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions, bool parallel)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (selector is null)
            throw new ArgumentNullException(nameof(selector));
        return new ActiveSelectEnumerable<TSource, TResult>(source, selector, selectorOptions, parallel);
    }
}