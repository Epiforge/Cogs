namespace Cogs.ActiveQuery;

/// <summary>
/// Represents a read-only collection of elements that is the result of calling <see cref="ActiveEnumerableExtensions.ActiveWhere{TSource}(IEnumerable{TSource}, Expression{Func{TSource, bool}})"/> or <see cref="ActiveEnumerableExtensions.ActiveWhere{TSource}(IEnumerable{TSource}, Expression{Func{TSource, bool}}, ActiveExpressionOptions?)"/>
/// </summary>
/// <typeparam name="TElement">The type of the elements in the sequence</typeparam>
sealed class ActiveWhereEnumerable<TElement> :
    DisposableValuesCache<(IEnumerable<TElement> source, Expression<Func<TElement, bool>> predicate, ActiveExpressionOptions? predicateOptions), ActiveWhereEnumerable<TElement>>.Value,
    IActiveEnumerable<TElement>,
    IObserveActiveExpressions<bool>
{
    object? access;
    Dictionary<IActiveExpression<TElement, bool>, int>? activeExpressionCounts;
    List<IActiveExpression<TElement, bool>>? activeExpressions;
    int count;

    public TElement this[int index] =>
        this.Execute(() =>
        {
            lock (access!)
            {
                for (var i = 0; i < activeExpressions!.Count; ++i)
                {
                    var activeExpression = activeExpressions[i];
                    if (!activeExpression.Value)
                        continue;
                    if (--index == -1)
                        return activeExpression.Arg;
                }
                throw new ArgumentOutOfRangeException(nameof(index));
            }
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

    void IObserveActiveExpressions<bool>.ActiveExpressionChanged(IObservableActiveExpression<bool> activeExpression, bool oldValue, bool newValue, Exception? oldFault, Exception? newFault)
    {
        if (activeExpression is IActiveExpression<TElement, bool> typedActiveExpression)
            this.Execute(() =>
            {
                lock (access!)
                {
                    if (!ReferenceEquals(oldFault, newFault))
                        ElementFaultChanged?.Invoke(this, new ElementFaultChangeEventArgs(typedActiveExpression.Arg, newFault, activeExpressionCounts![typedActiveExpression]));
                    if (oldValue != newValue)
                    {
                        var action = newValue ? NotifyCollectionChangedAction.Add : NotifyCollectionChangedAction.Remove;
                        var countIteration = newValue ? 1 : -1;
                        var translatedIndex = -1;
                        for (int i = 0, ii = activeExpressions!.Count; i < ii; ++i)
                        {
                            var iActiveExpression = activeExpressions[i];
                            if (iActiveExpression.Value)
                                ++translatedIndex;
                            if (iActiveExpression == activeExpression)
                            {
                                Count += countIteration;
                                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, typedActiveExpression.Arg, translatedIndex));
                            }
                        }
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
            if (activeExpressions is not null && value is bool result)
            {
                for (int i = 0, ii = activeExpressions.Count; i < ii; ++i)
                    if (activeExpressions[i].Value == result)
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

    public IEnumerator<TElement> GetEnumerator() =>
        this.Execute(() => GetEnumeratorInContext());

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    IEnumerator<TElement> GetEnumeratorInContext()
    {
        lock (access!)
            foreach (var activeExpression in activeExpressions!)
                if (activeExpression.Value)
                    yield return activeExpression.Arg;
    }

    int IList.IndexOf(object value) =>
        this.Execute(() =>
        {
            if (activeExpressions is not null && value is bool result)
            {
                for (int i = 0, ii = activeExpressions.Count; i < ii; ++i)
                    if (activeExpressions[i].Value == result)
                        return i;
            }
            return -1;
        });

    void IList.Insert(int index, object value) =>
        throw new NotSupportedException();

    protected override void OnInitialized()
    {
        access = new object();
        var (source, predicate, predicateOptions) = Key;
        SynchronizationContext = (source as ISynchronized)?.SynchronizationContext;
        (activeExpressions, activeExpressionCounts, count) = this.Execute(() =>
        {
            lock (access)
            {
                var activeExpressions = new List<IActiveExpression<TElement, bool>>();
                var activeExpressionCounts = new Dictionary<IActiveExpression<TElement, bool>, int>();
                var count = 0;

                void processElement(TElement element)
                {
                    var activeExpression = ActiveExpression.Create(predicate, element, predicateOptions);
                    activeExpressions!.Add(activeExpression);
                    if (activeExpression.Value)
                        ++count;
                    if (activeExpressionCounts!.TryGetValue(activeExpression, out var existingCount))
                        activeExpressionCounts[activeExpression] = existingCount + 1;
                    else
                    {
                        activeExpressionCounts.Add(activeExpression, 1);
                        activeExpression.AddActiveExpressionOserver(this);
                    }
                }

                if (source is IList<TElement> sourceList)
                    for (int i = 0, ii = sourceList.Count; i < ii; ++i)
                        processElement(sourceList[i]);
                else
                    foreach (var element in source)
                        processElement(element);
                if (source is INotifyCollectionChanged collectionChangeNotifier)
                    collectionChangeNotifier.CollectionChanged += SourceChanged;
                return (activeExpressions, activeExpressionCounts, count);
            }
        });
    }

    protected override void OnTerminated() =>
        this.Execute(() =>
        {
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
        });

    void IList.Remove(object value) =>
        throw new NotSupportedException();

    void IList.RemoveAt(int index) =>
        throw new NotSupportedException();

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity", Justification = @"Splitting this up into more methods is ¯\_(ツ)_/¯")]
    [SuppressMessage("Reliability", "CA2000: Dispose objects before losing scope", Justification = "They'll get disposed, chill out")]
    void SourceChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        var (source, predicate, predicateOptions) = Key;
        lock (access!)
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
                            var activeExpression = activeExpressions![e.OldStartingIndex + i];
                            activeExpressions.RemoveAt(e.OldStartingIndex + i);
                            var activeExpressionCount = activeExpressionCounts![activeExpression];
                            if (activeExpressionCount > 1)
                                activeExpressionCounts[activeExpression] = activeExpressionCount - 1;
                            else
                            {
                                activeExpressionCounts.Remove(activeExpression);
                                activeExpression.RemoveActiveExpressionObserver(this);
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
                            activeExpressions!.Insert(e.NewStartingIndex + i, activeExpression);
                            if (activeExpressionCounts!.TryGetValue(activeExpression, out var existingCount))
                                activeExpressionCounts[activeExpression] = existingCount + 1;
                            else
                            {
                                activeExpressionCounts.Add(activeExpression, 1);
                                activeExpression.AddActiveExpressionOserver(this);
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
                        var movedActiveExpressions = activeExpressions!.GetRange(e.OldStartingIndex, e.OldItems.Count);
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
                    foreach (var activeExpression in activeExpressionCounts!.Keys)
                    {
                        activeExpression.RemoveActiveExpressionObserver(this);
                        for (int i = 0, ii = activeExpressionCounts[activeExpression]; i < ii; ++i)
                            activeExpression.Dispose();
                    }
                    activeExpressions!.Clear();
                    activeExpressionCounts.Clear();

                    void processElement(TElement element)
                    {
                        var activeExpression = ActiveExpression.Create(predicate!, element, predicateOptions);
                        activeExpressions!.Add(activeExpression);
                        if (activeExpression.Value)
                            ++newCount;
                        if (activeExpressionCounts!.TryGetValue(activeExpression, out var existingCount))
                            activeExpressionCounts[activeExpression] = existingCount + 1;
                        else
                        {
                            activeExpressionCounts.Add(activeExpression, 1);
                            activeExpression.AddActiveExpressionOserver(this);
                        }
                    }

                    if (source is IList<TElement> sourceList)
                        for (int i = 0, ii = sourceList.Count; i < ii; ++i)
                            processElement(sourceList[i]);
                    else
                        foreach (var element in source)
                            processElement(element);
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

    static readonly DisposableValuesCache<(IEnumerable<TElement> source, Expression<Func<TElement, bool>> predicate, ActiveExpressionOptions? predicateOptions), ActiveWhereEnumerable<TElement>> cache = new(new EqualityComparer());

    internal static IActiveEnumerable<TElement> Get(IEnumerable<TElement> source, Expression<Func<TElement, bool>> predicate, ActiveExpressionOptions? predicateOptions)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (predicate is null)
            throw new ArgumentNullException(nameof(predicate));
        return cache[(source, predicate, predicateOptions)];
    }

    class EqualityComparer :
        IEqualityComparer<(IEnumerable<TElement> source, Expression<Func<TElement, bool>> predicate, ActiveExpressionOptions? predicateOptions)>
    {
        public bool Equals((IEnumerable<TElement> source, Expression<Func<TElement, bool>> predicate, ActiveExpressionOptions? predicateOptions) x, (IEnumerable<TElement> source, Expression<Func<TElement, bool>> predicate, ActiveExpressionOptions? predicateOptions) y) =>
            ReferenceEquals(x.source, y.source) && ExpressionEqualityComparer.Default.Equals(x.predicate, y.predicate) && (x.predicateOptions is null && y.predicateOptions is null || x.predicateOptions is not null && y.predicateOptions is not null && x.predicateOptions.Equals(y.predicateOptions));

        public int GetHashCode((IEnumerable<TElement> source, Expression<Func<TElement, bool>> predicate, ActiveExpressionOptions? predicateOptions) obj) =>
            HashCode.Combine(obj.source, ExpressionEqualityComparer.Default.GetHashCode(obj.predicate), obj.predicateOptions);
    }
}
