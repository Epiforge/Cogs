namespace Cogs.ActiveQuery;

/// <summary>
/// Provides a set of <c>static</c> (<c>Shared</c> in Visual Basic) methods for actively querying objects that implement <see cref="IEnumerable{T}"/>
/// </summary>
[SuppressMessage("Code Analysis", "CA1502: Avoid excessive complexity")]
[SuppressMessage("Code Analysis", "CA1506: Avoid excessive class coupling")]
public static class ActiveEnumerableExtensions
{
    #region Aggregate

    /// <summary>
    /// Actively applies an accumulator function over a sequence
    /// </summary>
    /// <typeparam name="TElement">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TAccumulate">The type of the accumulator value</typeparam>
    /// <typeparam name="TResult">The type of the resulting value</typeparam>
    /// <param name="source">An <see cref="IActiveEnumerable{TElement}"/> to aggregate over</param>
    /// <param name="seedFactory">A method to produce the initial accumulator value when the sequence changes</param>
    /// <param name="func">An accumulator method to be invoked on each element</param>
    /// <param name="resultSelector">A method to transform the final accumulator value into the result value</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the transformed final accumulator value</returns>
    public static IActiveValue<TResult?> ActiveAggregate<TElement, TAccumulate, TResult>(this IActiveEnumerable<TElement> source, Func<TAccumulate> seedFactory, Func<TAccumulate, TElement, TAccumulate> func, Func<TAccumulate, TResult> resultSelector)
    {
        var changeNotifyingSource = source as INotifyCollectionChanged;
        ActiveValue<TResult?>? activeValue = null;

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                activeValue!.OperationFault = null;
                activeValue!.Value = source.Aggregate(seedFactory(), func, resultSelector);
            }
            catch (Exception ex)
            {
                activeValue!.Value = default;
                activeValue!.OperationFault = ex;
            }
        }

        return source.Execute(() =>
        {
            void dispose()
            {
                if (changeNotifyingSource is { })
                    changeNotifyingSource.CollectionChanged -= collectionChanged;
            }

            try
            {
                activeValue = new ActiveValue<TResult?>(source.Aggregate(seedFactory(), func, resultSelector), elementFaultChangeNotifier: source, onDispose: dispose);
            }
            catch (Exception ex)
            {
                activeValue = new ActiveValue<TResult?>(default, ex, source, dispose);
            }
            if (changeNotifyingSource is { })
                changeNotifyingSource.CollectionChanged += collectionChanged;
            return activeValue;
        });
    }

    #endregion

    #region All

    /// <summary>
    /// Actively determines whether all elements of a sequence satisfy a condition
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> that contains the elements to apply the predicate to</param>
    /// <param name="predicate">A function to test each element for a condition</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>true</c> when every element of the source sequence passes the test in the specified predicate, or if the sequence is empty; otherwise, <c>false</c></returns>
    public static IActiveValue<bool> ActiveAll<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, bool>> predicate) =>
        ActiveAll(source, predicate, null);

    /// <summary>
    /// Actively determines whether all elements of a sequence satisfy a condition
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> that contains the elements to apply the predicate to</param>
    /// <param name="predicate">A function to test each element for a condition</param>
    /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>true</c> when every element of the source sequence passes the test in the specified predicate, or if the sequence is empty; otherwise, <c>false</c></returns>
    public static IActiveValue<bool> ActiveAll<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions? predicateOptions)
    {
        var readOnlySource = source as IReadOnlyCollection<TSource>;
        var changeNotifyingSource = source as INotifyCollectionChanged;
        IActiveEnumerable<TSource> where;
        ActiveValue<bool>? activeValue = null;

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            activeValue!.Value = where.Count == (readOnlySource?.Count ?? source.Count());

        return (source as ISynchronized).Execute(() =>
        {
            where = ActiveWhere(source, predicate, predicateOptions);
            activeValue = new ActiveValue<bool>(where.Count == (readOnlySource?.Count ?? source.Count()), elementFaultChangeNotifier: where, onDispose: () =>
            {
                where.CollectionChanged -= collectionChanged;
                if (changeNotifyingSource is { })
                    changeNotifyingSource.CollectionChanged -= collectionChanged;
                where.Dispose();
            });
            where.CollectionChanged += collectionChanged;
            if (changeNotifyingSource is { })
                changeNotifyingSource.CollectionChanged += collectionChanged;
            return activeValue;
        });
    }

    #endregion All

    #region Any

    /// <summary>
    /// Actively determines whether a sequence contains any elements
    /// </summary>
    /// <param name="source">The <see cref="IEnumerable"/> to check for emptiness</param>
    /// <returns>>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>true</c> if the source sequence contains any elements; otherwise, <c>false</c></returns>
    public static IActiveValue<bool> ActiveAny(this IEnumerable source)
    {
        var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
        if (source is INotifyCollectionChanged changeNotifyingSource)
        {
            var synchronizedSource = source as ISynchronized;
            ActiveValue<bool>? activeValue = null;

            void sourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizedSource.Execute(() => activeValue!.Value = source.Cast<object>().Any());

            return synchronizedSource.Execute(() =>
            {
                activeValue = new ActiveValue<bool>(source.Cast<object>().Any(), elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changeNotifyingSource.CollectionChanged -= sourceCollectionChanged);
                changeNotifyingSource.CollectionChanged += sourceCollectionChanged;
                return activeValue;
            });
        }
        try
        {
            return new ActiveValue<bool>(source.Cast<object>().Any(), elementFaultChangeNotifier: elementFaultChangeNotifier);
        }
        catch (Exception ex)
        {
            return new ActiveValue<bool>(default, ex, elementFaultChangeNotifier);
        }
    }

    /// <summary>
    /// Actively determines whether any element of a sequence satisfies a condition
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> that contains elements to apply the predicate to</param>
    /// <param name="predicate">A function to test each element for a condition</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>true</c> if any elements in the source sequence pass the test in the specified predicate; otherwise, <c>false</c></returns>
    public static IActiveValue<bool> ActiveAny<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, bool>> predicate) =>
        ActiveAny(source, predicate, null);

    /// <summary>
    /// Actively determines whether any element of a sequence satisfies a condition
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> that contains elements to apply the predicate to</param>
    /// <param name="predicate">A function to test each element for a condition</param>
    /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>true</c> if any elements in the source sequence pass the test in the specified predicate; otherwise, <c>false</c></returns>
    public static IActiveValue<bool> ActiveAny<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions? predicateOptions)
    {
        IActiveEnumerable<TSource> where;
        ActiveValue<bool>? activeValue = null;

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            activeValue!.Value = where.Count > 0;

        return (source as ISynchronized).Execute(() =>
        {
            where = ActiveWhere(source, predicate, predicateOptions);
            activeValue = new ActiveValue<bool>(where.Count > 0, elementFaultChangeNotifier: where, onDispose: () =>
            {
                where.CollectionChanged -= collectionChanged;
                where.Dispose();
            });
            where.CollectionChanged += collectionChanged;
            return activeValue;
        });
    }

    #endregion Any

    #region Average

    /// <summary>
    /// Actively computes the average of a sequence of values
    /// </summary>
    /// <typeparam name="TSource">The type of the values</typeparam>
    /// <param name="source">A sequence of values to calculate the average of</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the average of the sequence of values</returns>
    public static IActiveValue<TSource?> ActiveAverage<TSource>(this IEnumerable<TSource> source) =>
        ActiveAverage(source, element => element);

    /// <summary>
    /// Actively computes the average of a sequence of values that are obtained by invoking a transform function on each element of the input sequence
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TResult">The type of the values being averaged</typeparam>
    /// <param name="source">A sequence of values to calculate the average of</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the average of the sequence of values</returns>
    public static IActiveValue<TResult?> ActiveAverage<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector) =>
        ActiveAverage(source, selector, null);

    /// <summary>
    /// Actively computes the average of a sequence of values that are obtained by invoking a transform function on each element of the input sequence
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TResult">The type of the values being averaged</typeparam>
    /// <param name="source">A sequence of values to calculate the average of</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the average of the sequence of values</returns>
    public static IActiveValue<TResult?> ActiveAverage<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions)
    {
        ActiveQueryOptions.Optimize(ref selector);

        var readOnlyCollection = source as IReadOnlyCollection<TSource>;
        var synchronizedSource = source as ISynchronized;
        var convertCount = CountConversion.GetConverter(typeof(TResult));
        var operations = new GenericOperations<TResult>();
        IActiveValue<TResult?> sum;
        ActiveValue<TResult?>? activeValue = null;

        int count() =>
            readOnlyCollection?.Count ?? source.Count();

        void propertyChanged(object sender, PropertyChangedEventArgs e) =>
            synchronizedSource.Execute(() =>
            {
                if (e.PropertyName == nameof(ActiveValue<TResult>.Value))
                {
                    var currentCount = count();
                    if (currentCount == 0)
                    {
                        activeValue!.Value = default;
                        activeValue!.OperationFault = ExceptionHelper.SequenceContainsNoElements;
                    }
                    else
                    {
                        activeValue!.OperationFault = null;
                        activeValue!.Value = operations!.Divide(sum.Value, (TResult)convertCount!(currentCount));
                    }
                }
            });

        return synchronizedSource.Execute(() =>
        {
            sum = ActiveSum(source, selector, selectorOptions);
            var currentCount = count();
            activeValue = new ActiveValue<TResult?>(currentCount > 0 ? operations.Divide(sum.Value, (TResult)convertCount!(currentCount)) : default, currentCount == 0 ? ExceptionHelper.SequenceContainsNoElements : null, sum, () =>
            {
                sum.PropertyChanged -= propertyChanged;
                sum.Dispose();
            });
            sum.PropertyChanged += propertyChanged;
            return activeValue;
        });
    }

    #endregion Average

    #region Cast

    /// <summary>
    /// Actively casts the elements of an <see cref="IEnumerable"/> to the specified type
    /// </summary>
    /// <typeparam name="TResult">The type to cast the elements of <paramref name="source"/> to</typeparam>
    /// <param name="source">The <see cref="IEnumerable"/> that contains the elements to be cast to type <typeparamref name="TResult"/></param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that contains each element of the source sequence cast to the specified type</returns>
    public static IActiveEnumerable<TResult?> ActiveCast<TResult>(this IEnumerable source) =>
        ActiveCast<TResult>(source, null);

    /// <summary>
    /// Actively casts the elements of an <see cref="IEnumerable"/> to the specified type
    /// </summary>
    /// <typeparam name="TResult">The type to cast the elements of <paramref name="source"/> to</typeparam>
    /// <param name="source">The <see cref="IEnumerable"/> that contains the elements to be cast to type <typeparamref name="TResult"/></param>
    /// <param name="castOptions">Options governing the behavior of active expressions created to perform the cast</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that contains each element of the source sequence cast to the specified type</returns>
    public static IActiveEnumerable<TResult?> ActiveCast<TResult>(this IEnumerable source, ActiveExpressionOptions? castOptions) =>
        ActiveSelect(source, element => (TResult)element!, castOptions);

    #endregion

    #region Concat

    /// <summary>
    /// Actively concatenates two sequences
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of the input sequences</typeparam>
    /// <param name="first">The first sequence to concatenate</param>
    /// <param name="second">The sequence to concatenate to the first sequence</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that contains the concatenated elements of the two input sequences</returns>
    public static IActiveEnumerable<TSource> ActiveConcat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second) =>
        new ActiveConcatEnumerable<TSource>(first, second);

    /// <summary>
    /// Actively concatenates two sequences on the specified <see cref="SynchronizationContext"/>
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of the input sequences</typeparam>
    /// <param name="first">The first sequence to concatenate</param>
    /// <param name="second">The sequence to concatenate to the first sequence</param>
    /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform operations</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that contains the concatenated elements of the two input sequences</returns>
    public static IActiveEnumerable<TSource> ActiveConcat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, SynchronizationContext synchronizationContext) =>
        new ActiveConcatEnumerable<TSource>(first, second, synchronizationContext);

    #endregion Concat

    #region Count

    /// <summary>
    /// Actively determines the number of elements in a sequence
    /// </summary>
    /// <param name="source">The <see cref="IEnumerable"/> from which to count elements</param>
    /// <returns>An active value the value of which is <c>true</c> if the source sequence contains any elements; otherwise, <c>false</c></returns>
    public static IActiveValue<int> ActiveCount(this IEnumerable source)
    {
        var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
        if (source is INotifyCollectionChanged changeNotifyingSource)
        {
            var synchronizedSource = source as ISynchronized;
            ActiveValue<int>? activeValue = null;

            void sourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizedSource.Execute(() => activeValue!.Value = source.Cast<object>().Count());

            return synchronizedSource.Execute(() =>
            {
                activeValue = new ActiveValue<int>(source.Cast<object>().Count(), elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changeNotifyingSource.CollectionChanged -= sourceCollectionChanged);
                changeNotifyingSource.CollectionChanged += sourceCollectionChanged;
                return activeValue;
            });
        }
        try
        {
            return new ActiveValue<int>(source.Cast<object>().Count(), elementFaultChangeNotifier: elementFaultChangeNotifier);
        }
        catch (Exception ex)
        {
            return new ActiveValue<int>(default, ex, elementFaultChangeNotifier);
        }
    }

    /// <summary>
    /// Actively determines the number of elements in a sequence
    /// </summary>
    /// <param name="source">The <see cref="IEnumerable"/> from which to count elements</param>
    /// <returns>An active value the value of which is <c>true</c> if the source sequence contains any elements; otherwise, <c>false</c></returns>
    public static IActiveValue<int> ActiveCount<TSource>(this IEnumerable<TSource> source)
    {
        var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
        if (source is INotifyCollectionChanged changeNotifyingSource)
        {
            var synchronizedSource = source as ISynchronized;
            ActiveValue<int>? activeValue = null;

            void sourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizedSource.Execute(() => activeValue!.Value = source.Count());

            return synchronizedSource.Execute(() =>
            {
                activeValue = new ActiveValue<int>(source.Count(), elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changeNotifyingSource.CollectionChanged -= sourceCollectionChanged);
                changeNotifyingSource.CollectionChanged += sourceCollectionChanged;
                return activeValue;
            });
        }
        try
        {
            return new ActiveValue<int>(source.Count(), elementFaultChangeNotifier: elementFaultChangeNotifier);
        }
        catch (Exception ex)
        {
            return new ActiveValue<int>(default, ex, elementFaultChangeNotifier);
        }
    }

    /// <summary>
    /// Actively determines the number of elements in a sequence that satisfies a condition
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> that contains elements to apply the predicate to</param>
    /// <param name="predicate">A function to test each element for a condition</param>
    /// <returns>An active value the value of which is the number of elements in the source sequence that pass the test in the specified predicate</returns>
    public static IActiveValue<int> ActiveCount<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, bool>> predicate) =>
        ActiveCount(source, predicate, null);

    /// <summary>
    /// Actively determines the number of elements in a sequence that satisfies a condition
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> that contains elements to apply the predicate to</param>
    /// <param name="predicate">A function to test each element for a condition</param>
    /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
    /// <returns>An active value the value of which is the number of elements in the source sequence that pass the test in the specified predicate</returns>
    public static IActiveValue<int> ActiveCount<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions? predicateOptions)
    {
        var readOnlySource = source as IReadOnlyCollection<TSource>;
        var changeNotifyingSource = source as INotifyCollectionChanged;
        IActiveEnumerable<TSource> where;
        ActiveValue<int>? activeValue = null;

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            activeValue!.Value = where.Count;

        return (source as ISynchronized).Execute(() =>
        {
            where = ActiveWhere(source, predicate, predicateOptions);
            activeValue = new ActiveValue<int>(where.Count, elementFaultChangeNotifier: where, onDispose: () =>
            {
                where.CollectionChanged -= collectionChanged;
                if (changeNotifyingSource is { })
                    changeNotifyingSource.CollectionChanged -= collectionChanged;
                where.Dispose();
            });
            where.CollectionChanged += collectionChanged;
            if (changeNotifyingSource is { })
                changeNotifyingSource.CollectionChanged += collectionChanged;
            return activeValue;
        });
    }

    #endregion Count

    #region Distinct

    /// <summary>
    /// Actively returns distinct elements from a sequence by using the default equality comparer to compare values
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">The sequence to remove duplicate elements from</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that contains distinct elements from the source sequence</returns>
    public static IActiveEnumerable<TSource> ActiveDistinct<TSource>(this IReadOnlyList<TSource> source) =>
        ActiveDistinct(source, EqualityComparer<TSource>.Default);

    /// <summary>
    /// Actively returns distinct elements from a sequence by using a specified <see cref="IEqualityComparer{T}"/> to compare values
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">The sequence to remove duplicate elements from</param>
    /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare values</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that contains distinct elements from the source sequence</returns>
    public static IActiveEnumerable<TSource> ActiveDistinct<TSource>(this IReadOnlyList<TSource> source, IEqualityComparer<TSource> comparer)
    {
        var changingSource = source as INotifyCollectionChanged;
        var synchronizedSource = source as ISynchronized;
        SynchronizedRangeObservableCollection<TSource> rangeObservableCollection;
        Dictionary<TSource, int>? distinctCounts = null;

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            synchronizedSource.Execute(() =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    distinctCounts = new Dictionary<TSource, int>(comparer);
                    var distinctValues = new List<TSource>();
                    foreach (var element in source)
                    {
                        if (distinctCounts.TryGetValue(element, out var distinctCount))
                            distinctCounts[element] = ++distinctCount;
                        else
                        {
                            distinctCounts.Add(element, 1);
                            distinctValues.Add(element);
                        }
                    }
                    rangeObservableCollection.ReplaceAll(distinctValues);
                }
                else if (e.Action != NotifyCollectionChangedAction.Move)
                {
                    if (e.OldItems is { } && e.OldStartingIndex >= 0)
                    {
                        var removingResults = new List<TSource>();
                        foreach (TSource oldItem in e.OldItems)
                        {
                            if (--distinctCounts![oldItem] == 0)
                            {
                                distinctCounts.Remove(oldItem);
                                removingResults.Add(oldItem);
                            }
                        }
                        if (removingResults.Count > 0)
                            rangeObservableCollection.RemoveRange(removingResults);
                    }
                    if (e.NewItems is { } && e.NewStartingIndex >= 0)
                    {
                        var addingResults = new List<TSource>();
                        foreach (TSource newItem in e.NewItems)
                        {
                            if (distinctCounts!.TryGetValue(newItem, out var distinctCount))
                                distinctCounts[newItem] = ++distinctCount;
                            else
                            {
                                distinctCounts.Add(newItem, 1);
                                addingResults.Add(newItem);
                            }
                        }
                        if (addingResults.Count > 0)
                            rangeObservableCollection.AddRange(addingResults);
                    }
                }
            });

        return synchronizedSource.Execute(() =>
        {
            rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>();

            if (changingSource is { })
                changingSource.CollectionChanged += collectionChanged;

            distinctCounts = new Dictionary<TSource, int>(comparer);
            foreach (var element in source)
            {
                if (distinctCounts.TryGetValue(element, out var distinctCount))
                    distinctCounts[element] = ++distinctCount;
                else
                {
                    distinctCounts.Add(element, 1);
                    rangeObservableCollection.Add(element);
                }
            }

            return new ActiveEnumerable<TSource>(rangeObservableCollection, () =>
            {
                if (changingSource is { })
                    changingSource.CollectionChanged -= collectionChanged;
            });
        });
    }

    #endregion Distinct

    #region ElementAt

    /// <summary>
    /// Actively returns the element at a specified index in a sequence
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return an element from</param>
    /// <param name="index">The zero-based index of the element to retrieve</param>
    /// <returns>>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the element at the specified position in the source sequence</returns>
    public static IActiveValue<TSource?> ActiveElementAt<TSource>(this IEnumerable<TSource> source, int index)
    {
        var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
        if (source is INotifyCollectionChanged changingSource)
        {
            var synchronizedSource = source as ISynchronized;
            ActiveValue<TSource?>? activeValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizedSource.Execute(() =>
                {
                    try
                    {
                        var value = source.ElementAt(index);
                        activeValue!.OperationFault = null;
                        activeValue!.Value = value;
                    }
                    catch (Exception ex)
                    {
                        activeValue!.OperationFault = ex;
                        activeValue!.Value = default;
                    }
                });

            return synchronizedSource.Execute(() =>
            {
                try
                {
                    activeValue = new ActiveValue<TSource?>(source.ElementAt(index), elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changingSource.CollectionChanged -= collectionChanged);
                    changingSource.CollectionChanged += collectionChanged;
                    return activeValue;
                }
                catch (Exception ex)
                {
                    activeValue = new ActiveValue<TSource?>(default, ex, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    changingSource.CollectionChanged += collectionChanged;
                    return activeValue;
                }
            });
        }
        try
        {
            return new ActiveValue<TSource?>(source.ElementAt(index), elementFaultChangeNotifier: elementFaultChangeNotifier);
        }
        catch (Exception ex)
        {
            return new ActiveValue<TSource?>(default, ex, elementFaultChangeNotifier);
        }
    }

    /// <summary>
    /// Actively returns the element at a specified index in a sequence
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return an element from</param>
    /// <param name="index">The zero-based index of the element to retrieve</param>
    /// <returns>>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the element at the specified position in the source sequence</returns>
    public static IActiveValue<TSource?> ActiveElementAt<TSource>(this IReadOnlyList<TSource> source, int index)
    {
        IActiveEnumerable<TSource> activeEnumerable;
        ActiveValue<TSource?>? activeValue = null;
        var indexOutOfRange = false;

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (indexOutOfRange && index >= 0 && index < activeEnumerable.Count)
            {
                activeValue!.OperationFault = null;
                indexOutOfRange = false;
            }
            else if (!indexOutOfRange && (index < 0 || index >= activeEnumerable.Count))
            {
                activeValue!.OperationFault = ExceptionHelper.IndexArgumentWasOutOfRange;
                indexOutOfRange = true;
            }
            activeValue!.Value = index >= 0 && index < activeEnumerable.Count ? activeEnumerable[index] : default;
        }

        return (source as ISynchronized).Execute(() =>
        {
            activeEnumerable = ToActiveEnumerable(source);
            indexOutOfRange = index < 0 || index >= activeEnumerable.Count;
            activeValue = new ActiveValue<TSource?>(!indexOutOfRange ? activeEnumerable[index] : default, indexOutOfRange ? ExceptionHelper.IndexArgumentWasOutOfRange : null, activeEnumerable, () =>
            {
                activeEnumerable.CollectionChanged -= collectionChanged;
                activeEnumerable.Dispose();
            });
            activeEnumerable.CollectionChanged += collectionChanged;
            return activeValue;
        });
    }

    #endregion ElementAt

    #region ElementAtOrDefault

    /// <summary>
    /// Actively returns the element at a specified index in a sequence or a default value if the index is out of range
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return an element from</param>
    /// <param name="index">The zero-based index of the element to retrieve</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>default</c>(<typeparamref name="TSource"/>) if the index is outside the bounds of the source sequence; otherwise, the element at the specified position in the source sequence</returns>
    public static IActiveValue<TSource> ActiveElementAtOrDefault<TSource>(this IEnumerable<TSource> source, int index)
    {
        var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
        if (source is INotifyCollectionChanged changingSource)
        {
            var synchronizedSource = source as ISynchronized;
            ActiveValue<TSource>? activeValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizedSource.Execute(() => activeValue!.Value = source.ElementAtOrDefault(index));

            return synchronizedSource.Execute(() =>
            {
                activeValue = new ActiveValue<TSource>(source.ElementAtOrDefault(index), elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changingSource.CollectionChanged -= collectionChanged);
                changingSource.CollectionChanged += collectionChanged;
                return activeValue;
            });
        }
        return new ActiveValue<TSource>(source.ElementAtOrDefault(index), elementFaultChangeNotifier: elementFaultChangeNotifier);
    }

    /// <summary>
    /// Actively returns the element at a specified index in a sequence or a default value if the index is out of range
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return an element from</param>
    /// <param name="index">The zero-based index of the element to retrieve</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>default</c>(<typeparamref name="TSource"/>) if the index is outside the bounds of the source sequence; otherwise, the element at the specified position in the source sequence</returns>
    public static IActiveValue<TSource?> ActiveElementAtOrDefault<TSource>(this IReadOnlyList<TSource> source, int index)
    {
        IActiveEnumerable<TSource> activeEnumerable;
        ActiveValue<TSource?>? activeValue = null;

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            activeValue!.Value = index >= 0 && index < activeEnumerable.Count ? activeEnumerable[index] : default;

        return (source as ISynchronized).Execute(() =>
        {
            activeEnumerable = ToActiveEnumerable(source);
            activeValue = new ActiveValue<TSource?>(index >= 0 && index < activeEnumerable.Count ? activeEnumerable[index] : default, elementFaultChangeNotifier: activeEnumerable, onDispose: () =>
            {
                activeEnumerable.CollectionChanged -= collectionChanged;
                activeEnumerable.Dispose();
            });
            activeEnumerable.CollectionChanged += collectionChanged;
            return activeValue;
        });
    }

    #endregion ElementAtOrDefault

    #region First

    /// <summary>
    /// Actively returns the first element of a sequence
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">The <see cref="IEnumerable{T}"/> to return the first element of</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the first element in the specified sequence</returns>
    public static IActiveValue<TSource?> ActiveFirst<TSource>(this IEnumerable<TSource> source)
    {
        var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
        if (source is INotifyCollectionChanged changingSource)
        {
            var synchronizedSource = source as ISynchronized;
            ActiveValue<TSource?>? activeValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizedSource.Execute(() =>
                {
                    try
                    {
                        var value = source.First();
                        activeValue!.OperationFault = null;
                        activeValue!.Value = value;
                    }
                    catch (Exception ex)
                    {
                        activeValue!.OperationFault = ex;
                        activeValue!.Value = default;
                    }
                });

            return synchronizedSource.Execute(() =>
            {
                try
                {
                    activeValue = new ActiveValue<TSource?>(source.First(), elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changingSource.CollectionChanged -= collectionChanged);
                    changingSource.CollectionChanged += collectionChanged;
                    return activeValue;
                }
                catch (Exception ex)
                {
                    activeValue = new ActiveValue<TSource?>(default, ex, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    changingSource.CollectionChanged += collectionChanged;
                    return activeValue;
                }
            });
        }
        try
        {
            return new ActiveValue<TSource?>(source.First(), elementFaultChangeNotifier: elementFaultChangeNotifier);
        }
        catch (Exception ex)
        {
            return new ActiveValue<TSource?>(default, ex, elementFaultChangeNotifier);
        }
    }

    /// <summary>
    /// Actively returns the first element in a sequence that satisfies a specified condition
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return an element from</param>
    /// <param name="predicate">A function to test each element for a condition</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the first element in the sequence that passes the test in the spredicate function</returns>
    public static IActiveValue<TSource?> ActiveFirst<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate) =>
        ActiveFirst(source, predicate, null);

    /// <summary>
    /// Actively returns the first element in a sequence that satisfies a specified condition
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return an element from</param>
    /// <param name="predicate">A function to test each element for a condition</param>
    /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the first element in the sequence that passes the test in the spredicate function</returns>
    public static IActiveValue<TSource?> ActiveFirst<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions? predicateOptions)
    {
        IActiveEnumerable<TSource> where;
        ActiveValue<TSource?>? activeValue = null;
        var none = false;

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (none && where.Count > 0)
            {
                activeValue!.OperationFault = null;
                none = false;
            }
            else if (!none && where.Count == 0)
            {
                activeValue!.OperationFault = ExceptionHelper.SequenceContainsNoElements;
                none = true;
            }
            activeValue!.Value = where.Count > 0 ? where[0] : default;
        }

        return (source as ISynchronized).Execute(() =>
        {
            where = ActiveWhere(source, predicate, predicateOptions);
            none = where.Count == 0;
            activeValue = new ActiveValue<TSource?>(!none ? where[0] : default, none ? ExceptionHelper.SequenceContainsNoElements : null, where, () =>
            {
                where.CollectionChanged -= collectionChanged;
                where.Dispose();
            });
            where.CollectionChanged += collectionChanged;
            return activeValue;
        });
    }

    #endregion First

    #region FirstOrDefault

    /// <summary>
    /// Actively returns the first element of a sequence, or a default value if the sequence contains no elements
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">The <see cref="IEnumerable{T}"/> to return the first element of</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>default</c>(<typeparamref name="TSource"/>) if <paramref name="source"/> is empty; otherwise, the first element in <paramref name="source"/></returns>
    public static IActiveValue<TSource> ActiveFirstOrDefault<TSource>(this IEnumerable<TSource> source)
    {
        var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
        if (source is INotifyCollectionChanged changingSource)
        {
            var synchronizedSource = source as ISynchronized;
            ActiveValue<TSource>? activeValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizedSource.Execute(() => activeValue!.Value = source.FirstOrDefault());

            return synchronizedSource.Execute(() =>
            {
                activeValue = new ActiveValue<TSource>(source.FirstOrDefault(), elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changingSource.CollectionChanged -= collectionChanged);
                changingSource.CollectionChanged += collectionChanged;
                return activeValue;
            });
        }
        return new ActiveValue<TSource>(source.FirstOrDefault(), elementFaultChangeNotifier: elementFaultChangeNotifier);
    }

    /// <summary>
    /// Actively returns the first element of the sequence that satisfies a condition or a default value if no such element is found
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">The <see cref="IEnumerable{T}"/> to return the first element of</param>
    /// <param name="predicate">A function to test each element for a condition</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>default</c>(<typeparamref name="TSource"/>) if <paramref name="source"/> is empty or if no element passes the test specified by <paramref name="predicate"/>; otherwise, the first element in <paramref name="source"/> that passes the test specified by <paramref name="predicate"/></returns>
    public static IActiveValue<TSource?> ActiveFirstOrDefault<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate) =>
        ActiveFirstOrDefault(source, predicate, null);

    /// <summary>
    /// Actively returns the first element of the sequence that satisfies a condition or a default value if no such element is found
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">The <see cref="IEnumerable{T}"/> to return the first element of</param>
    /// <param name="predicate">A function to test each element for a condition</param>
    /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>default</c>(<typeparamref name="TSource"/>) if <paramref name="source"/> is empty or if no element passes the test specified by <paramref name="predicate"/>; otherwise, the first element in <paramref name="source"/> that passes the test specified by <paramref name="predicate"/></returns>
    public static IActiveValue<TSource?> ActiveFirstOrDefault<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions? predicateOptions)
    {
        IActiveEnumerable<TSource> where;
        ActiveValue<TSource?>? activeValue = null;

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            activeValue!.Value = where.Count > 0 ? where[0] : default;

        return (source as ISynchronized).Execute(() =>
        {
            where = ActiveWhere(source, predicate, predicateOptions);
            activeValue = new ActiveValue<TSource?>(where.Count > 0 ? where[0] : default, elementFaultChangeNotifier: where, onDispose: () =>
            {
                where.CollectionChanged -= collectionChanged;
                where.Dispose();
            });
            where.CollectionChanged += collectionChanged;
            return activeValue;
        });
    }

    #endregion FirstOrDefault

    #region GroupBy

    /// <summary>
    /// Actively groups the elements of a sequence according to a specified key selector function
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> whose elements to group</param>
    /// <param name="keySelector">A function to extract the key for each element</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> where each element is an <see cref="ActiveGrouping{TKey, TElement}"/> object contains a sequence of objects and a key</returns>
    public static IActiveEnumerable<IActiveGrouping<TKey?, TSource>> ActiveGroupBy<TSource, TKey>(this IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector) =>
        ActiveGroupBy(source, keySelector, IndexingStrategy.HashTable);

    /// <summary>
    /// Actively groups the elements of a sequence according to a specified key selector function using the specified indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> whose elements to group</param>
    /// <param name="keySelector">A function to extract the key for each element</param>
    /// <param name="indexingStrategy">The indexing strategy to use when grouping</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> where each element is an <see cref="ActiveGrouping{TKey, TElement}"/> object contains a sequence of objects and a key</returns>
    public static IActiveEnumerable<IActiveGrouping<TKey?, TSource>> ActiveGroupBy<TSource, TKey>(this IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector, IndexingStrategy indexingStrategy) =>
        ActiveGroupBy(source, keySelector, null, indexingStrategy, null, null);

    /// <summary>
    /// Actively groups the elements of a sequence according to a specified key selector function using the <see cref="IndexingStrategy.HashTable"/> indexing strategy and compares the keys by using a specified equality comparer
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> whose elements to group</param>
    /// <param name="keySelector">A function to extract the key for each element</param>
    /// <param name="equalityComparer">An <see cref="IEqualityComparer{T}"/> to compare keys</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> where each element is an <see cref="ActiveGrouping{TKey, TElement}"/> object contains a sequence of objects and a key</returns>
    public static IActiveEnumerable<IActiveGrouping<TKey?, TSource>> ActiveGroupBy<TSource, TKey>(this IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector, IEqualityComparer<TKey> equalityComparer) =>
        ActiveGroupBy(source, keySelector, null, IndexingStrategy.HashTable, equalityComparer, null);

    /// <summary>
    /// Actively groups the elements of a sequence according to a specified key selector function using the <see cref="IndexingStrategy.SelfBalancingBinarySearchTree"/> indexing strategy and compares the keys by using a specified comparer
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> whose elements to group</param>
    /// <param name="keySelector">A function to extract the key for each element</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> where each element is an <see cref="ActiveGrouping{TKey, TElement}"/> object contains a sequence of objects and a key</returns>
    public static IActiveEnumerable<IActiveGrouping<TKey?, TSource>> ActiveGroupBy<TSource, TKey>(this IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey> comparer) =>
        ActiveGroupBy(source, keySelector, null, IndexingStrategy.SelfBalancingBinarySearchTree, null, comparer);

    /// <summary>
    /// Actively groups the elements of a sequence according to a specified key selector function
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> whose elements to group</param>
    /// <param name="keySelector">A function to extract the key for each element</param>
    /// <param name="keySelectorOptions">Options governing the behavior of active expressions created using <paramref name="keySelector"/></param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> where each element is an <see cref="ActiveGrouping{TKey, TElement}"/> object contains a sequence of objects and a key</returns>
    public static IActiveEnumerable<IActiveGrouping<TKey?, TSource>> ActiveGroupBy<TSource, TKey>(this IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector, ActiveExpressionOptions keySelectorOptions) =>
        ActiveGroupBy(source, keySelector, keySelectorOptions, IndexingStrategy.HashTable, null, null);

    /// <summary>
    /// Actively groups the elements of a sequence according to a specified key selector function using the specified indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> whose elements to group</param>
    /// <param name="keySelector">A function to extract the key for each element</param>
    /// <param name="keySelectorOptions">Options governing the behavior of active expressions created using <paramref name="keySelector"/></param>
    /// <param name="indexingStrategy">The indexing strategy to use when grouping</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> where each element is an <see cref="ActiveGrouping{TKey, TElement}"/> object contains a sequence of objects and a key</returns>
    public static IActiveEnumerable<IActiveGrouping<TKey?, TSource>> ActiveGroupBy<TSource, TKey>(this IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector, ActiveExpressionOptions keySelectorOptions, IndexingStrategy indexingStrategy) =>
        ActiveGroupBy(source, keySelector, keySelectorOptions, indexingStrategy, null, null);

    /// <summary>
    /// Actively groups the elements of a sequence according to a specified key selector function using the <see cref="IndexingStrategy.HashTable"/> indexing strategy and compares the keys by using a specified equality comparer
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> whose elements to group</param>
    /// <param name="keySelector">A function to extract the key for each element</param>
    /// <param name="keySelectorOptions">Options governing the behavior of active expressions created using <paramref name="keySelector"/></param>
    /// <param name="equalityComparer">An <see cref="IEqualityComparer{T}"/> to compare keys</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> where each element is an <see cref="ActiveGrouping{TKey, TElement}"/> object contains a sequence of objects and a key</returns>
    public static IActiveEnumerable<IActiveGrouping<TKey?, TSource>> ActiveGroupBy<TSource, TKey>(this IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector, ActiveExpressionOptions keySelectorOptions, IEqualityComparer<TKey> equalityComparer) =>
        ActiveGroupBy(source, keySelector, keySelectorOptions, IndexingStrategy.HashTable, equalityComparer, null);

    /// <summary>
    /// Actively groups the elements of a sequence according to a specified key selector function using the <see cref="IndexingStrategy.SelfBalancingBinarySearchTree"/> indexing strategy and compares the keys by using a specified comparer
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> whose elements to group</param>
    /// <param name="keySelector">A function to extract the key for each element</param>
    /// <param name="keySelectorOptions">Options governing the behavior of active expressions created using <paramref name="keySelector"/></param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> where each element is an <see cref="ActiveGrouping{TKey, TElement}"/> object contains a sequence of objects and a key</returns>
    public static IActiveEnumerable<IActiveGrouping<TKey?, TSource>> ActiveGroupBy<TSource, TKey>(this IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector, ActiveExpressionOptions keySelectorOptions, IComparer<TKey> comparer) =>
        ActiveGroupBy(source, keySelector, keySelectorOptions, IndexingStrategy.SelfBalancingBinarySearchTree, null, comparer);

    static IActiveEnumerable<IActiveGrouping<TKey?, TSource>> ActiveGroupBy<TSource, TKey>(IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector, ActiveExpressionOptions? keySelectorOptions, IndexingStrategy indexingStrategy, IEqualityComparer<TKey>? equalityComparer, IComparer<TKey>? comparer)
    {
        ActiveQueryOptions.Optimize(ref keySelector);
        var synchronizedSource = source as ISynchronized;
        EnumerableRangeActiveExpression<TSource, TKey> rangeActiveExpression;
        SynchronizedRangeObservableCollection<IActiveGrouping<TKey?, TSource>>? rangeObservableCollection = null;

        var collectionAndGroupingDictionary = (IDictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, IActiveGrouping<TKey?, TSource> grouping)>)(indexingStrategy switch
        {
            IndexingStrategy.HashTable => equalityComparer is null ? new NullableKeyDictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, IActiveGrouping<TKey?, TSource> grouping)>() : new NullableKeyDictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, IActiveGrouping<TKey?, TSource> grouping)>(equalityComparer),
            IndexingStrategy.SelfBalancingBinarySearchTree => comparer is null ? new NullableKeySortedDictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, IActiveGrouping<TKey?, TSource> grouping)>() : new NullableKeySortedDictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, IActiveGrouping<TKey?, TSource> grouping)>(comparer),
            _ => throw new ArgumentOutOfRangeException(nameof(indexingStrategy), $"{nameof(indexingStrategy)} must be {IndexingStrategy.HashTable} or {IndexingStrategy.SelfBalancingBinarySearchTree}"),
        });
        var collectionAndGroupingDictionaryAccess = new object();

        void addElement(TSource element, TKey? key, int count = 1)
        {
            SynchronizedRangeObservableCollection<TSource> groupingObservableCollection;
            if (!collectionAndGroupingDictionary!.TryGetValue(key! /* This is okay, it's always a nullable key dictionary */, out var collectionAndGrouping))
            {
                groupingObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizedSource?.SynchronizationContext);
                var grouping = new ActiveGrouping<TKey?, TSource>(key, groupingObservableCollection);
                collectionAndGroupingDictionary.Add(key! /* This is okay, it's always a nullable key dictionary */, (groupingObservableCollection, grouping));
                rangeObservableCollection!.Add(grouping);
            }
            else
                groupingObservableCollection = collectionAndGrouping.groupingObservableCollection;
            groupingObservableCollection.AddRange(element.Repeat(count));
        }

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            synchronizedSource.Execute(() =>
            {
                lock (collectionAndGroupingDictionaryAccess)
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        collectionAndGroupingDictionary = indexingStrategy switch
                        {
                            IndexingStrategy.HashTable => equalityComparer is null ? new NullableKeyDictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, IActiveGrouping<TKey?, TSource> grouping)>() : new NullableKeyDictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, IActiveGrouping<TKey?, TSource> grouping)>(equalityComparer),
                            IndexingStrategy.SelfBalancingBinarySearchTree => comparer is null ? new NullableKeySortedDictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, IActiveGrouping<TKey?, TSource> grouping)>() : new NullableKeySortedDictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, IActiveGrouping<TKey?, TSource> grouping)>(comparer),
                            _ => throw new ArgumentOutOfRangeException(nameof(indexingStrategy), $"{nameof(indexingStrategy)} must be {IndexingStrategy.HashTable} or {IndexingStrategy.SelfBalancingBinarySearchTree}"),
                        };
                        rangeObservableCollection!.Clear();
                        foreach (var (element, key) in rangeActiveExpression.GetResults())
                            addElement(element, key);
                    }
                    else if (e.Action != NotifyCollectionChangedAction.Move)
                    {
                        if (e.OldItems is not null)
                            foreach (var (element, key) in e.OldItems.Cast<(TSource element, TKey? key)>())
                                removeElement(element, key);
                        if (e.NewItems is not null)
                            foreach (var (element, key) in e.NewItems.Cast<(TSource element, TKey? key)>())
                                addElement(element, key);
                    }
                }
            });

        void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TKey?> e) =>
            synchronizedSource.Execute(() =>
            {
                lock (collectionAndGroupingDictionaryAccess)
                    addElement(e.Element! /* this could be null, but it won't matter if it is */, e.Result! /* this could be null, but it won't matter if it is */, e.Count);
            });

        void elementResultChanging(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TKey?> e) =>
            synchronizedSource.Execute(() =>
            {
                lock (collectionAndGroupingDictionaryAccess)
                    removeElement(e.Element! /* this could be null, but it won't matter if it is */, e.Result! /* this could be null, but it won't matter if it is */, e.Count);
            });

        void removeElement(TSource element, TKey? key, int count = 1)
        {
            var (groupingObservableCollection, grouping) = collectionAndGroupingDictionary![key! /* This is okay, it's always a nullable key dictionary */];
            while (--count >= 0)
                groupingObservableCollection.Remove(element);
            if (groupingObservableCollection.Count == 0)
            {
                rangeObservableCollection!.Remove(grouping);
                grouping.DisposeInternal();
                collectionAndGroupingDictionary.Remove(key! /* This is okay, it's always a nullable key dictionary */);
            }
        }

        return synchronizedSource.Execute(() =>
        {
            lock (collectionAndGroupingDictionaryAccess)
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, keySelector, keySelectorOptions);
                rangeObservableCollection = new SynchronizedRangeObservableCollection<IActiveGrouping<TKey?, TSource>>(synchronizedSource?.SynchronizationContext);
                rangeActiveExpression.CollectionChanged += collectionChanged;
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.ElementResultChanging += elementResultChanging;
                foreach (var (element, key) in rangeActiveExpression.GetResults())
                    addElement(element, key);

                return new ActiveEnumerable<IActiveGrouping<TKey?, TSource>>(rangeObservableCollection, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.CollectionChanged -= collectionChanged;
                    rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                    rangeActiveExpression.ElementResultChanging -= elementResultChanging;
                    rangeActiveExpression.Dispose();

                    lock (collectionAndGroupingDictionaryAccess)
                        foreach (var key in collectionAndGroupingDictionary.Keys.ToImmutableArray())
                            if (collectionAndGroupingDictionary.TryRemove(key, out var collectionAndGrouping))
                                collectionAndGrouping.grouping.DisposeInternal();
                });
            }
        });
    }

    #endregion GroupBy

    #region Last

    /// <summary>
    /// Actively returns the last element of a sequence
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return the last element of</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the value at the last position in the source sequence</returns>
    public static IActiveValue<TSource?> ActiveLast<TSource>(this IEnumerable<TSource> source)
    {
        var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
        if (source is INotifyCollectionChanged changingSource)
        {
            var synchronizedSource = source as ISynchronized;
            ActiveValue<TSource?>? activeValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizedSource.Execute(() =>
                {
                    try
                    {
                        var value = source.Last();
                        activeValue!.OperationFault = null;
                        activeValue!.Value = value;
                    }
                    catch (Exception ex)
                    {
                        activeValue!.OperationFault = ex;
                        activeValue!.Value = default;
                    }
                });

            return synchronizedSource.Execute(() =>
            {
                try
                {
                    activeValue = new ActiveValue<TSource?>(source.Last(), elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changingSource.CollectionChanged -= collectionChanged);
                    changingSource.CollectionChanged += collectionChanged;
                    return activeValue;
                }
                catch (Exception ex)
                {
                    activeValue = new ActiveValue<TSource?>(default!, ex, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    changingSource.CollectionChanged += collectionChanged;
                    return activeValue;
                }
            });
        }
        try
        {
            return new ActiveValue<TSource?>(source.Last(), elementFaultChangeNotifier: elementFaultChangeNotifier);
        }
        catch (Exception ex)
        {
            return new ActiveValue<TSource?>(default, ex, elementFaultChangeNotifier);
        }
    }

    /// <summary>
    /// Actively returns the last element of a sequence that satisfies a specified condition
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return the last element of</param>
    /// <param name="predicate">A function to test each element for a condition</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the last element in the sequence that passes the test in the spredicate function</returns>
    public static IActiveValue<TSource?> ActiveLast<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate) =>
        ActiveLast(source, predicate, null);

    /// <summary>
    /// Actively returns the last element of a sequence that satisfies a specified condition
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return the last element of</param>
    /// <param name="predicate">A function to test each element for a condition</param>
    /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the last element in the sequence that passes the test in the spredicate function</returns>
    public static IActiveValue<TSource?> ActiveLast<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions? predicateOptions)
    {
        IActiveEnumerable<TSource> where;
        ActiveValue<TSource?>? activeValue = null;
        var none = false;

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (none && where.Count > 0)
            {
                activeValue!.OperationFault = null;
                none = false;
            }
            else if (!none && where.Count == 0)
            {
                activeValue!.OperationFault = ExceptionHelper.SequenceContainsNoElements;
                none = true;
            }
            activeValue!.Value = where.Count > 0 ? where[^1] : default;
        }

        return (source as ISynchronized).Execute(() =>
        {
            where = ActiveWhere(source, predicate, predicateOptions);
            none = where.Count == 0;
            activeValue = new ActiveValue<TSource?>(!none ? where[^1] : default, none ? ExceptionHelper.SequenceContainsNoElements : null, where, () =>
            {
                where.CollectionChanged -= collectionChanged;
                where.Dispose();
            });
            where.CollectionChanged += collectionChanged;
            return activeValue;
        });
    }

    #endregion Last

    #region LastOrDefault

    /// <summary>
    /// Actively Returns the last element of a sequence, or a default value if the sequence contains no elements
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return the last element of</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>default</c>(<typeparamref name="TSource"/>) if the source sequence is empty; otherwise, the last element in the <see cref="IEnumerable{T}"/></returns>
    public static IActiveValue<TSource> ActiveLastOrDefault<TSource>(this IEnumerable<TSource> source)
    {
        var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
        if (source is INotifyCollectionChanged changingSource)
        {
            var synchronizedSource = source as ISynchronized;
            ActiveValue<TSource>? activeValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizedSource.Execute(() => activeValue!.Value = source.LastOrDefault());

            return synchronizedSource.Execute(() =>
            {
                activeValue = new ActiveValue<TSource>(source.LastOrDefault(), elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changingSource.CollectionChanged -= collectionChanged);
                changingSource.CollectionChanged += collectionChanged;
                return activeValue;
            });
        }
        return new ActiveValue<TSource>(source.LastOrDefault(), elementFaultChangeNotifier: elementFaultChangeNotifier);
    }

    /// <summary>
    /// Actively returns the last element of a sequence that satisfies a condition or a default value if no such element is found
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return an element from</param>
    /// <param name="predicate">A function to test each element for a condition</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>default</c>(<typeparamref name="TSource"/>) if the sequence is empty or if no elements pass the test in the predicate function; otherwise, the last element that passes the test in the predicate function</returns>
    public static IActiveValue<TSource?> ActiveLastOrDefault<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate) =>
        ActiveLastOrDefault(source, predicate, null);

    /// <summary>
    /// Actively returns the last element of a sequence that satisfies a condition or a default value if no such element is found
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return an element from</param>
    /// <param name="predicate">A function to test each element for a condition</param>
    /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>default</c>(<typeparamref name="TSource"/>) if the sequence is empty or if no elements pass the test in the predicate function; otherwise, the last element that passes the test in the predicate function</returns>
    public static IActiveValue<TSource?> ActiveLastOrDefault<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions? predicateOptions)
    {
        IActiveEnumerable<TSource> where;
        ActiveValue<TSource?>? activeValue = null;

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            activeValue!.Value = where.Count > 0 ? where[^1] : default;

        return (source as ISynchronized).Execute(() =>
        {
            where = ActiveWhere(source, predicate, predicateOptions);
            activeValue = new ActiveValue<TSource?>(where.Count > 0 ? where[^1] : default, elementFaultChangeNotifier: where, onDispose: () =>
            {
                where.CollectionChanged -= collectionChanged;
                where.Dispose();
            });
            where.CollectionChanged += collectionChanged;
            return activeValue;
        });
    }

    #endregion LastOrDefault

    #region Max

    /// <summary>
    /// Actively returns the maximum value in a sequence of values
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">A sequence of values to determine the maximum value of</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the maximum value in the sequence</returns>
    public static IActiveValue<TSource?> ActiveMax<TSource>(this IEnumerable<TSource> source) =>
        ActiveMax(source, element => element);

    /// <summary>
    /// Actively invokes a transform function on each element of a sequence and returns the maximum value
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TResult">The type of the maximum value</typeparam>
    /// <param name="source">A sequence of values to determine the maximum value of</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the maximum value in the sequence</returns>
    public static IActiveValue<TResult?> ActiveMax<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector) =>
        ActiveMax(source, selector, null);

    /// <summary>
    /// Actively invokes a transform function on each element of a sequence and returns the maximum value
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TResult">The type of the maximum value</typeparam>
    /// <param name="source">A sequence of values to determine the maximum value of</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the maximum value in the sequence</returns>
    public static IActiveValue<TResult?> ActiveMax<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions)
    {
        ActiveQueryOptions.Optimize(ref selector);

        var comparer = Comparer<TResult?>.Default;
        var synchronizedSource = source as ISynchronized;
        EnumerableRangeActiveExpression<TSource, TResult> rangeActiveExpression;
        ActiveValue<TResult?>? activeValue = null;
        var activeValueAccess = new object();

        void dispose()
        {
            rangeActiveExpression.CollectionChanged -= collectionChanged;
            rangeActiveExpression.ElementResultChanged -= elementResultChanged;
            rangeActiveExpression.Dispose();
        }

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            synchronizedSource.Execute(() =>
            {
                lock (activeValueAccess)
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        try
                        {
                            activeValue!.OperationFault = null;
                            activeValue!.Value = rangeActiveExpression.GetResults().Max(er => er.result);
                        }
                        catch (Exception ex)
                        {
                            activeValue!.OperationFault = ex;
                            activeValue!.Value = default;
                        }
                    }
                    else if (e.Action != NotifyCollectionChangedAction.Move)
                    {
                        var activeValueValue = activeValue!.Value;
                        if ((e.OldItems?.Count ?? 0) > 0)
                        {
                            var removedMax = e.OldItems.Cast<(TSource element, TResult? result)>().Max(er => er.result);
                            if ((activeValueValue is null ? -1 : comparer!.Compare(activeValueValue, removedMax)) == 0)
                            {
                                try
                                {
                                    var value = rangeActiveExpression.GetResultsUnderLock().Max(er => er.result);
                                    activeValue!.OperationFault = null;
                                    activeValue!.Value = value;
                                }
                                catch (Exception ex)
                                {
                                    activeValue!.OperationFault = ex;
                                }
                            }
                        }
                        activeValueValue = activeValue!.Value;
                        if ((e.NewItems?.Count ?? 0) > 0)
                        {
                            var addedMax = e.NewItems.Cast<(TSource element, TResult? result)>().Max(er => er.result);
                            if (activeValue!.OperationFault is { } || (activeValueValue is null ? -1 : comparer!.Compare(activeValueValue, addedMax)) < 0)
                            {
                                activeValue!.OperationFault = null;
                                activeValue!.Value = addedMax;
                            }
                        }
                    }
                }
            });

        void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TResult?> e) =>
            synchronizedSource.Execute(() =>
            {
                lock (activeValueAccess)
                {
                    var activeValueValue = activeValue!.Value;
                    var comparison = 0;
                    if (activeValueValue is { } && e.Result is { })
                        comparison = comparer!.Compare(activeValueValue, e.Result);
                    else if (activeValueValue is null)
                        comparison = -1;
                    else if (e.Result is null)
                        comparison = 1;
                    if (comparison < 0)
                        activeValue!.Value = e.Result;
                    else if (comparison > 0)
                        activeValue!.Value = rangeActiveExpression.GetResultsUnderLock().Max(er => er.result);
                }
            });

        return synchronizedSource.Execute(() =>
        {
            lock (activeValueAccess)
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                try
                {
                    activeValue = new ActiveValue<TResult?>(rangeActiveExpression.GetResults().Max(er => er.result), null, rangeActiveExpression, dispose);
                    rangeActiveExpression.CollectionChanged += collectionChanged;
                    rangeActiveExpression.ElementResultChanged += elementResultChanged;
                    return activeValue;
                }
                catch (Exception ex)
                {
                    activeValue = new ActiveValue<TResult?>(default, ex, rangeActiveExpression, dispose);
                    rangeActiveExpression.CollectionChanged += collectionChanged;
                    rangeActiveExpression.ElementResultChanged += elementResultChanged;
                    return activeValue;
                }
            }
        });
    }

    #endregion Max

    #region Min

    /// <summary>
    /// Actively returns the minimum value in a sequence of values
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">A sequence of values to determine the minimum value of</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the minimum value in the sequence</returns>
    public static IActiveValue<TSource?> ActiveMin<TSource>(this IEnumerable<TSource> source) =>
        ActiveMin(source, element => element);

    /// <summary>
    /// Actively invokes a transform function on each element of a sequence and returns the minimum value
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TResult">The type of the minimum value</typeparam>
    /// <param name="source">A sequence of values to determine the minimum value of</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the minimum value in the sequence</returns>
    public static IActiveValue<TResult?> ActiveMin<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector) =>
        ActiveMin(source, selector, null);

    /// <summary>
    /// Actively invokes a transform function on each element of a sequence and returns the minimum value
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TResult">The type of the minimum value</typeparam>
    /// <param name="source">A sequence of values to determine the minimum value of</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the minimum value in the sequence</returns>
    public static IActiveValue<TResult?> ActiveMin<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions)
    {
        ActiveQueryOptions.Optimize(ref selector);

        var comparer = Comparer<TResult?>.Default;
        var synchronizedSource = source as ISynchronized;
        EnumerableRangeActiveExpression<TSource, TResult> rangeActiveExpression;
        ActiveValue<TResult?>? activeValue = null;
        var activeValueAccess = new object();

        void dispose()
        {
            rangeActiveExpression.CollectionChanged -= collectionChanged;
            rangeActiveExpression.ElementResultChanged -= elementResultChanged;
            rangeActiveExpression.Dispose();
        }

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            synchronizedSource.Execute(() =>
            {
                lock (activeValueAccess)
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        try
                        {
                            activeValue!.OperationFault = null;
                            activeValue!.Value = rangeActiveExpression.GetResults().Min(er => er.result);
                        }
                        catch (Exception ex)
                        {
                            activeValue!.OperationFault = ex;
                            activeValue!.Value = default;
                        }
                    }
                    else if (e.Action != NotifyCollectionChangedAction.Move)
                    {
                        var activeValueValue = activeValue!.Value;
                        if ((e.OldItems?.Count ?? 0) > 0)
                        {
                            var removedMin = e.OldItems.Cast<(TSource element, TResult? result)>().Min(er => er.result);
                            if ((activeValueValue is null ? -1 : comparer!.Compare(activeValueValue, removedMin)) == 0)
                            {
                                try
                                {
                                    var value = rangeActiveExpression.GetResultsUnderLock().Min(er => er.result);
                                    activeValue!.OperationFault = null;
                                    activeValue!.Value = value;
                                }
                                catch (Exception ex)
                                {
                                    activeValue!.OperationFault = ex;
                                }
                            }
                        }
                        activeValueValue = activeValue!.Value;
                        if ((e.NewItems?.Count ?? 0) > 0)
                        {
                            var addedMin = e.NewItems.Cast<(TSource element, TResult? result)>().Min(er => er.result);
                            if (activeValue!.OperationFault is { } || (activeValueValue is null ? -1 : comparer!.Compare(activeValueValue, addedMin)) > 0)
                            {
                                activeValue!.OperationFault = null;
                                activeValue!.Value = addedMin;
                            }
                        }
                    }
                }
            });

        void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TResult?> e) =>
            synchronizedSource.Execute(() =>
            {
                lock (activeValueAccess)
                {
                    var activeValueValue = activeValue!.Value;
                    var comparison = 0;
                    if (activeValueValue is { } && e.Result is { })
                        comparison = comparer!.Compare(activeValueValue, e.Result);
                    else if (activeValueValue is null)
                        comparison = -1;
                    else if (e.Result is null)
                        comparison = 1;
                    if (comparison > 0)
                        activeValue!.Value = e.Result;
                    else if (comparison < 0)
                        activeValue!.Value = rangeActiveExpression.GetResultsUnderLock().Min(er => er.result);
                }
            });

        return synchronizedSource.Execute(() =>
        {
            lock (activeValueAccess)
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                try
                {
                    activeValue = new ActiveValue<TResult?>(rangeActiveExpression.GetResults().Min(er => er.result), null, rangeActiveExpression, dispose);
                    rangeActiveExpression.CollectionChanged += collectionChanged;
                    rangeActiveExpression.ElementResultChanged += elementResultChanged;
                    return activeValue;
                }
                catch (Exception ex)
                {
                    activeValue = new ActiveValue<TResult?>(default, ex, rangeActiveExpression, dispose);
                    rangeActiveExpression.CollectionChanged += collectionChanged;
                    rangeActiveExpression.ElementResultChanged += elementResultChanged;
                    return activeValue;
                }
            }
        });
    }

    #endregion Min

    #region OfType

    /// <summary>
    /// Actively filters the elements of an <see cref="IEnumerable"/> based on a specified type
    /// </summary>
    /// <typeparam name="TResult">The type to filter the elements of the sequence on</typeparam>
    /// <param name="source">The <see cref="IEnumerable"/> the elements of which to filter</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that contains elements from the input sequence of type <typeparamref name="TResult"/></returns>
    public static IActiveEnumerable<TResult> ActiveOfType<TResult>(this IEnumerable source)
    {
        var synchronizedSource = source as ISynchronized;
        var notifyingSource = source as INotifyCollectionChanged;
        SynchronizedRangeObservableCollection<TResult> rangeObservableCollection;

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            synchronizedSource.Execute(() =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                    rangeObservableCollection.Reset(source.OfType<TResult>());
                else if (e.Action != NotifyCollectionChangedAction.Move)
                {
                    if (e.OldItems is { })
                        rangeObservableCollection.RemoveRange(e.OldItems.OfType<TResult>());
                    if (e.NewItems is { })
                        rangeObservableCollection.AddRange(e.NewItems.OfType<TResult>());
                }
            });

        return synchronizedSource.Execute(() =>
        {
            rangeObservableCollection = new SynchronizedRangeObservableCollection<TResult>(synchronizedSource?.SynchronizationContext, source.OfType<TResult>());

            if (notifyingSource is { })
                notifyingSource.CollectionChanged += collectionChanged;

            return new ActiveEnumerable<TResult>(rangeObservableCollection, source as INotifyElementFaultChanges, () =>
            {
                if (notifyingSource is { })
                    notifyingSource.CollectionChanged -= collectionChanged;
            });
        });
    }

    #endregion OfType

    #region OrderBy

    /// <summary>
    /// Actively sorts the elements of a sequence in ascending order according to a key using the <see cref="IndexingStrategy.HashTable"/> indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">A sequence of values to order</param>
    /// <param name="keySelectorExpression">An expression to extract a key from an element</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> whose elements are sorted according to a key</returns>
    public static IActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, IComparable>> keySelectorExpression) =>
        ActiveOrderBy(source, keySelectorExpression, false);

    /// <summary>
    /// Actively sorts the elements of a sequence in the specified order according to a key using the <see cref="IndexingStrategy.HashTable"/> indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">A sequence of values to order</param>
    /// <param name="keySelectorExpression">An expression to extract a key from an element</param>
    /// <param name="isDescending"><c>true</c> to sort in descending order; otherwise, sort in ascending order</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> whose elements are sorted according to a key</returns>
    public static IActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, IComparable>> keySelectorExpression, bool isDescending) =>
        ActiveOrderBy(source, keySelectorExpression, null, isDescending);

    /// <summary>
    /// Actively sorts the elements of a sequence in ascending order according to a key using the <see cref="IndexingStrategy.HashTable"/> indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">A sequence of values to order</param>
    /// <param name="keySelectorExpression">An expression to extract a key from an element</param>
    /// <param name="keySelectorExpressionOptions">Options governing the behavior of active expressions created using <paramref name="keySelectorExpression"/></param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> whose elements are sorted according to a key</returns>
    public static IActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, IComparable>> keySelectorExpression, ActiveExpressionOptions? keySelectorExpressionOptions) =>
        ActiveOrderBy(source, keySelectorExpression, keySelectorExpressionOptions, false);

    /// <summary>
    /// Actively sorts the elements of a sequence in the specified order according to a key using the <see cref="IndexingStrategy.HashTable"/> indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">A sequence of values to order</param>
    /// <param name="keySelectorExpression">An expression to extract a key from an element</param>
    /// <param name="keySelectorExpressionOptions">Options governing the behavior of active expressions created using <paramref name="keySelectorExpression"/></param>
    /// <param name="isDescending"><c>true</c> to sort in descending order; otherwise, sort in ascending order</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> whose elements are sorted according to a key</returns>
    public static IActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, IComparable>> keySelectorExpression, ActiveExpressionOptions? keySelectorExpressionOptions, bool isDescending) =>
        ActiveOrderBy(source, new ActiveOrderingKeySelector<TSource>(keySelectorExpression, keySelectorExpressionOptions, isDescending));

    /// <summary>
    /// Actively sorts the elements of a sequence according to a series of <see cref="ActiveOrderingKeySelector{T}"/> objects using the <see cref="IndexingStrategy.HashTable"/> indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">A sequence of values to order</param>
    /// <param name="keySelectors">A series of <see cref="ActiveOrderingKeySelector{T}"/>, the position of each determining its ordering priority</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> whose elements are sorted according to keys</returns>
    public static IActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, params ActiveOrderingKeySelector<TSource>[] keySelectors) =>
        ActiveOrderBy(source, IndexingStrategy.HashTable, keySelectors);

    /// <summary>
    /// Actively sorts the elements of a sequence in ascending order according to a key using the specified indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">A sequence of values to order</param>
    /// <param name="indexingStrategy">The indexing strategy to use</param>
    /// <param name="keySelectorExpression">An expression to extract a key from an element</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> whose elements are sorted according to a key</returns>
    public static IActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, IndexingStrategy indexingStrategy, Expression<Func<TSource, IComparable>> keySelectorExpression) =>
        ActiveOrderBy(source, indexingStrategy, keySelectorExpression, false);

    /// <summary>
    /// Actively sorts the elements of a sequence in the specified order according to a key using the specified indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">A sequence of values to order</param>
    /// <param name="indexingStrategy">The indexing strategy to use</param>
    /// <param name="keySelectorExpression">An expression to extract a key from an element</param>
    /// <param name="isDescending"><c>true</c> to sort in descending order; otherwise, sort in ascending order</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> whose elements are sorted according to a key</returns>
    public static IActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, IndexingStrategy indexingStrategy, Expression<Func<TSource, IComparable>> keySelectorExpression, bool isDescending) =>
        ActiveOrderBy(source, indexingStrategy, keySelectorExpression, null, isDescending);

    /// <summary>
    /// Actively sorts the elements of a sequence in ascending order according to a key using the specified indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">A sequence of values to order</param>
    /// <param name="indexingStrategy">The indexing strategy to use</param>
    /// <param name="keySelectorExpression">An expression to extract a key from an element</param>
    /// <param name="keySelectorExpressionOptions">Options governing the behavior of active expressions created using <paramref name="keySelectorExpression"/></param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> whose elements are sorted according to a key</returns>
    public static IActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, IndexingStrategy indexingStrategy, Expression<Func<TSource, IComparable>> keySelectorExpression, ActiveExpressionOptions? keySelectorExpressionOptions) =>
        ActiveOrderBy(source, indexingStrategy, keySelectorExpression, keySelectorExpressionOptions, false);

    /// <summary>
    /// Actively sorts the elements of a sequence in the specified order according to a key using the specified indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">A sequence of values to order</param>
    /// <param name="indexingStrategy">The indexing strategy to use</param>
    /// <param name="keySelectorExpression">An expression to extract a key from an element</param>
    /// <param name="keySelectorExpressionOptions">Options governing the behavior of active expressions created using <paramref name="keySelectorExpression"/></param>
    /// <param name="isDescending"><c>true</c> to sort in descending order; otherwise, sort in ascending order</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> whose elements are sorted according to a key</returns>
    public static IActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, IndexingStrategy indexingStrategy, Expression<Func<TSource, IComparable>> keySelectorExpression, ActiveExpressionOptions? keySelectorExpressionOptions, bool isDescending) =>
        ActiveOrderBy(source, indexingStrategy, new ActiveOrderingKeySelector<TSource>(keySelectorExpression, keySelectorExpressionOptions, isDescending));

    /// <summary>
    /// Actively sorts the elements of a sequence according to a series of <see cref="ActiveOrderingKeySelector{T}"/> objects using the specified indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">A sequence of values to order</param>
    /// <param name="indexingStrategy">The indexing strategy to use</param>
    /// <param name="keySelectors">A series of <see cref="ActiveOrderingKeySelector{T}"/>, the position of each determining its ordering priority</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> whose elements are sorted according to keys</returns>
    public static IActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, IndexingStrategy indexingStrategy, params ActiveOrderingKeySelector<TSource>[] keySelectors)
    {
        if ((keySelectors?.Length ?? 0) == 0)
            return ToActiveEnumerable(source);

        keySelectors = keySelectors.Select(s => new ActiveOrderingKeySelector<TSource>(ActiveQueryOptions.Optimize(s.Expression), s.ExpressionOptions, s.IsDescending)).ToArray();

        ActiveOrderingComparer<TSource>? comparer = null;
        var equalityComparer = EqualityComparer<TSource>.Default;
        SynchronizedRangeObservableCollection<TSource>? rangeObservableCollection = null;
        IDictionary<TSource, (int startingIndex, int count)>? startingIndiciesAndCounts = null;
        var startingIndiciesAndCountsAccess = indexingStrategy is not IndexingStrategy.NoneOrInherit ? new object() : null;
        var synchronizedSource = source as ISynchronized;

        void rebuildStartingIndiciesAndCounts(IReadOnlyList<TSource> fromSort)
        {
            switch (indexingStrategy)
            {
                case IndexingStrategy.HashTable:
                    startingIndiciesAndCounts = new NullableKeyDictionary<TSource, (int startingIndex, int count)>();
                    break;
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    startingIndiciesAndCounts = new NullableKeySortedDictionary<TSource, (int startingIndex, int count)>();
                    break;
            }
            for (var i = 0; i < fromSort.Count; ++i)
            {
                var element = fromSort[i];
                if (startingIndiciesAndCounts!.TryGetValue(element, out var startingIndexAndCount))
                    startingIndiciesAndCounts[element] = (startingIndexAndCount.startingIndex, startingIndexAndCount.count + 1);
                else
                    startingIndiciesAndCounts.Add(element, (i, 1));
            }
        }

        void repositionElement(TSource element)
        {
            int startingIndex, count;
            if (indexingStrategy == IndexingStrategy.NoneOrInherit)
            {
                var indicies = rangeObservableCollection!.IndiciesOf(element);
                count = indicies.Count();
                if (count == 0)
                    return;
                startingIndex = indicies.First();
            }
            else if (startingIndiciesAndCounts!.TryGetValue(element, out var startingIndexAndCount))
                (startingIndex, count) = startingIndexAndCount;
            else
                return;
            var index = startingIndex;

            bool performMove()
            {
                if (startingIndex != index)
                {
                    if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                        startingIndiciesAndCounts![element] = (index, count);
                    rangeObservableCollection!.MoveRange(startingIndex, index, count);
                    return true;
                }
                return false;
            }

            if (indexingStrategy == IndexingStrategy.NoneOrInherit)
            {
                while (index > 0 && comparer!.Compare(element, rangeObservableCollection![index - 1]) < 0)
                    --index;
                while (index < rangeObservableCollection!.Count - count && comparer!.Compare(element, rangeObservableCollection[index + count]) > 0)
                    ++index;
                performMove();
            }
            else
            {
                while (index > 0)
                {
                    var otherElement = rangeObservableCollection![index - 1];
                    if (comparer!.Compare(element, otherElement) >= 0)
                        break;
                    var (otherStartingIndex, otherCount) = startingIndiciesAndCounts![otherElement];
                    startingIndiciesAndCounts[otherElement] = (otherStartingIndex + count, otherCount);
                    index -= otherCount;
                }
                if (!performMove())
                {
                    while (index < rangeObservableCollection!.Count - count)
                    {
                        var otherElement = rangeObservableCollection[index + count];
                        if (comparer!.Compare(element, otherElement) <= 0)
                            break;
                        var (otherStartingIndex, otherCount) = startingIndiciesAndCounts![otherElement];
                        startingIndiciesAndCounts[otherElement] = (otherStartingIndex - count, otherCount);
                        index += otherCount;
                    }
                    performMove();
                }
            }
        }

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            synchronizedSource.Execute(() =>
            {
                if (startingIndiciesAndCountsAccess is not null)
                    Monitor.Enter(startingIndiciesAndCountsAccess);
                try
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        var sortedSource = source.ToList();
                        sortedSource.Sort(comparer);
                        if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                            rebuildStartingIndiciesAndCounts(sortedSource);
                        rangeObservableCollection!.Reset(sortedSource);
                    }
                    else if (e.Action != NotifyCollectionChangedAction.Move)
                    {
                        if (e.OldItems is { } && e.OldItems.Count > 0)
                        {
                            if (e.OldItems.Count == rangeObservableCollection!.Count)
                            {
                                switch (indexingStrategy)
                                {
                                    case IndexingStrategy.HashTable:
                                        startingIndiciesAndCounts = new Dictionary<TSource, (int startingIndex, int count)>();
                                        break;
                                    case IndexingStrategy.SelfBalancingBinarySearchTree:
                                        startingIndiciesAndCounts = new SortedDictionary<TSource, (int startingIndex, int count)>();
                                        break;
                                }
                                rangeObservableCollection.Clear();
                            }
                            else if (indexingStrategy == IndexingStrategy.NoneOrInherit)
                                foreach (var elementAndResults in e.OldItems.Cast<(TSource element, IComparable? comparable)>().GroupBy(er => er.element, er => er.comparable))
                                    rangeObservableCollection.RemoveRange(rangeObservableCollection.IndexOf(elementAndResults.Key), elementAndResults.Count());
                            else
                                foreach (var elementAndResults in e.OldItems.Cast<(TSource element, IComparable? comparable)>().GroupBy(er => er.element, er => er.comparable))
                                {
                                    var element = elementAndResults.Key;
                                    var (startingIndex, currentCount) = startingIndiciesAndCounts![element];
                                    var removedCount = elementAndResults.Count();
                                    rangeObservableCollection.RemoveRange(startingIndex, removedCount);
                                    if (removedCount == currentCount)
                                        startingIndiciesAndCounts.Remove(element);
                                    else
                                        startingIndiciesAndCounts[element] = (startingIndex, currentCount - removedCount);
                                    foreach (var otherElement in startingIndiciesAndCounts.Keys.ToImmutableArray())
                                    {
                                        var (otherStartingIndex, otherCount) = startingIndiciesAndCounts[otherElement];
                                        if (otherStartingIndex > startingIndex)
                                            startingIndiciesAndCounts[otherElement] = (otherStartingIndex - removedCount, otherCount);
                                    }
                                }
                        }
                        if ((e.NewItems?.Count ?? 0) > 0)
                        {
                            if (rangeObservableCollection!.Count == 0)
                            {
                                var sorted = e.NewItems.Cast<(TSource element, IComparable? comparable)>().Select(er => er.element).ToList();
                                sorted.Sort(comparer);
                                if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                                    rebuildStartingIndiciesAndCounts(sorted);
                                rangeObservableCollection.Reset(sorted);
                            }
                            else
                                foreach (var elementAndResults in e.NewItems.Cast<(TSource element, IComparable? comparable)>().GroupBy(er => er.element, er => er.comparable))
                                {
                                    var element = elementAndResults.Key;
                                    var count = elementAndResults.Count();
                                    var index = 0;
                                    while (index < rangeObservableCollection.Count && comparer!.Compare(element, rangeObservableCollection[index]) >= 0)
                                        ++index;
                                    if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                                        foreach (var startingIndexAndCountKv in startingIndiciesAndCounts.ToList())
                                        {
                                            var otherElement = startingIndexAndCountKv.Key;
                                            if (!equalityComparer!.Equals(otherElement, element))
                                            {
                                                var (otherStartingIndex, otherCount) = startingIndexAndCountKv.Value;
                                                if (otherStartingIndex >= index)
                                                    startingIndiciesAndCounts![otherElement] = (otherStartingIndex + count, otherCount);
                                            }
                                        }
                                    rangeObservableCollection.InsertRange(index, Enumerable.Range(0, count).Select(i => element));
                                    if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                                    {
                                        if (startingIndiciesAndCounts!.TryGetValue(element, out var startingIndexAndCount))
                                            startingIndiciesAndCounts[element] = (startingIndexAndCount.startingIndex, startingIndexAndCount.count + count);
                                        else
                                            startingIndiciesAndCounts.Add(element, (index, count));
                                    }
                                }
                        }
                    }
                }
                finally
                {
                    if (startingIndiciesAndCountsAccess is not null)
                        Monitor.Exit(startingIndiciesAndCountsAccess);
                }
            });

        void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, IComparable?> e) =>
            synchronizedSource.Execute(() =>
            {
                if (startingIndiciesAndCountsAccess is not null)
                    Monitor.Enter(startingIndiciesAndCountsAccess);
                try
                {
                    repositionElement(e.Element! /* this could be null, but it won't matter if it is */);
                }
                finally
                {
                    if (startingIndiciesAndCountsAccess is not null)
                        Monitor.Exit(startingIndiciesAndCountsAccess);
                }
            });

        return synchronizedSource.Execute(() =>
        {
            if (startingIndiciesAndCountsAccess is not null)
                Monitor.Enter(startingIndiciesAndCountsAccess);
            try
            {
                var keySelections = keySelectors.Select(selector => (rangeActiveExpression: EnumerableRangeActiveExpression<TSource, IComparable>.Create(source, selector.Expression, selector.ExpressionOptions), isDescending: selector.IsDescending)).ToList();
                comparer = new ActiveOrderingComparer<TSource>(keySelections.Select(selection => (selection.rangeActiveExpression, selection.isDescending)).ToList(), indexingStrategy);
                var (lastRangeActiveExpression, lastIsDescending) = keySelections[^1];
                var sortedSource = source.ToList();
                sortedSource.Sort(comparer);

                if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                    rebuildStartingIndiciesAndCounts(sortedSource);

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizedSource?.SynchronizationContext, sortedSource);
                var mergedElementFaultChangeNotifier = new MergedElementFaultChangeNotifier(keySelections.Select(selection => selection.rangeActiveExpression));
                lastRangeActiveExpression.CollectionChanged += collectionChanged;
                foreach (var (rangeActiveExpression, isDescending) in keySelections)
                    rangeActiveExpression.ElementResultChanged += elementResultChanged;
                return new ActiveEnumerable<TSource>(rangeObservableCollection, mergedElementFaultChangeNotifier, () =>
                {
                    comparer.Dispose();
                    lastRangeActiveExpression.CollectionChanged -= collectionChanged;
                    foreach (var (rangeActiveExpression, isDescending) in keySelections)
                    {
                        rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                        rangeActiveExpression.Dispose();
                    }
                    mergedElementFaultChangeNotifier.Dispose();
                });
            }
            finally
            {
                if (startingIndiciesAndCountsAccess is not null)
                    Monitor.Exit(startingIndiciesAndCountsAccess);
            }
        });
    }

    #endregion OrderBy

    #region Select

    /// <summary>
    /// Actively projects each element of a sequence into a new form using the specified indexing strategy
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
    /// <param name="source">A sequence of values to invoke a transform function on</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
    public static IActiveEnumerable<TResult?> ActiveSelect<TResult>(this IEnumerable source, Expression<Func<object?, TResult>> selector) =>
        ActiveSelect(source, selector, null);

    /// <summary>
    /// Actively projects each element of a sequence into a new form using the specified indexing strategy
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
    /// <param name="source">A sequence of values to invoke a transform function on</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
    public static IActiveEnumerable<TResult?> ActiveSelect<TResult>(this IEnumerable source, Expression<Func<object?, TResult>> selector, ActiveExpressionOptions? selectorOptions) =>
        ActiveSelect(source, selector, selectorOptions, false);

    /// <summary>
    /// Actively projects each element of a sequence into a new form using the specified indexing strategy
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
    /// <param name="source">A sequence of values to invoke a transform function on</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="parallel"><c>true</c> if the query should be initialized in parallel; otherwise, <c>false</c> (default)</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
    public static IActiveEnumerable<TResult?> ActiveSelect<TResult>(this IEnumerable source, Expression<Func<object?, TResult>> selector, bool parallel) =>
        ActiveSelect(source, selector, null, parallel);

    /// <summary>
    /// Actively projects each element of a sequence into a new form using the specified indexing strategy
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
    /// <param name="source">A sequence of values to invoke a transform function on</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
    /// <param name="parallel"><c>true</c> if the query should be initialized in parallel; otherwise, <c>false</c> (default)</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
    public static IActiveEnumerable<TResult?> ActiveSelect<TResult>(this IEnumerable source, Expression<Func<object?, TResult>> selector, ActiveExpressionOptions? selectorOptions, bool parallel)
    {
        ActiveQueryOptions.Optimize(ref selector);
        return ActiveSelectEnumerable<TResult>.Get(source, selector, selectorOptions, parallel);
    }

    /// <summary>
    /// Actively projects each element of a sequence into a new form using the <see cref="IndexingStrategy.HashTable"/> indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
    /// <param name="source">A sequence of values to invoke a transform function on</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
    public static IActiveEnumerable<TResult?> ActiveSelect<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector) =>
        ActiveSelect(source, selector, null);

    /// <summary>
    /// Actively projects each element of a sequence into a new form using the specified indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
    /// <param name="source">A sequence of values to invoke a transform function on</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
    public static IActiveEnumerable<TResult?> ActiveSelect<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions) =>
        ActiveSelect(source, selector, selectorOptions, false);

    /// <summary>
    /// Actively projects each element of a sequence into a new form using the <see cref="IndexingStrategy.HashTable"/> indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
    /// <param name="source">A sequence of values to invoke a transform function on</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="parallel"><c>true</c> if the query should be initialized in parallel; otherwise, <c>false</c> (default)</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
    public static IActiveEnumerable<TResult?> ActiveSelect<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, bool parallel) =>
        ActiveSelect(source, selector, null, parallel);

    /// <summary>
    /// Actively projects each element of a sequence into a new form using the specified indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
    /// <param name="source">A sequence of values to invoke a transform function on</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
    /// <param name="parallel"><c>true</c> if the query should be initialized in parallel; otherwise, <c>false</c> (default)</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
    public static IActiveEnumerable<TResult?> ActiveSelect<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions, bool parallel)
    {
        ActiveQueryOptions.Optimize(ref selector);
        return ActiveSelectEnumerable<TSource, TResult>.Get(source, selector, selectorOptions, parallel);
    }

    #endregion Select

    #region SelectMany

    /// <summary>
    /// Actively projects each element of a sequence to an <see cref="IEnumerable{T}"/> and flattens the resulting sequences into one sequence using the <see cref="IndexingStrategy.HashTable"/> indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
    /// <param name="source">A sequence of values to project</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the one-to-many transform function on each element of the input sequence</returns>
    public static IActiveEnumerable<TResult> ActiveSelectMany<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, IEnumerable<TResult>>> selector) =>
        ActiveSelectMany(source, selector, null);

    /// <summary>
    /// Actively projects each element of a sequence to an <see cref="IEnumerable{T}"/> and flattens the resulting sequences into one sequence using the <see cref="IndexingStrategy.HashTable"/> indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
    /// <param name="source">A sequence of values to project</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the one-to-many transform function on each element of the input sequence</returns>
    public static IActiveEnumerable<TResult> ActiveSelectMany<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, IEnumerable<TResult>>> selector, ActiveExpressionOptions? selectorOptions) =>
        ActiveSelectMany(source, selector, selectorOptions, false);

    /// <summary>
    /// Actively projects each element of a sequence to an <see cref="IEnumerable{T}"/> and flattens the resulting sequences into one sequence using the specified indexing strategy
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
    /// <param name="source">A sequence of values to project</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
    /// <param name="parallel"><c>true</c> if the query should be initialized in parallel; otherwise, <c>false</c> (default)</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the one-to-many transform function on each element of the input sequence</returns>
    public static IActiveEnumerable<TResult> ActiveSelectMany<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, IEnumerable<TResult>>> selector, ActiveExpressionOptions? selectorOptions, bool parallel)
    {
        ActiveQueryOptions.Optimize(ref selector);
        return ActiveSelectManyEnumerable<TSource, TResult>.Get(source, selector, selectorOptions, parallel);
    }

    #endregion SelectMany

    #region Single

    /// <summary>
    /// Actively returns the only element of a sequence, and becomes faulted if there is not exactly one element in the sequence
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return the single element of</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the single element of the input sequence</returns>
    public static IActiveValue<TSource?> ActiveSingle<TSource>(this IEnumerable<TSource> source)
    {
        var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
        if (source is INotifyCollectionChanged changingSource)
        {
            var synchronizedSource = source as ISynchronized;
            ActiveValue<TSource?>? activeValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizedSource.Execute(() =>
                {
                    try
                    {
                        var value = source.Single();
                        activeValue!.OperationFault = null;
                        activeValue!.Value = value;
                    }
                    catch (Exception ex)
                    {
                        activeValue!.OperationFault = ex;
                        activeValue!.Value = default;
                    }
                });

            return synchronizedSource.Execute(() =>
            {
                try
                {
                    activeValue = new ActiveValue<TSource?>(source.Single(), elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changingSource.CollectionChanged -= collectionChanged);
                    changingSource.CollectionChanged += collectionChanged;
                    return activeValue;
                }
                catch (Exception ex)
                {
                    activeValue = new ActiveValue<TSource?>(default!, ex, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    changingSource.CollectionChanged += collectionChanged;
                    return activeValue;
                }
            });
        }
        try
        {
            return new ActiveValue<TSource?>(source.Single(), elementFaultChangeNotifier: elementFaultChangeNotifier);
        }
        catch (Exception ex)
        {
            return new ActiveValue<TSource?>(default, ex, elementFaultChangeNotifier);
        }
    }

    /// <summary>
    /// Actively returns the only element of a sequence that satisfies a specified condition, and becomes faulted if more than one such element exists
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return the single element of</param>
    /// <param name="predicate">A function to test an element for a condition</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the single element of the input sequence that satisfies a condition</returns>
    public static IActiveValue<TSource?> ActiveSingle<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate) =>
        ActiveSingle(source, predicate, null);

    /// <summary>
    /// Actively returns the only element of a sequence that satisfies a specified condition, and becomes faulted if more than one such element exists
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return the single element of</param>
    /// <param name="predicate">A function to test an element for a condition</param>
    /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the single element of the input sequence that satisfies a condition</returns>
    public static IActiveValue<TSource?> ActiveSingle<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions? predicateOptions)
    {
        IActiveEnumerable<TSource> where;
        ActiveValue<TSource?>? activeValue = null;
        var none = false;
        var moreThanOne = false;

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (none && where.Count > 0)
            {
                activeValue!.OperationFault = null;
                none = false;
            }
            else if (!none && where.Count == 0)
            {
                activeValue!.OperationFault = ExceptionHelper.SequenceContainsNoElements;
                none = true;
                moreThanOne = false;
            }
            if (moreThanOne && where.Count <= 1)
            {
                activeValue!.OperationFault = null;
                moreThanOne = false;
            }
            else if (!moreThanOne && where.Count > 1)
            {
                activeValue!.OperationFault = ExceptionHelper.SequenceContainsMoreThanOneElement;
                none = false;
                moreThanOne = true;
            }
            activeValue!.Value = where.Count == 1 ? where[0] : default;
        }

        return (source as ISynchronized).Execute(() =>
        {
            where = ActiveWhere(source, predicate, predicateOptions);
            Exception? operationFault = null;
            if (none = where.Count == 0)
                operationFault = ExceptionHelper.SequenceContainsNoElements;
            else if (moreThanOne = where.Count > 1)
                operationFault = ExceptionHelper.SequenceContainsMoreThanOneElement;
            activeValue = new ActiveValue<TSource?>(operationFault is null ? where[0] : default, operationFault, where, () =>
            {
                where.CollectionChanged -= collectionChanged;
                where.Dispose();
            });
            where.CollectionChanged += collectionChanged;
            return activeValue;
        });
    }

    #endregion Single

    #region SingleOrDefault

    /// <summary>
    /// Actively returns the only element of a sequence, or a default value if the sequence is empty; becomes faulted if there is more than one element in the sequence
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return the single element of</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the single element of the input sequence, or <c>default</c>(<typeparamref name="TSource"/>) if the sequence contains no elements</returns>
    public static IActiveValue<TSource?> ActiveSingleOrDefault<TSource>(this IEnumerable<TSource> source)
    {
        var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
        if (source is INotifyCollectionChanged changingSource)
        {
            var synchronizedSource = source as ISynchronized;
            ActiveValue<TSource?>? activeValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizedSource.Execute(() =>
                {
                    try
                    {
                        var value = source.SingleOrDefault();
                        activeValue!.OperationFault = null;
                        activeValue!.Value = value;
                    }
                    catch (Exception ex)
                    {
                        activeValue!.OperationFault = ex;
                        activeValue!.Value = default;
                    }
                });

            return synchronizedSource.Execute(() =>
            {
                try
                {
                    activeValue = new ActiveValue<TSource?>(source.SingleOrDefault(), null, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    changingSource.CollectionChanged += collectionChanged;
                    return activeValue;
                }
                catch (Exception ex)
                {
                    activeValue = new ActiveValue<TSource?>(default, ex, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    changingSource.CollectionChanged += collectionChanged;
                    return activeValue;
                }
            });
        }
        try
        {
            return new ActiveValue<TSource?>(source.SingleOrDefault(), elementFaultChangeNotifier: elementFaultChangeNotifier);
        }
        catch (Exception ex)
        {
            return new ActiveValue<TSource?>(default, ex, elementFaultChangeNotifier);
        }
    }

    /// <summary>
    /// Actively returns the only element of a sequence that satisfies a specified condition or a default value if no such element exists; becomes faulted if more than one element satisfies the condition
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return the single element of</param>
    /// <param name="predicate">A function to test an element for a condition</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the single element of the input sequence that satisfies the condition, or <c>default</c>(<typeparamref name="TSource"/>) if no such element is found</returns>
    public static IActiveValue<TSource?> ActiveSingleOrDefault<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate) =>
        ActiveSingleOrDefault(source, predicate, null);

    /// <summary>
    /// Actively returns the only element of a sequence that satisfies a specified condition or a default value if no such element exists; becomes faulted if more than one element satisfies the condition
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return the single element of</param>
    /// <param name="predicate">A function to test an element for a condition</param>
    /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the single element of the input sequence that satisfies the condition, or <c>default</c>(<typeparamref name="TSource"/>) if no such element is found</returns>
    public static IActiveValue<TSource?> ActiveSingleOrDefault<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions? predicateOptions)
    {
        IActiveEnumerable<TSource> where;
        ActiveValue<TSource?>? activeValue = null;
        var moreThanOne = false;

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (moreThanOne && where.Count <= 1)
            {
                activeValue!.OperationFault = null;
                moreThanOne = false;
            }
            else if (!moreThanOne && where.Count > 1)
            {
                activeValue!.OperationFault = ExceptionHelper.SequenceContainsMoreThanOneElement;
                moreThanOne = true;
            }
            activeValue!.Value = where.Count == 1 ? where[0] : default;
        }

        return (source as ISynchronized).Execute(() =>
        {
            where = ActiveWhere(source, predicate, predicateOptions);
            var operationFault = (moreThanOne = where.Count > 1) ? ExceptionHelper.SequenceContainsMoreThanOneElement : null;
            activeValue = new ActiveValue<TSource?>(!moreThanOne && where.Count == 1 ? where[0] : default, operationFault, where, () =>
            {
                where.CollectionChanged -= collectionChanged;
                where.Dispose();
            });
            where.CollectionChanged += collectionChanged;
            return activeValue;
        });
    }

    #endregion SingleOrDefault

    #region Sum

    /// <summary>
    /// Actively computes the sum of a sequence of values
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">A sequence of values to calculate the sum of</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the sum of the values in the sequence</returns>
    public static IActiveValue<TSource?> ActiveSum<TSource>(this IEnumerable<TSource> source) =>
        ActiveSum(source, element => element);

    /// <summary>
    /// Actively computes the sum of the sequence of values that are obtained by invoking a transform function on each element of the input sequence
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TResult">The type of the values being summed</typeparam>
    /// <param name="source">A sequence of values that are used to calculate a sum</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the sum of the projected values</returns>
    public static IActiveValue<TResult?> ActiveSum<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector) =>
        ActiveSum(source, selector, null);

    /// <summary>
    /// Actively computes the sum of the sequence of values that are obtained by invoking a transform function on each element of the input sequence
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TResult">The type of the values being summed</typeparam>
    /// <param name="source">A sequence of values that are used to calculate a sum</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
    /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the sum of the projected values</returns>
    public static IActiveValue<TResult?> ActiveSum<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions)
    {
        ActiveQueryOptions.Optimize(ref selector);

        var operations = new GenericOperations<TResult>();
        var synchronizedSource = source as ISynchronized;
        EnumerableRangeActiveExpression<TSource, TResult> rangeActiveExpression;
        ActiveValue<TResult?>? activeValue = null;
        var activeValueAccess = new object();
        var resultsChanging = new NullableKeyDictionary<TSource, (TResult? result, int instances)>();

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            synchronizedSource.Execute(() =>
            {
                lock (activeValueAccess)
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        try
                        {
                            activeValue!.Value = rangeActiveExpression.GetResults().Select(er => er.result).Aggregate(operations!.Add!);
                        }
                        catch (InvalidOperationException)
                        {
                            activeValue!.Value = default;
                        }
                    }
                    else if (e.Action != NotifyCollectionChangedAction.Move)
                    {
                        var sum = activeValue!.Value;
                        if ((e.OldItems?.Count ?? 0) > 0)
                            sum = (sum is null ? Enumerable.Empty<TResult?>() : new TResult?[] { sum }).Concat(e.OldItems.Cast<(TSource element, TResult? result)>().Select(er => er.result)).Aggregate(operations!.Subtract!);
                        if ((e.NewItems?.Count ?? 0) > 0)
                            sum = (sum is null ? Enumerable.Empty<TResult?>() : new TResult?[] { sum }).Concat(e.NewItems.Cast<(TSource element, TResult? result)>().Select(er => er.result)).Aggregate(operations!.Add!);
                        activeValue!.Value = sum;
                    }
                }
            });

        void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TResult?> e) =>
            synchronizedSource.Execute(() =>
            {
                lock (activeValueAccess)
                {
                    var (result, instances) = resultsChanging![e.Element];
                    resultsChanging.Remove(e.Element);
                    if (operations!.Subtract(e.Result, result) is TResult difference)
                        activeValue!.Value = operations.Add(activeValue!.Value, difference.Repeat(instances).Aggregate(operations.Add!));
                }
            });

        void elementResultChanging(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TResult?> e) =>
            synchronizedSource.Execute(() =>
            {
                lock (activeValueAccess)
                    resultsChanging!.Add(e.Element! /* this could be null, but it won't matter if it is */, (e.Result, e.Count));
            });

        return synchronizedSource.Execute(() =>
        {
            lock (activeValueAccess)
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);

                void dispose()
                {
                    rangeActiveExpression.CollectionChanged -= collectionChanged;
                    rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                    rangeActiveExpression.ElementResultChanging -= elementResultChanging;

                    rangeActiveExpression.Dispose();
                }

                try
                {
                    activeValue = new ActiveValue<TResult?>(rangeActiveExpression.GetResults().Select(er => er.result).Aggregate(operations.Add!), null, rangeActiveExpression, dispose);
                    rangeActiveExpression.CollectionChanged += collectionChanged;
                    rangeActiveExpression.ElementResultChanged += elementResultChanged;
                    rangeActiveExpression.ElementResultChanging += elementResultChanging;
                    return activeValue;
                }
                catch (InvalidOperationException)
                {
                    activeValue = new ActiveValue<TResult?>(default!, null, rangeActiveExpression, dispose);
                    rangeActiveExpression.CollectionChanged += collectionChanged;
                    rangeActiveExpression.ElementResultChanged += elementResultChanged;
                    rangeActiveExpression.ElementResultChanging += elementResultChanging;
                    return activeValue;
                }
            }
        });
    }

    #endregion Sum

    #region SwitchContext

    /// <summary>
    /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is kept consistent on the current thread's <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable"/> that implements <see cref="INotifyCollectionChanged"/> (use <see cref="SwitchContextEventually(IEnumerable)"/> instead when this method may produce a deadlock and/or only eventual consistency is required)
    /// </summary>
    /// <param name="source">An <see cref="IEnumerable"/></param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is kept consistent with <paramref name="source"/> on the current thread's <see cref="SynchronizationContext"/></returns>
    public static IActiveEnumerable<object> SwitchContext(this IEnumerable source) =>
        SwitchContext(source, SynchronizationContext.Current);

    /// <summary>
    /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is kept consistent on a specified <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable"/> that implements <see cref="INotifyCollectionChanged"/> (use <see cref="SwitchContextEventually(IEnumerable, SynchronizationContext)"/> instead when this method may produce a deadlock and/or only eventual consistency is required)
    /// </summary>
    /// <param name="source">An <see cref="IEnumerable"/></param>
    /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform consistency operations</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is kept consistent with <paramref name="source"/> on <paramref name="synchronizationContext"/></returns>
    public static IActiveEnumerable<object> SwitchContext(this IEnumerable source, SynchronizationContext synchronizationContext) =>
        SwitchContext(source, synchronizationContext, false);

    /// <summary>
    /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is kept consistent on the current thread's <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable"/> that implements <see cref="INotifyCollectionChanged"/> (use <see cref="SwitchContextEventually(IEnumerable)"/> instead when this method may produce a deadlock and/or only eventual consistency is required)
    /// </summary>
    /// <param name="source">An <see cref="IEnumerable"/></param>
    /// <param name="raiseCollectionChangedEventsForIndividualElements">Whether to raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon in batch</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is kept consistent with <paramref name="source"/> on the current thread's <see cref="SynchronizationContext"/></returns>
    public static IActiveEnumerable<object> SwitchContext(this IEnumerable source, bool raiseCollectionChangedEventsForIndividualElements) =>
        SwitchContext(source, SynchronizationContext.Current, raiseCollectionChangedEventsForIndividualElements);

    /// <summary>
    /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is kept consistent on a specified <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable"/> that implements <see cref="INotifyCollectionChanged"/> (use <see cref="SwitchContextEventually(IEnumerable, SynchronizationContext)"/> instead when this method may produce a deadlock and/or only eventual consistency is required)
    /// </summary>
    /// <param name="source">An <see cref="IEnumerable"/></param>
    /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform consistency operations</param>
    /// <param name="raiseCollectionChangedEventsForIndividualElements">Whether to raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon in batch</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is kept consistent with <paramref name="source"/> on <paramref name="synchronizationContext"/></returns>
    public static IActiveEnumerable<object> SwitchContext(this IEnumerable source, SynchronizationContext synchronizationContext, bool raiseCollectionChangedEventsForIndividualElements)
    {
        SynchronizedRangeObservableCollection<object>? rangeObservableCollection = null;

        async void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IReadOnlyList<object>? resetValues = null;
            if (e.Action == NotifyCollectionChangedAction.Reset)
                resetValues = source.Cast<object>().ToImmutableArray();
            await rangeObservableCollection.ExecuteAsync(() =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        rangeObservableCollection!.InsertRange(e.NewStartingIndex, e.NewItems.Cast<object>());
                        break;
                    case NotifyCollectionChangedAction.Move:
                        rangeObservableCollection!.MoveRange(e.OldStartingIndex, e.NewStartingIndex, e.OldItems.Count);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        rangeObservableCollection!.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        rangeObservableCollection!.ReplaceRange(e.OldStartingIndex, e.OldItems.Count, e.NewItems.Cast<object>());
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        rangeObservableCollection!.Reset(resetValues!);
                        break;
                }
            }).ConfigureAwait(false);
        }

        return (source as ISynchronized).Execute(() =>
        {
            var notifier = source as INotifyCollectionChanged;
            rangeObservableCollection = new SynchronizedRangeObservableCollection<object>(synchronizationContext, source.Cast<object>(), raiseCollectionChangedEventsForIndividualElements);
            if (notifier is { })
                notifier.CollectionChanged += collectionChanged;
            return new ActiveEnumerable<object>(rangeObservableCollection, source as INotifyElementFaultChanges, () =>
            {
                if (notifier is { })
                    notifier.CollectionChanged -= collectionChanged;
            });
        });
    }

    /// <summary>
    /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is kept consistent on the current thread's <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable{T}"/> that implements <see cref="INotifyCollectionChanged"/> (use <see cref="SwitchContextEventually{TSource}(IEnumerable{TSource})"/> instead when this method may produce a deadlock and/or only eventual consistency is required)
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of source</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/></param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is kept consistent with <paramref name="source"/> on the current thread's <see cref="SynchronizationContext"/></returns>
    public static IActiveEnumerable<TSource> SwitchContext<TSource>(this IEnumerable<TSource> source) =>
        SwitchContext(source, SynchronizationContext.Current);

    /// <summary>
    /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is kept consistent on a specified <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable{T}"/> that implements <see cref="INotifyCollectionChanged"/> (use <see cref="SwitchContextEventually{TSource}(IEnumerable{TSource}, SynchronizationContext)"/> instead when this method may produce a deadlock and/or only eventual consistency is required)
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of source</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/></param>
    /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform consistency operations</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is kept consistent with <paramref name="source"/> on <paramref name="synchronizationContext"/></returns>
    public static IActiveEnumerable<TSource> SwitchContext<TSource>(this IEnumerable<TSource> source, SynchronizationContext synchronizationContext) =>
        SwitchContext(source, synchronizationContext, false);

    /// <summary>
    /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is kept consistent on the current thread's <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable{T}"/> that implements <see cref="INotifyCollectionChanged"/> (use <see cref="SwitchContextEventually{TSource}(IEnumerable{TSource})"/> instead when this method may produce a deadlock and/or only eventual consistency is required)
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of source</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/></param>
    /// <param name="raiseCollectionChangedEventsForIndividualElements">Whether to raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon in batch</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is kept consistent with <paramref name="source"/> on the current thread's <see cref="SynchronizationContext"/></returns>
    public static IActiveEnumerable<TSource> SwitchContext<TSource>(this IEnumerable<TSource> source, bool raiseCollectionChangedEventsForIndividualElements) =>
        SwitchContext(source, SynchronizationContext.Current, raiseCollectionChangedEventsForIndividualElements);

    /// <summary>
    /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is kept consistent on a specified <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable{T}"/> that implements <see cref="INotifyCollectionChanged"/> (use <see cref="SwitchContextEventually{TSource}(IEnumerable{TSource}, SynchronizationContext)"/> instead when this method may produce a deadlock and/or only eventual consistency is required)
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of source</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/></param>
    /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform consistency operations</param>
    /// <param name="raiseCollectionChangedEventsForIndividualElements">Whether to raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon in batch</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is kept consistent with <paramref name="source"/> on <paramref name="synchronizationContext"/></returns>
    public static IActiveEnumerable<TSource> SwitchContext<TSource>(this IEnumerable<TSource> source, SynchronizationContext synchronizationContext, bool raiseCollectionChangedEventsForIndividualElements)
    {
        SynchronizedRangeObservableCollection<TSource>? rangeObservableCollection = null;

        async void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IReadOnlyList<TSource>? resetValues = null;
            if (e.Action == NotifyCollectionChangedAction.Reset)
                resetValues = source.ToImmutableArray();
            await rangeObservableCollection.ExecuteAsync(() =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        rangeObservableCollection!.InsertRange(e.NewStartingIndex, e.NewItems.Cast<TSource>());
                        break;
                    case NotifyCollectionChangedAction.Move:
                        rangeObservableCollection!.MoveRange(e.OldStartingIndex, e.NewStartingIndex, e.OldItems.Count);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        rangeObservableCollection!.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        rangeObservableCollection!.ReplaceRange(e.OldStartingIndex, e.OldItems.Count, e.NewItems.Cast<TSource>());
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        rangeObservableCollection!.Reset(resetValues!);
                        break;
                }
            }).ConfigureAwait(false);
        }

        return (source as ISynchronized).Execute(() =>
        {
            var notifier = source as INotifyCollectionChanged;
            rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizationContext, source, raiseCollectionChangedEventsForIndividualElements);
            if (notifier is not null)
                notifier.CollectionChanged += collectionChanged;
            return new ActiveEnumerable<TSource>(rangeObservableCollection, source as INotifyElementFaultChanges, () =>
            {
                if (notifier is not null)
                    notifier.CollectionChanged -= collectionChanged;
            });
        });
    }

    #endregion SwitchContext

    #region SwitchContextEventually

    /// <summary>
    /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is eventually made consistent on the current thread's <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable"/> that implements <see cref="INotifyCollectionChanged"/> (use this method instead of <see cref="SwitchContext(IEnumerable)"/> when the same may produce a deadlock and/or only eventual consistency is required)
    /// </summary>
    /// <param name="source">An <see cref="IEnumerable"/></param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is eventually made consistent with <paramref name="source"/> on the current thread's <see cref="SynchronizationContext"/></returns>
    public static IActiveEnumerable<object> SwitchContextEventually(this IEnumerable source) =>
        SwitchContextEventually(source, SynchronizationContext.Current);

    /// <summary>
    /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is eventually made consistent on a specified <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable"/> that implements <see cref="INotifyCollectionChanged"/> (use this method instead of <see cref="SwitchContext(IEnumerable, SynchronizationContext)"/> when the same may produce a deadlock and/or only eventual consistency is required)
    /// </summary>
    /// <param name="source">An <see cref="IEnumerable"/></param>
    /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform consistency operations</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is eventually made consistent with <paramref name="source"/> on <paramref name="synchronizationContext"/></returns>
    public static IActiveEnumerable<object> SwitchContextEventually(this IEnumerable source, SynchronizationContext synchronizationContext) =>
        SwitchContextEventually(source, synchronizationContext, false);

    /// <summary>
    /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is eventually made consistent on the current thread's <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable"/> that implements <see cref="INotifyCollectionChanged"/> (use this method instead of <see cref="SwitchContext(IEnumerable)"/> when the same may produce a deadlock and/or only eventual consistency is required)
    /// </summary>
    /// <param name="source">An <see cref="IEnumerable"/></param>
    /// <param name="raiseCollectionChangedEventsForIndividualElements">Whether to raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon in batch</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is eventually made consistent with <paramref name="source"/> on the current thread's <see cref="SynchronizationContext"/></returns>
    public static IActiveEnumerable<object> SwitchContextEventually(this IEnumerable source, bool raiseCollectionChangedEventsForIndividualElements) =>
        SwitchContextEventually(source, SynchronizationContext.Current, raiseCollectionChangedEventsForIndividualElements);

    /// <summary>
    /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is eventually made consistent on a specified <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable"/> that implements <see cref="INotifyCollectionChanged"/> (use this method instead of <see cref="SwitchContext(IEnumerable, SynchronizationContext)"/> when the same may produce a deadlock and/or only eventual consistency is required)
    /// </summary>
    /// <param name="source">An <see cref="IEnumerable"/></param>
    /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform consistency operations</param>
    /// <param name="raiseCollectionChangedEventsForIndividualElements">Whether to raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon in batch</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is eventually made consistent with <paramref name="source"/> on <paramref name="synchronizationContext"/></returns>
    [SuppressMessage("Code Analysis", "CA2000: Dispose objects before losing scope")]
    public static IActiveEnumerable<object> SwitchContextEventually(this IEnumerable source, SynchronizationContext synchronizationContext, bool raiseCollectionChangedEventsForIndividualElements)
    {
        var notifier = source as INotifyCollectionChanged;
        var queue = new AsyncProcessingQueue<Func<Task>>(async asyncAction => await asyncAction().ConfigureAwait(false));
        var rangeObservableCollection = new SynchronizedRangeObservableCollection<object>(synchronizationContext, raiseCollectionChangedEventsForIndividualElements);

        void unhandledException(object sender, ProcessingQueueUnhandledExceptionEventArgs<Func<Task>> e) =>
            collectionChanged(source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

        queue.UnhandledException += unhandledException;

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var oldStartingIndex = e.OldStartingIndex;
            var oldItems = (e.OldItems?.Cast<object>() ?? Enumerable.Empty<object>()).ToImmutableArray();
            var newStartingIndex = e.NewStartingIndex;
            var newItems = (e.NewItems?.Cast<object>() ?? Enumerable.Empty<object>()).ToImmutableArray();
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    queue.Enqueue(() => rangeObservableCollection.InsertRangeAsync(newStartingIndex, newItems));
                    break;
                case NotifyCollectionChangedAction.Move:
                    queue.Enqueue(() => rangeObservableCollection.MoveRangeAsync(oldStartingIndex, newStartingIndex, oldItems.Length));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    queue.Enqueue(() => rangeObservableCollection.RemoveRangeAsync(oldStartingIndex, oldItems.Length));
                    break;
                case NotifyCollectionChangedAction.Replace:
                    queue.Enqueue(() => rangeObservableCollection.ReplaceRangeAsync(oldStartingIndex, oldItems.Length, newItems));
                    break;
                case NotifyCollectionChangedAction.Reset:
                    var resetItems = (source as ISynchronized).Execute(() => source.Cast<object>().ToImmutableArray());
                    queue.Enqueue(() => rangeObservableCollection.ResetAsync(resetItems));
                    break;
            }
        }

        collectionChanged(source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

        if (notifier is { })
            notifier.CollectionChanged += collectionChanged;

        return new ActiveEnumerable<object>(rangeObservableCollection, source as INotifyElementFaultChanges, () =>
        {
            if (notifier is { })
                notifier.CollectionChanged -= collectionChanged;
            queue.UnhandledException -= unhandledException;
            queue.Dispose();
        });
    }

    /// <summary>
    /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is eventually made consistent on the current thread's <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable{T}"/> that implements <see cref="INotifyCollectionChanged"/> (use this method instead of <see cref="SwitchContext{TSource}(IEnumerable{TSource})"/> when the same may produce a deadlock and/or only eventual consistency is required)
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of source</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/></param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is eventually made consistent with <paramref name="source"/> on the current thread's <see cref="SynchronizationContext"/></returns>
    public static IActiveEnumerable<TSource> SwitchContextEventually<TSource>(this IEnumerable<TSource> source) =>
        SwitchContextEventually(source, SynchronizationContext.Current);

    /// <summary>
    /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is eventually made consistent on a specified <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable{T}"/> that implements <see cref="INotifyCollectionChanged"/> (use this method instead of <see cref="SwitchContext{TSource}(IEnumerable{TSource}, SynchronizationContext)"/> when the same may produce a deadlock and/or only eventual consistency is required)
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of source</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/></param>
    /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform consistency operations</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is eventually made consistent with <paramref name="source"/> on <paramref name="synchronizationContext"/></returns>
    public static IActiveEnumerable<TSource> SwitchContextEventually<TSource>(this IEnumerable<TSource> source, SynchronizationContext synchronizationContext) =>
        SwitchContextEventually(source, synchronizationContext, false);

    /// <summary>
    /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is eventually made consistent on the current thread's <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable{T}"/> that implements <see cref="INotifyCollectionChanged"/> (use this method instead of <see cref="SwitchContext{TSource}(IEnumerable{TSource})"/> when the same may produce a deadlock and/or only eventual consistency is required)
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of source</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/></param>
    /// <param name="raiseCollectionChangedEventsForIndividualElements">Whether to raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon in batch</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is eventually made consistent with <paramref name="source"/> on the current thread's <see cref="SynchronizationContext"/></returns>
    public static IActiveEnumerable<TSource> SwitchContextEventually<TSource>(this IEnumerable<TSource> source, bool raiseCollectionChangedEventsForIndividualElements) =>
        SwitchContextEventually(source, SynchronizationContext.Current, raiseCollectionChangedEventsForIndividualElements);

    /// <summary>
    /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is eventually made consistent on a specified <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable{T}"/> that implements <see cref="INotifyCollectionChanged"/> (use this method instead of <see cref="SwitchContext{TSource}(IEnumerable{TSource}, SynchronizationContext)"/> when the same may produce a deadlock and/or only eventual consistency is required)
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of source</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/></param>
    /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform consistency operations</param>
    /// <param name="raiseCollectionChangedEventsForIndividualElements">Whether to raise individual <see cref="INotifyCollectionChanged.CollectionChanged"/> events for each element operated upon in batch</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is eventually made consistent with <paramref name="source"/> on <paramref name="synchronizationContext"/></returns>
    [SuppressMessage("Code Analysis", "CA2000: Dispose objects before losing scope")]
    public static IActiveEnumerable<TSource> SwitchContextEventually<TSource>(this IEnumerable<TSource> source, SynchronizationContext synchronizationContext, bool raiseCollectionChangedEventsForIndividualElements)
    {
        var notifier = source as INotifyCollectionChanged;
        var queue = new AsyncProcessingQueue<Func<Task>>(async asyncAction => await asyncAction().ConfigureAwait(false));
        var rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizationContext, raiseCollectionChangedEventsForIndividualElements);

        void unhandledException(object sender, ProcessingQueueUnhandledExceptionEventArgs<Func<Task>> e) =>
            collectionChanged(source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

        queue.UnhandledException += unhandledException;

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var oldStartingIndex = e.OldStartingIndex;
            var oldItems = (e.OldItems?.Cast<TSource>() ?? Enumerable.Empty<TSource>()).ToImmutableArray();
            var newStartingIndex = e.NewStartingIndex;
            var newItems = (e.NewItems?.Cast<TSource>() ?? Enumerable.Empty<TSource>()).ToImmutableArray();
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    queue.Enqueue(() => rangeObservableCollection.InsertRangeAsync(newStartingIndex, newItems));
                    break;
                case NotifyCollectionChangedAction.Move:
                    queue.Enqueue(() => rangeObservableCollection.MoveRangeAsync(oldStartingIndex, newStartingIndex, oldItems.Length));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    queue.Enqueue(() => rangeObservableCollection.RemoveRangeAsync(oldStartingIndex, oldItems.Length));
                    break;
                case NotifyCollectionChangedAction.Replace:
                    queue.Enqueue(() => rangeObservableCollection.ReplaceRangeAsync(oldStartingIndex, oldItems.Length, newItems));
                    break;
                case NotifyCollectionChangedAction.Reset:
                    queue.Enqueue(async () =>
                    {
                        var resetItems = await (source as ISynchronized).ExecuteAsync(() => source.ToImmutableArray()).ConfigureAwait(false);
                        await rangeObservableCollection.ResetAsync(resetItems).ConfigureAwait(false);
                    });
                    break;
            }
        }

        collectionChanged(source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

        if (notifier is { })
            notifier.CollectionChanged += collectionChanged;

        return new ActiveEnumerable<TSource>(rangeObservableCollection, source as INotifyElementFaultChanges, () =>
        {
            if (notifier is { })
                notifier.CollectionChanged -= collectionChanged;
            queue.UnhandledException -= unhandledException;
            queue.Dispose();
        });
    }

    #endregion SwitchContextEventually

    #region ToActiveEnumerable

    /// <summary>
    /// Converts an <see cref="IEnumerable{T}"/> into an <see cref="IActiveEnumerable{TElement}"/>
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to convert</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> equivalent to <paramref name="source"/> (and mutates with it so long as <paramref name="source"/> implements <see cref="INotifyCollectionChanged"/>)</returns>
    public static IActiveEnumerable<TSource> ToActiveEnumerable<TSource>(this IEnumerable<TSource> source)
    {
        if (source is IReadOnlyList<TSource> readOnlyList and ISynchronized)
            return new ActiveEnumerable<TSource>(readOnlyList);

        var changingSource = source as INotifyCollectionChanged;
        var synchronizedSource = source as ISynchronized;
        SynchronizedRangeObservableCollection<TSource>? rangeObservableCollection = null;

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            synchronizedSource.Execute(() =>
            {
                var oldItems = e.OldItems is { } ? e.OldItems.Cast<TSource>() : Enumerable.Empty<TSource>();
                var oldItemsCount = e.OldItems is { } ? e.OldItems.Count : 0;
                var newItems = e.NewItems is { } ? e.NewItems.Cast<TSource>() : Enumerable.Empty<TSource>();
                var newItemsCount = e.NewItems is { } ? e.NewItems.Count : 0;
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Reset:
                        rangeObservableCollection!.Reset(source);
                        break;
                    case NotifyCollectionChangedAction.Move when oldItemsCount > 0:
                        rangeObservableCollection.MoveRange(e.OldStartingIndex, e.NewStartingIndex, oldItemsCount);
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        rangeObservableCollection!.ReplaceRange(e.OldStartingIndex, oldItemsCount, newItems);
                        break;
                    default:
                        if (oldItemsCount > 0)
                            rangeObservableCollection!.RemoveRange(e.OldStartingIndex, oldItemsCount);
                        if (newItemsCount > 0)
                            rangeObservableCollection!.InsertRange(e.NewStartingIndex, newItems);
                        break;
                }
            });

        return synchronizedSource.Execute(() =>
        {
            rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizedSource?.SynchronizationContext, source);
            if (changingSource is { })
                changingSource.CollectionChanged += collectionChanged;
            return new ActiveEnumerable<TSource>(rangeObservableCollection, source as INotifyElementFaultChanges, () =>
            {
                if (changingSource is { })
                    changingSource.CollectionChanged -= collectionChanged;
            });
        });
    }

    #endregion ToActiveEnumerable

    #region ToActiveDictionary

    /// <summary>
    /// Generates an <see cref="ActiveDictionary{TKey, TValue}"/> which actively projects each element of a sequence into a key/value pair
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TKey">The type of the keys in the <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
    /// <typeparam name="TValue">The type of the values in the <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
    /// <param name="source">A series of values to transform into key/value pairs</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> the key/value pairs of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
    public static IActiveDictionary<TKey, TValue> ToActiveDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector) =>
        ToActiveDictionary(source, selector, null, IndexingStrategy.HashTable, null, null);

    /// <summary>
    /// Generates an <see cref="ActiveDictionary{TKey, TValue}"/> which actively projects each element of a sequence into a key/value pair using the specified <see cref="IndexingStrategy"/>
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TKey">The type of the keys in the <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
    /// <typeparam name="TValue">The type of the values in the <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
    /// <param name="source">A series of values to transform into key/value pairs</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="indexingStategy">The indexing strategy of the <see cref="ActiveDictionary{TKey, TValue}"/></param>
    /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> the key/value pairs of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
    public static IActiveDictionary<TKey, TValue> ToActiveDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector, IndexingStrategy indexingStategy) =>
        ToActiveDictionary(source, selector, null, indexingStategy, null, null);

    /// <summary>
    /// Generates an <see cref="ActiveDictionary{TKey, TValue}"/> which actively projects each element of a sequence into a key/value pair using the specified <see cref="IEqualityComparer{T}"/>
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TKey">The type of the keys in the <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
    /// <typeparam name="TValue">The type of the values in the <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
    /// <param name="source">A series of values to transform into key/value pairs</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="keyEqualityComparer">An <see cref="IEqualityComparer{T}"/> to compare keys</param>
    /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> the key/value pairs of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
    public static IActiveDictionary<TKey, TValue> ToActiveDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector, IEqualityComparer<TKey>? keyEqualityComparer) =>
        ToActiveDictionary(source, selector, null, IndexingStrategy.HashTable, keyEqualityComparer, null);

    /// <summary>
    /// Generates an <see cref="ActiveDictionary{TKey, TValue}"/> which actively projects each element of a sequence into a key/value pair using the specified <see cref="IComparer{T}"/>
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TKey">The type of the keys in the <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
    /// <typeparam name="TValue">The type of the values in the <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
    /// <param name="source">A series of values to transform into key/value pairs</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="keyComparer">An <see cref="IComparer{T}"/> to compare keys</param>
    /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> the key/value pairs of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
    public static IActiveDictionary<TKey, TValue> ToActiveDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector, IComparer<TKey>? keyComparer) =>
        ToActiveDictionary(source, selector, null, IndexingStrategy.SelfBalancingBinarySearchTree, null, keyComparer);

    /// <summary>
    /// Generates an <see cref="ActiveDictionary{TKey, TValue}"/> which actively projects each element of a sequence into a key/value pair
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TKey">The type of the keys in the <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
    /// <typeparam name="TValue">The type of the values in the <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
    /// <param name="source">A series of values to transform into key/value pairs</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
    /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> the key/value pairs of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
    public static IActiveDictionary<TKey, TValue> ToActiveDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector, ActiveExpressionOptions? selectorOptions) =>
        ToActiveDictionary(source, selector, selectorOptions, IndexingStrategy.HashTable, null, null);

    /// <summary>
    /// Generates an <see cref="ActiveDictionary{TKey, TValue}"/> which actively projects each element of a sequence into a key/value pair using the specified <see cref="IndexingStrategy"/>
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TKey">The type of the keys in the <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
    /// <typeparam name="TValue">The type of the values in the <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
    /// <param name="source">A series of values to transform into key/value pairs</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
    /// <param name="indexingStategy">The indexing strategy of the <see cref="ActiveDictionary{TKey, TValue}"/></param>
    /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> the key/value pairs of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
    public static IActiveDictionary<TKey, TValue> ToActiveDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector, ActiveExpressionOptions? selectorOptions, IndexingStrategy indexingStategy) =>
        ToActiveDictionary(source, selector, selectorOptions, indexingStategy, null, null);

    /// <summary>
    /// Generates an <see cref="ActiveDictionary{TKey, TValue}"/> which actively projects each element of a sequence into a key/value pair using the specified <see cref="IEqualityComparer{T}"/>
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TKey">The type of the keys in the <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
    /// <typeparam name="TValue">The type of the values in the <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
    /// <param name="source">A series of values to transform into key/value pairs</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
    /// <param name="keyEqualityComparer">An <see cref="IEqualityComparer{T}"/> to compare keys</param>
    /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> the key/value pairs of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
    public static IActiveDictionary<TKey, TValue> ToActiveDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector, ActiveExpressionOptions? selectorOptions, IEqualityComparer<TKey>? keyEqualityComparer) =>
        ToActiveDictionary(source, selector, selectorOptions, IndexingStrategy.HashTable, keyEqualityComparer, null);

    /// <summary>
    /// Generates an <see cref="ActiveDictionary{TKey, TValue}"/> which actively projects each element of a sequence into a key/value pair using the specified <see cref="IComparer{T}"/>
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TKey">The type of the keys in the <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
    /// <typeparam name="TValue">The type of the values in the <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
    /// <param name="source">A series of values to transform into key/value pairs</param>
    /// <param name="selector">A transform function to apply to each element</param>
    /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
    /// <param name="keyComparer">An <see cref="IComparer{T}"/> to compare keys</param>
    /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> the key/value pairs of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
    public static IActiveDictionary<TKey, TValue> ToActiveDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector, ActiveExpressionOptions? selectorOptions, IComparer<TKey>? keyComparer) =>
        ToActiveDictionary(source, selector, selectorOptions, IndexingStrategy.SelfBalancingBinarySearchTree, null, keyComparer);

    static IActiveDictionary<TKey, TValue> ToActiveDictionary<TSource, TKey, TValue>(IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector, ActiveExpressionOptions? selectorOptions, IndexingStrategy indexingStategy, IEqualityComparer<TKey>? keyEqualityComparer, IComparer<TKey>? keyComparer)
    {
        ActiveQueryOptions.Optimize(ref selector);

        var synchronizedSource = source as ISynchronized;
        IDictionary<TKey, int> duplicateKeys;
        var isFaultedDuplicateKeys = false;
        var isFaultedNullKey = false;
        var nullKeys = 0;
        EnumerableRangeActiveExpression<TSource, KeyValuePair<TKey, TValue>> rangeActiveExpression;
        ISynchronizedObservableRangeDictionary<TKey, TValue> rangeObservableDictionary;
        ActiveDictionary<TKey, TValue>? activeDictionary = null;
        var activeDictionaryAccess = new object();

        void checkOperationFault()
        {
            if (nullKeys > 0 && !isFaultedNullKey)
            {
                isFaultedNullKey = true;
                activeDictionary!.OperationFault = ExceptionHelper.KeyNull;
            }
            else if (nullKeys == 0 && isFaultedNullKey)
            {
                isFaultedNullKey = false;
                activeDictionary!.OperationFault = null;
            }

            if (!isFaultedNullKey)
            {
                if (duplicateKeys.Count > 0 && !isFaultedDuplicateKeys)
                {
                    isFaultedDuplicateKeys = true;
                    activeDictionary!.OperationFault = ExceptionHelper.SameKeyAlreadyAdded;
                }
                else if (duplicateKeys.Count == 0 && isFaultedDuplicateKeys)
                {
                    isFaultedDuplicateKeys = false;
                    activeDictionary!.OperationFault =  null;
                }
            }
        }

        void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            synchronizedSource.Execute(() =>
            {
                lock (activeDictionaryAccess)
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        IDictionary<TKey, TValue> replacementDictionary;
                        if (indexingStategy == IndexingStrategy.SelfBalancingBinarySearchTree)
                        {
                            duplicateKeys = keyComparer is null ? new SortedDictionary<TKey, int>() : new SortedDictionary<TKey, int>(keyComparer);
                            replacementDictionary = keyComparer is null ? new SortedDictionary<TKey, TValue>() : new SortedDictionary<TKey, TValue>(keyComparer);
                        }
                        else
                        {
                            duplicateKeys = keyEqualityComparer is null ? new Dictionary<TKey, int>() : new Dictionary<TKey, int>(keyEqualityComparer);
                            replacementDictionary = keyEqualityComparer is null ? new Dictionary<TKey, TValue>() : new Dictionary<TKey, TValue>(keyEqualityComparer);
                        }
                        var resultsFaultsAndCounts = rangeActiveExpression.GetResultsFaultsAndCounts();
                        nullKeys = resultsFaultsAndCounts.Count(rfc => rfc.result.Key is null);
                        var distinctResultsFaultsAndCounts = resultsFaultsAndCounts.Where(rfc => rfc.result.Key is { }).GroupBy(rfc => rfc.result.Key).ToList();
                        foreach (var keyValuePair in distinctResultsFaultsAndCounts.Select(g => g.First().result))
                            replacementDictionary.Add(keyValuePair);
                        rangeObservableDictionary.Reset(replacementDictionary);
                        foreach (var (key, duplicateCount) in distinctResultsFaultsAndCounts.Select(g => (key: g.Key, duplicateCount: g.Sum(rfc => rfc.count) - 1)).Where(kc => kc.duplicateCount > 0))
                            duplicateKeys.Add(key, duplicateCount);
                        checkOperationFault();
                    }
                    else if (e.Action != NotifyCollectionChangedAction.Move)
                    {
                        if (e.OldItems is { } && e.OldItems.Count > 0)
                        {
                            foreach (var (element, result) in e.OldItems.Cast<(TSource element, KeyValuePair<TKey, TValue> keyValuePair)>())
                            {
                                var key = result.Key;
                                if (key is null)
                                    --nullKeys;
                                else if (duplicateKeys.TryGetValue(key, out var duplicates))
                                {
                                    if (duplicates == 1)
                                        duplicateKeys.Remove(key);
                                    else
                                        duplicateKeys[key] = duplicates - 1;
                                }
                                else
                                    rangeObservableDictionary.Remove(key);
                            }
                            checkOperationFault();
                        }
                        if (e.NewItems is { } && e.NewItems.Count > 0)
                        {
                            foreach (var (element, result) in e.NewItems.Cast<(TSource element, KeyValuePair<TKey, TValue> keyValuePair)>())
                            {
                                var key = result.Key;
                                if (key is null)
                                    ++nullKeys;
                                else if (rangeObservableDictionary.ContainsKey(key))
                                {
                                    if (duplicateKeys.TryGetValue(key, out var duplicates))
                                        duplicateKeys[key] = duplicates + 1;
                                    else
                                        duplicateKeys.Add(key, 1);
                                }
                                else
                                    rangeObservableDictionary.Add(key, result.Value);
                            }
                            checkOperationFault();
                        }
                    }
                }
            });

        void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, KeyValuePair<TKey, TValue>> e) =>
            synchronizedSource.Execute(() =>
            {
                lock (activeDictionaryAccess)
                {
                    var result = e.Result;
                    var key = result.Key;
                    var count = e.Count;
                    if (key is null)
                        nullKeys += count;
                    else if (rangeObservableDictionary.ContainsKey(key))
                    {
                        if (duplicateKeys.TryGetValue(key, out var duplicates))
                            duplicateKeys[key] = duplicates + count;
                        else
                            duplicateKeys.Add(key, count);
                    }
                    else
                    {
                        rangeObservableDictionary.Add(key, result.Value);
                        if (count > 1)
                            duplicateKeys.Add(key, count - 1);
                    }
                    checkOperationFault();
                }
            });

        void elementResultChanging(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, KeyValuePair<TKey, TValue>> e) =>
            synchronizedSource.Execute(() =>
            {
                lock (activeDictionaryAccess)
                {
                    var key = e.Result.Key;
                    var count = e.Count;
                    if (key is null)
                        nullKeys -= count;
                    else if (duplicateKeys.TryGetValue(key, out var duplicates))
                    {
                        if (duplicates <= count)
                            duplicateKeys.Remove(key);
                        else
                            duplicateKeys[key] = duplicates - count;
                    }
                    else
                        rangeObservableDictionary.Remove(key);
                    checkOperationFault();
                }
            });

        return synchronizedSource.Execute(() =>
        {
            lock (activeDictionaryAccess)
            {
                switch (indexingStategy)
                {
                    case IndexingStrategy.SelfBalancingBinarySearchTree:
                        duplicateKeys = keyComparer is null ? new SortedDictionary<TKey, int>() : new SortedDictionary<TKey, int>(keyComparer);
                        rangeObservableDictionary = keyComparer is null ? new SynchronizedObservableSortedDictionary<TKey, TValue>(synchronizedSource?.SynchronizationContext) : new SynchronizedObservableSortedDictionary<TKey, TValue>(synchronizedSource?.SynchronizationContext, keyComparer);
                        break;
                    default:
                        duplicateKeys = keyEqualityComparer is null ? new Dictionary<TKey, int>() : new Dictionary<TKey, int>(keyEqualityComparer);
                        rangeObservableDictionary = keyEqualityComparer is null ? new SynchronizedObservableDictionary<TKey, TValue>(synchronizedSource?.SynchronizationContext) : new SynchronizedObservableDictionary<TKey, TValue>(synchronizedSource?.SynchronizationContext, keyEqualityComparer);
                        break;
                }

                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                var resultsFaultsAndCounts = rangeActiveExpression.GetResultsFaultsAndCounts();
                nullKeys = resultsFaultsAndCounts.Count(rfc => rfc.result.Key is null);
                var distinctResultsFaultsAndCounts = resultsFaultsAndCounts.Where(rfc => rfc.result.Key is { }).GroupBy(rfc => rfc.result.Key).ToList();
                rangeObservableDictionary.AddRange(distinctResultsFaultsAndCounts.Select(g => g.First().result));
                foreach (var (key, duplicateCount) in distinctResultsFaultsAndCounts.Select(g => (key: g.Key, duplicateCount: g.Sum(rfc => rfc.count) - 1)).Where(kc => kc.duplicateCount > 0))
                    duplicateKeys.Add(key, duplicateCount);
                activeDictionary = new ActiveDictionary<TKey, TValue>(rangeObservableDictionary, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.CollectionChanged -= collectionChanged;
                    rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                    rangeActiveExpression.ElementResultChanging -= elementResultChanging;
                    rangeActiveExpression.Dispose();
                });
                rangeActiveExpression.CollectionChanged += collectionChanged;
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.ElementResultChanging += elementResultChanging;
                checkOperationFault();
                return activeDictionary;
            }
        });
    }

    #endregion ToActiveDictionary

    #region Where

    /// <summary>
    /// Actively filters a sequence of values based on a predicate
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to filter</param>
    /// <param name="predicate">A function to test each element for a condition</param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that contains elements from the input sequence that satisfy the condition</returns>
    public static IActiveEnumerable<TSource> ActiveWhere<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, bool>> predicate) =>
        ActiveWhere(source, predicate, null);

    /// <summary>
    /// Actively filters a sequence of values based on a predicate
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to filter</param>
    /// <param name="predicate">A function to test each element for a condition</param>
    /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
    /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that contains elements from the input sequence that satisfy the condition</returns>
    public static IActiveEnumerable<TSource> ActiveWhere<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions? predicateOptions)
    {
        ActiveQueryOptions.Optimize(ref predicate);
        return ActiveWhereEnumerable<TSource>.Get(source, predicate, predicateOptions);
    }

    #endregion Where
}
