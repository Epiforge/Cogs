namespace Cogs.ActiveQuery;

sealed class ActiveOrderingComparer<TElement> :
    SyncDisposable,
    IComparer<TElement>
{
    static readonly EqualityComparer<TElement> equalityComparer = EqualityComparer<TElement>.Default;

    public ActiveOrderingComparer(IReadOnlyList<(EnumerableRangeActiveExpression<TElement, IComparable> rangeActiveExpression, bool isDescending)> selectors, IndexingStrategy indexingStrategy)
    {
        this.indexingStrategy = indexingStrategy;
        switch (this.indexingStrategy)
        {
            case IndexingStrategy.HashTable:
                comparables = new NullableKeyDictionary<TElement, List<IComparable?>>();
                counts = new NullableKeyDictionary<TElement, int>();
                break;
            case IndexingStrategy.SelfBalancingBinarySearchTree:
                comparables = new NullableKeySortedDictionary<TElement, List<IComparable?>>();
                counts = new NullableKeySortedDictionary<TElement, int>();
                break;
        }

        lock (comparablesAccess)
        {
            this.selectors = selectors.ToImmutableArray();
            if (this.indexingStrategy != IndexingStrategy.NoneOrInherit)
            {
                foreach (var (rangeActiveExpression, isDescending) in this.selectors)
                {
                    rangeActiveExpression.ElementResultChanged += RangeActiveExpressionElementResultChanged;
                    rangeActiveExpression.CollectionChanged += RangeActiveExpressionCollectionChanged;
                }
                lastSelector = this.selectors[this.selectors.Count - 1];
                rangeActiveExpressionIndicies = new Dictionary<EnumerableRangeActiveExpression<TElement, IComparable>, int>();
                var index = -1;
                foreach (var (rangeActiveExpression, isDescending) in this.selectors.Take(1))
                {
                    rangeActiveExpressionIndicies.Add(rangeActiveExpression, ++index);
                    foreach (var elementAndResults in rangeActiveExpression.GetResults().GroupBy(er => er.element, er => er.result))
                    {
                        var element = elementAndResults.Key;
                        var elementComparables = new List<IComparable?>();
                        comparables!.Add(element, elementComparables);
                        elementComparables.Add(elementAndResults.First());
                        counts!.Add(element, elementAndResults.Count());
                    }
                }
                foreach (var (rangeActiveExpression, isDescending) in this.selectors.Skip(1))
                {
                    rangeActiveExpressionIndicies.Add(rangeActiveExpression, ++index);
                    foreach (var elementAndResults in rangeActiveExpression.GetResults().GroupBy(er => er.element, er => er.result))
                        comparables![elementAndResults.Key].Add(elementAndResults.First());
                }
            }
        }
    }

    IDictionary<TElement, List<IComparable?>>? comparables;
    readonly object comparablesAccess = new();
    IDictionary<TElement, int>? counts;
    readonly IndexingStrategy indexingStrategy;
    readonly (EnumerableRangeActiveExpression<TElement, IComparable> rangeActiveExpression, bool isDescending) lastSelector;
    Dictionary<EnumerableRangeActiveExpression<TElement, IComparable>, int>? rangeActiveExpressionIndicies;
    readonly IReadOnlyList<(EnumerableRangeActiveExpression<TElement, IComparable> rangeActiveExpression, bool isDescending)> selectors;

    public int Compare(TElement x, TElement y)
    {
        IReadOnlyList<IComparable?> xList, yList;
        if (indexingStrategy == IndexingStrategy.NoneOrInherit)
        {
            xList = GetComparables(x);
            yList = GetComparables(y);
        }
        else
        {
            if (comparables!.TryGetValue(x, out var rawXList))
                xList = rawXList;
            else
                xList = Enumerable.Range(0, selectors.Count).Select(i => (IComparable?)null).ToImmutableArray();
            if (comparables!.TryGetValue(y, out var rawYList))
                yList = rawYList;
            else
                yList = Enumerable.Range(0, selectors.Count).Select(i => (IComparable?)null).ToImmutableArray();
        }
        for (var i = 0; i < selectors.Count; ++i)
        {
            var isDescending = selectors[i].isDescending;
            var xComparable = xList[i];
            var yComparable = yList[i];
            if (xComparable is null)
                return yComparable is null ? 0 : isDescending ? 1 : -1;
            else if (yComparable is null)
                return isDescending ? -1 : 1;
            var comparison = xComparable.CompareTo(yComparable);
            if (comparison != 0)
                return comparison * (isDescending ? -1 : 1);
        }
        return 0;
    }

    protected override bool Dispose(bool disposing)
    {
        if (indexingStrategy != IndexingStrategy.NoneOrInherit)
            foreach (var (rangeActiveExpression, isDescending) in selectors)
            {
                rangeActiveExpression.ElementResultChanged -= RangeActiveExpressionElementResultChanged;
                rangeActiveExpression.CollectionChanged -= RangeActiveExpressionCollectionChanged;
            }
        return true;
    }

    IReadOnlyList<IComparable?> GetComparables(TElement element) =>
        selectors.Select(expressionAndOrder => expressionAndOrder.rangeActiveExpression.GetResultsUnderLock().First(er => equalityComparer.Equals(er.element, element)).result).ToImmutableArray();

    void RangeActiveExpressionElementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TElement, IComparable?> e)
    {
        lock (comparablesAccess)
            if (comparables!.TryGetValue(e.Element, out var comparablesForElement))
                comparablesForElement[rangeActiveExpressionIndicies![(EnumerableRangeActiveExpression<TElement, IComparable>)sender]] = e.Result!;
    }

    void RangeActiveExpressionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        lock (comparablesAccess)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset && sender == lastSelector.rangeActiveExpression)
            {
                switch (indexingStrategy)
                {
                    case IndexingStrategy.HashTable:
                        comparables = new NullableKeyDictionary<TElement, List<IComparable?>>();
                        counts = new NullableKeyDictionary<TElement, int>();
                        break;
                    case IndexingStrategy.SelfBalancingBinarySearchTree:
                        comparables = new NullableKeySortedDictionary<TElement, List<IComparable?>>();
                        counts = new NullableKeySortedDictionary<TElement, int>();
                        break;
                }
                rangeActiveExpressionIndicies = new Dictionary<EnumerableRangeActiveExpression<TElement, IComparable>, int>();
                var index = -1;
                foreach (var (rangeActiveExpression, isDescending) in selectors.Take(1))
                {
                    rangeActiveExpressionIndicies.Add(rangeActiveExpression, ++index);
                    foreach (var elementAndResults in rangeActiveExpression.GetResults().GroupBy(er => er.element, er => er.result))
                    {
                        var element = elementAndResults.Key;
                        var elementComparables = new List<IComparable?>();
                        comparables!.Add(element, elementComparables);
                        elementComparables.Add(elementAndResults.First());
                        counts!.Add(element, elementAndResults.Count());
                    }
                }
                foreach (var (rangeActiveExpression, isDescending) in selectors.Skip(1))
                {
                    rangeActiveExpressionIndicies.Add(rangeActiveExpression, ++index);
                    foreach (var elementAndResults in rangeActiveExpression.GetResults().GroupBy(er => er.element, er => er.result))
                        comparables![elementAndResults.Key].Add(elementAndResults.First());
                }
            }
            else if (e.Action != NotifyCollectionChangedAction.Move)
            {
                if ((e.OldItems?.Count ?? 0) > 0 && sender == lastSelector.rangeActiveExpression)
                {
                    foreach (var elementAndResults in e.OldItems.Cast<(TElement element, IComparable? comparable)>().GroupBy(er => er.element, er => er.comparable))
                    {
                        var element = elementAndResults.Key;
                        var currentCount = counts![element];
                        var removedCount = elementAndResults.Count();
                        if (currentCount - removedCount == 0)
                        {
                            counts.Remove(element);
                            comparables!.Remove(element);
                        }
                        else
                            counts[element] = currentCount - removedCount;
                    }
                }
                if ((e.NewItems?.Count ?? 0) > 0)
                {
                    var rangeActiveExpressionIndex = rangeActiveExpressionIndicies![(EnumerableRangeActiveExpression<TElement, IComparable>)sender];
                    if (rangeActiveExpressionIndex == 0)
                        foreach (var elementAndResults in e.NewItems.Cast<(TElement element, IComparable? comparable)>().GroupBy(er => er.element, er => er.comparable))
                        {
                            var element = elementAndResults.Key;
                            var count = elementAndResults.Count();
                            if (!comparables!.TryGetValue(element, out var elementComparables))
                            {
                                elementComparables = new List<IComparable?>();
                                comparables.Add(elementAndResults.Key, elementComparables);
                                elementComparables.Add(elementAndResults.First());
                                counts!.Add(element, count);
                            }
                            else
                                counts![element] += count;
                        }
                    else
                        foreach (var elementAndResults in e.NewItems.Cast<(TElement element, IComparable? comparable)>().GroupBy(er => er.element, er => er.comparable))
                        {
                            var comparablesList = comparables![elementAndResults.Key];
                            if (comparablesList.Count == rangeActiveExpressionIndex)
                                comparablesList.Add(elementAndResults.First());
                        }
                }
            }
        }
    }
}
