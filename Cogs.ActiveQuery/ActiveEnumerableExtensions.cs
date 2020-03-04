using Cogs.ActiveExpressions;
using Cogs.Collections;
using Cogs.Collections.Synchronized;
using Cogs.Reflection;
using Cogs.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Gear.ActiveQuery
{
    /// <summary>
    /// Provides a set of <c>static</c> (<c>Shared</c> in Visual Basic) methods for actively querying objects that implement <see cref="IEnumerable{T}"/>
    /// </summary>
    public static class ActiveEnumerableExtensions
    {
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
            Action<bool>? setValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) => setValue!(where.Count == (readOnlySource?.Count ?? source.Count()));

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.CollectionChanged += collectionChanged;
                if (changeNotifyingSource is { })
                    changeNotifyingSource.CollectionChanged += collectionChanged;

                return new ActiveValue<bool>(where.Count == (readOnlySource?.Count ?? source.Count()), out setValue, elementFaultChangeNotifier: where, onDispose: () =>
                {
                    where.CollectionChanged -= collectionChanged;
                    if (changeNotifyingSource is { })
                        changeNotifyingSource.CollectionChanged -= collectionChanged;
                    where.Dispose();
                });
            })!;
        }

        #endregion All

        #region Any

        /// <summary>
        /// Actively determines whether a sequence contains any elements
        /// </summary>
        /// <param name="source">The <see cref="IEnumerable"/> to check for emptiness</param>
        /// <returns>>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>true</c> if the source sequence contains any elements; otherwise, <c>false</c></returns>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static IActiveValue<bool> ActiveAny(this IEnumerable source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyCollectionChanged changeNotifyingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<bool>? setValue = null;

                void sourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => synchronizedSource.SequentialExecute(() => setValue!(source.Cast<object>().Any()));

                return synchronizedSource.SequentialExecute(() =>
                {
                    changeNotifyingSource.CollectionChanged += sourceCollectionChanged;
                    return new ActiveValue<bool>(source.Cast<object>().Any(), out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changeNotifyingSource.CollectionChanged -= sourceCollectionChanged);
                })!;
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
            Action<bool>? setValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) => setValue!(where.Count > 0);

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.CollectionChanged += collectionChanged;

                return new ActiveValue<bool>(where.Count > 0, out setValue, elementFaultChangeNotifier: where, onDispose: () =>
                {
                    where.CollectionChanged -= collectionChanged;
                    where.Dispose();
                });
            })!;
        }

        #endregion Any

        #region Average

        /// <summary>
        /// Actively computes the average of a sequence of values
        /// </summary>
        /// <typeparam name="TSource">The type of the values</typeparam>
        /// <param name="source">A sequence of values to calculate the average of</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the average of the sequence of values</returns>
        public static IActiveValue<TSource> ActiveAverage<TSource>(this IEnumerable<TSource> source) =>
            ActiveAverage(source, element => element);

        /// <summary>
        /// Actively computes the average of a sequence of values that are obtained by invoking a transform function on each element of the input sequence
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the values being averaged</typeparam>
        /// <param name="source">A sequence of values to calculate the average of</param>
        /// <param name="selector">A transform function to apply to each element</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the average of the sequence of values</returns>
        public static IActiveValue<TResult> ActiveAverage<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector) =>
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
        public static IActiveValue<TResult> ActiveAverage<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var readOnlyCollection = source as IReadOnlyCollection<TSource>;
            var synchronizedSource = source as ISynchronized;
            var convertCount = CountConversion.GetConverter(typeof(TResult));
            var operations = new GenericOperations<TResult>();
            IActiveValue<TResult> sum;
            Action<TResult>? setValue = null;
            Action<Exception?>? setOperationFault = null;

            int count() => readOnlyCollection?.Count ?? source.Count();

            void propertyChanged(object sender, PropertyChangedEventArgs e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.PropertyName == nameof(ActiveValue<TResult>.Value))
                    {
                        var currentCount = count();
                        if (currentCount == 0)
                        {
                            setValue!(default!);
                            setOperationFault!(ExceptionHelper.SequenceContainsNoElements);
                        }
                        else
                        {
                            setOperationFault!(null);
                            setValue!(operations.Divide(sum.Value, (TResult)convertCount(currentCount)));
                        }
                    }
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                sum = ActiveSum(source, selector, selectorOptions);
                sum.PropertyChanged += propertyChanged;

                var currentCount = count();
                return new ActiveValue<TResult>(currentCount > 0 ? operations.Divide(sum.Value, (TResult)convertCount(currentCount)) : default, out setValue, currentCount == 0 ? ExceptionHelper.SequenceContainsNoElements : null, out setOperationFault, sum, () =>
                {
                    sum.PropertyChanged -= propertyChanged;
                    sum.Dispose();
                });
            })!;
        }

        #endregion Average

        #region Cast

        /// <summary>
        /// Actively casts the elements of an <see cref="IEnumerable"/> to the specified type
        /// </summary>
        /// <typeparam name="TResult">The type to cast the elements of <paramref name="source"/> to</typeparam>
        /// <param name="source">The <see cref="IEnumerable"/> that contains the elements to be cast to type <typeparamref name="TResult"/></param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that contains each element of the source sequence cast to the specified type</returns>
        public static IActiveEnumerable<TResult> ActiveCast<TResult>(this IEnumerable source) =>
            ActiveCast<TResult>(source, null);

        /// <summary>
        /// Actively casts the elements of an <see cref="IEnumerable"/> to the specified type
        /// </summary>
        /// <typeparam name="TResult">The type to cast the elements of <paramref name="source"/> to</typeparam>
        /// <param name="source">The <see cref="IEnumerable"/> that contains the elements to be cast to type <typeparamref name="TResult"/></param>
        /// <param name="castOptions">Options governing the behavior of active expressions created to perform the cast</param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that contains each element of the source sequence cast to the specified type</returns>
        public static IActiveEnumerable<TResult> ActiveCast<TResult>(this IEnumerable source, ActiveExpressionOptions? castOptions) =>
            ActiveCast<TResult>(source, castOptions, IndexingStrategy.HashTable);

        /// <summary>
        /// Actively casts the elements of an <see cref="IEnumerable"/> to the specified type
        /// </summary>
        /// <typeparam name="TResult">The type to cast the elements of <paramref name="source"/> to</typeparam>
        /// <param name="source">The <see cref="IEnumerable"/> that contains the elements to be cast to type <typeparamref name="TResult"/></param>
        /// <param name="indexingStrategy">The strategy used to find the index within <paramref name="source"/> of elements that change</param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that contains each element of the source sequence cast to the specified type</returns>
        public static IActiveEnumerable<TResult> ActiveCast<TResult>(this IEnumerable source, IndexingStrategy indexingStrategy) =>
            ActiveCast<TResult>(source, null, indexingStrategy);

        /// <summary>
        /// Actively casts the elements of an <see cref="IEnumerable"/> to the specified type
        /// </summary>
        /// <typeparam name="TResult">The type to cast the elements of <paramref name="source"/> to</typeparam>
        /// <param name="source">The <see cref="IEnumerable"/> that contains the elements to be cast to type <typeparamref name="TResult"/></param>
        /// <param name="castOptions">Options governing the behavior of active expressions created to perform the cast</param>
        /// <param name="indexingStrategy">The strategy used to find the index within <paramref name="source"/> of elements that change</param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that contains each element of the source sequence cast to the specified type</returns>
        public static IActiveEnumerable<TResult> ActiveCast<TResult>(this IEnumerable source, ActiveExpressionOptions? castOptions, IndexingStrategy indexingStrategy) =>
            ActiveSelect(source, element => (TResult)element!, castOptions, indexingStrategy);

        #endregion

        #region Concat

        /// <summary>
        /// Actively concatenates two sequences
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences</typeparam>
        /// <param name="first">The first sequence to concatenate</param>
        /// <param name="second">The sequence to concatenate to the first sequence</param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that contains the concatenated elements of the two input sequences</returns>
        public static IActiveEnumerable<TSource> ActiveConcat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            var synchronizedFirst = first as ISynchronized;
            var synchronizedSecond = second as ISynchronized;

            if (synchronizedFirst is { } && synchronizedSecond is { } && synchronizedFirst.SynchronizationContext != synchronizedSecond.SynchronizationContext)
                throw new InvalidOperationException($"{nameof(first)} and {nameof(second)} are both synchronizable but using different synchronization contexts; select a different overload of {nameof(ActiveConcat)} to specify the synchronization context to use");

            var synchronizationContext = synchronizedFirst?.SynchronizationContext ?? synchronizedSecond?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext;

            SynchronizedRangeObservableCollection<TSource>? rangeObservableCollection = null;
            IActiveEnumerable<TSource> firstEnumerable;
            IActiveEnumerable<TSource> secondEnumerable;

            void firstCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizationContext.Execute(() =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                        rangeObservableCollection!.ReplaceRange(0, rangeObservableCollection.Count - secondEnumerable.Count, first);
                    else
                    {
                        if (e.OldItems is { } && e.NewItems is { } && e.OldStartingIndex >= 0 && e.OldStartingIndex == e.NewStartingIndex)
                        {
                            if (e.OldItems.Count == 1 && e.NewItems.Count == 1)
                                rangeObservableCollection!.Replace(e.OldStartingIndex, (TSource)e.NewItems[0]);
                            else
                                rangeObservableCollection!.ReplaceRange(e.OldStartingIndex, e.OldItems.Count, e.NewItems.Cast<TSource>());
                        }
                        else
                        {
                            if (e.OldItems is { } && e.OldStartingIndex >= 0)
                                rangeObservableCollection!.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                            if (e.NewItems is { } && e.NewStartingIndex >= 0)
                                rangeObservableCollection!.InsertRange(e.NewStartingIndex, e.NewItems.Cast<TSource>());
                        }
                    }
                });

            void secondCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizationContext.Execute(() =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                        rangeObservableCollection!.ReplaceRange(firstEnumerable.Count, rangeObservableCollection.Count - firstEnumerable.Count, second);
                    else
                    {
                        if (e.OldItems is { } && e.NewItems is { } && e.OldStartingIndex >= 0 && e.OldStartingIndex == e.NewStartingIndex)
                        {
                            if (e.OldItems.Count == 1 && e.NewItems.Count == 1)
                                rangeObservableCollection!.Replace(firstEnumerable.Count + e.OldStartingIndex, (TSource)e.NewItems[0]);
                            else
                                rangeObservableCollection!.ReplaceRange(firstEnumerable.Count + e.OldStartingIndex, e.OldItems.Count, e.NewItems.Cast<TSource>());
                        }
                        else
                        {
                            if (e.OldItems is { } && e.OldStartingIndex >= 0)
                                rangeObservableCollection!.RemoveRange(firstEnumerable.Count + e.OldStartingIndex, e.OldItems.Count);
                            if (e.NewItems is { } && e.NewStartingIndex >= 0)
                                rangeObservableCollection!.InsertRange(firstEnumerable.Count + e.NewStartingIndex, e.NewItems.Cast<TSource>());
                        }
                    }
                });

            return synchronizationContext.Execute(() =>
            {
                firstEnumerable = ToActiveEnumerable(first);
                secondEnumerable = ToActiveEnumerable(second);

                firstEnumerable.CollectionChanged += firstCollectionChanged;
                secondEnumerable.CollectionChanged += secondCollectionChanged;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizationContext, first.Concat(second));
                var mergedElementFaultChangeNotifier = new MergedElementFaultChangeNotifier(firstEnumerable, secondEnumerable);

                return new ActiveEnumerable<TSource>(rangeObservableCollection, mergedElementFaultChangeNotifier, () =>
                {
                    firstEnumerable.CollectionChanged -= firstCollectionChanged;
                    secondEnumerable.CollectionChanged -= secondCollectionChanged;
                    mergedElementFaultChangeNotifier.Dispose();
                });
            })!;
        }

        /// <summary>
        /// Actively concatenates two sequences on the specified <see cref="SynchronizationContext"/>
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences</typeparam>
        /// <param name="first">The first sequence to concatenate</param>
        /// <param name="second">The sequence to concatenate to the first sequence</param>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform operations</param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that contains the concatenated elements of the two input sequences</returns>
        public static IActiveEnumerable<TSource> ActiveConcat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, SynchronizationContext synchronizationContext)
        {
            if (synchronizationContext is null)
                throw new ArgumentNullException(nameof(synchronizationContext));

            SynchronizedRangeObservableCollection<TSource>? rangeObservableCollection = null;
            IActiveEnumerable<TSource> firstEnumerable;
            IActiveEnumerable<TSource> secondEnumerable;

            void firstCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizationContext.Execute(() =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                        rangeObservableCollection!.ReplaceRange(0, rangeObservableCollection.Count - secondEnumerable.Count, first);
                    else
                    {
                        if (e.OldItems is { } && e.NewItems is { } && e.OldStartingIndex >= 0 && e.OldStartingIndex == e.NewStartingIndex)
                        {
                            if (e.OldItems.Count == 1 && e.NewItems.Count == 1)
                                rangeObservableCollection!.Replace(e.OldStartingIndex, (TSource)e.NewItems[0]);
                            else
                                rangeObservableCollection!.ReplaceRange(e.OldStartingIndex, e.OldItems.Count, e.NewItems.Cast<TSource>());
                        }
                        else
                        {
                            if (e.OldItems is { } && e.OldStartingIndex >= 0)
                                rangeObservableCollection!.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                            if (e.NewItems is { } && e.NewStartingIndex >= 0)
                                rangeObservableCollection!.InsertRange(e.NewStartingIndex, e.NewItems.Cast<TSource>());
                        }
                    }
                });

            void secondCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizationContext.Execute(() =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                        rangeObservableCollection!.ReplaceRange(firstEnumerable.Count, rangeObservableCollection.Count - firstEnumerable.Count, second);
                    else
                    {
                        if (e.OldItems is { } && e.NewItems is { } && e.OldStartingIndex >= 0 && e.OldStartingIndex == e.NewStartingIndex)
                        {
                            if (e.OldItems.Count == 1 && e.NewItems.Count == 1)
                                rangeObservableCollection!.Replace(firstEnumerable.Count + e.OldStartingIndex, (TSource)e.NewItems[0]);
                            else
                                rangeObservableCollection!.ReplaceRange(firstEnumerable.Count + e.OldStartingIndex, e.OldItems.Count, e.NewItems.Cast<TSource>());
                        }
                        else
                        {
                            if (e.OldItems is { } && e.OldStartingIndex >= 0)
                                rangeObservableCollection!.RemoveRange(firstEnumerable.Count + e.OldStartingIndex, e.OldItems.Count);
                            if (e.NewItems is { } && e.NewStartingIndex >= 0)
                                rangeObservableCollection!.InsertRange(firstEnumerable.Count + e.NewStartingIndex, e.NewItems.Cast<TSource>());
                        }
                    }
                });

            return synchronizationContext.Execute(() =>
            {
                firstEnumerable = ToActiveEnumerable(first);
                secondEnumerable = ToActiveEnumerable(second);

                firstEnumerable.CollectionChanged += firstCollectionChanged;
                secondEnumerable.CollectionChanged += secondCollectionChanged;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizationContext, first.Concat(second));
                var mergedElementFaultChangeNotifier = new MergedElementFaultChangeNotifier(firstEnumerable, secondEnumerable);

                return new ActiveEnumerable<TSource>(rangeObservableCollection, mergedElementFaultChangeNotifier, () =>
                {
                    firstEnumerable.CollectionChanged -= firstCollectionChanged;
                    secondEnumerable.CollectionChanged -= secondCollectionChanged;
                    mergedElementFaultChangeNotifier.Dispose();
                });
            })!;
        }

        #endregion Concat

        #region Count

        /// <summary>
        /// Actively determines the number of elements in a sequence
        /// </summary>
        /// <param name="source">The <see cref="IEnumerable"/> from which to count elements</param>
        /// <returns>An active value the value of which is <c>true</c> if the source sequence contains any elements; otherwise, <c>false</c></returns>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static IActiveValue<int> ActiveCount(this IEnumerable source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyCollectionChanged changeNotifyingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<int>? setValue = null;

                void sourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => synchronizedSource.SequentialExecute(() => setValue!(source.Cast<object>().Count()));

                return synchronizedSource.SequentialExecute(() =>
                {
                    changeNotifyingSource.CollectionChanged += sourceCollectionChanged;
                    return new ActiveValue<int>(source.Cast<object>().Count(), out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changeNotifyingSource.CollectionChanged -= sourceCollectionChanged);
                })!;
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
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static IActiveValue<int> ActiveCount<TSource>(this IEnumerable<TSource> source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyCollectionChanged changeNotifyingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<int>? setValue = null;

                void sourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => synchronizedSource.SequentialExecute(() => setValue!(source.Count()));

                return synchronizedSource.SequentialExecute(() =>
                {
                    changeNotifyingSource.CollectionChanged += sourceCollectionChanged;
                    return new ActiveValue<int>(source.Count(), out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changeNotifyingSource.CollectionChanged -= sourceCollectionChanged);
                })!;
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
            Action<int>? setValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) => setValue!(where.Count);

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.CollectionChanged += collectionChanged;
                if (changeNotifyingSource is { })
                    changeNotifyingSource.CollectionChanged += collectionChanged;

                return new ActiveValue<int>(where.Count, out setValue, elementFaultChangeNotifier: where, onDispose: () =>
                {
                    where.CollectionChanged -= collectionChanged;
                    if (changeNotifyingSource is { })
                        changeNotifyingSource.CollectionChanged -= collectionChanged;
                    where.Dispose();
                });
            })!;
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
                synchronizedSource.SequentialExecute(() =>
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

            return synchronizedSource.SequentialExecute(() =>
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
            })!;
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
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static IActiveValue<TSource> ActiveElementAt<TSource>(this IEnumerable<TSource> source, int index)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyCollectionChanged changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<TSource>? setValue = null;
                Action<Exception?>? setOperationFault = null;

                void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        try
                        {
                            var value = source.ElementAt(index);
                            setOperationFault!(null);
                            setValue!(value);
                        }
                        catch (Exception ex)
                        {
                            setOperationFault!(ex);
                            setValue!(default!);
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.CollectionChanged += collectionChanged;
                    try
                    {
                        return new ActiveValue<TSource>(source.ElementAt(index), out setValue, out setOperationFault, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changingSource.CollectionChanged -= collectionChanged);
                    }
                    catch (Exception ex)
                    {
                        return new ActiveValue<TSource>(default!, out setValue, ex, out setOperationFault, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    }
                })!;
            }
            try
            {
                return new ActiveValue<TSource>(source.ElementAt(index), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<TSource>(default!, ex, elementFaultChangeNotifier);
            }
        }

        /// <summary>
        /// Actively returns the element at a specified index in a sequence
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to return an element from</param>
        /// <param name="index">The zero-based index of the element to retrieve</param>
        /// <returns>>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the element at the specified position in the source sequence</returns>
        public static IActiveValue<TSource> ActiveElementAt<TSource>(this IReadOnlyList<TSource> source, int index)
        {
            IActiveEnumerable<TSource> activeEnumerable;
            Action<TSource>? setValue = null;
            Action<Exception?>? setOperationFault = null;
            var indexOutOfRange = false;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (indexOutOfRange && index >= 0 && index < activeEnumerable.Count)
                {
                    setOperationFault!(null);
                    indexOutOfRange = false;
                }
                else if (!indexOutOfRange && (index < 0 || index >= activeEnumerable.Count))
                {
                    setOperationFault!(ExceptionHelper.IndexArgumentWasOutOfRange);
                    indexOutOfRange = true;
                }
                setValue!(index >= 0 && index < activeEnumerable.Count ? activeEnumerable[index] : default);
            }

            return (source as ISynchronized).SequentialExecute(() =>
            {
                activeEnumerable = ToActiveEnumerable(source);
                activeEnumerable.CollectionChanged += collectionChanged;

                indexOutOfRange = index < 0 || index >= activeEnumerable.Count;
                return new ActiveValue<TSource>(!indexOutOfRange ? activeEnumerable[index] : default, out setValue, indexOutOfRange ? ExceptionHelper.IndexArgumentWasOutOfRange : null, out setOperationFault, activeEnumerable, () =>
                {
                    activeEnumerable.CollectionChanged -= collectionChanged;
                    activeEnumerable.Dispose();
                });
            })!;
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
                Action<TSource>? setValue = null;

                void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) => synchronizedSource.SequentialExecute(() => setValue!(source.ElementAtOrDefault(index)));

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.CollectionChanged += collectionChanged;
                    return new ActiveValue<TSource>(source.ElementAtOrDefault(index), out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changingSource.CollectionChanged -= collectionChanged);
                })!;
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
        public static IActiveValue<TSource> ActiveElementAtOrDefault<TSource>(this IReadOnlyList<TSource> source, int index)
        {
            IActiveEnumerable<TSource> activeEnumerable;
            Action<TSource>? setValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) => setValue!(index >= 0 && index < activeEnumerable.Count ? activeEnumerable[index] : default);

            return (source as ISynchronized).SequentialExecute(() =>
            {
                activeEnumerable = ToActiveEnumerable(source);
                activeEnumerable.CollectionChanged += collectionChanged;

                return new ActiveValue<TSource>(index >= 0 && index < activeEnumerable.Count ? activeEnumerable[index] : default, out setValue, elementFaultChangeNotifier: activeEnumerable, onDispose: () =>
                {
                    activeEnumerable.CollectionChanged -= collectionChanged;
                    activeEnumerable.Dispose();
                });
            })!;
        }

        #endregion ElementAtOrDefault

        #region First

        /// <summary>
        /// Actively returns the first element of a sequence
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to return the first element of</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the first element in the specified sequence</returns>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static IActiveValue<TSource> ActiveFirst<TSource>(this IEnumerable<TSource> source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyCollectionChanged changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<TSource>? setValue = null;
                Action<Exception?>? setOperationFault = null;

                void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        try
                        {
                            var value = source.First();
                            setOperationFault!(null);
                            setValue!(value);
                        }
                        catch (Exception ex)
                        {
                            setOperationFault!(ex);
                            setValue!(default!);
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.CollectionChanged += collectionChanged;
                    try
                    {
                        return new ActiveValue<TSource>(source.First(), out setValue, out setOperationFault, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    }
                    catch (Exception ex)
                    {
                        return new ActiveValue<TSource>(default!, out setValue, ex, out setOperationFault, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    }
                })!;
            }
            try
            {
                return new ActiveValue<TSource>(source.First(), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<TSource>(default!, ex, elementFaultChangeNotifier);
            }
        }

        /// <summary>
        /// Actively returns the first element in a sequence that satisfies a specified condition
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to return an element from</param>
        /// <param name="predicate">A function to test each element for a condition</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the first element in the sequence that passes the test in the spredicate function</returns>
        public static IActiveValue<TSource> ActiveFirst<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate) =>
            ActiveFirst(source, predicate, null);

        /// <summary>
        /// Actively returns the first element in a sequence that satisfies a specified condition
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to return an element from</param>
        /// <param name="predicate">A function to test each element for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the first element in the sequence that passes the test in the spredicate function</returns>
        public static IActiveValue<TSource> ActiveFirst<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions? predicateOptions)
        {
            IActiveEnumerable<TSource> where;
            Action<TSource>? setValue = null;
            Action<Exception?>? setOperationFault = null;
            var none = false;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (none && where.Count > 0)
                {
                    setOperationFault!(null);
                    none = false;
                }
                else if (!none && where.Count == 0)
                {
                    setOperationFault!(ExceptionHelper.SequenceContainsNoElements);
                    none = true;
                }
                setValue!(where.Count > 0 ? where[0] : default);
            }

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.CollectionChanged += collectionChanged;

                none = where.Count == 0;
                return new ActiveValue<TSource>(!none ? where[0] : default, out setValue, none ? ExceptionHelper.SequenceContainsNoElements : null, out setOperationFault, where, () =>
                {
                    where.CollectionChanged -= collectionChanged;
                    where.Dispose();
                });
            })!;
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
                Action<TSource>? setValue = null;

                void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) => synchronizedSource.SequentialExecute(() => setValue!(source.FirstOrDefault()));

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.CollectionChanged += collectionChanged;
                    return new ActiveValue<TSource>(source.FirstOrDefault(), out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changingSource.CollectionChanged -= collectionChanged);
                })!;
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
        public static IActiveValue<TSource> ActiveFirstOrDefault<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate) =>
            ActiveFirstOrDefault(source, predicate, null);

        /// <summary>
        /// Actively returns the first element of the sequence that satisfies a condition or a default value if no such element is found
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to return the first element of</param>
        /// <param name="predicate">A function to test each element for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>default</c>(<typeparamref name="TSource"/>) if <paramref name="source"/> is empty or if no element passes the test specified by <paramref name="predicate"/>; otherwise, the first element in <paramref name="source"/> that passes the test specified by <paramref name="predicate"/></returns>
        public static IActiveValue<TSource> ActiveFirstOrDefault<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions? predicateOptions)
        {
            IActiveEnumerable<TSource> where;
            Action<TSource>? setValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) => setValue!(where.Count > 0 ? where[0] : default);

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.CollectionChanged += collectionChanged;

                return new ActiveValue<TSource>(where.Count > 0 ? where[0] : default, out setValue, elementFaultChangeNotifier: where, onDispose: () =>
                {
                    where.CollectionChanged -= collectionChanged;
                    where.Dispose();
                });
            })!;
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
        public static IActiveEnumerable<ActiveGrouping<TKey, TSource>> ActiveGroupBy<TSource, TKey>(this IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector) =>
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
        public static IActiveEnumerable<ActiveGrouping<TKey, TSource>> ActiveGroupBy<TSource, TKey>(this IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector, IndexingStrategy indexingStrategy) =>
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
        public static IActiveEnumerable<ActiveGrouping<TKey, TSource>> ActiveGroupBy<TSource, TKey>(this IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector, IEqualityComparer<TKey> equalityComparer) =>
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
        public static IActiveEnumerable<ActiveGrouping<TKey, TSource>> ActiveGroupBy<TSource, TKey>(this IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey> comparer) =>
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
        public static IActiveEnumerable<ActiveGrouping<TKey, TSource>> ActiveGroupBy<TSource, TKey>(this IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector, ActiveExpressionOptions keySelectorOptions) =>
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
        public static IActiveEnumerable<ActiveGrouping<TKey, TSource>> ActiveGroupBy<TSource, TKey>(this IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector, ActiveExpressionOptions keySelectorOptions, IndexingStrategy indexingStrategy) =>
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
        public static IActiveEnumerable<ActiveGrouping<TKey, TSource>> ActiveGroupBy<TSource, TKey>(this IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector, ActiveExpressionOptions keySelectorOptions, IEqualityComparer<TKey> equalityComparer) =>
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
        public static IActiveEnumerable<ActiveGrouping<TKey, TSource>> ActiveGroupBy<TSource, TKey>(this IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector, ActiveExpressionOptions keySelectorOptions, IComparer<TKey> comparer) =>
            ActiveGroupBy(source, keySelector, keySelectorOptions, IndexingStrategy.SelfBalancingBinarySearchTree, null, comparer);

        static IActiveEnumerable<ActiveGrouping<TKey, TSource>> ActiveGroupBy<TSource, TKey>(IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector, ActiveExpressionOptions? keySelectorOptions, IndexingStrategy indexingStrategy, IEqualityComparer<TKey>? equalityComparer, IComparer<TKey>? comparer)
        {
            ActiveQueryOptions.Optimize(ref keySelector);

            var synchronizedSource = source as ISynchronized;
            EnumerableRangeActiveExpression<TSource, TKey> rangeActiveExpression;
            SynchronizedRangeObservableCollection<ActiveGrouping<TKey, TSource>>? rangeObservableCollection = null;

            var collectionAndGroupingDictionary = (IDictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, ActiveGrouping<TKey, TSource> grouping)>)(indexingStrategy switch
            {
                IndexingStrategy.HashTable => equalityComparer is null ? new Dictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, ActiveGrouping<TKey, TSource> grouping)>() : new Dictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, ActiveGrouping<TKey, TSource> grouping)>(equalityComparer),
                IndexingStrategy.SelfBalancingBinarySearchTree => comparer is null ? new SortedDictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, ActiveGrouping<TKey, TSource> grouping)>() : new SortedDictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, ActiveGrouping<TKey, TSource> grouping)>(comparer),
                _ => throw new ArgumentOutOfRangeException(nameof(indexingStrategy), $"{nameof(indexingStrategy)} must be {IndexingStrategy.HashTable} or {IndexingStrategy.SelfBalancingBinarySearchTree}"),
            });

            void addElement(TSource element, TKey key, int count = 1)
            {
                SynchronizedRangeObservableCollection<TSource> groupingObservableCollection;
                if (!collectionAndGroupingDictionary.TryGetValue(key, out var collectionAndGrouping))
                {
                    groupingObservableCollection = new SynchronizedRangeObservableCollection<TSource>();
                    var grouping = new ActiveGrouping<TKey, TSource>(key, groupingObservableCollection);
                    collectionAndGroupingDictionary.Add(key, (groupingObservableCollection, grouping));
                    rangeObservableCollection!.Add(grouping);
                }
                else
                    groupingObservableCollection = collectionAndGrouping.groupingObservableCollection;
                groupingObservableCollection.AddRange(element.Repeat(count));
            }

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TKey> e) => synchronizedSource.SequentialExecute(() => addElement(e.Element, e.Result, e.Count));

            void elementResultChanging(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TKey> e) => synchronizedSource.SequentialExecute(() => removeElement(e.Element, e.Result, e.Count));

            void genericCollectionChanged(object sender, INotifyGenericCollectionChangedEventArgs<(TSource element, TKey key)> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        collectionAndGroupingDictionary = indexingStrategy switch
                        {
                            IndexingStrategy.HashTable => equalityComparer is null ? new Dictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, ActiveGrouping<TKey, TSource> grouping)>() : new Dictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, ActiveGrouping<TKey, TSource> grouping)>(equalityComparer),
                            IndexingStrategy.SelfBalancingBinarySearchTree => comparer is null ? new SortedDictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, ActiveGrouping<TKey, TSource> grouping)>() : new SortedDictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, ActiveGrouping<TKey, TSource> grouping)>(comparer),
                            _ => throw new ArgumentOutOfRangeException(nameof(indexingStrategy), $"{nameof(indexingStrategy)} must be {IndexingStrategy.HashTable} or {IndexingStrategy.SelfBalancingBinarySearchTree}"),
                        };
                        rangeObservableCollection!.Clear();
                        foreach (var (element, key) in rangeActiveExpression.GetResults())
                            addElement(element, key);
                    }
                    else if (e.Action != NotifyCollectionChangedAction.Move)
                    {
                        if (e.OldItems is { })
                            foreach (var (element, key) in e.OldItems)
                                removeElement(element, key);
                        if (e.NewItems is { })
                            foreach (var (element, key) in e.NewItems)
                                addElement(element, key);
                    }
                });

            void removeElement(TSource element, TKey key, int count = 1)
            {
                var (groupingObservableCollection, grouping) = collectionAndGroupingDictionary[key];
                while (--count >= 0)
                    groupingObservableCollection.Remove(element);
                if (groupingObservableCollection.Count == 0)
                {
                    rangeObservableCollection!.Remove(grouping);
                    grouping.Dispose();
                    collectionAndGroupingDictionary.Remove(key);
                }
            }

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, keySelector, keySelectorOptions);
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.ElementResultChanging += elementResultChanging;
                rangeActiveExpression.GenericCollectionChanged += genericCollectionChanged;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<ActiveGrouping<TKey, TSource>>();
                foreach (var (element, key) in rangeActiveExpression.GetResults())
                    addElement(element, key);

                return new ActiveEnumerable<ActiveGrouping<TKey, TSource>>(rangeObservableCollection, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                    rangeActiveExpression.ElementResultChanging -= elementResultChanging;
                    rangeActiveExpression.GenericCollectionChanged -= genericCollectionChanged;
                });
            })!;
        }

        #endregion GroupBy

        #region Last

        /// <summary>
        /// Actively returns the last element of a sequence
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to return the last element of</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the value at the last position in the source sequence</returns>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static IActiveValue<TSource> ActiveLast<TSource>(this IEnumerable<TSource> source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyCollectionChanged changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<TSource>? setValue = null;
                Action<Exception?>? setOperationFault = null;

                void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        try
                        {
                            var value = source.Last();
                            setOperationFault!(null);
                            setValue!(value);
                        }
                        catch (Exception ex)
                        {
                            setOperationFault!(ex);
                            setValue!(default!);
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.CollectionChanged += collectionChanged;
                    try
                    {
                        return new ActiveValue<TSource>(source.Last(), out setValue, out setOperationFault, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    }
                    catch (Exception ex)
                    {
                        return new ActiveValue<TSource>(default!, out setValue, ex, out setOperationFault, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    }
                })!;
            }
            try
            {
                return new ActiveValue<TSource>(source.Last(), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<TSource>(default!, ex, elementFaultChangeNotifier);
            }
        }

        /// <summary>
        /// Actively returns the last element of a sequence that satisfies a specified condition
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to return the last element of</param>
        /// <param name="predicate">A function to test each element for a condition</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the last element in the sequence that passes the test in the spredicate function</returns>
        public static IActiveValue<TSource> ActiveLast<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate) =>
            ActiveLast(source, predicate, null);

        /// <summary>
        /// Actively returns the last element of a sequence that satisfies a specified condition
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to return the last element of</param>
        /// <param name="predicate">A function to test each element for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the last element in the sequence that passes the test in the spredicate function</returns>
        public static IActiveValue<TSource> ActiveLast<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions? predicateOptions)
        {
            IActiveEnumerable<TSource> where;
            Action<TSource>? setValue = null;
            Action<Exception?>? setOperationFault = null;
            var none = false;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (none && where.Count > 0)
                {
                    setOperationFault!(null);
                    none = false;
                }
                else if (!none && where.Count == 0)
                {
                    setOperationFault!(ExceptionHelper.SequenceContainsNoElements);
                    none = true;
                }
                setValue!(where.Count > 0 ? where[where.Count - 1] : default);
            }

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.CollectionChanged += collectionChanged;

                none = where.Count == 0;
                return new ActiveValue<TSource>(!none ? where[where.Count - 1] : default, out setValue, none ? ExceptionHelper.SequenceContainsNoElements : null, out setOperationFault, where, () =>
                {
                    where.CollectionChanged -= collectionChanged;
                    where.Dispose();
                });
            })!;
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
                Action<TSource>? setValue = null;

                void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) => synchronizedSource.SequentialExecute(() => setValue!(source.LastOrDefault()));

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.CollectionChanged += collectionChanged;
                    return new ActiveValue<TSource>(source.LastOrDefault(), out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changingSource.CollectionChanged -= collectionChanged);
                })!;
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
        public static IActiveValue<TSource> ActiveLastOrDefault<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate) =>
            ActiveLastOrDefault(source, predicate, null);

        /// <summary>
        /// Actively returns the last element of a sequence that satisfies a condition or a default value if no such element is found
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to return an element from</param>
        /// <param name="predicate">A function to test each element for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>default</c>(<typeparamref name="TSource"/>) if the sequence is empty or if no elements pass the test in the predicate function; otherwise, the last element that passes the test in the predicate function</returns>
        public static IActiveValue<TSource> ActiveLastOrDefault<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions? predicateOptions)
        {
            IActiveEnumerable<TSource> where;
            Action<TSource>? setValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) => setValue!(where.Count > 0 ? where[where.Count - 1] : default);

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.CollectionChanged += collectionChanged;

                return new ActiveValue<TSource>(where.Count > 0 ? where[where.Count - 1] : default, out setValue, elementFaultChangeNotifier: where, onDispose: () =>
                {
                    where.CollectionChanged -= collectionChanged;
                    where.Dispose();
                });
            })!;
        }

        #endregion LastOrDefault

        #region Max

        /// <summary>
        /// Actively returns the maximum value in a sequence of values
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the maximum value in the sequence</returns>
        public static IActiveValue<TSource> ActiveMax<TSource>(this IEnumerable<TSource> source) =>
            ActiveMax(source, element => element);

        /// <summary>
        /// Actively invokes a transform function on each element of a sequence and returns the maximum value
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the maximum value</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of</param>
        /// <param name="selector">A transform function to apply to each element</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the maximum value in the sequence</returns>
        public static IActiveValue<TResult> ActiveMax<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector) =>
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
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static IActiveValue<TResult> ActiveMax<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var comparer = Comparer<TResult>.Default;
            var synchronizedSource = source as ISynchronized;
            EnumerableRangeActiveExpression<TSource, TResult> rangeActiveExpression;
            ActiveValue<TResult>? activeValue = null;
            Action<TResult>? setValue = null;
            Action<Exception?>? setOperationFault = null;

            void dispose()
            {
                rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                rangeActiveExpression.GenericCollectionChanged -= genericCollectionChanged;
                rangeActiveExpression.Dispose();
            }

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var comparison = comparer.Compare(activeValue!.Value, e.Result);
                    if (comparison < 0)
                        setValue!(e.Result);
                    else if (comparison > 0)
                        setValue!(rangeActiveExpression.GetResultsUnderLock().Max(er => er.result));
                });

            void genericCollectionChanged(object sender, INotifyGenericCollectionChangedEventArgs<(TSource element, TResult result)> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        try
                        {
                            setOperationFault!(null);
                            setValue!(rangeActiveExpression.GetResults().Max(er => er.result));
                        }
                        catch (Exception ex)
                        {
                            setOperationFault!(ex);
                            setValue!(default!);
                        }
                    }
                    else if (e.Action != NotifyCollectionChangedAction.Move)
                    {
                        if ((e.OldItems?.Count ?? 0) > 0)
                        {
                            var removedMax = e.OldItems.Max(er => er.result);
                            if (comparer.Compare(activeValue!.Value, removedMax) == 0)
                            {
                                try
                                {
                                    var value = rangeActiveExpression.GetResultsUnderLock().Max(er => er.result);
                                    setOperationFault!(null);
                                    setValue!(value);
                                }
                                catch (Exception ex)
                                {
                                    setOperationFault!(ex);
                                }
                            }
                        }
                        if ((e.NewItems?.Count ?? 0) > 0)
                        {
                            var addedMax = e.NewItems.Max(er => er.result);
                            if (activeValue!.OperationFault is { } || comparer.Compare(activeValue.Value, addedMax) < 0)
                            {
                                setOperationFault!(null);
                                setValue!(addedMax);
                            }
                        }
                    }
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.GenericCollectionChanged += genericCollectionChanged;

                try
                {
                    return activeValue = new ActiveValue<TResult>(rangeActiveExpression.GetResults().Max(er => er.result), out setValue, null, out setOperationFault, rangeActiveExpression, dispose);
                }
                catch (Exception ex)
                {
                    return activeValue = new ActiveValue<TResult>(default!, out setValue, ex, out setOperationFault, rangeActiveExpression, dispose);
                }
            })!;
        }

        #endregion Max

        #region Min

        /// <summary>
        /// Actively returns the minimum value in a sequence of values
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the minimum value in the sequence</returns>
        public static IActiveValue<TSource> ActiveMin<TSource>(this IEnumerable<TSource> source) =>
            ActiveMin(source, element => element);

        /// <summary>
        /// Actively invokes a transform function on each element of a sequence and returns the minimum value
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the minimum value</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of</param>
        /// <param name="selector">A transform function to apply to each element</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the minimum value in the sequence</returns>
        public static IActiveValue<TResult> ActiveMin<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector) =>
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
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static IActiveValue<TResult> ActiveMin<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var comparer = Comparer<TResult>.Default;
            var synchronizedSource = source as ISynchronized;
            EnumerableRangeActiveExpression<TSource, TResult> rangeActiveExpression;
            ActiveValue<TResult>? activeValue = null;
            Action<TResult>? setValue = null;
            Action<Exception?>? setOperationFault = null;

            void dispose()
            {
                rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                rangeActiveExpression.GenericCollectionChanged -= genericCollectionChanged;
                rangeActiveExpression.Dispose();
            }

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var comparison = comparer.Compare(activeValue!.Value, e.Result);
                    if (comparison > 0)
                        setValue!(e.Result);
                    else if (comparison < 0)
                        setValue!(rangeActiveExpression.GetResultsUnderLock().Min(er => er.result));
                });

            void genericCollectionChanged(object sender, INotifyGenericCollectionChangedEventArgs<(TSource element, TResult result)> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        try
                        {
                            setOperationFault!(null);
                            setValue!(rangeActiveExpression.GetResults().Min(er => er.result));
                        }
                        catch (Exception ex)
                        {
                            setOperationFault!(ex);
                            setValue!(default!);
                        }
                    }
                    else if (e.Action != NotifyCollectionChangedAction.Move)
                    {
                        if ((e.OldItems?.Count ?? 0) > 0)
                        {
                            var removedMin = e.OldItems.Min(er => er.result);
                            if (comparer.Compare(activeValue!.Value, removedMin) == 0)
                            {
                                try
                                {
                                    var value = rangeActiveExpression.GetResultsUnderLock().Min(er => er.result);
                                    setOperationFault!(null);
                                    setValue!(value);
                                }
                                catch (Exception ex)
                                {
                                    setOperationFault!(ex);
                                }
                            }
                        }
                        if ((e.NewItems?.Count ?? 0) > 0)
                        {
                            var addedMin = e.NewItems.Min(er => er.result);
                            if (activeValue!.OperationFault is { } || comparer.Compare(activeValue.Value, addedMin) > 0)
                            {
                                setOperationFault!(null);
                                setValue!(addedMin);
                            }
                        }
                    }
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.GenericCollectionChanged += genericCollectionChanged;

                try
                {
                    return activeValue = new ActiveValue<TResult>(rangeActiveExpression.GetResults().Min(er => er.result), out setValue, null, out setOperationFault, rangeActiveExpression, dispose);
                }
                catch (Exception ex)
                {
                    return activeValue = new ActiveValue<TResult>(default!, out setValue, ex, out setOperationFault, rangeActiveExpression, dispose);
                }
            })!;
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
                synchronizedSource.SequentialExecute(() =>
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

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeObservableCollection = new SynchronizedRangeObservableCollection<TResult>(source.OfType<TResult>());

                if (notifyingSource is { })
                    notifyingSource.CollectionChanged += collectionChanged;

                return new ActiveEnumerable<TResult>(rangeObservableCollection, source as INotifyElementFaultChanges, () =>
                {
                    if (notifyingSource is { })
                        notifyingSource.CollectionChanged -= collectionChanged;
                });
            })!;
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
            if (keySelectors.Length == 0)
                return ToActiveEnumerable(source);

            keySelectors = keySelectors.Select(s => new ActiveOrderingKeySelector<TSource>(ActiveQueryOptions.Optimize(s.Expression), s.ExpressionOptions, s.IsDescending)).ToArray();

            ActiveOrderingComparer<TSource>? comparer = null;
            var equalityComparer = EqualityComparer<TSource>.Default;
            SynchronizedRangeObservableCollection<TSource>? rangeObservableCollection = null;
            IDictionary<TSource, (int startingIndex, int count)>? startingIndiciesAndCounts = null;
            var synchronizedSource = source as ISynchronized;

            void rebuildStartingIndiciesAndCounts(IReadOnlyList<TSource> fromSort)
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

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, IComparable> e) => synchronizedSource.SequentialExecute(() => repositionElement(e.Element));

            void genericCollectionChanged(object sender, INotifyGenericCollectionChangedEventArgs<(TSource element, IComparable comparable)> e) =>
                synchronizedSource.SequentialExecute(() =>
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
                                foreach (var elementAndResults in e.OldItems.GroupBy(er => er.element, er => er.comparable))
                                    rangeObservableCollection.RemoveRange(rangeObservableCollection.IndexOf(elementAndResults.Key), elementAndResults.Count());
                            else
                                foreach (var elementAndResults in e.OldItems.GroupBy(er => er.element, er => er.comparable))
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
                                var sorted = e.NewItems.Select(er => er.element).ToList();
                                sorted.Sort(comparer);
                                if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                                    rebuildStartingIndiciesAndCounts(sorted);
                                rangeObservableCollection.Reset(sorted);
                            }
                            else
                                foreach (var elementAndResults in e.NewItems.GroupBy(er => er.element, er => er.comparable))
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
                                            if (!equalityComparer.Equals(otherElement, element))
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
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                var keySelections = keySelectors.Select(selector => (rangeActiveExpression: EnumerableRangeActiveExpression<TSource, IComparable>.Create(source, selector.Expression, selector.ExpressionOptions), isDescending: selector.IsDescending)).ToList();
                comparer = new ActiveOrderingComparer<TSource>(keySelections.Select(selection => (selection.rangeActiveExpression, selection.isDescending)).ToList(), indexingStrategy);
                var (lastRangeActiveExpression, lastIsDescending) = keySelections[keySelections.Count - 1];
                lastRangeActiveExpression.GenericCollectionChanged += genericCollectionChanged;
                foreach (var (rangeActiveExpression, isDescending) in keySelections)
                    rangeActiveExpression.ElementResultChanged += elementResultChanged;
                var sortedSource = source.ToList();
                sortedSource.Sort(comparer);

                if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                    rebuildStartingIndiciesAndCounts(sortedSource);

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(sortedSource);
                var mergedElementFaultChangeNotifier = new MergedElementFaultChangeNotifier(keySelections.Select(selection => selection.rangeActiveExpression));
                return new ActiveEnumerable<TSource>(rangeObservableCollection, mergedElementFaultChangeNotifier, () =>
                {
                    lastRangeActiveExpression.GenericCollectionChanged -= genericCollectionChanged;
                    foreach (var (rangeActiveExpression, isDescending) in keySelections)
                    {
                        rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                        rangeActiveExpression.Dispose();
                    }
                    mergedElementFaultChangeNotifier.Dispose();
                });
            })!;
        }

        #endregion OrderBy

        #region Select

        /// <summary>
        /// Actively projects each element of a sequence into a new form using the <see cref="IndexingStrategy.HashTable"/> indexing strategy
        /// </summary>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on</param>
        /// <param name="selector">A transform function to apply to each element</param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
        public static IActiveEnumerable<TResult> ActiveSelect<TResult>(this IEnumerable source, Expression<Func<object?, TResult>> selector) =>
            ActiveSelect(source, selector, null);

        /// <summary>
        /// Actively projects each element of a sequence into a new form using the <see cref="IndexingStrategy.HashTable"/> indexing strategy
        /// </summary>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on</param>
        /// <param name="selector">A transform function to apply to each element</param>
        /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
        public static IActiveEnumerable<TResult> ActiveSelect<TResult>(this IEnumerable source, Expression<Func<object?, TResult>> selector, ActiveExpressionOptions? selectorOptions) =>
            ActiveSelect(source, selector, selectorOptions, IndexingStrategy.HashTable);

        /// <summary>
        /// Actively projects each element of a sequence into a new form using the specified indexing strategy
        /// </summary>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on</param>
        /// <param name="selector">A transform function to apply to each element</param>
        /// <param name="indexingStrategy">The indexing strategy to use</param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
        public static IActiveEnumerable<TResult> ActiveSelect<TResult>(this IEnumerable source, Expression<Func<object?, TResult>> selector, IndexingStrategy indexingStrategy) =>
            ActiveSelect(source, selector, null, indexingStrategy);

        /// <summary>
        /// Actively projects each element of a sequence into a new form using the specified indexing strategy
        /// </summary>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on</param>
        /// <param name="selector">A transform function to apply to each element</param>
        /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
        /// <param name="indexingStrategy">The indexing strategy to use</param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
        public static IActiveEnumerable<TResult> ActiveSelect<TResult>(this IEnumerable source, Expression<Func<object?, TResult>> selector, ActiveExpressionOptions? selectorOptions, IndexingStrategy indexingStrategy)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var sourceToIndicies = indexingStrategy switch
            {
                IndexingStrategy.NoneOrInherit => null,
                IndexingStrategy.SelfBalancingBinarySearchTree => throw new ArgumentOutOfRangeException(nameof(indexingStrategy), $"{nameof(indexingStrategy)} must be {IndexingStrategy.HashTable} or {IndexingStrategy.NoneOrInherit}"),
                _ => new Dictionary<object, List<int>>(),
            };

            var synchronizedSource = source as ISynchronized;
            EnumerableRangeActiveExpression<TResult> rangeActiveExpression;
            SynchronizedRangeObservableCollection<TResult>? rangeObservableCollection = null;

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<object?, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var sourceElement = e.Element;
                    var newResultElement = e.Result;
                    var indicies = indexingStrategy != IndexingStrategy.NoneOrInherit ? sourceToIndicies![sourceElement!] : source.Cast<object>().IndiciesOf(sourceElement).ToList();
                    rangeObservableCollection!.Replace(indicies[0], newResultElement);
                    foreach (var remainingIndex in indicies.Skip(1))
                        rangeObservableCollection[remainingIndex] = newResultElement;
                });

            void genericCollectionChanged(object sender, INotifyGenericCollectionChangedEventArgs<(object? element, TResult result)> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Reset:
                            if (indexingStrategy == IndexingStrategy.NoneOrInherit)
                                rangeObservableCollection!.Reset(rangeActiveExpression.GetResults().Select(er => er.result));
                            else
                            {
                                sourceToIndicies = indexingStrategy switch
                                {
                                    IndexingStrategy.SelfBalancingBinarySearchTree => throw new ArgumentOutOfRangeException(nameof(indexingStrategy), $"{nameof(indexingStrategy)} must be {IndexingStrategy.HashTable} or {IndexingStrategy.NoneOrInherit}"),
                                    _ => new Dictionary<object, List<int>>(),
                                };
                                rangeObservableCollection!.Reset(rangeActiveExpression.GetResults().Select(indexedInitializer));
                            }
                            break;
                        case NotifyCollectionChangedAction.Move:
                            if (e.NewItems.Count > 0 && e.OldStartingIndex != e.NewStartingIndex)
                            {
                                if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                                {
                                    int movementEnd = e.OldStartingIndex + e.NewItems.Count, move = e.NewStartingIndex - e.OldStartingIndex, displacementStart, displacementEnd, displace;
                                    if (e.OldStartingIndex < e.NewStartingIndex)
                                    {
                                        displacementStart = movementEnd;
                                        displacementEnd = e.NewStartingIndex + e.NewItems.Count;
                                        displace = e.NewItems.Count * -1;
                                    }
                                    else
                                    {
                                        displacementStart = e.NewStartingIndex;
                                        displacementEnd = e.OldStartingIndex;
                                        displace = e.NewItems.Count;
                                    }
                                    foreach (var element in sourceToIndicies!.Keys.ToList())
                                    {
                                        var indiciesList = sourceToIndicies[element];
                                        for (int i = 0, ii = indiciesList.Count; i < ii; ++i)
                                        {
                                            var index = indiciesList[i];
                                            if (index >= e.OldStartingIndex && index < movementEnd)
                                                indiciesList[i] = index + move;
                                            else if (index >= displacementStart && index < displacementEnd)
                                                indiciesList[i] = index + displace;
                                        }
                                    }
                                }
                                rangeObservableCollection!.MoveRange(e.OldStartingIndex, e.NewStartingIndex, e.NewItems.Count);
                            }
                            break;
                        case NotifyCollectionChangedAction.Add:
                        case NotifyCollectionChangedAction.Remove:
                        case NotifyCollectionChangedAction.Replace:
                            if (e.OldItems is { } && e.OldItems.Count > 0)
                            {
                                rangeObservableCollection!.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                                if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                                {
                                    var endIndex = e.OldStartingIndex + e.OldItems.Count;
                                    foreach (var element in sourceToIndicies!.Keys.ToList())
                                    {
                                        var indiciesList = sourceToIndicies[element];
                                        for (var i = 0; i < indiciesList.Count;)
                                        {
                                            var listIndex = indiciesList[i];
                                            if (listIndex >= e.OldStartingIndex)
                                            {
                                                if (listIndex >= endIndex)
                                                {
                                                    indiciesList[i] = listIndex - e.OldItems.Count;
                                                    ++i;
                                                }
                                                else
                                                    indiciesList.RemoveAt(i);
                                            }
                                            else
                                                ++i;
                                        }
                                        if (indiciesList.Count == 0)
                                            sourceToIndicies.Remove(element);
                                    }
                                }
                            }
                            if (e.NewItems is { } && e.NewItems.Count > 0)
                            {
                                if (indexingStrategy == IndexingStrategy.NoneOrInherit)
                                    rangeObservableCollection!.InsertRange(e.NewStartingIndex, e.NewItems.Select(er => er.result));
                                else
                                {
                                    foreach (var indiciesList in sourceToIndicies!.Values)
                                        for (int i = 0, ii = indiciesList.Count; i < ii; ++i)
                                        {
                                            var listIndex = indiciesList[i];
                                            if (listIndex >= e.NewStartingIndex)
                                                indiciesList[i] = listIndex + e.NewItems.Count;
                                        }
                                    rangeObservableCollection!.InsertRange(e.NewStartingIndex, e.NewItems.Select((er, sIndex) =>
                                    {
                                        var (element, result) = er;
                                        if (!sourceToIndicies.TryGetValue(element!, out var indiciesList))
                                        {
                                            indiciesList = new List<int>();
                                            sourceToIndicies.Add(element!, indiciesList);
                                        }
                                        indiciesList.Add(e.NewStartingIndex + sIndex);
                                        return result;
                                    }));
                                }
                            }
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                });

            TResult indexedInitializer((object element, TResult result) er, int index)
            {
                var (element, result) = er;
                if (!sourceToIndicies!.TryGetValue(element, out var indicies))
                {
                    indicies = new List<int>();
                    sourceToIndicies.Add(element, indicies);
                }
                indicies.Add(index);
                return result;
            }

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.GenericCollectionChanged += genericCollectionChanged;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TResult>(indexingStrategy != IndexingStrategy.NoneOrInherit ? rangeActiveExpression.GetResults().Select(indexedInitializer) : rangeActiveExpression.GetResults().Select(er => er.result));
                return new ActiveEnumerable<TResult>(rangeObservableCollection, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                    rangeActiveExpression.GenericCollectionChanged -= genericCollectionChanged;

                    rangeActiveExpression.Dispose();
                });
            })!;
        }

        /// <summary>
        /// Actively projects each element of a sequence into a new form using the <see cref="IndexingStrategy.HashTable"/> indexing strategy
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on</param>
        /// <param name="selector">A transform function to apply to each element</param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
        public static IActiveEnumerable<TResult> ActiveSelect<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector) =>
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
        public static IActiveEnumerable<TResult> ActiveSelect<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions) =>
            ActiveSelect(source, selector, selectorOptions, IndexingStrategy.HashTable);

        /// <summary>
        /// Actively projects each element of a sequence into a new form using the <see cref="IndexingStrategy.HashTable"/> indexing strategy
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on</param>
        /// <param name="selector">A transform function to apply to each element</param>
        /// <param name="indexingStrategy">The indexing strategy to use</param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
        public static IActiveEnumerable<TResult> ActiveSelect<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, IndexingStrategy indexingStrategy) =>
            ActiveSelect(source, selector, null, indexingStrategy);

        /// <summary>
        /// Actively projects each element of a sequence into a new form using the specified indexing strategy
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on</param>
        /// <param name="selector">A transform function to apply to each element</param>
        /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
        /// <param name="indexingStrategy">The indexing strategy to use</param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the transform function on each element of <paramref name="source"/></returns>
        public static IActiveEnumerable<TResult> ActiveSelect<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions, IndexingStrategy indexingStrategy)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var sourceToIndicies = (IDictionary<TSource, List<int>>)(indexingStrategy switch
            {
                IndexingStrategy.NoneOrInherit => null,
                IndexingStrategy.SelfBalancingBinarySearchTree => new SortedDictionary<TSource, List<int>>(),
                _ => new Dictionary<TSource, List<int>>(),
            });

            var synchronizedSource = source as ISynchronized;
            EnumerableRangeActiveExpression<TSource, TResult> rangeActiveExpression;
            SynchronizedRangeObservableCollection<TResult>? rangeObservableCollection = null;

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var sourceElement = e.Element;
                    var newResultElement = e.Result;
                    var indicies = indexingStrategy != IndexingStrategy.NoneOrInherit ? (IReadOnlyList<int>)sourceToIndicies![sourceElement] : source.IndiciesOf(sourceElement).ToImmutableArray();
                    rangeObservableCollection!.Replace(indicies[0], newResultElement);
                    foreach (var remainingIndex in indicies.Skip(1))
                        rangeObservableCollection[remainingIndex] = newResultElement;
                });

            void genericCollectionChanged(object sender, INotifyGenericCollectionChangedEventArgs<(TSource element, TResult result)> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Reset:
                            rangeObservableCollection!.Reset(indexingStrategy != IndexingStrategy.NoneOrInherit ? rangeActiveExpression.GetResults().Select(indexedInitializer) : rangeActiveExpression.GetResults().Select(er => er.result));
                            if (indexingStrategy == IndexingStrategy.NoneOrInherit)
                                rangeObservableCollection.Reset(rangeActiveExpression.GetResults().Select(er => er.result));
                            else
                            {
                                sourceToIndicies = indexingStrategy switch
                                {
                                    IndexingStrategy.SelfBalancingBinarySearchTree => new SortedDictionary<TSource, List<int>>(),
                                    _ => new Dictionary<TSource, List<int>>(),
                                };
                                rangeObservableCollection.Reset(rangeActiveExpression.GetResults().Select(indexedInitializer));
                            }
                            break;
                        case NotifyCollectionChangedAction.Move:
                            if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                            {
                                int movementEnd = e.OldStartingIndex + e.NewItems.Count, move = e.NewStartingIndex - e.OldStartingIndex, displacementStart, displacementEnd, displace;
                                if (e.OldStartingIndex < e.NewStartingIndex)
                                {
                                    displacementStart = movementEnd;
                                    displacementEnd = e.NewStartingIndex + e.NewItems.Count;
                                    displace = e.NewItems.Count * -1;
                                }
                                else
                                {
                                    displacementStart = e.NewStartingIndex;
                                    displacementEnd = e.OldStartingIndex;
                                    displace = e.NewItems.Count;
                                }
                                foreach (var element in sourceToIndicies!.Keys.ToList())
                                {
                                    var indiciesList = sourceToIndicies[element];
                                    for (int i = 0, ii = indiciesList.Count; i < ii; ++i)
                                    {
                                        var index = indiciesList[i];
                                        if (index >= e.OldStartingIndex && index < movementEnd)
                                            indiciesList[i] = index + move;
                                        else if (index >= displacementStart && index < displacementEnd)
                                            indiciesList[i] = index + displace;
                                    }
                                }
                            }
                            rangeObservableCollection!.MoveRange(e.OldStartingIndex, e.NewStartingIndex, e.NewItems.Count);
                            break;
                        case NotifyCollectionChangedAction.Add:
                        case NotifyCollectionChangedAction.Remove:
                        case NotifyCollectionChangedAction.Replace:
                            if (e.OldItems is { } && e.OldItems.Count > 0)
                            {
                                rangeObservableCollection!.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                                if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                                {
                                    var endIndex = e.OldStartingIndex + e.OldItems.Count;
                                    foreach (var element in sourceToIndicies!.Keys.ToList())
                                    {
                                        var indiciesList = sourceToIndicies[element];
                                        for (var i = 0; i < indiciesList.Count;)
                                        {
                                            var listIndex = indiciesList[i];
                                            if (listIndex >= e.OldStartingIndex)
                                            {
                                                if (listIndex >= endIndex)
                                                {
                                                    indiciesList[i] = listIndex - e.OldItems.Count;
                                                    ++i;
                                                }
                                                else
                                                    indiciesList.RemoveAt(i);
                                            }
                                            else
                                                ++i;
                                        }
                                        if (indiciesList.Count == 0)
                                            sourceToIndicies.Remove(element);
                                    }
                                }
                            }
                            if (e.NewItems is { } && e.NewItems.Count > 0)
                            {
                                if (indexingStrategy == IndexingStrategy.NoneOrInherit)
                                    rangeObservableCollection!.InsertRange(e.NewStartingIndex, e.NewItems.Select(er => er.result));
                                else
                                {
                                    foreach (var indiciesList in sourceToIndicies!.Values)
                                        for (int i = 0, ii = indiciesList.Count; i < ii; ++i)
                                        {
                                            var listIndex = indiciesList[i];
                                            if (listIndex >= e.NewStartingIndex)
                                                indiciesList[i] = listIndex + e.NewItems.Count;
                                        }
                                    rangeObservableCollection!.InsertRange(e.NewStartingIndex, e.NewItems.Select((er, sIndex) =>
                                    {
                                        var (element, result) = er;
                                        if (!sourceToIndicies.TryGetValue(element, out var indiciesList))
                                        {
                                            indiciesList = new List<int>();
                                            sourceToIndicies.Add(element, indiciesList);
                                        }
                                        indiciesList.Add(e.NewStartingIndex + sIndex);
                                        return result;
                                    }));
                                }
                            }
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                });

            TResult indexedInitializer((TSource element, TResult result) er, int index)
            {
                var (element, result) = er;
                if (!sourceToIndicies!.TryGetValue(element, out var indicies))
                {
                    indicies = new List<int>();
                    sourceToIndicies.Add(element, indicies);
                }
                indicies.Add(index);
                return result;
            }

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.GenericCollectionChanged += genericCollectionChanged;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TResult>(indexingStrategy != IndexingStrategy.NoneOrInherit ? rangeActiveExpression.GetResults().Select(indexedInitializer) : rangeActiveExpression.GetResults().Select(er => er.result));
                return new ActiveEnumerable<TResult>(rangeObservableCollection, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                    rangeActiveExpression.GenericCollectionChanged -= genericCollectionChanged;

                    rangeActiveExpression.Dispose();
                });
            })!;
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
            ActiveSelectMany(source, selector, selectorOptions, IndexingStrategy.HashTable);

        /// <summary>
        /// Actively projects each element of a sequence to an <see cref="IEnumerable{T}"/> and flattens the resulting sequences into one sequence using the specified indexing strategy
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
        /// <param name="source">A sequence of values to project</param>
        /// <param name="selector">A transform function to apply to each element</param>
        /// <param name="indexingStrategy">The indexing strategy to use</param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the one-to-many transform function on each element of the input sequence</returns>
        public static IActiveEnumerable<TResult> ActiveSelectMany<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, IEnumerable<TResult>>> selector, IndexingStrategy indexingStrategy) =>
            ActiveSelectMany(source, selector, null, indexingStrategy);

        /// <summary>
        /// Actively projects each element of a sequence to an <see cref="IEnumerable{T}"/> and flattens the resulting sequences into one sequence using the specified indexing strategy
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
        /// <param name="source">A sequence of values to project</param>
        /// <param name="selector">A transform function to apply to each element</param>
        /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
        /// <param name="indexingStrategy">The indexing strategy to use</param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the one-to-many transform function on each element of the input sequence</returns>
        public static IActiveEnumerable<TResult> ActiveSelectMany<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, IEnumerable<TResult>>> selector, ActiveExpressionOptions? selectorOptions, IndexingStrategy indexingStrategy)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var sourceEqualityComparer = EqualityComparer<TSource>.Default;
            var changingResultToSource = new Dictionary<INotifyCollectionChanged, TSource>();
            IDictionary<TSource, INotifyCollectionChanged> sourceToChangingResult;
            IDictionary<TSource, int> sourceToCount;
            IDictionary<TSource, List<int>> sourceToStartingIndicies;
            switch (indexingStrategy)
            {
                case IndexingStrategy.HashTable:
                    sourceToChangingResult = new Dictionary<TSource, INotifyCollectionChanged>();
                    sourceToCount = new Dictionary<TSource, int>();
                    sourceToStartingIndicies = new Dictionary<TSource, List<int>>();
                    break;
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    sourceToChangingResult = new SortedDictionary<TSource, INotifyCollectionChanged>();
                    sourceToCount = new SortedDictionary<TSource, int>();
                    sourceToStartingIndicies = new SortedDictionary<TSource, List<int>>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(indexingStrategy), $"{nameof(indexingStrategy)} must be {IndexingStrategy.HashTable} or {IndexingStrategy.SelfBalancingBinarySearchTree}");
            }

            var synchronizableSource = source as ISynchronized;
            EnumerableRangeActiveExpression<TSource, IEnumerable<TResult>> rangeActiveExpression;
            SynchronizedRangeObservableCollection<TResult>? rangeObservableCollection = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizableSource.SequentialExecute(() =>
                {
                    var oldItems = e.OldItems is { } ? e.OldItems.Cast<TResult>() : Enumerable.Empty<TResult>();
                    var oldItemsCount = e.OldItems is { } ? e.OldItems.Count : 0;
                    var oldStartingIndex = e.OldStartingIndex;
                    var newItems = e.NewItems is { } ? e.NewItems.Cast<TResult>() : Enumerable.Empty<TResult>();
                    var newItemsCount = e.NewItems is { } ? e.NewItems.Count : 0;
                    var newStartingIndex = e.NewStartingIndex;
                    var element = changingResultToSource[(INotifyCollectionChanged)sender];
                    var previousCount = sourceToCount[element];
                    var action = e.Action;
                    var result = (IEnumerable<TResult>)sender;
                    var countDifference = action == NotifyCollectionChangedAction.Reset ? result.Count() - previousCount : newItemsCount - oldItemsCount;
                    if (countDifference != 0)
                        sourceToCount[element] = previousCount + countDifference;
                    var startingIndiciesList = sourceToStartingIndicies[element];
                    for (int i = 0, ii = startingIndiciesList.Count; i < ii; ++i)
                    {
                        var startingIndex = startingIndiciesList[i];
                        switch (action)
                        {
                            case NotifyCollectionChangedAction.Reset:
                                if (previousCount > 0)
                                    rangeObservableCollection!.ReplaceRange(startingIndex, previousCount, result);
                                else
                                    rangeObservableCollection!.InsertRange(startingIndex, result);
                                break;
                            case NotifyCollectionChangedAction.Replace when oldStartingIndex == newStartingIndex:
                                rangeObservableCollection!.ReplaceRange(startingIndex + oldStartingIndex, oldItemsCount, newItems);
                                break;
                            case NotifyCollectionChangedAction.Move when oldItems.SequenceEqual(newItems):
                                rangeObservableCollection!.MoveRange(startingIndex + oldStartingIndex, startingIndex + newStartingIndex, oldItemsCount);
                                break;
                            default:
                                rangeObservableCollection!.RemoveRange(startingIndex + oldStartingIndex, oldItemsCount);
                                rangeObservableCollection.InsertRange(startingIndex + newStartingIndex, newItems);
                                break;
                        }
                        if (countDifference != 0)
                            foreach (var adjustingStartingIndiciesKv in sourceToStartingIndicies)
                            {
                                var adjustingElement = adjustingStartingIndiciesKv.Key;
                                var adjustingStartingIndicies = adjustingStartingIndiciesKv.Value;
                                if (sourceEqualityComparer.Equals(element, adjustingElement))
                                    for (int j = 0, jj = adjustingStartingIndicies.Count; j < jj; ++j)
                                    {
                                        var adjustingStartingIndex = adjustingStartingIndicies[j];
                                        if (adjustingStartingIndex > startingIndex)
                                            adjustingStartingIndicies[j] = adjustingStartingIndex + countDifference;
                                    }
                                else
                                    for (int j = 0, jj = adjustingStartingIndicies.Count; j < jj; ++j)
                                    {
                                        var adjustingStartingIndex = adjustingStartingIndicies[j];
                                        if (adjustingStartingIndex >= startingIndex)
                                            adjustingStartingIndicies[j] = adjustingStartingIndex + countDifference;
                                    }
                            }
                    }
                });

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, IEnumerable<TResult>> e) =>
                synchronizableSource.SequentialExecute(() =>
                {
                    var element = e.Element;
                    if (sourceToChangingResult.TryGetValue(element, out var previousChangingResult))
                    {
                        changingResultToSource.Remove(previousChangingResult);
                        sourceToChangingResult.Remove(element);
                        previousChangingResult.CollectionChanged -= collectionChanged;
                    }
                    var previousCount = sourceToCount[element];
                    var result = e.Result;
                    var newCount = result.Count();
                    sourceToCount[element] = newCount;
                    var countDifference = newCount - previousCount;
                    var startingIndiciesList = sourceToStartingIndicies[element];
                    for (int i = 0, ii = startingIndiciesList.Count; i < ii; ++i)
                    {
                        var startingIndex = startingIndiciesList[i];
                        rangeObservableCollection!.ReplaceRange(startingIndex, previousCount, result);
                        foreach (var adjustingStartingIndiciesKv in sourceToStartingIndicies)
                        {
                            var adjustingElement = adjustingStartingIndiciesKv.Key;
                            var adjustingStartingIndicies = adjustingStartingIndiciesKv.Value;
                            if (sourceEqualityComparer.Equals(element, adjustingElement))
                                for (int j = 0, jj = adjustingStartingIndicies.Count; j < jj; ++j)
                                {
                                    var adjustingStartingIndex = adjustingStartingIndicies[j];
                                    if (adjustingStartingIndex > startingIndex)
                                        adjustingStartingIndicies[j] = adjustingStartingIndex + countDifference;
                                }
                            else
                                for (int j = 0, jj = adjustingStartingIndicies.Count; j < jj; ++j)
                                {
                                    var adjustingStartingIndex = adjustingStartingIndicies[j];
                                    if (adjustingStartingIndex >= startingIndex)
                                        adjustingStartingIndicies[j] = adjustingStartingIndex + countDifference;
                                }
                        }
                    }
                    if (result is INotifyCollectionChanged newChangingResult)
                    {
                        newChangingResult.CollectionChanged += collectionChanged;
                        changingResultToSource.Add(newChangingResult, element);
                        sourceToChangingResult.Add(element, newChangingResult);
                    }
                });

            void genericCollectionChanged(object sender, INotifyGenericCollectionChangedEventArgs<(TSource element, IEnumerable<TResult> results)> e) =>
                synchronizableSource.SequentialExecute(() =>
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Reset:
                            foreach (var changingResult in changingResultToSource.Keys)
                                changingResult.CollectionChanged -= collectionChanged;
                            switch (indexingStrategy)
                            {
                                case IndexingStrategy.HashTable:
                                    sourceToChangingResult = new Dictionary<TSource, INotifyCollectionChanged>();
                                    sourceToCount = new Dictionary<TSource, int>();
                                    sourceToStartingIndicies = new Dictionary<TSource, List<int>>();
                                    break;
                                case IndexingStrategy.SelfBalancingBinarySearchTree:
                                    sourceToChangingResult = new SortedDictionary<TSource, INotifyCollectionChanged>();
                                    sourceToCount = new SortedDictionary<TSource, int>();
                                    sourceToStartingIndicies = new SortedDictionary<TSource, List<int>>();
                                    break;
                            }
                            rangeObservableCollection!.Reset(rangeActiveExpression.GetResults().SelectMany(initializer));
                            break;
                        case NotifyCollectionChangedAction.Move:
                            {
                                var count = e.NewItems.SelectMany(er => er.results).Count();
                                if (count > 0 && e.OldStartingIndex != e.NewStartingIndex)
                                {
                                    var indexTranslation = sourceToStartingIndicies.SelectMany(kv => kv.Value).OrderBy(resultIndex => resultIndex).ToImmutableArray();
                                    var fromIndex = indexTranslation[e.OldStartingIndex];
                                    var toIndex = e.OldStartingIndex > e.NewStartingIndex ? indexTranslation[e.NewStartingIndex] : (e.NewStartingIndex == indexTranslation.Length - 1 ? rangeObservableCollection!.Count : indexTranslation[e.NewStartingIndex + 1]) - count;
                                    int movementEnd = fromIndex + count, move = toIndex - fromIndex, displacementStart, displacementEnd, displace;
                                    if (fromIndex < toIndex)
                                    {
                                        displacementStart = movementEnd;
                                        displacementEnd = toIndex + count;
                                        displace = count * -1;
                                    }
                                    else
                                    {
                                        displacementStart = toIndex;
                                        displacementEnd = fromIndex;
                                        displace = count;
                                    }
                                    foreach (var element in sourceToStartingIndicies.Keys.ToList())
                                    {
                                        var startingIndiciesList = sourceToStartingIndicies[element];
                                        for (int i = 0, ii = startingIndiciesList.Count; i < ii; ++i)
                                        {
                                            var startingIndex = startingIndiciesList[i];
                                            if (startingIndex >= fromIndex && startingIndex < movementEnd)
                                                startingIndiciesList[i] = startingIndex + move;
                                            else if (startingIndex >= displacementStart && startingIndex < displacementEnd)
                                                startingIndiciesList[i] = startingIndex + displace;
                                        }
                                    }
                                    rangeObservableCollection!.MoveRange(fromIndex, toIndex, count);
                                }
                            }
                            break;
                        case NotifyCollectionChangedAction.Add:
                        case NotifyCollectionChangedAction.Remove:
                        case NotifyCollectionChangedAction.Replace:
                            if ((e.OldItems?.Count ?? 0) > 0)
                            {
                                var count = e.OldItems.SelectMany(er => er.results).Count();
                                if (count > 0)
                                {
                                    var startIndex = e.OldStartingIndex == 0 ? 0 : sourceToStartingIndicies.SelectMany(kv => kv.Value).OrderBy(resultIndex => resultIndex).ElementAt(e.OldStartingIndex);
                                    rangeObservableCollection!.RemoveRange(startIndex, count);
                                    var endIndex = startIndex + count;
                                    foreach (var element in sourceToStartingIndicies.Keys.ToList())
                                    {
                                        var startingIndiciesList = sourceToStartingIndicies[element];
                                        for (var i = 0; i < startingIndiciesList.Count;)
                                        {
                                            var startingListIndex = startingIndiciesList[i];
                                            if (startingListIndex >= startIndex)
                                            {
                                                if (startingListIndex >= endIndex)
                                                {
                                                    startingIndiciesList[i] = startingListIndex - count;
                                                    ++i;
                                                }
                                                else
                                                    startingIndiciesList.RemoveAt(i);
                                            }
                                            else
                                                ++i;
                                        }
                                        if (startingIndiciesList.Count == 0)
                                        {
                                            sourceToCount.Remove(element);
                                            sourceToStartingIndicies.Remove(element);
                                            if (sourceToChangingResult.TryGetValue(element, out var changingResult))
                                            {
                                                changingResultToSource.Remove(changingResult);
                                                sourceToChangingResult.Remove(element);
                                                changingResult.CollectionChanged -= collectionChanged;
                                            }
                                        }
                                    }
                                }
                            }
                            if ((e.NewItems?.Count ?? 0) > 0)
                            {
                                int resultsIndex;
                                if (e.NewStartingIndex == 0)
                                    resultsIndex = 0;
                                else
                                {
                                    var indexTranslation = sourceToStartingIndicies.SelectMany(kv => kv.Value).OrderBy(resultIndex => resultIndex).ToImmutableArray();
                                    resultsIndex = e.NewStartingIndex < indexTranslation.Length ? indexTranslation[e.NewStartingIndex] : rangeObservableCollection!.Count;
                                }

                                var iterativeResultsIndex = resultsIndex;
                                var newSourceToStartingIndicies = (IDictionary<TSource, List<int>>)(indexingStrategy switch
                                {
                                    IndexingStrategy.SelfBalancingBinarySearchTree => new SortedDictionary<TSource, List<int>>(),
                                    _ => new Dictionary<TSource, List<int>>(),
                                });
                                IEnumerable<TResult> indexingSelector((TSource element, IEnumerable<TResult> result) er)
                                {
                                    var (element, result) = er;
                                    if (!newSourceToStartingIndicies.TryGetValue(element, out var newStartingIndicies))
                                    {
                                        newStartingIndicies = new List<int>();
                                        newSourceToStartingIndicies.Add(element, newStartingIndicies);
                                    }
                                    newStartingIndicies.Add(iterativeResultsIndex);
                                    var resultCount = result.Count();
                                    iterativeResultsIndex += resultCount;
                                    if (!sourceToCount.ContainsKey(element))
                                        sourceToCount.Add(element, resultCount);
                                    if (result is INotifyCollectionChanged changingResult && !changingResultToSource.ContainsKey(changingResult))
                                    {
                                        changingResult.CollectionChanged += collectionChanged;
                                        changingResultToSource.Add(changingResult, element);
                                        sourceToChangingResult.Add(element, changingResult);
                                    }
                                    return er.result;
                                }

                                var results = e.NewItems.SelectMany(indexingSelector).ToList();
                                var count = results.Count;
                                if (count > 0)
                                {
                                    foreach (var startingIndiciesList in sourceToStartingIndicies.Values)
                                        for (int i = 0, ii = startingIndiciesList.Count; i < ii; ++i)
                                        {
                                            var startingIndex = startingIndiciesList[i];
                                            if (startingIndex >= resultsIndex)
                                                startingIndiciesList[i] = startingIndex + count;
                                        }
                                    rangeObservableCollection!.InsertRange(resultsIndex, results);
                                }
                                foreach (var kv in newSourceToStartingIndicies)
                                {
                                    var key = kv.Key;
                                    if (sourceToStartingIndicies.TryGetValue(key, out var startingIndicies))
                                    {
                                        startingIndicies.AddRange(kv.Value);
                                        startingIndicies.Sort();
                                    }
                                    else
                                        sourceToStartingIndicies.Add(key, kv.Value);
                                }
                            }
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                });

            var initializationCount = 0;

            IEnumerable<TResult> initializer((TSource element, IEnumerable<TResult> result) er)
            {
                var (element, result) = er;
                if (!sourceToStartingIndicies.TryGetValue(element, out var startingIndicies))
                {
                    startingIndicies = new List<int>();
                    sourceToStartingIndicies.Add(element, startingIndicies);
                }
                startingIndicies.Add(initializationCount);
                var resultCount = result.Count();
                initializationCount += resultCount;
                if (!sourceToCount.ContainsKey(element))
                    sourceToCount.Add(element, result.Count());
                if (result is INotifyCollectionChanged changingResult)
                {
                    changingResult.CollectionChanged += collectionChanged;
                    changingResultToSource.Add(changingResult, element);
                    sourceToChangingResult.Add(element, changingResult);
                }
                return result;
            }

            return synchronizableSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.GenericCollectionChanged += genericCollectionChanged;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TResult>(rangeActiveExpression.GetResults().SelectMany(initializer));
                return new ActiveEnumerable<TResult>(rangeObservableCollection, onDispose: () =>
                {
                    foreach (var changingResult in changingResultToSource.Keys)
                        changingResult.CollectionChanged -= collectionChanged;
                    rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                    rangeActiveExpression.GenericCollectionChanged -= genericCollectionChanged;
                });
            })!;
        }

        #endregion SelectMany

        #region Single

        /// <summary>
        /// Actively returns the only element of a sequence, and becomes faulted if there is not exactly one element in the sequence
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to return the single element of</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the single element of the input sequence</returns>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static IActiveValue<TSource> ActiveSingle<TSource>(this IEnumerable<TSource> source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyCollectionChanged changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<TSource>? setValue = null;
                Action<Exception?>? setOperationFault = null;

                void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        try
                        {
                            var value = source.Single();
                            setOperationFault!(null);
                            setValue!(value);
                        }
                        catch (Exception ex)
                        {
                            setOperationFault!(ex);
                            setValue!(default!);
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.CollectionChanged += collectionChanged;
                    try
                    {
                        return new ActiveValue<TSource>(source.Single(), out setValue, out setOperationFault, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    }
                    catch (Exception ex)
                    {
                        return new ActiveValue<TSource>(default!, out setValue, ex, out setOperationFault, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    }
                })!;
            }
            try
            {
                return new ActiveValue<TSource>(source.Single(), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<TSource>(default!, ex, elementFaultChangeNotifier);
            }
        }

        /// <summary>
        /// Actively returns the only element of a sequence that satisfies a specified condition, and becomes faulted if more than one such element exists
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to return the single element of</param>
        /// <param name="predicate">A function to test an element for a condition</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the single element of the input sequence that satisfies a condition</returns>
        public static IActiveValue<TSource> ActiveSingle<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate) =>
            ActiveSingle(source, predicate, null);

        /// <summary>
        /// Actively returns the only element of a sequence that satisfies a specified condition, and becomes faulted if more than one such element exists
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to return the single element of</param>
        /// <param name="predicate">A function to test an element for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the single element of the input sequence that satisfies a condition</returns>
        public static IActiveValue<TSource> ActiveSingle<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions? predicateOptions)
        {
            IActiveEnumerable<TSource> where;
            Action<TSource>? setValue = null;
            Action<Exception?>? setOperationFault = null;
            var none = false;
            var moreThanOne = false;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (none && where.Count > 0)
                {
                    setOperationFault!(null);
                    none = false;
                }
                else if (!none && where.Count == 0)
                {
                    setOperationFault!(ExceptionHelper.SequenceContainsNoElements);
                    none = true;
                    moreThanOne = false;
                }
                if (moreThanOne && where.Count <= 1)
                {
                    setOperationFault!(null);
                    moreThanOne = false;
                }
                else if (!moreThanOne && where.Count > 1)
                {
                    setOperationFault!(ExceptionHelper.SequenceContainsMoreThanOneElement);
                    none = false;
                    moreThanOne = true;
                }
                setValue!(where.Count == 1 ? where[0] : default);
            }

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.CollectionChanged += collectionChanged;

                Exception? operationFault = null;
                if (none = where.Count == 0)
                    operationFault = ExceptionHelper.SequenceContainsNoElements;
                else if (moreThanOne = where.Count > 1)
                    operationFault = ExceptionHelper.SequenceContainsMoreThanOneElement;
                return new ActiveValue<TSource>(operationFault is null ? where[0] : default, out setValue, operationFault, out setOperationFault, where, () =>
                {
                    where.CollectionChanged -= collectionChanged;
                    where.Dispose();
                });
            })!;
        }

        #endregion Single

        #region SingleOrDefault

        /// <summary>
        /// Actively returns the only element of a sequence, or a default value if the sequence is empty; becomes faulted if there is more than one element in the sequence
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to return the single element of</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the single element of the input sequence, or <c>default</c>(<typeparamref name="TSource"/>) if the sequence contains no elements</returns>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static IActiveValue<TSource> ActiveSingleOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyCollectionChanged changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<TSource>? setValue = null;
                Action<Exception?>? setOperationFault = null;

                void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        try
                        {
                            var value = source.SingleOrDefault();
                            setOperationFault!(null);
                            setValue!(value);
                        }
                        catch (Exception ex)
                        {
                            setOperationFault!(ex);
                            setValue!(default!);
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.CollectionChanged += collectionChanged;
                    try
                    {
                        return new ActiveValue<TSource>(source.SingleOrDefault(), out setValue, null, out setOperationFault, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    }
                    catch (Exception ex)
                    {
                        return new ActiveValue<TSource>(default!, out setValue, ex, out setOperationFault, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    }
                })!;
            }
            try
            {
                return new ActiveValue<TSource>(source.SingleOrDefault(), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<TSource>(default!, ex, elementFaultChangeNotifier);
            }
        }

        /// <summary>
        /// Actively returns the only element of a sequence that satisfies a specified condition or a default value if no such element exists; becomes faulted if more than one element satisfies the condition
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to return the single element of</param>
        /// <param name="predicate">A function to test an element for a condition</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the single element of the input sequence that satisfies the condition, or <c>default</c>(<typeparamref name="TSource"/>) if no such element is found</returns>
        public static IActiveValue<TSource> ActiveSingleOrDefault<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate) =>
            ActiveSingleOrDefault(source, predicate, null);

        /// <summary>
        /// Actively returns the only element of a sequence that satisfies a specified condition or a default value if no such element exists; becomes faulted if more than one element satisfies the condition
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to return the single element of</param>
        /// <param name="predicate">A function to test an element for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the single element of the input sequence that satisfies the condition, or <c>default</c>(<typeparamref name="TSource"/>) if no such element is found</returns>
        public static IActiveValue<TSource> ActiveSingleOrDefault<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions? predicateOptions)
        {
            IActiveEnumerable<TSource> where;
            Action<TSource>? setValue = null;
            Action<Exception?>? setOperationFault = null;
            var moreThanOne = false;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (moreThanOne && where.Count <= 1)
                {
                    setOperationFault!(null);
                    moreThanOne = false;
                }
                else if (!moreThanOne && where.Count > 1)
                {
                    setOperationFault!(ExceptionHelper.SequenceContainsMoreThanOneElement);
                    moreThanOne = true;
                }
                setValue!(where.Count == 1 ? where[0] : default);
            }

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.CollectionChanged += collectionChanged;

                var operationFault = (moreThanOne = where.Count > 1) ? ExceptionHelper.SequenceContainsMoreThanOneElement : null;
                return new ActiveValue<TSource>(!moreThanOne && where.Count == 1 ? where[0] : default, out setValue, operationFault, out setOperationFault, where, () =>
                {
                    where.CollectionChanged -= collectionChanged;
                    where.Dispose();
                });
            })!;
        }

        #endregion SingleOrDefault

        #region Sum

        /// <summary>
        /// Actively computes the sum of a sequence of values
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">A sequence of values to calculate the sum of</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the sum of the values in the sequence</returns>
        public static IActiveValue<TSource> ActiveSum<TSource>(this IEnumerable<TSource> source) =>
            ActiveSum(source, element => element);

        /// <summary>
        /// Actively computes the sum of the sequence of values that are obtained by invoking a transform function on each element of the input sequence
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the values being summed</typeparam>
        /// <param name="source">A sequence of values that are used to calculate a sum</param>
        /// <param name="selector">A transform function to apply to each element</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the sum of the projected values</returns>
        public static IActiveValue<TResult> ActiveSum<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector) =>
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
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        public static IActiveValue<TResult> ActiveSum<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions? selectorOptions)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var operations = new GenericOperations<TResult>();
            var synchronizedSource = source as ISynchronized;
            EnumerableRangeActiveExpression<TSource, TResult> rangeActiveExpression;
            ActiveValue<TResult>? activeValue = null;
            Action<TResult>? setValue = null;
            var resultsChanging = new Dictionary<TSource, (TResult result, int instances)>();

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var (result, instances) = resultsChanging[e.Element];
                    resultsChanging.Remove(e.Element);
                    setValue!(operations.Add(activeValue!.Value, operations.Subtract(e.Result, result).Repeat(instances).Aggregate(operations.Add)));
                });

            void elementResultChanging(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TResult> e) => synchronizedSource.SequentialExecute(() => resultsChanging.Add(e.Element, (e.Result, e.Count)));

            void genericCollectionChanged(object sender, INotifyGenericCollectionChangedEventArgs<(TSource element, TResult result)> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        try
                        {
                            setValue!(rangeActiveExpression.GetResults().Select(er => er.result).Aggregate(operations.Add));
                        }
                        catch (InvalidOperationException)
                        {
                            setValue!(default!);
                        }
                    }
                    else if (e.Action != NotifyCollectionChangedAction.Move)
                    {
                        var sum = activeValue!.Value;
                        if ((e.OldItems?.Count ?? 0) > 0)
                            sum = new TResult[] { sum }.Concat(e.OldItems.Select(er => er.result)).Aggregate(operations.Subtract);
                        if ((e.NewItems?.Count ?? 0) > 0)
                            sum = new TResult[] { sum }.Concat(e.NewItems.Select(er => er.result)).Aggregate(operations.Add);
                        setValue!(sum);
                    }
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.ElementResultChanging += elementResultChanging;
                rangeActiveExpression.GenericCollectionChanged += genericCollectionChanged;

                void dispose()
                {
                    rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                    rangeActiveExpression.ElementResultChanging -= elementResultChanging;
                    rangeActiveExpression.GenericCollectionChanged -= genericCollectionChanged;

                    rangeActiveExpression.Dispose();
                }

                try
                {
                    return activeValue = new ActiveValue<TResult>(rangeActiveExpression.GetResults().Select(er => er.result).Aggregate(operations.Add), out setValue, null, rangeActiveExpression, dispose);
                }
                catch (InvalidOperationException)
                {
                    return activeValue = new ActiveValue<TResult>(default!, out setValue, null, rangeActiveExpression, dispose);
                }
            })!;
        }

        #endregion Sum

        #region SwitchContext

        /// <summary>
        /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is kept consistent on the current thread's <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable"/> that implements <see cref="INotifyCollectionChanged"/> or <see cref="INotifyGenericCollectionChanged{T}"/>
        /// </summary>
        /// <param name="source">An <see cref="IEnumerable"/></param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is kept consistent with <paramref name="source"/> on the current thread's <see cref="SynchronizationContext"/></returns>
        public static IActiveEnumerable<object> SwitchContext(this IEnumerable source) =>
            SwitchContext(source, SynchronizationContext.Current);

        /// <summary>
        /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is kept consistent on a specified <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable"/> that implements <see cref="INotifyCollectionChanged"/> or <see cref="INotifyGenericCollectionChanged{T}"/>
        /// </summary>
        /// <param name="source">An <see cref="IEnumerable"/></param>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform consistency operations</param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is kept consistent with <paramref name="source"/> on <paramref name="synchronizationContext"/></returns>
        public static IActiveEnumerable<object> SwitchContext(this IEnumerable source, SynchronizationContext synchronizationContext)
        {
            SynchronizedRangeObservableCollection<object>? rangeObservableCollection = null;

            async void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                IReadOnlyList<object>? resetValues = null;
                if (e.Action == NotifyCollectionChangedAction.Reset)
                    resetValues = source.Cast<object>().ToImmutableArray();
                await rangeObservableCollection.SequentialExecuteAsync(() =>
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

            return (source as ISynchronized).SequentialExecute(() =>
            {
                var notifier = source as INotifyCollectionChanged;
                if (notifier is { })
                    notifier.CollectionChanged += collectionChanged;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<object>(synchronizationContext, source.Cast<object>());
                return new ActiveEnumerable<object>(rangeObservableCollection, source as INotifyElementFaultChanges, () =>
                {
                    if (notifier is { })
                        notifier.CollectionChanged -= collectionChanged;
                });
            })!;
        }

        /// <summary>
        /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is kept consistent on the current thread's <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable{T}"/> that implements <see cref="INotifyCollectionChanged"/> or <see cref="INotifyGenericCollectionChanged{T}"/>
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/></param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is kept consistent with <paramref name="source"/> on the current thread's <see cref="SynchronizationContext"/></returns>
        public static IActiveEnumerable<TSource> SwitchContext<TSource>(this IEnumerable<TSource> source) =>
            SwitchContext(source, SynchronizationContext.Current);

        /// <summary>
        /// Creates an <see cref="IActiveEnumerable{TElement}"/> that is kept consistent on a specified <see cref="SynchronizationContext"/> with a specified <see cref="IEnumerable{T}"/> that implements <see cref="INotifyCollectionChanged"/> or <see cref="INotifyGenericCollectionChanged{T}"/>
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/></param>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform consistency operations</param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> that is kept consistent with <paramref name="source"/> on <paramref name="synchronizationContext"/></returns>
        public static IActiveEnumerable<TSource> SwitchContext<TSource>(this IEnumerable<TSource> source, SynchronizationContext synchronizationContext)
        {
            SynchronizedRangeObservableCollection<TSource>? rangeObservableCollection = null;

            async void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                IReadOnlyList<TSource>? resetValues = null;
                if (e.Action == NotifyCollectionChangedAction.Reset)
                    resetValues = source.ToImmutableArray();
                await rangeObservableCollection.SequentialExecuteAsync(() =>
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

            async void genericCollectionChanged(object sender, INotifyGenericCollectionChangedEventArgs<TSource> e)
            {
                IReadOnlyList<TSource>? resetValues = null;
                if (e.Action == NotifyCollectionChangedAction.Reset)
                    resetValues = source.ToImmutableArray();
                await rangeObservableCollection.SequentialExecuteAsync(() =>
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            rangeObservableCollection!.InsertRange(e.NewStartingIndex, e.NewItems);
                            break;
                        case NotifyCollectionChangedAction.Move:
                            rangeObservableCollection!.MoveRange(e.OldStartingIndex, e.NewStartingIndex, e.OldItems.Count);
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            rangeObservableCollection!.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                            break;
                        case NotifyCollectionChangedAction.Replace:
                            rangeObservableCollection!.ReplaceRange(e.OldStartingIndex, e.OldItems.Count, e.NewItems);
                            break;
                        case NotifyCollectionChangedAction.Reset:
                            rangeObservableCollection!.Reset(resetValues!);
                            break;
                    }
                }).ConfigureAwait(false);
            }

            return (source as ISynchronized).SequentialExecute(() =>
            {
                var notifier = source as INotifyCollectionChanged;
                var genericNotifier = source as INotifyGenericCollectionChanged<TSource>;
                if (genericNotifier is { })
                    genericNotifier.GenericCollectionChanged += genericCollectionChanged;
                else if (notifier is { })
                    notifier.CollectionChanged += collectionChanged;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizationContext, source);
                return new ActiveEnumerable<TSource>(rangeObservableCollection, source as INotifyElementFaultChanges, () =>
                {
                    if (genericNotifier is { })
                        genericNotifier.GenericCollectionChanged -= genericCollectionChanged;
                    else if (notifier is { })
                        notifier.CollectionChanged -= collectionChanged;
                });
            })!;
        }

        #endregion SwitchContext

        #region ToActiveEnumerable

        /// <summary>
        /// Converts an <see cref="IEnumerable{T}"/> into an <see cref="IActiveEnumerable{TElement}"/>
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to convert</param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> equivalent to <paramref name="source"/> (and mutates with it so long as <paramref name="source"/> implements <see cref="INotifyCollectionChanged"/> or <see cref="INotifyGenericCollectionChanged{T}"/>)</returns>
        public static IActiveEnumerable<TSource> ToActiveEnumerable<TSource>(this IEnumerable<TSource> source)
        {
            if (source is IReadOnlyList<TSource> readOnlyList && source is ISynchronized)
                return new ActiveEnumerable<TSource>(readOnlyList);

            var changingSource = source as INotifyCollectionChanged;
            var synchronizedSource = source as ISynchronized;
            SynchronizedRangeObservableCollection<TSource>? rangeObservableCollection = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizedSource.SequentialExecute(() =>
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

            return synchronizedSource.SequentialExecute(() =>
            {
                if (changingSource is { })
                    changingSource.CollectionChanged += collectionChanged;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(source);
                return new ActiveEnumerable<TSource>(rangeObservableCollection, source as INotifyElementFaultChanges, () =>
                {
                    if (changingSource is { })
                        changingSource.CollectionChanged -= collectionChanged;
                });
            })!;
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
        public static ActiveDictionary<TKey, TValue> ToActiveDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector) =>
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
        public static ActiveDictionary<TKey, TValue> ToActiveDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector, IndexingStrategy indexingStategy) =>
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
        public static ActiveDictionary<TKey, TValue> ToActiveDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector, IEqualityComparer<TKey>? keyEqualityComparer) =>
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
        public static ActiveDictionary<TKey, TValue> ToActiveDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector, IComparer<TKey>? keyComparer) =>
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
        public static ActiveDictionary<TKey, TValue> ToActiveDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector, ActiveExpressionOptions? selectorOptions) =>
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
        public static ActiveDictionary<TKey, TValue> ToActiveDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector, ActiveExpressionOptions? selectorOptions, IndexingStrategy indexingStategy) =>
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
        public static ActiveDictionary<TKey, TValue> ToActiveDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector, ActiveExpressionOptions? selectorOptions, IEqualityComparer<TKey>? keyEqualityComparer) =>
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
        public static ActiveDictionary<TKey, TValue> ToActiveDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector, ActiveExpressionOptions? selectorOptions, IComparer<TKey>? keyComparer) =>
            ToActiveDictionary(source, selector, selectorOptions, IndexingStrategy.SelfBalancingBinarySearchTree, null, keyComparer);

        static ActiveDictionary<TKey, TValue> ToActiveDictionary<TSource, TKey, TValue>(IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector, ActiveExpressionOptions? selectorOptions, IndexingStrategy indexingStategy, IEqualityComparer<TKey>? keyEqualityComparer, IComparer<TKey>? keyComparer)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var synchronizedSource = source as ISynchronized;
            IDictionary<TKey, int> duplicateKeys;
            var isFaultedDuplicateKeys = false;
            var isFaultedNullKey = false;
            var nullKeys = 0;
            EnumerableRangeActiveExpression<TSource, KeyValuePair<TKey, TValue>> rangeActiveExpression;
            ISynchronizedObservableRangeDictionary<TKey, TValue> rangeObservableDictionary;
            Action<Exception?>? setOperationFault = null;

            void checkOperationFault()
            {
                if (nullKeys > 0 && !isFaultedNullKey)
                {
                    isFaultedNullKey = true;
                    setOperationFault!(ExceptionHelper.KeyNull);
                }
                else if (nullKeys == 0 && isFaultedNullKey)
                {
                    isFaultedNullKey = false;
                    setOperationFault!(null);
                }

                if (!isFaultedNullKey)
                {
                    if (duplicateKeys.Count > 0 && !isFaultedDuplicateKeys)
                    {
                        isFaultedDuplicateKeys = true;
                        setOperationFault!(ExceptionHelper.SameKeyAlreadyAdded);
                    }
                    else if (duplicateKeys.Count == 0 && isFaultedDuplicateKeys)
                    {
                        isFaultedDuplicateKeys = false;
                        setOperationFault!(null);
                    }
                }
            }

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, KeyValuePair<TKey, TValue>> e) =>
                synchronizedSource.SequentialExecute(() =>
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
                });

            void elementResultChanging(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, KeyValuePair<TKey, TValue>> e) =>
                synchronizedSource.SequentialExecute(() =>
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
                });

            void genericCollectionChanged(object sender, INotifyGenericCollectionChangedEventArgs<(TSource element, KeyValuePair<TKey, TValue> keyValuePair)> e) =>
                synchronizedSource.SequentialExecute(() =>
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
                            foreach (var (element, result) in e.OldItems)
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
                            foreach (var (element, result) in e.NewItems)
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
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                switch (indexingStategy)
                {
                    case IndexingStrategy.SelfBalancingBinarySearchTree:
                        duplicateKeys = keyComparer is null ? new SortedDictionary<TKey, int>() : new SortedDictionary<TKey, int>(keyComparer);
                        rangeObservableDictionary = keyComparer is null ? new SynchronizedObservableSortedDictionary<TKey, TValue>() : new SynchronizedObservableSortedDictionary<TKey, TValue>(keyComparer);
                        break;
                    default:
                        duplicateKeys = keyEqualityComparer is null ? new Dictionary<TKey, int>() : new Dictionary<TKey, int>(keyEqualityComparer);
                        rangeObservableDictionary = keyEqualityComparer is null ? new SynchronizedObservableDictionary<TKey, TValue>() : new SynchronizedObservableDictionary<TKey, TValue>(keyEqualityComparer);
                        break;
                }

                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.ElementResultChanging += elementResultChanging;
                rangeActiveExpression.GenericCollectionChanged += genericCollectionChanged;

                var resultsFaultsAndCounts = rangeActiveExpression.GetResultsFaultsAndCounts();
                nullKeys = resultsFaultsAndCounts.Count(rfc => rfc.result.Key is null);
                var distinctResultsFaultsAndCounts = resultsFaultsAndCounts.Where(rfc => rfc.result.Key is { }).GroupBy(rfc => rfc.result.Key).ToList();
                rangeObservableDictionary.AddRange(distinctResultsFaultsAndCounts.Select(g => g.First().result));
                foreach (var (key, duplicateCount) in distinctResultsFaultsAndCounts.Select(g => (key: g.Key, duplicateCount: g.Sum(rfc => rfc.count) - 1)).Where(kc => kc.duplicateCount > 0))
                    duplicateKeys.Add(key, duplicateCount);
                var activeDictionary = new ActiveDictionary<TKey, TValue>(rangeObservableDictionary, out setOperationFault, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                    rangeActiveExpression.ElementResultChanging -= elementResultChanging;
                    rangeActiveExpression.GenericCollectionChanged -= genericCollectionChanged;
                    rangeActiveExpression.Dispose();
                });
                checkOperationFault();

                return activeDictionary;
            })!;
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

            var synchronizedSource = source as ISynchronized;
            EnumerableRangeActiveExpression<TSource, bool> rangeActiveExpression;
            SynchronizedRangeObservableCollection<TSource>? rangeObservableCollection = null;

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, bool> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Result)
                        rangeObservableCollection!.AddRange(Enumerable.Range(0, e.Count).Select(i => e.Element));
                    else
                    {
                        var equalityComparer = EqualityComparer<TSource>.Default;
                        rangeObservableCollection!.RemoveAll(element => equalityComparer.Equals(element, e.Element));
                    }
                });

            void genericCollectionChanged(object sender, INotifyGenericCollectionChangedEventArgs<(TSource element, bool included)> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                        rangeObservableCollection!.Reset(rangeActiveExpression.GetResults().Where(er => er.result).Select(er => er.element));
                    else if (e.Action != NotifyCollectionChangedAction.Move)
                    {
                        if ((e.OldItems?.Count ?? 0) > 0)
                            rangeObservableCollection!.RemoveRange(e.OldItems.Where(er => er.included).Select(er => er.element));
                        if ((e.NewItems?.Count ?? 0) > 0)
                            rangeObservableCollection!.AddRange(e.NewItems.Where(er => er.included).Select(er => er.element));
                    }
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, predicate, predicateOptions);
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.GenericCollectionChanged += genericCollectionChanged;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(rangeActiveExpression.GetResults().Where(er => er.result).Select(er => er.element));
                return new ActiveEnumerable<TSource>(rangeObservableCollection, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                    rangeActiveExpression.GenericCollectionChanged -= genericCollectionChanged;

                    rangeActiveExpression.Dispose();
                });
            })!;
        }

        #endregion Where
    }
}
