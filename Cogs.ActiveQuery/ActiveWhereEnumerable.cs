namespace Cogs.ActiveQuery;

/// <summary>
/// Represents a read-only collection of elements that is the result of calling <see cref="ActiveEnumerableExtensions.ActiveWhere{TSource}(IEnumerable{TSource}, Expression{Func{TSource, bool}})"/> or <see cref="ActiveEnumerableExtensions.ActiveWhere{TSource}(IEnumerable{TSource}, Expression{Func{TSource, bool}}, ActiveExpressionOptions?)"/>
/// </summary>
/// <typeparam name="TElement">The type of the elements in the sequence</typeparam>
public sealed class ActiveWhereEnumerable<TElement> :
    SyncDisposable,
    IActiveEnumerable<TElement>
{
    internal ActiveWhereEnumerable(IEnumerable<TElement> source, Expression<Func<TElement, bool>> predicate, ActiveExpressionOptions? predicateOptions)
    {
        access = new object();
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        this.predicateOptions = predicateOptions;
        SynchronizationContext = (source as ISynchronized)?.SynchronizationContext;
        (activeExpressions, activeExpressionCounts, count) = this.Execute(() =>
        {
            lock (access)
            {
                var activeExpressions = new List<IActiveExpression<TElement, bool>>();
                var activeExpressionCounts = new Dictionary<IActiveExpression<TElement, bool>, int>();
                var count = 0;
                foreach (var element in source)
                {
                    var activeExpression = ActiveExpression.Create(this.predicate, element, this.predicateOptions);
                    activeExpressions.Add(activeExpression);
                    if (activeExpression.Value)
                        ++count;
                    if (activeExpressionCounts.TryGetValue(activeExpression, out var existingCount))
                        activeExpressionCounts[activeExpression] = existingCount + 1;
                    else
                    {
                        activeExpressionCounts.Add(activeExpression, 1);
                        activeExpression.PropertyChanged += ActiveExpressionPropertyChanged;
                        activeExpression.PropertyChanging += ActiveExpressionPropertyChanging;
                    }
                }
                if (source is INotifyCollectionChanged collectionChangeNotifier)
                    collectionChangeNotifier.CollectionChanged += SourceChanged;
                return (activeExpressions, activeExpressionCounts, count);
            }
        });
    }

    readonly object access;
    readonly Dictionary<IActiveExpression<TElement, bool>, int> activeExpressionCounts;
    readonly List<IActiveExpression<TElement, bool>> activeExpressions;
    int count;
    readonly Expression<Func<TElement, bool>> predicate;
    readonly ActiveExpressionOptions? predicateOptions;
    readonly IEnumerable<TElement> source;

    /// <inheritdoc/>
    public TElement this[int index]
    {
        get
        {
            for (var i = 0; i < activeExpressions.Count; ++i)
            {
                var activeExpression = activeExpressions[i];
                if (!activeExpression.Value)
                    continue;
                if (--index == -1)
                    return activeExpression.Arg;
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    /// <inheritdoc/>
    public int Count
    {
        get => count;
        private set => SetBackedProperty(ref count, in value);
    }

    /// <inheritdoc/>
    public SynchronizationContext? SynchronizationContext { get; }

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanged;

    /// <inheritdoc/>
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanging;

    void ActiveExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        var isFaultChange = e.PropertyName == nameof(IActiveExpression<TElement, bool>.Fault);
        var isValueChange = e.PropertyName == nameof(IActiveExpression<TElement, bool>.Value);
        if (sender is IActiveExpression<TElement, bool> activeExpression && (isFaultChange || isValueChange))
            this.Execute(() =>
            {
                lock (access)
                {
                    if (isFaultChange)
                        ElementFaultChanged?.Invoke(this, new ElementFaultChangeEventArgs(activeExpression.Arg, activeExpression.Fault, activeExpressionCounts[activeExpression]));
                    else if (isValueChange)
                    {
                        var action = activeExpression.Value ? NotifyCollectionChangedAction.Add : NotifyCollectionChangedAction.Remove;
                        var countIteration = activeExpression.Value ? 1 : -1;
                        var translatedIndex = -1;
                        for (int i = 0, ii = activeExpressions.Count; i < ii; ++i)
                        {
                            var iActiveExpression = activeExpressions[i];
                            if (iActiveExpression.Value)
                                ++translatedIndex;
                            if (iActiveExpression == activeExpression)
                            {
                                Count += countIteration;
                                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, activeExpression.Arg, translatedIndex));
                            }
                        }
                    }
                }
            });
    }

    void ActiveExpressionPropertyChanging(object sender, PropertyChangingEventArgs e)
    {
        var isFaultChange = e.PropertyName == nameof(IActiveExpression<TElement, bool>.Fault);
        if (sender is IActiveExpression<TElement, bool> activeExpression && isFaultChange)
            this.Execute(() =>
            {
                lock (access)
                    ElementFaultChanging?.Invoke(this, new ElementFaultChangeEventArgs(activeExpression.Arg, activeExpression.Fault, activeExpressionCounts[activeExpression]));
            });
    }

    /// <inheritdoc/>
    protected override bool Dispose(bool disposing)
    {
        if (disposing)
            this.Execute(() =>
            {
                lock (access)
                {
                    foreach (var activeExpression in activeExpressionCounts.Keys)
                    {
                        activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                        activeExpression.PropertyChanging -= ActiveExpressionPropertyChanging;
                        for (int i = 0, ii = activeExpressionCounts[activeExpression]; i < ii; ++i)
                            activeExpression.Dispose();
                    }
                    if (source is INotifyCollectionChanged collectionChangeNotifier)
                        collectionChangeNotifier.CollectionChanged -= SourceChanged;
                }
            });
        return true;
    }

    /// <inheritdoc/>
    public IReadOnlyList<(object? element, Exception? fault)> GetElementFaults() =>
        this.Execute(() =>
        {
            lock (access)
            {
                var result = new List<(object? element, Exception? fault)>();
                foreach (var activeExpression in activeExpressionCounts.Keys)
                    if (activeExpression.Fault is { } fault)
                        result.Add((activeExpression.Arg, fault));
                return result.AsReadOnly();
            }
        });

    /// <inheritdoc/>
    public IEnumerator<TElement> GetEnumerator() =>
        this.Execute(() => GetEnumeratorInContext());

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    IEnumerator<TElement> GetEnumeratorInContext()
    {
        lock (access)
            foreach (var activeExpression in activeExpressions)
                if (activeExpression.Value)
                    yield return activeExpression.Arg;
    }

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity", Justification = @"Splitting this up into more methods is ¯\_(ツ)_/¯")]
    [SuppressMessage("Reliability", "CA2000: Dispose objects before losing scope", Justification = "They'll get disposed, chill out")]
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
                    var oldItems = new List<TElement>();
                    if (e.OldItems is not null && e.OldStartingIndex >= 0)
                        for (var i = e.OldItems.Count - 1; i >= 0; --i)
                        {
                            var element = (TElement)e.OldItems[i];
                            var activeExpression = activeExpressions[e.OldStartingIndex + i];
                            activeExpressions.RemoveAt(e.OldStartingIndex + i);
                            var activeExpressionCount = activeExpressionCounts[activeExpression];
                            if (activeExpressionCount > 1)
                                activeExpressionCounts[activeExpression] = activeExpressionCount - 1;
                            else
                            {
                                activeExpressionCounts.Remove(activeExpression);
                                activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                                activeExpression.PropertyChanging -= ActiveExpressionPropertyChanging;
                            }
                            if (activeExpression.Value)
                                oldItems.Add(element);
                            activeExpression.Dispose();
                        }
                    var newItems = new List<TElement>();
                    if (e.NewItems is not null && e.NewStartingIndex >= 0)
                        for (var i = 0; i < e.NewItems.Count; ++i)
                        {
                            var element = (TElement)e.NewItems[i];
                            var activeExpression = ActiveExpression.Create(predicate, element, predicateOptions);
                            activeExpressions.Insert(e.NewStartingIndex + i, activeExpression);
                            if (activeExpressionCounts.TryGetValue(activeExpression, out var existingCount))
                                activeExpressionCounts[activeExpression] = existingCount + 1;
                            else
                            {
                                activeExpressionCounts.Add(activeExpression, 1);
                                activeExpression.PropertyChanged += ActiveExpressionPropertyChanged;
                                activeExpression.PropertyChanging += ActiveExpressionPropertyChanging;
                            }
                            if (activeExpression.Value)
                                newItems.Add(element);
                        }
                    if (newItems.Count > 0)
                    {
                        if (oldItems.Count > 0)
                            eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems.AsReadOnly(), oldItems.AsReadOnly(), TranslateIndex(e.NewStartingIndex));
                        else
                            eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems.AsReadOnly(), TranslateIndex(e.NewStartingIndex));
                    }
                    else if (oldItems.Count > 0)
                        eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems.AsReadOnly(), TranslateIndex(e.OldStartingIndex));
                    newCount = count + newItems.Count - oldItems.Count;
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldItems.Count > 0)
                    {
                        var oldStartingIndex = TranslateIndex(e.OldStartingIndex);
                        var movedActiveExpressions = activeExpressions.GetRange(e.OldStartingIndex, e.OldItems.Count);
                        activeExpressions.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                        activeExpressions.InsertRange(e.NewStartingIndex, movedActiveExpressions);
                        var newStartingIndex = TranslateIndex(e.NewStartingIndex);
                        if (oldStartingIndex != newStartingIndex)
                        {
                            var movedItems = movedActiveExpressions.Where(ae => ae.Value).Select(ae => ae.Arg).ToList().AsReadOnly();
                            if (movedItems.Count > 0)
                                eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, movedItems, newStartingIndex, oldStartingIndex);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var activeExpression in activeExpressionCounts.Keys)
                    {
                        activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                        activeExpression.PropertyChanging -= ActiveExpressionPropertyChanging;
                        activeExpression.Dispose();
                    }
                    activeExpressions.Clear();
                    activeExpressionCounts.Clear();
                    foreach (var element in source)
                    {
                        var activeExpression = ActiveExpression.Create(predicate, element, predicateOptions);
                        activeExpressions.Add(activeExpression);
                        if (activeExpression.Value)
                            ++newCount;
                        if (activeExpressionCounts.TryGetValue(activeExpression, out var existingCount))
                            activeExpressionCounts[activeExpression] = existingCount + 1;
                        else
                        {
                            activeExpressionCounts.Add(activeExpression, 1);
                            activeExpression.PropertyChanged += ActiveExpressionPropertyChanged;
                            activeExpression.PropertyChanging += ActiveExpressionPropertyChanging;
                        }
                    }
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

    int TranslateIndex(int index) =>
        index - activeExpressions.Take(index).Count(ae => !ae.Value);
}
