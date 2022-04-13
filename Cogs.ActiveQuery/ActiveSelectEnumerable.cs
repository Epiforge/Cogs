namespace Cogs.ActiveQuery;

sealed class ActiveSelectEnumerable<TResult> :
    DisposableValuesCache<(IEnumerable source, Expression<Func<object?, TResult>> selector, ActiveExpressionOptions? selectorOptions), ActiveSelectEnumerable<TResult>>.Value,
    IActiveEnumerable<TResult>,
    IObserveActiveExpressions<TResult>
{
    object? access;
    Dictionary<IActiveExpression<object?, TResult>, int>? activeExpressionCounts;
    List<IActiveExpression<object?, TResult>>? activeExpressions;
    int count;
    bool isDisposed;

    public TResult this[int index] =>
        this.Execute(() =>
        {
            lock (access!)
                return activeExpressions![index].Value!;
        });

    public int Count
    {
        get => count;
        private set => SetBackedProperty(ref count, in value);
    }

    public bool IsDisposed
    {
        get => isDisposed;
        private set => SetBackedProperty(ref isDisposed, in value);
    }

    public SynchronizationContext? SynchronizationContext { get; private set; }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
#pragma warning disable CS0067
    public event EventHandler<DisposalNotificationEventArgs>? DisposalOverridden;
#pragma warning restore CS0067
    public event EventHandler<DisposalNotificationEventArgs>? Disposed;
    public event EventHandler<DisposalNotificationEventArgs>? Disposing;
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanged;
#pragma warning disable CS0067
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanging;
#pragma warning restore CS0067

    void IObserveActiveExpressions<TResult>.ActiveExpressionChanged(IObservableActiveExpression<TResult> activeExpression, TResult? oldValue, TResult? newValue, Exception? oldFault, Exception? newFault)
    {
        if (activeExpression is IActiveExpression<object?, TResult> typedActiveExpression)
            this.Execute(() =>
            {
                lock (access!)
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

    public IReadOnlyList<(object? element, Exception? fault)> GetElementFaults() =>
        this.Execute(() =>
        {
            lock (access!)
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
        lock (access!)
            foreach (var activeExpression in activeExpressions!)
                yield return activeExpression.Value!;
    }

    protected override void OnInitialized()
    {
        access = new object();
        var (source, selector, selectorOptions) = Key;
        SynchronizationContext = (source as ISynchronized)?.SynchronizationContext;
        (activeExpressions, activeExpressionCounts, count) = this.Execute(() =>
        {
            lock (access)
            {
                var activeExpressions = new List<IActiveExpression<object?, TResult>>();
                var activeExpressionCounts = new Dictionary<IActiveExpression<object?, TResult>, int>();

                void processElement(object? element)
                {
                    var activeExpression = ActiveExpression.Create(selector, element, selectorOptions);
                    activeExpressions!.Add(activeExpression);
                    if (activeExpressionCounts!.TryGetValue(activeExpression, out var existingCount))
                        activeExpressionCounts[activeExpression] = existingCount + 1;
                    else
                    {
                        activeExpressionCounts.Add(activeExpression, 1);
                        activeExpression.AddActiveExpressionOserver(this);
                    }
                }

                if (source is IList sourceList)
                    for (int i = 0, ii = sourceList.Count; i < ii; ++i)
                        processElement(sourceList[i]);
                else
                    foreach (var element in source)
                        processElement(element);
                if (source is INotifyCollectionChanged collectionChangeNotifier)
                    collectionChangeNotifier.CollectionChanged += SourceChanged;
                ;
                return (activeExpressions, activeExpressionCounts, activeExpressions.Count);
            }
        });
    }

    protected override void OnTerminated() =>
        this.Execute(() =>
        {
            var disposalEventArgs = new DisposalNotificationEventArgs(false);
            Disposing?.Invoke(this, disposalEventArgs);
            lock (access!)
            {
                foreach (var activeExpression in activeExpressionCounts!.Keys)
                {
                    activeExpression.RemoveActiveExpressionObserver(this);
                    for (int i = 0, ii = activeExpressionCounts[activeExpression]; i < ii; ++i)
                        activeExpression.Dispose();
                }
                if (Key.source is INotifyCollectionChanged collectionChangeNotifier)
                    collectionChangeNotifier.CollectionChanged -= SourceChanged;
            }
            IsDisposed = true;
            Disposed?.Invoke(this, disposalEventArgs);
        });

    void SourceChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        var (source, selector, selectorOptions) = Key;
        lock (access!)
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
                        var removedActiveExpressions = activeExpressions!.GetRange(e.OldStartingIndex, e.OldItems.Count);
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
                        var movedActiveExpressions = activeExpressions!.GetRange(e.OldStartingIndex, e.OldItems.Count);
                        activeExpressions.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                        activeExpressions.InsertRange(e.NewStartingIndex, movedActiveExpressions);
                        eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, movedActiveExpressions.Select(ae => ae.Value).ToImmutableArray(), e.NewStartingIndex, e.OldStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var activeExpression in activeExpressionCounts!.Keys)
                    {
                        activeExpression.RemoveActiveExpressionObserver(this);
                        for (int i = 0, ii = activeExpressionCounts[activeExpression]; i < ii; ++i)
                            activeExpression.Dispose();
                    }
                    activeExpressions!.Clear();
                    activeExpressionCounts.Clear();

                    void processElement(object? element)
                    {
                        var activeExpression = ActiveExpression.Create(selector!, element, selectorOptions);
                        activeExpressions!.Add(activeExpression);
                        if (activeExpressionCounts!.TryGetValue(activeExpression, out var existingCount))
                            activeExpressionCounts[activeExpression] = existingCount + 1;
                        else
                        {
                            activeExpressionCounts.Add(activeExpression, 1);
                            activeExpression.AddActiveExpressionOserver(this);
                        }
                    }

                    if (source is IList sourceList)
                        for (int i = 0, ii = sourceList.Count; i < ii; ++i)
                            processElement(sourceList[i]);
                    else
                        foreach (var element in source)
                            processElement(element);
                    newCount = activeExpressions.Count;
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

    static readonly DisposableValuesCache<(IEnumerable source, Expression<Func<object?, TResult>> selector, ActiveExpressionOptions? selectorOptions), ActiveSelectEnumerable<TResult>> cache = new(new EqualityComparer());

    internal static IActiveEnumerable<TResult> Get(IEnumerable source, Expression<Func<object?, TResult>> selector, ActiveExpressionOptions? selectorOptions)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (selector is null)
            throw new ArgumentNullException(nameof(selector));
        return cache[(source, selector, selectorOptions)];
    }

    class EqualityComparer :
        IEqualityComparer<(IEnumerable source, Expression<Func<object?, TResult>> selector, ActiveExpressionOptions? selectorOptions)>
    {
        public bool Equals((IEnumerable source, Expression<Func<object?, TResult>> selector, ActiveExpressionOptions? selectorOptions) x, (IEnumerable source, Expression<Func<object?, TResult>> selector, ActiveExpressionOptions? selectorOptions) y) =>
            ReferenceEquals(x.source, y.source) && ExpressionEqualityComparer.Default.Equals(x.selector, y.selector) && (x.selectorOptions is null && y.selectorOptions is null || x.selectorOptions is not null && y.selectorOptions is not null && x.selectorOptions.Equals(y.selectorOptions));

        public int GetHashCode((IEnumerable source, Expression<Func<object?, TResult>> selector, ActiveExpressionOptions? selectorOptions) obj) =>
            HashCode.Combine(obj.source, ExpressionEqualityComparer.Default.GetHashCode(obj.selector), obj.selectorOptions);
    }
}
sealed class ActiveSelectEnumerable<TSource, TResult> :
    DisposableValuesCache<(IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions), ActiveSelectEnumerable<TSource, TResult>>.Value,
    IActiveEnumerable<TResult>,
    IObserveActiveExpressions<TResult>
{
    object? access;
    Dictionary<IActiveExpression<TSource, TResult>, int>? activeExpressionCounts;
    List<IActiveExpression<TSource, TResult>>? activeExpressions;
    int count;
    bool isDisposed;

    public TResult this[int index] =>
        this.Execute(() =>
        {
            lock (access!)
                return activeExpressions![index].Value!;
        });

    public int Count
    {
        get => count;
        private set => SetBackedProperty(ref count, in value);
    }

    public bool IsDisposed
    {
        get => isDisposed;
        private set => SetBackedProperty(ref isDisposed, in value);
    }

    public SynchronizationContext? SynchronizationContext { get; private set; }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
#pragma warning disable CS0067
    public event EventHandler<DisposalNotificationEventArgs>? DisposalOverridden;
#pragma warning restore CS0067
    public event EventHandler<DisposalNotificationEventArgs>? Disposed;
    public event EventHandler<DisposalNotificationEventArgs>? Disposing;
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanged;
#pragma warning disable CS0067
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanging;
#pragma warning restore CS0067

    void IObserveActiveExpressions<TResult>.ActiveExpressionChanged(IObservableActiveExpression<TResult> activeExpression, TResult? oldValue, TResult? newValue, Exception? oldFault, Exception? newFault)
    {
        if (activeExpression is IActiveExpression<TSource, TResult> typedActiveExpression)
            this.Execute(() =>
            {
                lock (access!)
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

    public IReadOnlyList<(object? element, Exception? fault)> GetElementFaults() =>
        this.Execute(() =>
        {
            lock (access!)
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
        lock (access!)
            foreach (var activeExpression in activeExpressions!)
                yield return activeExpression.Value!;
    }

    protected override void OnInitialized()
    {
        access = new object();
        var (source, selector, selectorOptions) = Key;
        SynchronizationContext = (source as ISynchronized)?.SynchronizationContext;
        (activeExpressions, activeExpressionCounts, count) = this.Execute(() =>
        {
            lock (access)
            {
                var activeExpressions = new List<IActiveExpression<TSource, TResult>>();
                var activeExpressionCounts = new Dictionary<IActiveExpression<TSource, TResult>, int>();

                void processElement(TSource element)
                {
                    var activeExpression = ActiveExpression.Create(selector, element, selectorOptions);
                    activeExpressions!.Add(activeExpression);
                    if (activeExpressionCounts!.TryGetValue(activeExpression, out var existingCount))
                        activeExpressionCounts[activeExpression] = existingCount + 1;
                    else
                    {
                        activeExpressionCounts.Add(activeExpression, 1);
                        activeExpression.AddActiveExpressionOserver(this);
                    }
                }

                if (source is IList<TSource> sourceList)
                    for (int i = 0, ii = sourceList.Count; i < ii; ++i)
                        processElement(sourceList[i]);
                else
                    foreach (var element in source)
                        processElement(element);
                if (source is INotifyCollectionChanged collectionChangeNotifier)
                    collectionChangeNotifier.CollectionChanged += SourceChanged;
                ;
                return (activeExpressions, activeExpressionCounts, activeExpressions.Count);
            }
        });
    }

    protected override void OnTerminated() =>
        this.Execute(() =>
        {
            var disposalEventArgs = new DisposalNotificationEventArgs(false);
            Disposing?.Invoke(this, disposalEventArgs);
            lock (access!)
            {
                foreach (var activeExpression in activeExpressionCounts!.Keys)
                {
                    activeExpression.RemoveActiveExpressionObserver(this);
                    for (int i = 0, ii = activeExpressionCounts[activeExpression]; i < ii; ++i)
                        activeExpression.Dispose();
                }
                if (Key.source is INotifyCollectionChanged collectionChangeNotifier)
                    collectionChangeNotifier.CollectionChanged -= SourceChanged;
            }
            IsDisposed = true;
            Disposed?.Invoke(this, disposalEventArgs);
        });

    void SourceChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        var (source, selector, selectorOptions) = Key;
        lock (access!)
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
                        var removedActiveExpressions = activeExpressions!.GetRange(e.OldStartingIndex, e.OldItems.Count);
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
                        var movedActiveExpressions = activeExpressions!.GetRange(e.OldStartingIndex, e.OldItems.Count);
                        activeExpressions.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                        activeExpressions.InsertRange(e.NewStartingIndex, movedActiveExpressions);
                        eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, movedActiveExpressions.Select(ae => ae.Value).ToImmutableArray(), e.NewStartingIndex, e.OldStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var activeExpression in activeExpressionCounts!.Keys)
                    {
                        activeExpression.RemoveActiveExpressionObserver(this);
                        for (int i = 0, ii = activeExpressionCounts[activeExpression]; i < ii; ++i)
                            activeExpression.Dispose();
                    }
                    activeExpressions!.Clear();
                    activeExpressionCounts.Clear();

                    void processElement(TSource element)
                    {
                        var activeExpression = ActiveExpression.Create(selector!, element, selectorOptions);
                        activeExpressions!.Add(activeExpression);
                        if (activeExpressionCounts!.TryGetValue(activeExpression, out var existingCount))
                            activeExpressionCounts[activeExpression] = existingCount + 1;
                        else
                        {
                            activeExpressionCounts.Add(activeExpression, 1);
                            activeExpression.AddActiveExpressionOserver(this);
                        }
                    }

                    if (source is IList<TSource> sourceList)
                        for (int i = 0, ii = sourceList.Count; i < ii; ++i)
                            processElement(sourceList[i]);
                    else
                        foreach (var element in source)
                            processElement(element);
                    newCount = activeExpressions.Count;
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

    static readonly DisposableValuesCache<(IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions), ActiveSelectEnumerable<TSource, TResult>> cache = new(new EqualityComparer());

    internal static IActiveEnumerable<TResult> Get(IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (selector is null)
            throw new ArgumentNullException(nameof(selector));
        return cache[(source, selector, selectorOptions)];
    }

    class EqualityComparer :
        IEqualityComparer<(IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions)>
    {
        public bool Equals((IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions) x, (IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions) y) =>
            ReferenceEquals(x.source, y.source) && ExpressionEqualityComparer.Default.Equals(x.selector, y.selector) && (x.selectorOptions is null && y.selectorOptions is null || x.selectorOptions is not null && y.selectorOptions is not null && x.selectorOptions.Equals(y.selectorOptions));

        public int GetHashCode((IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions) obj) =>
            HashCode.Combine(obj.source, ExpressionEqualityComparer.Default.GetHashCode(obj.selector), obj.selectorOptions);
    }
}