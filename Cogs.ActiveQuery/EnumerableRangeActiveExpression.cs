
namespace Cogs.ActiveQuery;

/// <summary>
/// Represents the sequence of results derived from creating an active expression for each element in a sequence
/// </summary>
/// <typeparam name="TResult">The type of the result of the active expression</typeparam>
sealed class EnumerableRangeActiveExpression<TResult> :
    SyncDisposable,
    INotifyElementFaultChanges,
    INotifyCollectionChanged
{
    EnumerableRangeActiveExpression(IEnumerable source, Expression<Func<object?, TResult>> expression, ActiveExpressionOptions? options, RangeActiveExpressionsKey rangeActiveExpressionsKey)
    {
        this.source = source;
        this.expression = expression;
        Options = options;
        this.rangeActiveExpressionsKey = rangeActiveExpressionsKey;
    }

    readonly Dictionary<IActiveExpression<object?, TResult>, int> activeExpressionCounts = new();
    readonly List<(object? element, IActiveExpression<object?, TResult> activeExpression)> activeExpressions = new();
    readonly AsyncReaderWriterLock activeExpressionsAccess = new();
    int disposalCount;
    readonly Expression<Func<object?, TResult>> expression;
    readonly RangeActiveExpressionsKey rangeActiveExpressionsKey;
    readonly IEnumerable source;

    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanged;
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanging;
    public event EventHandler<RangeActiveExpressionResultChangeEventArgs<object?, TResult?>>? ElementResultChanged;
    public event EventHandler<RangeActiveExpressionResultChangeEventArgs<object?, TResult?>>? ElementResultChanging;
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public ActiveExpressionOptions? Options { get; }

    void ActiveExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        var activeExpression = (IActiveExpression<object, TResult>)sender;
        if (e.PropertyName == nameof(IActiveExpression<object, TResult>.Fault))
        {
            using (activeExpressionsAccess.ReaderLock())
                OnElementFaultChanged(activeExpression.Arg, activeExpression.Fault, activeExpressionCounts[activeExpression]);
        }
        else if (e.PropertyName == nameof(IActiveExpression<object, TResult>.Value))
        {
            using (activeExpressionsAccess.ReaderLock())
                OnElementResultChanged(activeExpression.Arg, activeExpression.Value, activeExpressionCounts[activeExpression]);
        }
    }

    void ActiveExpressionPropertyChanging(object sender, PropertyChangingEventArgs e)
    {
        var activeExpression = (IActiveExpression<object, TResult>)sender;
        if (e.PropertyName == nameof(IActiveExpression<object, TResult>.Fault))
        {
            using (activeExpressionsAccess.ReaderLock())
                OnElementFaultChanging(activeExpression.Arg, activeExpression.Fault, activeExpressionCounts[activeExpression]);
        }
        else if (e.PropertyName == nameof(IActiveExpression<object, TResult>.Value))
        {
            using (activeExpressionsAccess.ReaderLock())
                OnElementResultChanging(activeExpression.Arg, activeExpression.Value, activeExpressionCounts[activeExpression]);
        }
    }

    List<IActiveExpression<object?, TResult>> AddActiveExpressionsUnderLock(int index, IReadOnlyList<object?> elements)
    {
        var addedActiveExpressions = new List<IActiveExpression<object?, TResult>>();
        activeExpressions.InsertRange(index, elements.Select(element =>
        {
            var activeExpression = ActiveExpression.Create(expression, element, Options);
            if (activeExpressionCounts.TryGetValue(activeExpression, out var activeExpressionCount))
                activeExpressionCounts[activeExpression] = activeExpressionCount + 1;
            else
            {
                activeExpressionCounts.Add(activeExpression, 1);
                activeExpression.PropertyChanging += ActiveExpressionPropertyChanging;
                activeExpression.PropertyChanged += ActiveExpressionPropertyChanged;
            }
            addedActiveExpressions.Add(activeExpression);
            return (element, (IActiveExpression<object?, TResult>)activeExpression);
        }));
        return addedActiveExpressions;
    }

    protected override bool Dispose(bool disposing)
    {
        lock (rangeActiveExpressionsAccess)
        {
            if (--disposalCount > 0)
                return false;
            rangeActiveExpressions.Remove(rangeActiveExpressionsKey);
        }
        RemoveActiveExpressions(0, activeExpressions.Count);
        if (source is INotifyCollectionChanged collectionChangedNotifier)
            collectionChangedNotifier.CollectionChanged -= SourceCollectionChanged;
        if (source is INotifyElementFaultChanges faultNotifier)
        {
            faultNotifier.ElementFaultChanged -= SourceElementFaultChanged;
            faultNotifier.ElementFaultChanging -= SourceElementFaultChanging;
        }
        return true;
    }

    public IReadOnlyList<(object? element, Exception? fault)> GetElementFaults()
    {
        using (activeExpressionsAccess.ReaderLock())
            return GetElementFaultsUnderLock();
    }

    internal IReadOnlyList<(object? element, Exception? fault)> GetElementFaultsUnderLock() =>
        activeExpressions.Select(eae => (eae.element, fault: eae.activeExpression.Fault)).Where(ef => ef.fault is { }).ToImmutableArray();

    public IReadOnlyList<(object element, TResult? result)> GetResults()
    {
        using (activeExpressionsAccess.ReaderLock())
            return GetResultsUnderLock();
    }

    internal IReadOnlyList<(object element, TResult? result)> GetResultsUnderLock() =>
        activeExpressions.Select(eae => (eae.element, eae.activeExpression.Value)).ToImmutableArray();

    void Initialize()
    {
        using (activeExpressionsAccess.WriterLock())
        {
            var elements = source.Cast<object>().ToImmutableArray();
            if (source is INotifyCollectionChanged collectionChangedNotifier)
                collectionChangedNotifier.CollectionChanged += SourceCollectionChanged;
            if (source is INotifyElementFaultChanges faultNotifier)
            {
                faultNotifier.ElementFaultChanged += SourceElementFaultChanged;
                faultNotifier.ElementFaultChanging += SourceElementFaultChanging;
            }
            AddActiveExpressionsUnderLock(0, elements);
        }
    }

    void OnCollectionChanged(NotifyCollectionChangedEventArgs e) =>
        CollectionChanged?.Invoke(this, e);

    void OnElementFaultChanged(ElementFaultChangeEventArgs e) =>
        ElementFaultChanged?.Invoke(this, e);

    void OnElementFaultChanged(object? element, Exception? fault, int count) =>
        OnElementFaultChanged(new ElementFaultChangeEventArgs(element, fault, count));

    void OnElementFaultChanging(ElementFaultChangeEventArgs e) =>
        ElementFaultChanging?.Invoke(this, e);

    void OnElementFaultChanging(object? element, Exception? fault, int count) =>
        OnElementFaultChanging(new ElementFaultChangeEventArgs(element, fault, count));

    void OnElementResultChanged(RangeActiveExpressionResultChangeEventArgs<object?, TResult?> e) =>
        ElementResultChanged?.Invoke(this, e);

    void OnElementResultChanged(object? element, TResult? result, int count) =>
        OnElementResultChanged(new RangeActiveExpressionResultChangeEventArgs<object?, TResult?>(element, result, count));

    void OnElementResultChanging(RangeActiveExpressionResultChangeEventArgs<object?, TResult?> e) =>
        ElementResultChanging?.Invoke(this, e);

    void OnElementResultChanging(object? element, TResult? result, int count) =>
        OnElementResultChanging(new RangeActiveExpressionResultChangeEventArgs<object?, TResult?>(element, result, count));

    IReadOnlyList<(object? element, TResult? result)> RemoveActiveExpressions(int index, int count)
    {
        List<(object? element, TResult? result)>? result = null;
        if (count > 0)
        {
            using (activeExpressionsAccess.WriterLock())
                result = RemoveActiveExpressionsUnderLock(index, count);
        }
        return (result ?? Enumerable.Empty<(object? element, TResult? result)>()).ToImmutableArray();
    }

    List<(object? element, TResult? result)> RemoveActiveExpressionsUnderLock(int index, int count)
    {
        var result = new List<(object? element, TResult? result)>();
        foreach (var (element, activeExpression) in activeExpressions.GetRange(index, count))
        {
            result.Add((element, activeExpression.Value));
            var activeExpressionCount = activeExpressionCounts[activeExpression];
            if (activeExpressionCount == 1)
            {
                activeExpressionCounts.Remove(activeExpression);
                activeExpression.PropertyChanging -= ActiveExpressionPropertyChanging;
                activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
            }
            else
                activeExpressionCounts[activeExpression] = activeExpressionCount - 1;
            activeExpression.Dispose();
        }
        activeExpressions.RemoveRange(index, count);
        return result;
    }

    void SourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        NotifyCollectionChangedEventArgs? eventArgs = null;
        using (activeExpressionsAccess.WriterLock())
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add when e.NewStartingIndex >= 0 && e.NewStartingIndex <= activeExpressions.Count:
                    eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, AddActiveExpressionsUnderLock(e.NewStartingIndex, e.NewItems.Cast<object?>().ToImmutableArray()).Select(ae => ((object?)ae.Arg, ae.Value)).ToImmutableArray(), e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Move when e.OldStartingIndex >= 0 && e.OldStartingIndex <= activeExpressions.Count - (e.OldItems.Count - 1) && e.NewStartingIndex >= 0 && e.NewStartingIndex <= activeExpressions.Count - (e.OldItems.Count - 1):
                    var moving = activeExpressions.GetRange(e.OldStartingIndex, e.OldItems.Count);
                    activeExpressions.RemoveRange(e.OldStartingIndex, moving.Count);
                    activeExpressions.InsertRange(e.NewStartingIndex, moving);
                    eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, moving.Select(eae => (eae.element, eae.activeExpression.Value)).ToImmutableArray(), e.NewStartingIndex, e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove when e.OldStartingIndex >= 0 && e.OldStartingIndex <= activeExpressions.Count - (e.OldItems.Count - 1):
                    eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, RemoveActiveExpressionsUnderLock(e.OldStartingIndex, e.OldItems.Count).ToImmutableArray(), e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace when e.OldStartingIndex >= 0 && e.OldStartingIndex <= activeExpressions.Count - (e.OldItems.Count - 1) && e.NewStartingIndex >= 0 && e.NewStartingIndex <= activeExpressions.Count - (e.NewItems.Count - 1):
                    var removed = RemoveActiveExpressionsUnderLock(e.OldStartingIndex, e.OldItems.Count).ToImmutableArray();
                    var added = AddActiveExpressionsUnderLock(e.NewStartingIndex, e.NewItems.Cast<object?>().ToImmutableArray()).Select(ae => ((object?)ae.Arg, ae.Value)).ToImmutableArray();
                    eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, added, removed, e.OldStartingIndex);
                    break;
                default:
                    RemoveActiveExpressionsUnderLock(0, activeExpressions.Count);
                    AddActiveExpressionsUnderLock(0, source.Cast<object>().ToImmutableArray());
                    eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                    break;
            }
        }
        OnCollectionChanged(eventArgs);
    }

    void SourceElementFaultChanged(object sender, ElementFaultChangeEventArgs e) =>
        ElementFaultChanged?.Invoke(sender, e);

    void SourceElementFaultChanging(object sender, ElementFaultChangeEventArgs e) =>
        ElementFaultChanging?.Invoke(sender, e);

    static readonly object rangeActiveExpressionsAccess = new();
    static readonly Dictionary<RangeActiveExpressionsKey, EnumerableRangeActiveExpression<TResult>> rangeActiveExpressions = new(new RangeActiveExpressionsKeyEqualityComparer());

    public static EnumerableRangeActiveExpression<TResult> Create(IEnumerable source, Expression<Func<object?, TResult>> expression, ActiveExpressionOptions? options = null)
    {
        EnumerableRangeActiveExpression<TResult> rangeActiveExpression;
        bool monitorCreated;
        var key = new RangeActiveExpressionsKey(source, expression, options);
        lock (rangeActiveExpressionsAccess)
        {
            if (monitorCreated = !rangeActiveExpressions.TryGetValue(key, out rangeActiveExpression))
            {
                rangeActiveExpression = new EnumerableRangeActiveExpression<TResult>(source, expression, options, key);
                rangeActiveExpressions.Add(key, rangeActiveExpression);
            }
            ++rangeActiveExpression.disposalCount;
        }
        if (monitorCreated)
            rangeActiveExpression.Initialize();
        return rangeActiveExpression;
    }

    record RangeActiveExpressionsKey(IEnumerable Source, Expression<Func<object?, TResult>> Expression, ActiveExpressionOptions? Options);

    class RangeActiveExpressionsKeyEqualityComparer :
        IEqualityComparer<RangeActiveExpressionsKey>
    {
        public bool Equals(RangeActiveExpressionsKey x, RangeActiveExpressionsKey y) =>
            ReferenceEquals(x.Source, y.Source) && ExpressionEqualityComparer.Default.Equals(x.Expression, y.Expression) && Equals(x.Options, y.Options);

        public int GetHashCode(RangeActiveExpressionsKey obj) =>
            HashCode.Combine(typeof(RangeActiveExpressionsKey), obj.Source?.GetHashCode() ?? 0, ExpressionEqualityComparer.Default.GetHashCode(obj.Expression), obj.Options?.GetHashCode() ?? 0);
    }
}

