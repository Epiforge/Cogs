using System;
using System.Collections.Generic;
using System.Text;

namespace Cogs.ActiveQuery;

sealed class ActiveSelectManyEnumerable<TSource, TResult> :
    DisposableValuesCache<(IEnumerable<TSource> source, Expression<Func<TSource, IEnumerable<TResult>>> selector, ActiveExpressionOptions? selectorOptions, bool parallel), ActiveSelectManyEnumerable<TSource, TResult>>.Value,
    IActiveEnumerable<TResult>,
    IObserveActiveExpressions<IEnumerable<TResult>>
{
    object? access;
    Dictionary<IActiveExpression<TSource, IEnumerable<TResult>>, int>? activeExpressionCounts;
    List<IActiveExpression<TSource, IEnumerable<TResult>>>? activeExpressions;
    int count;

    public TResult this[int index] =>
        this.Execute(() =>
        {
            lock (access!)
            {
                for (int i = 0, ii = activeExpressions!.Count; i < ii; ++i)
                {
                    var enumerable = activeExpressions[i].Value;
                    var enumerableCount = enumerable.Count();
                    if (index >= enumerableCount)
                    {
                        index -= enumerableCount;
                        continue;
                    }
                    return enumerable.ElementAt(index);
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
    public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanging;

    void IObserveActiveExpressions<IEnumerable<TResult>>.ActiveExpressionChanged(IObservableActiveExpression<IEnumerable<TResult>> activeExpression, IEnumerable<TResult>? oldValue, IEnumerable<TResult>? newValue, Exception? oldFault, Exception? newFault)
    {
        if (activeExpression is IActiveExpression<TSource, IEnumerable<TResult>> typedActiveExpression)
            this.Execute(() =>
            {
                lock (access!)
                {
                    if (!ReferenceEquals(oldFault, newFault))
                        ElementFaultChanged?.Invoke(this, new ElementFaultChangeEventArgs(typedActiveExpression.Arg, newFault, activeExpressionCounts![typedActiveExpression]));
                    if (!EqualityComparer<IEnumerable<TResult>>.Default.Equals(oldValue!, newValue!))
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

    public bool Contains(object value) =>
        this.Execute(() =>
        {
            lock (access!)
            {
                if (activeExpressions is not null && value is TResult result)
                {
                    var comparer = EqualityComparer<TResult>.Default;
                    for (int i = 0, ii = activeExpressions.Count; i < ii; ++i)
                    {
                        var activeExpression = activeExpressions[i];
                        if (activeExpression.Value is { } enumerable)
                            for (int j = 0, jj = enumerable.Count(); j < jj; ++j)
                                if (comparer.Equals(enumerable.ElementAt(j), result))
                                    return true;
                    }
                }
                return false;
            }
        });

    public void CopyTo(Array array, int index) =>
        this.Execute(() =>
        {
            lock (access!)
            {
                if (activeExpressions is null)
                    return;
                var index = -1;
                for (int i = 0, ii = activeExpressions.Count; i < ii; ++i)
                {
                    var activeExpression = activeExpressions[i];
                    if (activeExpression.Value is { } enumerable)
                        for (int j = 0, jj = enumerable.Count(); j < jj; ++j)
                        {
                            if (index + 1 >= array.Length)
                                break;
                            array.SetValue(enumerable.ElementAt(j), ++index);
                        }
                }
            }
        });

    public IReadOnlyList<(object? element, Exception? fault)> GetElementFaults() =>
        this.Execute(() =>
        {
            lock (access!)
            {
                var result = new List<(object? element, Exception? fault)>();
                foreach (var activeExpression in activeExpressionCounts!.Keys)
                {
                    if (activeExpression.Fault is { } fault)
                        result.Add((activeExpression.Arg, fault));
                    if (activeExpression.Value is INotifyElementFaultChanges faultNotifier)
                        result.AddRange(faultNotifier.GetElementFaults());
                }
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
                foreach (var element in activeExpression.Value ?? Enumerable.Empty<TResult>())
                    yield return element;
    }

    int IList.IndexOf(object value) =>
        this.Execute(() =>
        {
            lock (access!)
            {
                if (activeExpressions is not null && value is TResult result)
                {
                    var comparer = EqualityComparer<TResult>.Default;
                    var index = -1;
                    for (int i = 0, ii = activeExpressions.Count; i < ii; ++i)
                    {
                        var activeExpression = activeExpressions[i];
                        if (activeExpression.Value is { } enumerable)
                            for (int j = 0, jj = enumerable.Count(); j < jj; ++j)
                            {
                                ++index;
                                if (comparer.Equals(enumerable.ElementAt(j), result))
                                    return index;
                            }
                    }
                }
                return -1;
            }
        });

    void IList.Insert(int index, object value) =>
        throw new NotSupportedException();

    protected override void OnInitialized()
    {
        access = new();
        var (source, selector, selectorOptions, parallel) = Key;
        SynchronizationContext = (source as ISynchronized)?.SynchronizationContext;
        (activeExpressions, activeExpressionCounts, count) = this.Execute(() =>
        {
            lock (access)
            {
                List<IActiveExpression<TSource, IEnumerable<TResult>>> activeExpressions;
                if (parallel)
                    activeExpressions = source.DataflowSelectAsync(element => (IActiveExpression<TSource, IEnumerable<TResult>>)ActiveExpression.Create(selector, element, selectorOptions)).Result.ToList();
                else if (source is IList<TSource> sourceList)
                {
                    activeExpressions = new List<IActiveExpression<TSource, IEnumerable<TResult>>>();
                    for (int i = 0, ii = sourceList.Count; i < ii; ++i)
                        activeExpressions.Add(ActiveExpression.Create(selector, sourceList[i], selectorOptions));
                }
                else
                {
                    activeExpressions = new List<IActiveExpression<TSource, IEnumerable<TResult>>>();
                    foreach (var element in source)
                        activeExpressions.Add(ActiveExpression.Create(selector, element, selectorOptions));
                }
                var activeExpressionCounts = new Dictionary<IActiveExpression<TSource, IEnumerable<TResult>>, int>();
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
                if (source is INotifyCollectionChanged collectionChangeNotifier)
                    collectionChangeNotifier.CollectionChanged += SourceChanged;
                return (activeExpressions, activeExpressionCounts, activeExpressions.Sum(activeExpression => activeExpression.Value.Count()));
            }
        });
    }

    protected override void OnTerminated() => throw new NotImplementedException();

    void IList.Remove(object value) =>
        throw new NotSupportedException();

    void IList.RemoveAt(int index) =>
        throw new NotSupportedException();

    void SourceChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        throw new NotImplementedException();
    }
}