/// <summary>
/// Represents the sequence of results derived from creating an active expression for each element in a sequence
/// </summary>
/// <typeparam name="TElement">The type of the elements in the sequence</typeparam>
/// <typeparam name="TResult">The type of the result of the active expression</typeparam>
sealed class EnumerableRangeActiveExpression<TElement, TResult> :
    SyncDisposable,
    INotifyElementFaultChanges,
    INotifyCollectionChanged
{
    EnumerableRangeActiveExpression(IEnumerable<TElement> source, Expression<Func<TElement, TResult>> expression, ActiveExpressionOptions? options, RangeActiveExpressionKey rangeActiveExpressionKey)
    {
        this.source = source;
        this.expression = expression;
        Options = options;
        this.rangeActiveExpressionKey = rangeActiveExpressionKey;
    }

    readonly Dictionary<IActiveExpression<TElement, TResult>, int> activeExpressionCounts = new();
    readonly List<(TElement element, IActiveExpression<TElement, TResult> activeExpression)> activeExpressions = new();
    readonly AsyncReaderWriterLock activeExpressionsAccess = new();
    int disposalCount;
    readonly Expression<Func<TElement, TResult>> expression;
    readonly RangeActiveExpressionKey rangeActiveExpressionKey;
    readonly IEnumerable<TElement> source;

    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanged;
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanging;
    public event EventHandler<RangeActiveExpressionResultChangeEventArgs<TElement, TResult?>>? ElementResultChanged;
    public event EventHandler<RangeActiveExpressionResultChangeEventArgs<TElement, TResult?>>? ElementResultChanging;
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public ActiveExpressionOptions? Options { get; }

    void ActiveExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        var activeExpression = (IActiveExpression<TElement, TResult>)sender;
        if (e.PropertyName == nameof(IActiveExpression<TElement, TResult>.Fault))
        {
            using (activeExpressionsAccess.ReaderLock())
                OnElementFaultChanged(activeExpression.Arg, activeExpression.Fault, activeExpressionCounts[activeExpression]);
        }
        else if (e.PropertyName == nameof(IActiveExpression<TElement, TResult>.Value))
        {
            using (activeExpressionsAccess.ReaderLock())
                OnElementResultChanged(activeExpression.Arg, activeExpression.Value, activeExpressionCounts[activeExpression]);
        }
    }

    void ActiveExpressionPropertyChanging(object sender, PropertyChangingEventArgs e)
    {
        var activeExpression = (IActiveExpression<TElement, TResult>)sender;
        if (e.PropertyName == nameof(IActiveExpression<TElement, TResult>.Fault))
        {
            using (activeExpressionsAccess.ReaderLock())
                OnElementFaultChanging(activeExpression.Arg, activeExpression.Fault, activeExpressionCounts[activeExpression]);
        }
        else if (e.PropertyName == nameof(IActiveExpression<TElement, TResult>.Value))
        {
            using (activeExpressionsAccess.ReaderLock())
                OnElementResultChanging(activeExpression.Arg, activeExpression.Value, activeExpressionCounts[activeExpression]);
        }
    }

    List<IActiveExpression<TElement, TResult>> AddActiveExpressionsUnderLock(int index, IReadOnlyList<TElement> elements)
    {
        var addedActiveExpressions = new List<IActiveExpression<TElement, TResult>>();
        activeExpressions.InsertRange(index, elements.Select(element =>
        {
            var activeExpression = ActiveExpression.Create(expression, element, Options);
            if (activeExpressionCounts.TryGetValue(activeExpression, out var activeExpressionCount))
                activeExpressionCounts[activeExpression] = activeExpressionCount + 1;
            else
            {
                activeExpressionCounts.Add(activeExpression, 1);
                activeExpression.PropertyChanging += ActiveExpressionPropertyChanging;
                activeExpression.PropertyChanged += ActiveExpressionPropertyChanged;
            }
            addedActiveExpressions.Add(activeExpression);
            return (element, (IActiveExpression<TElement, TResult>)activeExpression);
        }));
        return addedActiveExpressions;
    }

    protected override bool Dispose(bool disposing)
    {
        lock (rangeActiveExpressionsAccess)
        {
            if (--disposalCount > 0)
                return false;
            rangeActiveExpressions.Remove(rangeActiveExpressionKey);
        }
        RemoveActiveExpressions(0, activeExpressions.Count);
        if (source is INotifyCollectionChanged collectionChangedNotifier)
            collectionChangedNotifier.CollectionChanged -= SourceCollectionChanged;
        if (source is INotifyElementFaultChanges faultNotifier)
        {
            faultNotifier.ElementFaultChanged -= SourceElementFaultChanged;
            faultNotifier.ElementFaultChanging -= SourceElementFaultChanging;
        }
        return true;
    }

    public IReadOnlyList<(object? element, Exception? fault)> GetElementFaults()
    {
        using (activeExpressionsAccess.ReaderLock())
            return GetElementFaultsUnderLock();
    }

    internal IReadOnlyList<(object? element, Exception? fault)> GetElementFaultsUnderLock() =>
        activeExpressions.Select(ae => (element: (object?)ae.element, fault: ae.activeExpression.Fault)).Where(ef => ef.fault is { }).ToImmutableArray();

    public IReadOnlyList<(TElement element, TResult? result)> GetResults()
    {
        using (activeExpressionsAccess.ReaderLock())
            return GetResultsUnderLock();
    }

    internal IReadOnlyList<(TElement element, TResult? result)> GetResultsUnderLock() =>
        activeExpressions.Select(ae => (ae.element, ae.activeExpression.Value)).ToImmutableArray();

    public IReadOnlyList<(TElement element, TResult result, Exception fault, int count)> GetResultsFaultsAndCounts()
    {
        using (activeExpressionsAccess.ReaderLock())
            return GetResultsFaultsAndCountsUnderLock();
    }

    internal IReadOnlyList<(TElement element, TResult result, Exception fault, int count)> GetResultsFaultsAndCountsUnderLock() =>
        activeExpressions.Select(ae => (ae.element, ae.activeExpression.Value, ae.activeExpression.Fault, activeExpressionCounts[ae.activeExpression])).ToImmutableArray();

    void Initialize()
    {
        using (activeExpressionsAccess.WriterLock())
        {
            var elements = source.ToImmutableArray();
            if (source is INotifyCollectionChanged collectionChangedNotifier)
                collectionChangedNotifier.CollectionChanged += SourceCollectionChanged;
            if (source is INotifyElementFaultChanges faultNotifier)
            {
                faultNotifier.ElementFaultChanged += SourceElementFaultChanged;
                faultNotifier.ElementFaultChanging += SourceElementFaultChanging;
            }
            AddActiveExpressionsUnderLock(0, elements);
        }
    }

    void OnCollectionChanged(NotifyCollectionChangedEventArgs e) =>
        CollectionChanged?.Invoke(this, e);

    void OnElementFaultChanged(ElementFaultChangeEventArgs e) =>
        ElementFaultChanged?.Invoke(this, e);

    void OnElementFaultChanged(TElement element, Exception? fault, int count) =>
        OnElementFaultChanged(new ElementFaultChangeEventArgs(element, fault, count));

    void OnElementFaultChanging(ElementFaultChangeEventArgs e) =>
        ElementFaultChanging?.Invoke(this, e);

    void OnElementFaultChanging(TElement element, Exception? fault, int count) =>
        OnElementFaultChanging(new ElementFaultChangeEventArgs(element, fault, count));

    void OnElementResultChanged(RangeActiveExpressionResultChangeEventArgs<TElement, TResult?> e) =>
        ElementResultChanged?.Invoke(this, e);

    void OnElementResultChanged(TElement element, TResult? result, int count) =>
        OnElementResultChanged(new RangeActiveExpressionResultChangeEventArgs<TElement, TResult?>(element, result, count));

    void OnElementResultChanging(RangeActiveExpressionResultChangeEventArgs<TElement, TResult?> e) =>
        ElementResultChanging?.Invoke(this, e);

    void OnElementResultChanging(TElement element, TResult? result, int count) =>
        OnElementResultChanging(new RangeActiveExpressionResultChangeEventArgs<TElement, TResult?>(element, result, count));

    IReadOnlyList<(TElement element, TResult? result)> RemoveActiveExpressions(int index, int count)
    {
        List<(TElement element, TResult? result)>? result = null;
        if (count > 0)
        {
            using (activeExpressionsAccess.WriterLock())
                result = RemoveActiveExpressionsUnderLock(index, count);
        }
        return (result ?? Enumerable.Empty<(TElement element, TResult? result)>()).ToImmutableArray();
    }

    List<(TElement element, TResult? result)> RemoveActiveExpressionsUnderLock(int index, int count)
    {
        var result = new List<(TElement element, TResult? result)>();
        foreach (var (element, activeExpression) in activeExpressions.GetRange(index, count))
        {
            result.Add((element, activeExpression.Value));
            var activeExpressionCount = activeExpressionCounts[activeExpression];
            if (activeExpressionCount == 1)
            {
                activeExpressionCounts.Remove(activeExpression);
                activeExpression.PropertyChanging -= ActiveExpressionPropertyChanging;
                activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
            }
            else
                activeExpressionCounts[activeExpression] = activeExpressionCount - 1;
            activeExpression.Dispose();
        }
        activeExpressions.RemoveRange(index, count);
        return result;
    }

    void SourceElementFaultChanged(object sender, ElementFaultChangeEventArgs e) =>
        ElementFaultChanged?.Invoke(sender, e);

    void SourceElementFaultChanging(object sender, ElementFaultChangeEventArgs e) =>
        ElementFaultChanging?.Invoke(sender, e);

    void SourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        NotifyCollectionChangedEventArgs? eventArgs = null;
        using (activeExpressionsAccess.WriterLock())
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add when e.NewStartingIndex >= 0 && e.NewStartingIndex <= activeExpressions.Count:
                    eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, AddActiveExpressionsUnderLock(e.NewStartingIndex, e.NewItems.Cast<TElement>().ToImmutableArray()).Select(ae => (ae.Arg, ae.Value)).ToImmutableArray(), e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Move when e.OldStartingIndex >= 0 && e.OldStartingIndex <= activeExpressions.Count - (e.OldItems.Count - 1) && e.NewStartingIndex >= 0 && e.NewStartingIndex <= activeExpressions.Count - (e.OldItems.Count - 1):
                    var moving = activeExpressions.GetRange(e.OldStartingIndex, e.OldItems.Count);
                    activeExpressions.RemoveRange(e.OldStartingIndex, moving.Count);
                    activeExpressions.InsertRange(e.NewStartingIndex, moving);
                    eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, moving.Select(eae => (eae.element, eae.activeExpression.Value)).ToImmutableArray(), e.NewStartingIndex, e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove when e.OldStartingIndex >= 0 && e.OldStartingIndex <= activeExpressions.Count - (e.OldItems.Count - 1):
                    eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, RemoveActiveExpressionsUnderLock(e.OldStartingIndex, e.OldItems.Count).ToImmutableArray(), e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace when e.OldStartingIndex >= 0 && e.OldStartingIndex <= activeExpressions.Count - (e.OldItems.Count - 1) && e.NewStartingIndex >= 0 && e.NewStartingIndex <= activeExpressions.Count - (e.NewItems.Count - 1):
                    var removed = RemoveActiveExpressionsUnderLock(e.OldStartingIndex, e.OldItems.Count).ToImmutableArray();
                    var added = AddActiveExpressionsUnderLock(e.NewStartingIndex, e.NewItems.Cast<TElement>().ToImmutableArray()).Select(ae => (ae.Arg, ae.Value)).ToImmutableArray();
                    eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, added, removed, e.OldStartingIndex);
                    break;
                default:
                    RemoveActiveExpressionsUnderLock(0, activeExpressions.Count);
                    AddActiveExpressionsUnderLock(0, source.Cast<TElement>().ToImmutableArray());
                    eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                    break;
            }
        }
        OnCollectionChanged(eventArgs);
    }

    static readonly object rangeActiveExpressionsAccess = new();
    static readonly Dictionary<RangeActiveExpressionKey, EnumerableRangeActiveExpression<TElement, TResult>> rangeActiveExpressions = new(new RangeActiveExpressionKeyEqualityComparer());

    public static EnumerableRangeActiveExpression<TElement, TResult> Create(IEnumerable<TElement> source, Expression<Func<TElement, TResult>> expression, ActiveExpressionOptions? options = null)
    {
        EnumerableRangeActiveExpression<TElement, TResult> rangeActiveExpression;
        bool monitorCreated;
        var key = new RangeActiveExpressionKey(source, expression, options);
        lock (rangeActiveExpressionsAccess)
        {
            if (monitorCreated = !rangeActiveExpressions.TryGetValue(key, out rangeActiveExpression))
            {
                rangeActiveExpression = new EnumerableRangeActiveExpression<TElement, TResult>(source, expression, options, key);
                rangeActiveExpressions.Add(key, rangeActiveExpression);
            }
            ++rangeActiveExpression.disposalCount;
        }
        if (monitorCreated)
            rangeActiveExpression.Initialize();
        return rangeActiveExpression;
    }

    record RangeActiveExpressionKey(IEnumerable<TElement> Source, Expression<Func<TElement, TResult>> Expression, ActiveExpressionOptions? Options);

    class RangeActiveExpressionKeyEqualityComparer : IEqualityComparer<RangeActiveExpressionKey>
    {
        public bool Equals(RangeActiveExpressionKey x, RangeActiveExpressionKey y) =>
            ReferenceEquals(x.Source, y.Source) && ExpressionEqualityComparer.Default.Equals(x.Expression, y.Expression) && Equals(x.Options, y.Options);

        public int GetHashCode(RangeActiveExpressionKey obj) =>
            HashCode.Combine(typeof(RangeActiveExpressionKey), obj.Source?.GetHashCode() ?? 0, ExpressionEqualityComparer.Default.GetHashCode(obj.Expression), obj.Options?.GetHashCode() ?? 0);
    }
}
