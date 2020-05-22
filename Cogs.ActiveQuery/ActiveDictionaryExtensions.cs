using Cogs.ActiveExpressions;
using Cogs.Collections;
using Cogs.Collections.Synchronized;
using Cogs.Reflection;
using Cogs.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Cogs.ActiveQuery
{
    /// <summary>
    /// Provides a set of <c>static</c> (<c>Shared</c> in Visual Basic) methods for actively querying objects that implement <see cref="IReadOnlyDictionary{TKey, TValue}"/>
    /// </summary>
    public static class ActiveDictionaryExtensions
    {
        #region All

        /// <summary>
        /// Actively determines whether all key/value pairs of a dictionary satisfy a condition
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">A <see cref="IReadOnlyDictionary{TKey, TValue}"/> that contains the key/value pairs to apply the predicate to</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>true</c> when every key/value pair of the source dictionary passes the test in the specified predicate, or if the dictionary is empty; otherwise, <c>false</c></returns>
        public static IActiveValue<bool> ActiveAll<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate) =>
            ActiveAll(source, predicate, null);

        /// <summary>
        /// Actively determines whether all key/value pairs of a dictionary satisfy a condition
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">A <see cref="IReadOnlyDictionary{TKey, TValue}"/> that contains the key/value pairs to apply the predicate to</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>true</c> when every key/value pair of the source dictionary passes the test in the specified predicate, or if the dictionary is empty; otherwise, <c>false</c></returns>
        public static IActiveValue<bool> ActiveAll<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, ActiveExpressionOptions? predicateOptions)
        {
            var changeNotifyingSource = source as INotifyDictionaryChanged<TKey, TValue>;
            IActiveDictionary<TKey, TValue> where;
            ActiveValue<bool>? activeValue = null;

            void dictionaryChanged(object sender, EventArgs e) => activeValue!.Value = where.Count == source.Count;

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                activeValue = new ActiveValue<bool>(where.Count == source.Count, elementFaultChangeNotifier: where, onDispose: () =>
                {
                    where.DictionaryChanged -= dictionaryChanged;
                    where.Dispose();
                    if (changeNotifyingSource is { })
                        changeNotifyingSource.DictionaryChanged -= dictionaryChanged;
                });
                where.DictionaryChanged += dictionaryChanged;
                if (changeNotifyingSource is { })
                    changeNotifyingSource.DictionaryChanged += dictionaryChanged;
                return activeValue;
            });
        }

        #endregion All

        #region Any

        /// <summary>
        /// Actively determines whether a dictionary contains any key/value pairs
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to check for emptiness</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>true</c> if the source dictionary contains any key/value pairs; otherwise, <c>false</c></returns>
        public static IActiveValue<bool> ActiveAny<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changeNotifyingSource)
            {
                var synchronizedSource = source as ISynchronized;
                ActiveValue<bool>? activeValue = null;

                void sourceChanged(object sender, EventArgs e) => synchronizedSource.SequentialExecute(() => activeValue!.Value = source.Count > 0);

                return synchronizedSource.SequentialExecute(() =>
                {
                    activeValue = new ActiveValue<bool>(source.Any(), elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changeNotifyingSource.DictionaryChanged -= sourceChanged);
                    changeNotifyingSource.DictionaryChanged += sourceChanged;
                    return activeValue;
                });
            }
            try
            {
                return new ActiveValue<bool>(source.Any(), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<bool>(default, ex, elementFaultChangeNotifier);
            }
        }

        /// <summary>
        /// Actively determines whether any key/value pair in a dictionary satisfies a condition
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">A <see cref="IReadOnlyDictionary{TKey, TValue}"/> that contains the key/value pairs to apply the predicate to</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>true</c> if any key/value pairs in the source dictionary pass the test in the specified predicate; otherwise, <c>false</c></returns>
        public static IActiveValue<bool> ActiveAny<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate) =>
            ActiveAny(source, predicate, null);

        /// <summary>
        /// Actively determines whether any key/value pair in a dictionary satisfies a condition
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">A <see cref="IReadOnlyDictionary{TKey, TValue}"/> that contains the key/value pairs to apply the predicate to</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>true</c> if any key/value pairs in the source dictionary pass the test in the specified predicate; otherwise, <c>false</c></returns>
        public static IActiveValue<bool> ActiveAny<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, ActiveExpressionOptions? predicateOptions)
        {
            var changeNotifyingSource = source as INotifyDictionaryChanged<TKey, TValue>;
            IActiveDictionary<TKey, TValue> where;
            ActiveValue<bool>? activeValue = null;

            void dictionaryChanged(object sender, EventArgs e) => activeValue!.Value = where.Count > 0;

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                activeValue = new ActiveValue<bool>(where.Count > 0, elementFaultChangeNotifier: where, onDispose: () =>
                {
                    where.DictionaryChanged -= dictionaryChanged;
                    where.Dispose();
                });
                where.DictionaryChanged += dictionaryChanged;
                return activeValue;
            });
        }

        #endregion Any

        #region Average

        /// <summary>
        /// Actively computes the average of that values of a dictionary
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">A dictionary that values of which to calculate the average</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the average of the values</returns>
        public static IActiveValue<TValue> ActiveAverage<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
            ActiveAverage(source, (key, value) => value);

        /// <summary>
        /// Actively computes the average of a sequence of values that are obtained by invoking a transform function on each key/value pair of the input dictionary
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the values being averaged</typeparam>
        /// <param name="source">A dictionary that values of which to calculate the average</param>
        /// <param name="selector">A transform function to apply to each key/value pair</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the average of the values</returns>
        public static IActiveValue<TResult> ActiveAverage<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector) =>
            ActiveAverage(source, selector, null);

        /// <summary>
        /// Actively computes the average of a sequence of values that are obtained by invoking a transform function on each key/value pair of the input dictionary
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the values being averaged</typeparam>
        /// <param name="source">A dictionary that values of which to calculate the average</param>
        /// <param name="selector">A transform function to apply to each key/value pair</param>
        /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the average of the values</returns>
        public static IActiveValue<TResult> ActiveAverage<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector, ActiveExpressionOptions? selectorOptions)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var convertCount = CountConversion.GetConverter(typeof(TResult));
            var operations = new GenericOperations<TResult>();
            var synchronizedSource = source as ISynchronized;
            IActiveValue<TResult> sum;
            ActiveValue<TResult>? activeValue = null;

            void propertyChanged(object sender, PropertyChangedEventArgs e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.PropertyName == nameof(ActiveValue<TResult>.Value))
                    {
                        var currentCount = source.Count;
                        if (currentCount == 0)
                        {
                            activeValue!.Value = default;
                            activeValue!.OperationFault = ExceptionHelper.SequenceContainsNoElements;
                        }
                        else
                        {
                            activeValue!.OperationFault = null;
                            activeValue!.Value = operations.Divide(sum.Value, (TResult)convertCount(currentCount));
                        }
                    }
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                sum = ActiveSum(source, selector, selectorOptions);
                var currentCount = source.Count;
                activeValue = new ActiveValue<TResult>(currentCount > 0 ? operations.Divide(sum.Value, (TResult)convertCount(currentCount)) : default, currentCount == 0 ? ExceptionHelper.SequenceContainsNoElements : null, sum, () =>
                {
                    sum.PropertyChanged -= propertyChanged;
                    sum.Dispose();
                });
                sum.PropertyChanged += propertyChanged;
                return activeValue;
            });
        }

        #endregion Average

        #region Count

        /// <summary>
        /// Actively determines the number of key/value pairs in a dictionary
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">A <see cref="IReadOnlyDictionary{TKey, TValue}"/></param>
        /// <returns>An active value the value of which is <c>true</c> if the source sequence contains any elements; otherwise, <c>false</c></returns>
        public static IActiveValue<int> ActiveCount<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changeNotifyingSource)
            {
                var synchronizedSource = source as ISynchronized;
                ActiveValue<int>? activeValue = null;

                void sourceDictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) => synchronizedSource.SequentialExecute(() => activeValue!.Value = source.Count);

                return synchronizedSource.SequentialExecute(() =>
                {
                    activeValue = new ActiveValue<int>(source.Count, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changeNotifyingSource.DictionaryChanged -= sourceDictionaryChanged);
                    changeNotifyingSource.DictionaryChanged += sourceDictionaryChanged;
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
        /// Actively counts whether all key/value pairs of a dictionary that satisfy a condition
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">A <see cref="IReadOnlyDictionary{TKey, TValue}"/> that contains the key/value pairs to apply the predicate to</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <returns>An active value the value of which is the number of key/value pairs of the source dictionary that the test in the specified predicate</returns>
        public static IActiveValue<int> ActiveCount<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate) =>
            ActiveCount(source, predicate, null);

        /// <summary>
        /// Actively counts whether all key/value pairs of a dictionary that satisfy a condition
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">A <see cref="IReadOnlyDictionary{TKey, TValue}"/> that contains the key/value pairs to apply the predicate to</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <returns>An active value the value of which is the number of key/value pairs of the source dictionary that the test in the specified predicate</returns>
        public static IActiveValue<int> ActiveCount<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, ActiveExpressionOptions? predicateOptions)
        {
            var changeNotifyingSource = source as INotifyDictionaryChanged<TKey, TValue>;
            IActiveDictionary<TKey, TValue> where;
            ActiveValue<int>? activeValue = null;

            void dictionaryChanged(object sender, EventArgs e) => activeValue!.Value = where.Count;

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                activeValue = new ActiveValue<int>(where.Count, elementFaultChangeNotifier: where, onDispose: () =>
                {
                    where.DictionaryChanged -= dictionaryChanged;
                    where.Dispose();
                    if (changeNotifyingSource is { })
                        changeNotifyingSource.DictionaryChanged -= dictionaryChanged;
                });
                where.DictionaryChanged += dictionaryChanged;
                if (changeNotifyingSource is { })
                    changeNotifyingSource.DictionaryChanged += dictionaryChanged;
                return activeValue;
            });
        }

        #endregion Count

        #region First

        /// <summary>
        /// Actively returns the first key/value pair in a dictionary using the <see cref="IComparer{T}"/> defined by the dictionary (or the default comparer for <typeparamref name="TKey"/>)
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the first key/value pair from</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the first key/value pair in the dictionary</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveFirst<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
            ActiveFirst(source, source.GetKeyComparer() ?? Comparer<TKey>.Default);

        /// <summary>
        /// Actively returns the first key/value pair in a dictionary using the specified comparer
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the first key/value pair from</param>
        /// <param name="keyComparer">A comparer to compare keys in <paramref name="source"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the first key/value pair in the dictionary</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveFirst<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, IComparer<TKey> keyComparer)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                ActiveValue<KeyValuePair<TKey, TValue>>? activeValue = null;

                void dispose() => changingSource.DictionaryChanged -= sourceChanged;

                void sourceChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        if (e.Action == NotifyDictionaryChangedAction.Reset)
                        {
                            try
                            {
                                activeValue!.OperationFault = null;
                                activeValue!.Value = source.OrderBy(kv => kv.Key, keyComparer).First();
                            }
                            catch (Exception ex)
                            {
                                activeValue!.OperationFault = ex;
                                activeValue!.Value = default;
                            }
                        }
                        else
                        {
                            if (e.OldItems?.Any(kv => keyComparer.Compare(kv.Key, activeValue!.Value.Key) == 0) ?? false)
                            {
                                try
                                {
                                    activeValue!.Value = source.OrderBy(kv => kv.Key, keyComparer).First();
                                }
                                catch (Exception ex)
                                {
                                    activeValue!.OperationFault = ex;
                                    activeValue!.Value = default;
                                }
                            }
                            if ((e.NewItems?.Count ?? 0) > 0)
                            {
                                var firstKv = e.NewItems.OrderBy(kv => kv.Key, keyComparer).First();
                                if (activeValue!.OperationFault is { } || keyComparer.Compare(firstKv.Key, activeValue.Value.Key) < 0)
                                {
                                    activeValue!.OperationFault = null;
                                    activeValue!.Value = firstKv;
                                }
                            }
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    try
                    {
                        activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderBy(kv => kv.Key, keyComparer).First(), elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                        changingSource.DictionaryChanged += sourceChanged;
                        return activeValue;
                    }
                    catch (Exception ex)
                    {
                        activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(default, ex, elementFaultChangeNotifier, dispose);
                        changingSource.DictionaryChanged += sourceChanged;
                        return activeValue;
                    }
                });
            }
            try
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderBy(kv => kv.Key, keyComparer).First(), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(default, ex, elementFaultChangeNotifier);
            }
        }

        /// <summary>
        /// Actively returns the first key/value pair in a dictionary that satisfies a specified condition using the <see cref="IComparer{T}"/> defined by the dictionary (or the default comparer for <typeparamref name="TKey"/>)
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the first key/value pair from</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the first key/value pair in the dictionary that passes the test in the predicate function</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveFirst<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate) =>
            ActiveFirst(source, predicate, null, source.GetKeyComparer() ?? Comparer<TKey>.Default);

        /// <summary>
        /// Actively returns the first key/value pair in a dictionary that satisfies a specified condition using the specified comparer
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the first key/value pair from</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <param name="keyComparer">A comparer to compare keys in <paramref name="source"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the first key/value pair in the dictionary that passes the test in the predicate function</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveFirst<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, IComparer<TKey> keyComparer) =>
            ActiveFirst(source, predicate, null, keyComparer);

        /// <summary>
        /// Actively returns the first key/value pair in a dictionary that satisfies a specified condition using the <see cref="IComparer{T}"/> defined by the dictionary (or the default comparer for <typeparamref name="TKey"/>)
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the first key/value pair from</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the first key/value pair in the dictionary that passes the test in the predicate function</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveFirst<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, ActiveExpressionOptions? predicateOptions) =>
            ActiveFirst(source, predicate, predicateOptions, source.GetKeyComparer() ?? Comparer<TKey>.Default);

        /// <summary>
        /// Actively returns the first key/value pair in a dictionary that satisfies a specified condition using the specified comparer
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the first key/value pair from</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <param name="keyComparer">A comparer to compare keys in <paramref name="source"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the first key/value pair in the dictionary that passes the test in the predicate function</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveFirst<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, ActiveExpressionOptions? predicateOptions, IComparer<TKey> keyComparer)
        {
            IActiveDictionary<TKey, TValue> where;
            ActiveValue<KeyValuePair<TKey, TValue>>? activeValue = null;
            var none = false;

            void dictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e)
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
                activeValue!.Value = where.OrderBy(kv => kv.Key, keyComparer).FirstOrDefault();
            }

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                none = where.Count == 0;
                activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(!none ? where.OrderBy(kv => kv.Key, keyComparer).First() : default, none ? ExceptionHelper.SequenceContainsNoElements : null, where, () =>
                {
                    where.DictionaryChanged -= dictionaryChanged;
                    where.Dispose();
                });
                where.DictionaryChanged += dictionaryChanged;
                return activeValue;
            });
        }

        #endregion First

        #region FirstOrDefault

        /// <summary>
        /// Actively returns the first key/value pair in a dictionary (or a default key/value pair if the dictionary does not contain any) using the <see cref="IComparer{T}"/> defined by the dictionary (or the default comparer for <typeparamref name="TKey"/>)
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the first key/value pair from</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the first key/value pair in the dictionary</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveFirstOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
            ActiveFirstOrDefault(source, source.GetKeyComparer() ?? Comparer<TKey>.Default);

        /// <summary>
        /// Actively returns the first key/value pair in a dictionary (or a default key/value pair if the dictionary does not contain any) using the specified comparer
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the first key/value pair from</param>
        /// <param name="keyComparer">A comparer to compare keys in <paramref name="source"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the first key/value pair in the dictionary</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveFirstOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, IComparer<TKey> keyComparer)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                ActiveValue<KeyValuePair<TKey, TValue>>? activeValue = null;
                var defaulted = false;

                void dispose() => changingSource.DictionaryChanged += sourceChanged;

                void sourceChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        if (e.Action == NotifyDictionaryChangedAction.Reset)
                        {
                            try
                            {
                                defaulted = false;
                                activeValue!.Value = source.OrderBy(kv => kv.Key, keyComparer).First();
                            }
                            catch
                            {
                                defaulted = true;
                                activeValue!.Value = default;
                            }
                        }
                        else
                        {
                            if (e.OldItems?.Any(kv => keyComparer.Compare(kv.Key, activeValue!.Value.Key) == 0) ?? false)
                            {
                                try
                                {
                                    activeValue!.Value = source.OrderBy(kv => kv.Key, keyComparer).First();
                                }
                                catch
                                {
                                    defaulted = true;
                                    activeValue!.Value = default;
                                }
                            }
                            if ((e.NewItems?.Count ?? 0) > 0)
                            {
                                var firstKv = e.NewItems.OrderBy(kv => kv.Key, keyComparer).First();
                                if (defaulted || keyComparer.Compare(firstKv.Key, activeValue!.Value.Key) < 0)
                                    activeValue!.Value = firstKv;
                            }
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    try
                    {
                        activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderBy(kv => kv.Key, keyComparer).First(), elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                        changingSource.DictionaryChanged += sourceChanged;
                        return activeValue;
                    }
                    catch
                    {
                        defaulted = true;
                        activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(default, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                        changingSource.DictionaryChanged += sourceChanged;
                        return activeValue;
                    }
                });
            }
            return new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderBy(kv => kv.Key, keyComparer).FirstOrDefault(), elementFaultChangeNotifier: elementFaultChangeNotifier);
        }

        /// <summary>
        /// Actively returns the first key/value pair in a dictionary that satisfies a specified condition (or a default key/value pair if the dictionary does not contain any) using the <see cref="IComparer{T}"/> defined by the dictionary (or the default comparer for <typeparamref name="TKey"/>)
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the first key/value pair from</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the first key/value pair in the dictionary that passes the test in the predicate function</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveFirstOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate) =>
            ActiveFirstOrDefault(source, predicate, null, source.GetKeyComparer() ?? Comparer<TKey>.Default);

        /// <summary>
        /// Actively returns the first key/value pair in a dictionary that satisfies a specified condition (or a default key/value pair if the dictionary does not contain any) using the specified comparer
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the first key/value pair from</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <param name="keyComparer">A comparer to compare keys in <paramref name="source"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the first key/value pair in the dictionary that passes the test in the predicate function</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveFirstOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, IComparer<TKey> keyComparer) =>
            ActiveFirstOrDefault(source, predicate, null, keyComparer);

        /// <summary>
        /// Actively returns the first key/value pair in a dictionary that satisfies a specified condition (or a default key/value pair if the dictionary does not contain any) using the <see cref="IComparer{T}"/> defined by the dictionary (or the default comparer for <typeparamref name="TKey"/>)
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the first key/value pair from</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the first key/value pair in the dictionary that passes the test in the predicate function</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveFirstOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, ActiveExpressionOptions? predicateOptions) =>
            ActiveFirstOrDefault(source, predicate, predicateOptions, source.GetKeyComparer() ?? Comparer<TKey>.Default);

        /// <summary>
        /// Actively returns the first key/value pair in a dictionary that satisfies a specified condition (or a default key/value pair if the dictionary does not contain any) using the specified comparer
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the first key/value pair from</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <param name="keyComparer">A comparer to compare keys in <paramref name="source"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the first key/value pair in the dictionary that passes the test in the predicate function</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveFirstOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, ActiveExpressionOptions? predicateOptions, IComparer<TKey> keyComparer)
        {
            IActiveDictionary<TKey, TValue> where;
            ActiveValue<KeyValuePair<TKey, TValue>>? activeValue = null;

            void dictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) => activeValue!.Value = where.OrderBy(kv => kv.Key, keyComparer).FirstOrDefault();

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(where.OrderBy(kv => kv.Key, keyComparer).FirstOrDefault(), elementFaultChangeNotifier: where, onDispose: () =>
                {
                    where.DictionaryChanged -= dictionaryChanged;
                    where.Dispose();
                });
                where.DictionaryChanged += dictionaryChanged;
                return activeValue;
            });
        }

        #endregion FirstOrDefault

        #region Last

        /// <summary>
        /// Actively returns the last key/value pair in a dictionary using the <see cref="IComparer{T}"/> defined by the dictionary (or the default comparer for <typeparamref name="TKey"/>)
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the last key/value pair from</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the last key/value pair in the dictionary</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveLast<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
            ActiveLast(source, source.GetKeyComparer() ?? Comparer<TKey>.Default);

        /// <summary>
        /// Actively returns the last key/value pair in a dictionary using the specified comparer
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the last key/value pair from</param>
        /// <param name="keyComparer">A comparer to compare keys in <paramref name="source"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the last key/value pair in the dictionary</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveLast<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, IComparer<TKey> keyComparer)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                ActiveValue<KeyValuePair<TKey, TValue>>? activeValue = null;

                void dispose() => changingSource.DictionaryChanged += sourceChanged;

                void sourceChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        if (e.Action == NotifyDictionaryChangedAction.Reset)
                        {
                            try
                            {
                                activeValue!.OperationFault = null;
                                activeValue!.Value = source.OrderByDescending(kv => kv.Key, keyComparer).First();
                            }
                            catch (Exception ex)
                            {
                                activeValue!.OperationFault = ex;
                                activeValue!.Value = default;
                            }
                        }
                        else
                        {
                            if (e.OldItems?.Any(kv => keyComparer.Compare(kv.Key, activeValue!.Value.Key) == 0) ?? false)
                            {
                                try
                                {
                                    activeValue!.Value = source.OrderByDescending(kv => kv.Key, keyComparer).First();
                                }
                                catch (Exception ex)
                                {
                                    activeValue!.OperationFault = ex;
                                }
                            }
                            if ((e.NewItems?.Count ?? 0) > 0)
                            {
                                var lastKv = e.NewItems.OrderByDescending(kv => kv.Key, keyComparer).First();
                                if (activeValue!.OperationFault is { } || keyComparer.Compare(lastKv.Key, activeValue.Value.Key) > 0)
                                {
                                    activeValue!.OperationFault = null;
                                    activeValue!.Value = lastKv;
                                }
                            }
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    try
                    {
                        activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderByDescending(kv => kv.Key, keyComparer).First(), elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                        changingSource.DictionaryChanged += sourceChanged;
                        return activeValue;
                    }
                    catch (Exception ex)
                    {
                        activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(default, ex, elementFaultChangeNotifier, dispose);
                        changingSource.DictionaryChanged += sourceChanged;
                        return activeValue;
                    }
                });
            }
            try
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderByDescending(kv => kv.Key, keyComparer).First(), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(default, ex, elementFaultChangeNotifier);
            }
        }

        /// <summary>
        /// Actively returns the last key/value pair in a dictionary that satisfies a specified condition using the <see cref="IComparer{T}"/> defined by the dictionary (or the default comparer for <typeparamref name="TKey"/>)
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the last key/value pair from</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the last key/value pair in the dictionary that passes the test in the predicate function</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveLast<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate) =>
            ActiveLast(source, predicate, null, source.GetKeyComparer() ?? Comparer<TKey>.Default);

        /// <summary>
        /// Actively returns the last key/value pair in a dictionary that satisfies a specified condition using the specified comparer
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the last key/value pair from</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <param name="keyComparer">A comparer to compare keys in <paramref name="source"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the last key/value pair in the dictionary that passes the test in the predicate function</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveLast<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, IComparer<TKey> keyComparer) =>
            ActiveLast(source, predicate, null, keyComparer);

        /// <summary>
        /// Actively returns the last key/value pair in a dictionary that satisfies a specified condition using the <see cref="IComparer{T}"/> defined by the dictionary (or the default comparer for <typeparamref name="TKey"/>)
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the last key/value pair from</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the last key/value pair in the dictionary that passes the test in the predicate function</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveLast<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, ActiveExpressionOptions? predicateOptions) =>
            ActiveLast(source, predicate, predicateOptions, source.GetKeyComparer() ?? Comparer<TKey>.Default);

        /// <summary>
        /// Actively returns the last key/value pair in a dictionary that satisfies a specified condition using the specified comparer
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the last key/value pair from</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <param name="keyComparer">A comparer to compare keys in <paramref name="source"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the last key/value pair in the dictionary that passes the test in the predicate function</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveLast<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, ActiveExpressionOptions? predicateOptions, IComparer<TKey> keyComparer)
        {
            IActiveDictionary<TKey, TValue> where;
            ActiveValue<KeyValuePair<TKey, TValue>>? activeValue = null;
            var none = false;

            void dictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e)
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
                activeValue!.Value = where.OrderByDescending(kv => kv.Key, keyComparer).FirstOrDefault();
            }

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                none = where.Count == 0;
                activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(!none ? source.OrderByDescending(kv => kv.Key, keyComparer).First() : default, none ? ExceptionHelper.SequenceContainsNoElements : null, where, () =>
                {
                    where.DictionaryChanged -= dictionaryChanged;
                    where.Dispose();
                });
                where.DictionaryChanged += dictionaryChanged;
                return activeValue;
            });
        }

        #endregion Last

        #region LastOrDefault

        /// <summary>
        /// Actively returns the last key/value pair in a dictionary (or a default key/value pair if the dictionary does not contain any) using the <see cref="IComparer{T}"/> defined by the dictionary (or the default comparer for <typeparamref name="TKey"/>)
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the last key/value pair from</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the last key/value pair in the dictionary</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveLastOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
            ActiveLastOrDefault(source, source.GetKeyComparer() ?? Comparer<TKey>.Default);

        /// <summary>
        /// Actively returns the last key/value pair in a dictionary (or a default key/value pair if the dictionary does not contain any) using the specified comparer
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the last key/value pair from</param>
        /// <param name="keyComparer">A comparer to compare keys in <paramref name="source"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the last key/value pair in the dictionary</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveLastOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, IComparer<TKey> keyComparer)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                ActiveValue<KeyValuePair<TKey, TValue>>? activeValue = null;
                var defaulted = false;

                void dispose() => changingSource.DictionaryChanged += sourceChanged;

                void sourceChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        if (e.Action == NotifyDictionaryChangedAction.Reset)
                        {
                            try
                            {
                                defaulted = false;
                                activeValue!.Value = source.OrderByDescending(kv => kv.Key, keyComparer).First();
                            }
                            catch
                            {
                                defaulted = true;
                                activeValue!.Value = default;
                            }
                        }
                        else
                        {
                            if (e.OldItems?.Any(kv => keyComparer.Compare(kv.Key, activeValue!.Value.Key) == 0) ?? false)
                            {
                                try
                                {
                                    activeValue!.Value = source.OrderByDescending(kv => kv.Key, keyComparer).First();
                                }
                                catch
                                {
                                    defaulted = true;
                                    activeValue!.Value = default;
                                }
                            }
                            if ((e.NewItems?.Count ?? 0) > 0)
                            {
                                var firstKv = e.NewItems.OrderByDescending(kv => kv.Key, keyComparer).First();
                                if (defaulted || keyComparer.Compare(firstKv.Key, activeValue!.Value.Key) > 0)
                                {
                                    defaulted = false;
                                    activeValue!.Value = firstKv;
                                }
                            }
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    try
                    {
                        activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderByDescending(kv => kv.Key, keyComparer).First(), elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                        changingSource.DictionaryChanged += sourceChanged;
                        return activeValue;
                    }
                    catch
                    {
                        defaulted = true;
                        activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(default, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                        changingSource.DictionaryChanged += sourceChanged;
                        return activeValue;
                    }
                });
            }
            return new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderByDescending(kv => kv.Key, keyComparer).FirstOrDefault(), elementFaultChangeNotifier: elementFaultChangeNotifier);
        }

        /// <summary>
        /// Actively returns the last key/value pair in a dictionary that satisfies a specified condition (or a default key/value pair if the dictionary does not contain any) using the <see cref="IComparer{T}"/> defined by the dictionary (or the default comparer for <typeparamref name="TKey"/>)
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the last key/value pair from</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the last key/value pair in the dictionary that passes the test in the predicate function</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveLastOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate) =>
            ActiveLastOrDefault(source, predicate, null, source.GetKeyComparer() ?? Comparer<TKey>.Default);

        /// <summary>
        /// Actively returns the last key/value pair in a dictionary that satisfies a specified condition (or a default key/value pair if the dictionary does not contain any) using the specified comparer
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the last key/value pair from</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <param name="keyComparer">A comparer to compare keys in <paramref name="source"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the last key/value pair in the dictionary that passes the test in the predicate function</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveLastOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, IComparer<TKey> keyComparer) =>
            ActiveLastOrDefault(source, predicate, null, keyComparer);

        /// <summary>
        /// Actively returns the last key/value pair in a dictionary that satisfies a specified condition (or a default key/value pair if the dictionary does not contain any) using the <see cref="IComparer{T}"/> defined by the dictionary (or the default comparer for <typeparamref name="TKey"/>)
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the last key/value pair from</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the last key/value pair in the dictionary that passes the test in the predicate function</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveLastOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, ActiveExpressionOptions? predicateOptions) =>
            ActiveLastOrDefault(source, predicate, predicateOptions, source.GetKeyComparer() ?? Comparer<TKey>.Default);

        /// <summary>
        /// Actively returns the last key/value pair in a dictionary that satisfies a specified condition (or a default key/value pair if the dictionary does not contain any) using the specified comparer
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the last key/value pair from</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <param name="keyComparer">A comparer to compare keys in <paramref name="source"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the last key/value pair in the dictionary that passes the test in the predicate function</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveLastOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, ActiveExpressionOptions? predicateOptions, IComparer<TKey> keyComparer)
        {
            IActiveDictionary<TKey, TValue> where;
            ActiveValue<KeyValuePair<TKey, TValue>>? activeValue = null;

            void dictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) => activeValue!.Value = where.OrderByDescending(kv => kv.Key, keyComparer).FirstOrDefault();

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(where.Count > 0 ? source.OrderByDescending(kv => kv.Key, keyComparer).First() : default, elementFaultChangeNotifier: where, onDispose: () =>
                {
                    where.DictionaryChanged -= dictionaryChanged;
                    where.Dispose();
                });
                where.DictionaryChanged += dictionaryChanged;
                return activeValue;
            });
        }

        #endregion LastOrDefault

        #region Max

        /// <summary>
        /// Actively returns the maximum value in a dictionary
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">A dictionary of key/value pairs to determine the maximum value of</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the maximum value in the dictionary</returns>
        public static IActiveValue<TValue> ActiveMax<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
            ActiveMax(source, (key, value) => value);

        /// <summary>
        /// Actively invokes a transform function on each key/value pair of a dictionary and returns the maximum value
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the maximum value</typeparam>
        /// <param name="source">A dictionary of key/value pairs to determine the maximum value of</param>
        /// <param name="selector">A transform function to apply to each key/value pair</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the maximum value in the dictionary</returns>
        public static IActiveValue<TResult> ActiveMax<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector) =>
            ActiveMax(source, selector, null);

        /// <summary>
        /// Actively invokes a transform function on each key/value pair of a dictionary and returns the maximum value
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the maximum value</typeparam>
        /// <param name="source">A dictionary of key/value pairs to determine the maximum value of</param>
        /// <param name="selector">A transform function to apply to each key/value pair</param>
        /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the maximum value in the dictionary</returns>
        public static IActiveValue<TResult> ActiveMax<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector, ActiveExpressionOptions? selectorOptions)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var comparer = Comparer<TResult>.Default;
            var synchronizedSource = source as ISynchronized;
            ReadOnlyDictionaryRangeActiveExpression<TKey, TValue, TResult> rangeActiveExpression;
            ActiveValue<TResult>? activeValue = null;

            void dispose()
            {
                rangeActiveExpression.DictionaryChanged -= rangeActiveExpressionChanged;
                rangeActiveExpression.ValueResultChanged -= valueResultChanged;
                rangeActiveExpression.Dispose();
            }

            void rangeActiveExpressionChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Action == NotifyDictionaryChangedAction.Reset)
                    {
                        try
                        {
                            activeValue!.OperationFault = null;
                            activeValue!.Value = rangeActiveExpression.GetResults().Max(kr => kr.result);
                        }
                        catch (Exception ex)
                        {
                            activeValue!.OperationFault = ex;
                            activeValue!.Value = default;
                        }
                    }
                    else
                    {
                        var activeValueValue = activeValue!.Value;
                        if ((e.OldItems?.Count ?? 0) > 0)
                        {
                            var removedMax = e.OldItems.Max(kv => kv.Value);
                            if ((activeValueValue is null ? -1 : comparer.Compare(activeValueValue, removedMax)) == 0)
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
                            var addedMax = e.NewItems.Max(kv => kv.Value);
                            if (activeValue!.OperationFault is { } || (activeValueValue is null ? -1 : comparer.Compare(activeValueValue, addedMax)) < 0)
                            {
                                activeValue!.OperationFault = null;
                                activeValue!.Value = addedMax;
                            }
                        }
                    }
                });

            void valueResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var activeValueValue = activeValue!.Value;
                    var comparison = 0;
                    if (activeValueValue is { } && e.Result is { })
                        comparison = comparer.Compare(activeValueValue, e.Result);
                    else if (activeValueValue is null)
                        comparison = -1;
                    else if (e.Result is null)
                        comparison = 1;
                    if (comparison < 0)
                        activeValue!.Value = e.Result;
                    else if (comparison > 0)
                        activeValue!.Value = rangeActiveExpression.GetResultsUnderLock().Max(er => er.result);
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                try
                {
                    activeValue = new ActiveValue<TResult>(rangeActiveExpression.GetResults().Max(kr => kr.result), null, rangeActiveExpression, dispose);
                    rangeActiveExpression.DictionaryChanged += rangeActiveExpressionChanged;
                    rangeActiveExpression.ValueResultChanged += valueResultChanged;
                    return activeValue;
                }
                catch (Exception ex)
                {
                    activeValue = new ActiveValue<TResult>(default, ex, rangeActiveExpression, dispose);
                    rangeActiveExpression.DictionaryChanged += rangeActiveExpressionChanged;
                    rangeActiveExpression.ValueResultChanged += valueResultChanged;
                    return activeValue;
                }
            });
        }

        #endregion Max

        #region Min

        /// <summary>
        /// Actively returns the minimum value in a dictionary
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">A dictionary of key/value pairs to determine the maximum value of</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the minimum value in the dictionary</returns>
        public static IActiveValue<TValue> ActiveMin<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
            ActiveMin(source, (key, value) => value);

        /// <summary>
        /// Actively invokes a transform function on each key/value pair of a dictionary and returns the minimum value
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the minimum value</typeparam>
        /// <param name="source">A dictionary of key/value pairs to determine the maximum value of</param>
        /// <param name="selector">A transform function to apply to each key/value pair</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the minimum value in the dictionary</returns>
        public static IActiveValue<TResult> ActiveMin<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector) =>
            ActiveMin(source, selector, null);

        /// <summary>
        /// Actively invokes a transform function on each key/value pair of a dictionary and returns the minimum value
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the minimum value</typeparam>
        /// <param name="source">A dictionary of key/value pairs to determine the maximum value of</param>
        /// <param name="selector">A transform function to apply to each key/value pair</param>
        /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the minimum value in the dictionary</returns>
        public static IActiveValue<TResult> ActiveMin<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector, ActiveExpressionOptions? selectorOptions)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var comparer = Comparer<TResult>.Default;
            var synchronizedSource = source as ISynchronized;
            ReadOnlyDictionaryRangeActiveExpression<TKey, TValue, TResult> rangeActiveExpression;
            ActiveValue<TResult>? activeValue = null;

            void dispose()
            {
                rangeActiveExpression.DictionaryChanged -= rangeActiveExpressionChanged;
                rangeActiveExpression.ValueResultChanged -= valueResultChanged;
                rangeActiveExpression.Dispose();
            }

            void rangeActiveExpressionChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Action == NotifyDictionaryChangedAction.Reset)
                    {
                        try
                        {
                            activeValue!.OperationFault = null;
                            activeValue!.Value = rangeActiveExpression.GetResults().Select(kr => kr.result).Min();
                        }
                        catch (Exception ex)
                        {
                            activeValue!.OperationFault = ex;
                            activeValue!.Value = default;
                        }
                    }
                    else
                    {
                        var activeValueValue = activeValue!.Value;
                        if ((e.OldItems?.Count ?? 0) > 0)
                        {
                            var removedMin = e.OldItems.Select(kv => kv.Value).Min();
                            if ((activeValueValue is null ? -1 : comparer.Compare(activeValueValue, removedMin)) == 0)
                            {
                                try
                                {
                                    var value = rangeActiveExpression.GetResultsUnderLock().Select(er => er.result).Min();
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
                            var addedMin = e.NewItems.Select(kv => kv.Value).Min();
                            if (activeValue!.OperationFault is { } || (activeValueValue is null ? -1 : comparer.Compare(activeValueValue, addedMin)) > 0)
                            {
                                activeValue!.OperationFault = null;
                                activeValue!.Value = addedMin;
                            }
                        }
                    }
                });

            void valueResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var activeValueValue = activeValue!.Value;
                    var comparison = 0;
                    if (activeValueValue is { } && e.Result is { })
                        comparison = comparer.Compare(activeValueValue, e.Result);
                    else if (activeValueValue is null)
                        comparison = -1;
                    else if (e.Result is null)
                        comparison = 1;
                    if (comparison > 0)
                        activeValue!.Value = e.Result;
                    else if (comparison < 0)
                        activeValue!.Value = rangeActiveExpression.GetResultsUnderLock().Select(er => er.result).Min();
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                try
                {
                    activeValue = new ActiveValue<TResult>(rangeActiveExpression.GetResults().Select(kr => kr.result).Min(), null, rangeActiveExpression, dispose);
                    rangeActiveExpression.DictionaryChanged += rangeActiveExpressionChanged;
                    rangeActiveExpression.ValueResultChanged += valueResultChanged;
                    return activeValue;
                }
                catch (Exception ex)
                {
                    activeValue = new ActiveValue<TResult>(default, ex, rangeActiveExpression, dispose);
                    rangeActiveExpression.DictionaryChanged += rangeActiveExpressionChanged;
                    rangeActiveExpression.ValueResultChanged += valueResultChanged;
                    return activeValue;
                }
            });
        }

        #endregion Min

        #region OfType

        static IEnumerable<KeyValuePair<TKey, TResult>> OfType<TKey, TValue, TResult>(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs) =>
            keyValuePairs.Select(kv => kv.Value is TResult result ? (key: kv.Key, value: kv.Value, isResult: true, result) : (key: kv.Key, value: kv.Value, isResult: false, result: default(TResult)!)).Where(t => t.isResult).Select(t => new KeyValuePair<TKey, TResult>(t.key, t.result));

        /// <summary>
        /// Actively filters the values of a <see cref="IReadOnlyDictionary{TKey, TValue}"/> based on a specified type
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type to filter the values of the dictionary on</typeparam>
        /// <param name="source">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> the values of which to filter</param>
        /// <returns>An <see cref="IReadOnlyDictionary{TKey, TValue}"/> that contains values from the input dictionary of type <typeparamref name="TResult"/></returns>
        public static IActiveDictionary<TKey, TResult> ActiveOfType<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source)
        {
            var synchronizedSource = source as ISynchronized;
            var notifyingSource = source as INotifyDictionaryChanged<TKey, TValue>;
            ISynchronizedObservableRangeDictionary<TKey, TResult> rangeObservableDictionary;

            void dictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    switch (e.Action)
                    {
                        case NotifyDictionaryChangedAction.Add:
                            rangeObservableDictionary.AddRange(OfType<TKey, TValue, TResult>(e.NewItems));
                            break;
                        case NotifyDictionaryChangedAction.Remove:
                            rangeObservableDictionary.RemoveRange(e.OldItems.Select(kv => kv.Key));
                            break;
                        case NotifyDictionaryChangedAction.Replace:
                            rangeObservableDictionary.ReplaceRange(e.OldItems.Select(kv => kv.Key), OfType<TKey, TValue, TResult>(e.NewItems));
                            break;
                        case NotifyDictionaryChangedAction.Reset:
                            var replacementDictionary = source.GetIndexingStrategy() == IndexingStrategy.SelfBalancingBinarySearchTree ? (IRangeDictionary<TKey, TResult>)(source.GetKeyComparer() is IComparer<TKey> comparer ? new ObservableSortedDictionary<TKey, TResult>(comparer) : new ObservableSortedDictionary<TKey, TResult>()) : (source.GetKeyEqualityComparer() is IEqualityComparer<TKey> equalityComparer ? new ObservableDictionary<TKey, TResult>(equalityComparer) : new ObservableDictionary<TKey, TResult>());
                            replacementDictionary.AddRange(OfType<TKey, TValue, TResult>(source));
                            rangeObservableDictionary.Reset(replacementDictionary);
                            break;
                    }
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeObservableDictionary = source.GetIndexingStrategy() == IndexingStrategy.SelfBalancingBinarySearchTree ? (ISynchronizedObservableRangeDictionary<TKey, TResult>)(source.GetKeyComparer() is IComparer<TKey> comparer ? new SynchronizedObservableSortedDictionary<TKey, TResult>(comparer) : new SynchronizedObservableSortedDictionary<TKey, TResult>()) : (source.GetKeyEqualityComparer() is IEqualityComparer<TKey> equalityComparer ? new SynchronizedObservableDictionary<TKey, TResult>(equalityComparer) : new SynchronizedObservableDictionary<TKey, TResult>());
                rangeObservableDictionary.AddRange(OfType<TKey, TValue, TResult>(source));

                if (notifyingSource is { })
                    notifyingSource.DictionaryChanged += dictionaryChanged;

                return new ActiveDictionary<TKey, TResult>(rangeObservableDictionary, source as INotifyElementFaultChanges, () =>
                {
                    if (notifyingSource is { })
                        notifyingSource.DictionaryChanged -= dictionaryChanged;
                });
            });
        }

        #endregion OfType

        #region Select

        /// <summary>
        /// Actively projects each key/value pair of a dictionary into a new form
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
        /// <param name="source">A dictionary of key/value pairs to invoke a transform function on</param>
        /// <param name="selector">A transform function to apply to each key/value pair</param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the transform function on each key/value pair of <paramref name="source"/></returns>
        public static IActiveEnumerable<TResult> ActiveSelect<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector) =>
            ActiveSelect(source, selector, null);

        /// <summary>
        /// Actively projects each key/value pair of a dictionary into a new form
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/></typeparam>
        /// <param name="source">A dictionary of key/value pairs to invoke a transform function on</param>
        /// <param name="selector">A transform function to apply to each key/value pair</param>
        /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> the elements of which are the result of invoking the transform function on each key/value pair of <paramref name="source"/></returns>
        public static IActiveEnumerable<TResult> ActiveSelect<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector, ActiveExpressionOptions? selectorOptions)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var synchronizedSource = source as ISynchronized;
            ReadOnlyDictionaryRangeActiveExpression<TKey, TValue, TResult> rangeActiveExpression;
            var keyToIndex = source.CreateSimilarNullableKeyDictionary<TKey, TValue, int>();
            SynchronizedRangeObservableCollection<TResult>? rangeObservableCollection = null;

            void rangeActiveExpressionChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Action == NotifyDictionaryChangedAction.Reset)
                    {
                        rangeObservableCollection!.Reset(rangeActiveExpression.GetResults().Select(((TKey key, TResult result) er, int index) =>
                        {
                            keyToIndex.Add(er.key, index);
                            return er.result;
                        }));
                    }
                    else
                    {
                        if (e.OldItems is { } && e.OldItems.Count > 0)
                        {
                            var removingIndicies = new List<int>();
                            foreach (var kv in e.OldItems)
                            {
                                var key = kv.Key;
                                removingIndicies.Add(keyToIndex[key]);
                                keyToIndex.Remove(key);
                            }
                            var rangeStart = -1;
                            var rangeCount = 0;
                            rangeObservableCollection.Execute(() =>
                            {
                                foreach (var removingIndex in removingIndicies.OrderByDescending(i => i))
                                {
                                    if (removingIndex != rangeStart - 1 && rangeStart != -1)
                                    {
                                        if (rangeCount == 1)
                                            rangeObservableCollection!.RemoveAt(rangeStart);
                                        else
                                            rangeObservableCollection!.RemoveRange(rangeStart, rangeCount);
                                        rangeCount = 0;
                                    }
                                    rangeStart = removingIndex;
                                    ++rangeCount;
                                }
                                if (rangeStart != -1)
                                {
                                    if (rangeCount == 1)
                                        rangeObservableCollection!.RemoveAt(rangeStart);
                                    else
                                        rangeObservableCollection!.RemoveRange(rangeStart, rangeCount);
                                }
                            });
                            var revisedKeyedIndicies = keyToIndex.OrderBy(kv => kv.Value);
                            keyToIndex = source.CreateSimilarDictionary<TKey, TValue, int>();
                            foreach (var (key, index) in revisedKeyedIndicies.Select((kv, index) => (kv.Key, index)))
                                keyToIndex.Add(key, index);
                        }
                        if ((e.NewItems?.Count ?? 0) > 0)
                        {
                            var currentCount = keyToIndex.Count;
                            rangeObservableCollection!.AddRange(e.NewItems.Select((KeyValuePair<TKey, TResult> kv, int index) =>
                            {
                                keyToIndex.Add(kv.Key, currentCount + index);
                                return kv.Value;
                            }));
                        }
                    }
                });

            void valueResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e) => synchronizedSource.SequentialExecute(() => rangeObservableCollection!.Replace(keyToIndex[e.Element! /* this could be null, but it won't matter if it is */], e.Result! /* this could be null, but it won't matter if it is */));

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeObservableCollection = new SynchronizedRangeObservableCollection<TResult>(rangeActiveExpression.GetResults().Select(((TKey key, TResult result) er, int index) =>
                {
                    keyToIndex.Add(er.key, index);
                    return er.result;
                }));
                rangeActiveExpression.DictionaryChanged += rangeActiveExpressionChanged;
                rangeActiveExpression.ValueResultChanged += valueResultChanged;
                return new ActiveEnumerable<TResult>(rangeObservableCollection, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.DictionaryChanged -= rangeActiveExpressionChanged;
                    rangeActiveExpression.ValueResultChanged -= valueResultChanged;
                    rangeActiveExpression.Dispose();
                });
            });
        }

        #endregion Select

        #region Single

        /// <summary>
        /// Actively returns the only key/value pair of a dictionary, and becomes faulted if there is not exactly one key/value pair in the dictionary
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the single key/value pair of</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the single key/value pair of the input dictionary</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveSingle<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                ActiveValue<KeyValuePair<TKey, TValue>>? activeValue = null;
                bool none = false, moreThanOne = false;

                void dispose() => changingSource.DictionaryChanged -= sourceChanged;

                void sourceChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        if (source.Count == 1)
                        {
                            none = false;
                            moreThanOne = false;
                            activeValue!.OperationFault = null;
                            activeValue!.Value = source.First();
                        }
                        else
                        {
                            if (source.Count == 0 && !none)
                            {
                                none = true;
                                moreThanOne = false;
                                activeValue!.OperationFault = ExceptionHelper.SequenceContainsNoElements;
                            }
                            else if (!moreThanOne)
                            {
                                none = false;
                                moreThanOne = true;
                                activeValue!.OperationFault = ExceptionHelper.SequenceContainsMoreThanOneElement;
                            }
                            activeValue!.Value = default;
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    switch (source.Count)
                    {
                        case 0:
                            none = true;
                            activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(default, ExceptionHelper.SequenceContainsNoElements, elementFaultChangeNotifier, dispose);
                            break;
                        case 1:
                            activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(source.First(), elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                            break;
                        default:
                            moreThanOne = true;
                            activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(default, ExceptionHelper.SequenceContainsMoreThanOneElement, elementFaultChangeNotifier, dispose);
                            break;
                    }
                    changingSource.DictionaryChanged += sourceChanged;
                    return activeValue;
                });
            }
            try
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(source.Single(), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(default, ex, elementFaultChangeNotifier);
            }
        }

        /// <summary>
        /// Actively returns the only key/value pair of a dictionary that satisfies a specified condition, and becomes faulted if more than one such key/value pair exists
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the single key/value pair of</param>
        /// <param name="predicate">A function to test a key/value pair for a condition</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the single key/value pair of the input dictionary that satisfies a condition</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveSingle<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate) =>
            ActiveSingle(source, predicate, null);

        /// <summary>
        /// Actively returns the only key/value pair of a dictionary that satisfies a specified condition, and becomes faulted if more than one such key/value pair exists
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the single key/value pair of</param>
        /// <param name="predicate">A function to test a key/value pair for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the single key/value pair of the input dictionary that satisfies a condition</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveSingle<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, ActiveExpressionOptions? predicateOptions)
        {
            IActiveDictionary<TKey, TValue> where;
            ActiveValue<KeyValuePair<TKey, TValue>>? activeValue = null;
            var none = false;
            var moreThanOne = false;

            void dictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e)
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
                activeValue!.Value = where.Count == 1 ? where.First() : default;
            }

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                Exception? operationFault = null;
                if (none = where.Count == 0)
                    operationFault = ExceptionHelper.SequenceContainsNoElements;
                else if (moreThanOne = where.Count > 1)
                    operationFault = ExceptionHelper.SequenceContainsMoreThanOneElement;
                activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(operationFault is null ? where.First() : default, operationFault, where, () =>
                {
                    where.DictionaryChanged -= dictionaryChanged;
                    where.Dispose();
                });
                where.DictionaryChanged += dictionaryChanged;
                return activeValue;
            });
        }

        #endregion Single

        #region SingleOrDefault

        /// <summary>
        /// Actively returns the only key/value pair of a dictionary, or a default key/value pair if the dictionary is empty; becomes faulted if there is more than one key/value pair in the dictionary
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the single key/value pair of</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the single key/value pair of the input dictionary, or <c>default</c>(<c>KeyValuePair&lt;</c><typeparamref name="TKey"/>, <typeparamref name="TValue"/><c>%gt;</c>) if the dictionary contains no key/value pairs</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveSingleOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                ActiveValue<KeyValuePair<TKey, TValue>>? activeValue = null;

                void dispose() => changingSource.DictionaryChanged += sourceChanged;

                void sourceChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        switch (source.Count)
                        {
                            case 0:
                                activeValue!.OperationFault = null;
                                activeValue!.Value = default;
                                break;
                            case 1:
                                activeValue!.OperationFault = null;
                                activeValue!.Value = source.First();
                                break;
                            default:
                                if (activeValue!.OperationFault is null)
                                    activeValue!.OperationFault = ExceptionHelper.SequenceContainsMoreThanOneElement;
                                activeValue!.Value = default;
                                break;
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    activeValue = source.Count switch
                    {
                        0 => activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(default, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose),
                        1 => activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(source.First(), elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose),
                        _ => activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(default, ExceptionHelper.SequenceContainsMoreThanOneElement, elementFaultChangeNotifier, dispose),
                    };
                    changingSource.DictionaryChanged += sourceChanged;
                    return activeValue;
                });
            }
            try
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(source.SingleOrDefault(), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(default, ex, elementFaultChangeNotifier);
            }
        }

        /// <summary>
        /// Actively returns the only key/value pair of a dictionary that satisfies a specified condition or a default key/value pair if no such key/value pair exists; becomes faulted if more than one key/value pair satisfies the condition
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the single key/value pair of</param>
        /// <param name="predicate">A function to test a key/value pair for a condition</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the single key/value pair of the input dictionary that satisfies a condition, or <c>default</c>(<c>KeyValuePair&lt;</c><typeparamref name="TKey"/>, <typeparamref name="TValue"/><c>%gt;</c>) if no such key/value pair is found</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveSingleOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate) =>
            ActiveSingleOrDefault(source, predicate, null);

        /// <summary>
        /// Actively returns the only key/value pair of a dictionary that satisfies a specified condition or a default key/value pair if no such key/value pair exists; becomes faulted if more than one key/value pair satisfies the condition
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return the single key/value pair of</param>
        /// <param name="predicate">A function to test a key/value pair for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the single key/value pair of the input dictionary that satisfies a condition, or <c>default</c>(<c>KeyValuePair&lt;</c><typeparamref name="TKey"/>, <typeparamref name="TValue"/><c>%gt;</c>) if no such key/value pair is found</returns>
        public static IActiveValue<KeyValuePair<TKey, TValue>> ActiveSingleOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, ActiveExpressionOptions? predicateOptions)
        {
            IActiveDictionary<TKey, TValue> where;
            ActiveValue<KeyValuePair<TKey, TValue>>? activeValue = null;
            var moreThanOne = false;

            void dictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e)
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
                activeValue!.Value = where.Count == 1 ? where.First() : default;
            }

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                var operationFault = (moreThanOne = where.Count > 1) ? ExceptionHelper.SequenceContainsMoreThanOneElement : null;
                activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(!moreThanOne && where.Count == 1 ? where.First() : default, operationFault, where, () =>
                {
                    where.DictionaryChanged -= dictionaryChanged;
                    where.Dispose();
                });
                where.DictionaryChanged += dictionaryChanged;
                return activeValue;
            });
        }

        #endregion SingleOrDefault

        #region Sum

        /// <summary>
        /// Actively computes the sum of the values in a dictionary
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">A dictionary that is used to calculate a sum</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the sum of the values in the dictionary</returns>
        public static IActiveValue<TValue> ActiveSum<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
            ActiveSum(source, (key, value) => value);

        /// <summary>
        /// Actively computes the sum of the sequence of values that are obtained by invoking a transform function on each key/value pair of the input sequence
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the values being summed</typeparam>
        /// <param name="source">A dictionary that is used to calculate a sum</param>
        /// <param name="selector">A transform function to apply to each key/value pair</param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the sum of the projected values</returns>
        public static IActiveValue<TResult> ActiveSum<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector) =>
            ActiveSum(source, selector, null);

        /// <summary>
        /// Actively computes the sum of the sequence of values that are obtained by invoking a transform function on each key/value pair of the input sequence
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the values being summed</typeparam>
        /// <param name="source">A dictionary that is used to calculate a sum</param>
        /// <param name="selector">A transform function to apply to each key/value pair</param>
        /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
        /// <returns>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the sum of the projected values</returns>
        public static IActiveValue<TResult> ActiveSum<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector, ActiveExpressionOptions? selectorOptions)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var operations = new GenericOperations<TResult>();
            var synchronizedSource = source as ISynchronized;
            ReadOnlyDictionaryRangeActiveExpression<TKey, TValue, TResult> rangeActiveExpression;
            ActiveValue<TResult>? activeValue = null;
            var valuesChanging = new NullableKeyDictionary<TKey, TResult>();

            void rangeActiveExpressionChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Action == NotifyDictionaryChangedAction.Reset)
                        activeValue!.Value = rangeActiveExpression.GetResults().Select(kr => kr.result).Aggregate(operations.Add);
                    else
                    {
                        var sum = activeValue!.Value;
                        if ((e.OldItems?.Count ?? 0) > 0)
                            sum = (sum is null ? Enumerable.Empty<TResult>() : new TResult[] { sum }).Concat(e.OldItems.Select(kv => kv.Value)).Aggregate(operations.Subtract);
                        if ((e.NewItems?.Count ?? 0) > 0)
                            sum = (sum is null ? Enumerable.Empty<TResult>() : new TResult[] { sum }).Concat(e.NewItems.Select(kv => kv.Value)).Aggregate(operations.Add);
                        activeValue!.Value = sum;
                    }
                });

            void valueResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var key = e.Element;
                    activeValue!.Value = operations.Add(activeValue!.Value, operations.Subtract(e.Result, valuesChanging[key! /* this could be null, but it won't matter if it is */]));
                    valuesChanging.Remove(key! /* this could be null, but it won't matter if it is */);
                });

            void valueResultChanging(object sender, RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e) => synchronizedSource.SequentialExecute(() => valuesChanging.Add(e.Element! /* this could be null, but it won't matter if it is */, e.Result! /* this could be null, but it won't matter if it is */));

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);

                void dispose()
                {
                    rangeActiveExpression.DictionaryChanged -= rangeActiveExpressionChanged;
                    rangeActiveExpression.ValueResultChanged -= valueResultChanged;
                    rangeActiveExpression.ValueResultChanging -= valueResultChanging;
                    rangeActiveExpression.Dispose();
                }

                try
                {
                    activeValue = new ActiveValue<TResult>(rangeActiveExpression.GetResults().Select(kr => kr.result).Aggregate(operations.Add), null, rangeActiveExpression, dispose);
                    rangeActiveExpression.DictionaryChanged += rangeActiveExpressionChanged;
                    rangeActiveExpression.ValueResultChanged += valueResultChanged;
                    rangeActiveExpression.ValueResultChanging += valueResultChanging;
                    return activeValue;
                }
                catch (InvalidOperationException)
                {
                    activeValue = new ActiveValue<TResult>(default, null, rangeActiveExpression, dispose);
                    rangeActiveExpression.DictionaryChanged += rangeActiveExpressionChanged;
                    rangeActiveExpression.ValueResultChanged += valueResultChanged;
                    rangeActiveExpression.ValueResultChanging += valueResultChanging;
                    return activeValue;
                }
            });
        }

        #endregion Sum

        #region SwitchContext

        /// <summary>
        /// Creates an <see cref="ActiveDictionary{TKey, TValue}"/> that is kept consistent the current thread's <see cref="SynchronizationContext"/> with a specified <see cref="IReadOnlyDictionary{TKey, TValue}"/> that implements <see cref="INotifyDictionaryChanged{TKey, TValue}"/>
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">A <see cref="IReadOnlyDictionary{TKey, TValue}"/></param>
        /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> that is kept consistent with <paramref name="source"/> the current thread's <see cref="SynchronizationContext"/></returns>
        public static IActiveDictionary<TKey, TValue> SwitchContext<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
            SwitchContext(source, SynchronizationContext.Current);

        /// <summary>
        /// Creates an <see cref="ActiveDictionary{TKey, TValue}"/> that is kept consistent on a specified <see cref="SynchronizationContext"/> with a specified <see cref="IReadOnlyDictionary{TKey, TValue}"/> that implements <see cref="INotifyDictionaryChanged{TKey, TValue}"/>
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">A <see cref="IReadOnlyDictionary{TKey, TValue}"/></param>
        /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> on which to perform consistency operations</param>
        /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> that is kept consistent with <paramref name="source"/> on <paramref name="synchronizationContext"/></returns>
        public static IActiveDictionary<TKey, TValue> SwitchContext<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, SynchronizationContext synchronizationContext)
        {
            ISynchronizedObservableRangeDictionary<TKey, TValue>? rangeObservableDictionary = null;

            async void dictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e)
            {
                IDictionary<TKey, TValue>? resetDictionary = null;
                if (e.Action == NotifyDictionaryChangedAction.Reset)
                {
                    switch (source.GetIndexingStrategy() ?? IndexingStrategy.NoneOrInherit)
                    {
                        case IndexingStrategy.SelfBalancingBinarySearchTree:
                            var keyComparer = source.GetKeyComparer();
                            resetDictionary = keyComparer is { } ? new NullableKeySortedDictionary<TKey, TValue>(keyComparer) : new NullableKeySortedDictionary<TKey, TValue>();
                            break;
                        default:
                            var keyEqualityComparer = source.GetKeyEqualityComparer();
                            resetDictionary = keyEqualityComparer is { } ? new NullableKeyDictionary<TKey, TValue>(keyEqualityComparer) : new NullableKeyDictionary<TKey, TValue>();
                            break;
                    }
                    foreach (var kv in source)
                        resetDictionary.Add(kv);
                }
                await rangeObservableDictionary.SequentialExecuteAsync(() =>
                {
                    switch (e.Action)
                    {
                        case NotifyDictionaryChangedAction.Add:
                            rangeObservableDictionary!.AddRange(e.NewItems);
                            break;
                        case NotifyDictionaryChangedAction.Remove:
                            rangeObservableDictionary!.RemoveRange(e.OldItems.Select(kv => kv.Key));
                            break;
                        case NotifyDictionaryChangedAction.Replace:
                            rangeObservableDictionary!.ReplaceRange(e.OldItems.Select(kv => kv.Key), e.NewItems);
                            break;
                        case NotifyDictionaryChangedAction.Reset:
                            rangeObservableDictionary!.Reset(resetDictionary!);
                            break;
                    }
                }).ConfigureAwait(false);
            }

            return (source as ISynchronized).SequentialExecute(() =>
            {
                var notifier = source as INotifyDictionaryChanged<TKey, TValue>;
                if (notifier is { })
                    notifier.DictionaryChanged += dictionaryChanged;

                IDictionary<TKey, TValue>? startingDictionary = null;
                switch (source.GetIndexingStrategy() ?? IndexingStrategy.NoneOrInherit)
                {
                    case IndexingStrategy.SelfBalancingBinarySearchTree:
                        var keyComparer = source.GetKeyComparer();
                        startingDictionary = keyComparer is { } ? new NullableKeySortedDictionary<TKey, TValue>(keyComparer) : new NullableKeySortedDictionary<TKey, TValue>();
                        foreach (var kv in source)
                            startingDictionary.Add(kv);
                        rangeObservableDictionary = keyComparer is { } ? new SynchronizedObservableSortedDictionary<TKey, TValue>(synchronizationContext, startingDictionary, keyComparer) : new SynchronizedObservableSortedDictionary<TKey, TValue>(synchronizationContext, startingDictionary);
                        break;
                    default:
                        var keyEqualityComparer = source.GetKeyEqualityComparer();
                        startingDictionary = keyEqualityComparer is { } ? new NullableKeyDictionary<TKey, TValue>(keyEqualityComparer) : new NullableKeyDictionary<TKey, TValue>();
                        foreach (var kv in source)
                            startingDictionary.Add(kv);
                        rangeObservableDictionary = keyEqualityComparer is { } ? new SynchronizedObservableDictionary<TKey, TValue>(synchronizationContext, startingDictionary, keyEqualityComparer) : new SynchronizedObservableDictionary<TKey, TValue>(synchronizationContext, startingDictionary);
                        break;
                }
                return new ActiveDictionary<TKey, TValue>(rangeObservableDictionary, source as INotifyElementFaultChanges, () =>
                {
                    if (notifier is { })
                        notifier.DictionaryChanged -= dictionaryChanged;
                });
            });
        }

        #endregion SwitchContext

        #region ToActiveDictionary

        /// <summary>
        /// Generates an <see cref="ActiveDictionary{TKey, TValue}"/> which actively projects each key/value pair of a dictionary into a key-value pair using the specified <see cref="IComparer{T}"/>
        /// </summary>
        /// <typeparam name="TSourceKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TSourceValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResultKey">The type of the keys in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <typeparam name="TResultValue">The type of the values in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <param name="source">A dictionary to transform into key/value pairs</param>
        /// <param name="selector">A transform function to apply to each key/value pair in <paramref name="source"/></param>
        /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> the key/value pairs of which are the result of invoking the transform function on each key/value pair in <paramref name="source"/></returns>
        public static IActiveDictionary<TResultKey, TResultValue> ToActiveDictionary<TSourceKey, TSourceValue, TResultKey, TResultValue>(this IReadOnlyDictionary<TSourceKey, TSourceValue> source, Expression<Func<TSourceKey, TSourceValue, KeyValuePair<TResultKey, TResultValue>>> selector) =>
            ToActiveDictionary(source, selector, null, source.GetIndexingStrategy() ?? IndexingStrategy.NoneOrInherit, null, null);

        /// <summary>
        /// Generates an <see cref="ActiveDictionary{TKey, TValue}"/> using the specified <see cref="IndexingStrategy"/> which actively projects each key/value pair of a dictionary into a key-value pair using the specified <see cref="IComparer{T}"/>
        /// </summary>
        /// <typeparam name="TSourceKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TSourceValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResultKey">The type of the keys in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <typeparam name="TResultValue">The type of the values in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <param name="source">A dictionary to transform into key/value pairs</param>
        /// <param name="selector">A transform function to apply to each key/value pair in <paramref name="source"/></param>
        /// <param name="indexingStrategy">The indexing strategy to use for the resulting <see cref="ActiveDictionary{TKey, TValue}"/></param>
        /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> using the specified <see cref="IndexingStrategy"/> the key/value pairs of which are the result of invoking the transform function on each key/value pair in <paramref name="source"/></returns>
        public static IActiveDictionary<TResultKey, TResultValue> ToActiveDictionary<TSourceKey, TSourceValue, TResultKey, TResultValue>(this IReadOnlyDictionary<TSourceKey, TSourceValue> source, Expression<Func<TSourceKey, TSourceValue, KeyValuePair<TResultKey, TResultValue>>> selector, IndexingStrategy indexingStrategy) =>
            ToActiveDictionary(source, selector, null, indexingStrategy, null, null);

        /// <summary>
        /// Generates an <see cref="ActiveDictionary{TKey, TValue}"/> using a hash table which actively projects each key/value pair of a dictionary into a key-value pair using the specified <see cref="IComparer{T}"/>
        /// </summary>
        /// <typeparam name="TSourceKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TSourceValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResultKey">The type of the keys in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <typeparam name="TResultValue">The type of the values in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <param name="source">A dictionary to transform into key/value pairs</param>
        /// <param name="selector">A transform function to apply to each key/value pair in <paramref name="source"/></param>
        /// <param name="keyEqualityComparer">An <see cref="IEqualityComparer{T}"/> to compare resulting keys</param>
        /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> using a hash table the key/value pairs of which are the result of invoking the transform function on each key/value pair in <paramref name="source"/></returns>
        public static IActiveDictionary<TResultKey, TResultValue> ToActiveDictionary<TSourceKey, TSourceValue, TResultKey, TResultValue>(this IReadOnlyDictionary<TSourceKey, TSourceValue> source, Expression<Func<TSourceKey, TSourceValue, KeyValuePair<TResultKey, TResultValue>>> selector, IEqualityComparer<TResultKey>? keyEqualityComparer) =>
            ToActiveDictionary(source, selector, null, IndexingStrategy.HashTable, keyEqualityComparer, null);

        /// <summary>
        /// Generates an <see cref="ActiveDictionary{TKey, TValue}"/> using a binary search tree which actively projects each key/value pair of a dictionary into a key-value pair using the specified <see cref="IComparer{T}"/>
        /// </summary>
        /// <typeparam name="TSourceKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TSourceValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResultKey">The type of the keys in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <typeparam name="TResultValue">The type of the values in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <param name="source">A dictionary to transform into key/value pairs</param>
        /// <param name="selector">A transform function to apply to each key/value pair in <paramref name="source"/></param>
        /// <param name="keyComparer">An <see cref="IComparer{T}"/> to compare resulting keys</param>
        /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> using a binary search tree the key/value pairs of which are the result of invoking the transform function on each key/value pair in <paramref name="source"/></returns>
        public static IActiveDictionary<TResultKey, TResultValue> ToActiveDictionary<TSourceKey, TSourceValue, TResultKey, TResultValue>(this IReadOnlyDictionary<TSourceKey, TSourceValue> source, Expression<Func<TSourceKey, TSourceValue, KeyValuePair<TResultKey, TResultValue>>> selector, IComparer<TResultKey>? keyComparer) =>
            ToActiveDictionary(source, selector, null, IndexingStrategy.SelfBalancingBinarySearchTree, null, keyComparer);

        /// <summary>
        /// Generates an <see cref="ActiveDictionary{TKey, TValue}"/> using a specified <see cref="IndexingStrategy"/> which actively projects each key/value pair of a dictionary into a key-value pair using the specified <see cref="IComparer{T}"/>
        /// </summary>
        /// <typeparam name="TSourceKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TSourceValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResultKey">The type of the keys in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <typeparam name="TResultValue">The type of the values in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <typeparam name="TResultKeyComparer">The type of the comparer for <typeparamref name="TResultKey"/> values</typeparam>
        /// <param name="source">A dictionary to transform into key/value pairs</param>
        /// <param name="selector">A transform function to apply to each key/value pair in <paramref name="source"/></param>
        /// <param name="keyComparer">An <see cref="IComparer{T}"/> to compare resulting keys</param>
        /// <param name="indexingStrategy">The <see cref="IndexingStrategy"/> to be used by the <see cref="ActiveDictionary{TKey, TValue}"/></param>
        /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> using a specified <see cref="IndexingStrategy"/> the key/value pairs of which are the result of invoking the transform function on each key/value pair in <paramref name="source"/></returns>
        public static IActiveDictionary<TResultKey, TResultValue> ToActiveDictionary<TSourceKey, TSourceValue, TResultKey, TResultValue, TResultKeyComparer>(this IReadOnlyDictionary<TSourceKey, TSourceValue> source, Expression<Func<TSourceKey, TSourceValue, KeyValuePair<TResultKey, TResultValue>>> selector, TResultKeyComparer keyComparer, IndexingStrategy indexingStrategy) where TResultKeyComparer : IComparer<TResultKey>, IEqualityComparer<TResultKey> =>
            ToActiveDictionary(source, selector, null, indexingStrategy, indexingStrategy != IndexingStrategy.SelfBalancingBinarySearchTree ? keyComparer : (IEqualityComparer<TResultKey>?)null, indexingStrategy == IndexingStrategy.SelfBalancingBinarySearchTree ? keyComparer : (IComparer<TResultKey>?)null);

        /// <summary>
        /// Generates an <see cref="ActiveDictionary{TKey, TValue}"/> which actively projects each key/value pair of a dictionary into a key-value pair using the specified <see cref="IComparer{T}"/>
        /// </summary>
        /// <typeparam name="TSourceKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TSourceValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResultKey">The type of the keys in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <typeparam name="TResultValue">The type of the values in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <param name="source">A dictionary to transform into key/value pairs</param>
        /// <param name="selector">A transform function to apply to each key/value pair in <paramref name="source"/></param>
        /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
        /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> the key/value pairs of which are the result of invoking the transform function on each key/value pair in <paramref name="source"/></returns>
        public static IActiveDictionary<TResultKey, TResultValue> ToActiveDictionary<TSourceKey, TSourceValue, TResultKey, TResultValue>(this IReadOnlyDictionary<TSourceKey, TSourceValue> source, Expression<Func<TSourceKey, TSourceValue, KeyValuePair<TResultKey, TResultValue>>> selector, ActiveExpressionOptions selectorOptions) =>
            ToActiveDictionary(source, selector, selectorOptions, source.GetIndexingStrategy() ?? IndexingStrategy.NoneOrInherit, null, null);

        /// <summary>
        /// Generates an <see cref="ActiveDictionary{TKey, TValue}"/> using the specified <see cref="IndexingStrategy"/> which actively projects each key/value pair of a dictionary into a key-value pair using the specified <see cref="IComparer{T}"/>
        /// </summary>
        /// <typeparam name="TSourceKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TSourceValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResultKey">The type of the keys in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <typeparam name="TResultValue">The type of the values in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <param name="source">A dictionary to transform into key/value pairs</param>
        /// <param name="selector">A transform function to apply to each key/value pair in <paramref name="source"/></param>
        /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
        /// <param name="indexingStrategy">The indexing strategy to use for the resulting <see cref="ActiveDictionary{TKey, TValue}"/></param>
        /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> using the specified <see cref="IndexingStrategy"/> the key/value pairs of which are the result of invoking the transform function on each key/value pair in <paramref name="source"/></returns>
        public static IActiveDictionary<TResultKey, TResultValue> ToActiveDictionary<TSourceKey, TSourceValue, TResultKey, TResultValue>(this IReadOnlyDictionary<TSourceKey, TSourceValue> source, Expression<Func<TSourceKey, TSourceValue, KeyValuePair<TResultKey, TResultValue>>> selector, ActiveExpressionOptions selectorOptions, IndexingStrategy indexingStrategy) =>
            ToActiveDictionary(source, selector, selectorOptions, indexingStrategy, null, null);

        /// <summary>
        /// Generates an <see cref="ActiveDictionary{TKey, TValue}"/> using a hash table which actively projects each key/value pair of a dictionary into a key-value pair using the specified <see cref="IComparer{T}"/>
        /// </summary>
        /// <typeparam name="TSourceKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TSourceValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResultKey">The type of the keys in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <typeparam name="TResultValue">The type of the values in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <param name="source">A dictionary to transform into key/value pairs</param>
        /// <param name="selector">A transform function to apply to each key/value pair in <paramref name="source"/></param>
        /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
        /// <param name="keyEqualityComparer">An <see cref="IEqualityComparer{T}"/> to compare resulting keys</param>
        /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> using a hash table the key/value pairs of which are the result of invoking the transform function on each key/value pair in <paramref name="source"/></returns>
        public static IActiveDictionary<TResultKey, TResultValue> ToActiveDictionary<TSourceKey, TSourceValue, TResultKey, TResultValue>(this IReadOnlyDictionary<TSourceKey, TSourceValue> source, Expression<Func<TSourceKey, TSourceValue, KeyValuePair<TResultKey, TResultValue>>> selector, ActiveExpressionOptions selectorOptions, IEqualityComparer<TResultKey> keyEqualityComparer) =>
            ToActiveDictionary(source, selector, selectorOptions, IndexingStrategy.HashTable, keyEqualityComparer, null);

        /// <summary>
        /// Generates an <see cref="ActiveDictionary{TKey, TValue}"/> using a binary search tree which actively projects each key/value pair of a dictionary into a key-value pair using the specified <see cref="IComparer{T}"/>
        /// </summary>
        /// <typeparam name="TSourceKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TSourceValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResultKey">The type of the keys in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <typeparam name="TResultValue">The type of the values in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <param name="source">A dictionary to transform into key/value pairs</param>
        /// <param name="selector">A transform function to apply to each key/value pair in <paramref name="source"/></param>
        /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
        /// <param name="keyComparer">An <see cref="IComparer{T}"/> to compare resulting keys</param>
        /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> using a binary search tree the key/value pairs of which are the result of invoking the transform function on each key/value pair in <paramref name="source"/></returns>
        public static IActiveDictionary<TResultKey, TResultValue> ToActiveDictionary<TSourceKey, TSourceValue, TResultKey, TResultValue>(this IReadOnlyDictionary<TSourceKey, TSourceValue> source, Expression<Func<TSourceKey, TSourceValue, KeyValuePair<TResultKey, TResultValue>>> selector, ActiveExpressionOptions selectorOptions, IComparer<TResultKey> keyComparer) =>
            ToActiveDictionary(source, selector, selectorOptions, IndexingStrategy.SelfBalancingBinarySearchTree, null, keyComparer);

        /// <summary>
        /// Generates an <see cref="ActiveDictionary{TKey, TValue}"/> using a specified <see cref="IndexingStrategy"/> which actively projects each key/value pair of a dictionary into a key-value pair using the specified <see cref="IComparer{T}"/>
        /// </summary>
        /// <typeparam name="TSourceKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TSourceValue">The type of the values in <paramref name="source"/></typeparam>
        /// <typeparam name="TResultKey">The type of the keys in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <typeparam name="TResultValue">The type of the values in the resulting <see cref="ActiveDictionary{TKey, TValue}"/></typeparam>
        /// <typeparam name="TResultKeyComparer">The type of the comparer for <typeparamref name="TResultKey"/> values</typeparam>
        /// <param name="source">A dictionary to transform into key/value pairs</param>
        /// <param name="selector">A transform function to apply to each key/value pair in <paramref name="source"/></param>
        /// <param name="selectorOptions">Options governing the behavior of active expressions created using <paramref name="selector"/></param>
        /// <param name="keyComparer">An <see cref="IComparer{T}"/> to compare resulting keys</param>
        /// <param name="indexingStrategy">The <see cref="IndexingStrategy"/> to be used by the <see cref="ActiveDictionary{TKey, TValue}"/></param>
        /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> using a specified <see cref="IndexingStrategy"/> the key/value pairs of which are the result of invoking the transform function on each key/value pair in <paramref name="source"/></returns>
        public static IActiveDictionary<TResultKey, TResultValue> ToActiveDictionary<TSourceKey, TSourceValue, TResultKey, TResultValue, TResultKeyComparer>(this IReadOnlyDictionary<TSourceKey, TSourceValue> source, Expression<Func<TSourceKey, TSourceValue, KeyValuePair<TResultKey, TResultValue>>> selector, ActiveExpressionOptions selectorOptions, TResultKeyComparer keyComparer, IndexingStrategy indexingStrategy) where TResultKeyComparer : IComparer<TResultKey>, IEqualityComparer<TResultKey> =>
            ToActiveDictionary(source, selector, selectorOptions, indexingStrategy, indexingStrategy != IndexingStrategy.SelfBalancingBinarySearchTree ? keyComparer : (IEqualityComparer<TResultKey>?)null, indexingStrategy == IndexingStrategy.SelfBalancingBinarySearchTree ? keyComparer : (IComparer<TResultKey>?)null);

        static IActiveDictionary<TResultKey, TResultValue> ToActiveDictionary<TSourceKey, TSourceValue, TResultKey, TResultValue>(IReadOnlyDictionary<TSourceKey, TSourceValue> source, Expression<Func<TSourceKey, TSourceValue, KeyValuePair<TResultKey, TResultValue>>> selector, ActiveExpressionOptions? selectorOptions, IndexingStrategy indexingStrategy, IEqualityComparer<TResultKey>? keyEqualityComparer, IComparer<TResultKey>? keyComparer)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var synchronizedSource = source as ISynchronized;
            IDictionary<TResultKey, int> duplicateKeys;
            var isFaultedDuplicateKeys = false;
            var isFaultedNullKey = false;
            var nullKeys = 0;
            ReadOnlyDictionaryRangeActiveExpression<TSourceKey, TSourceValue, KeyValuePair<TResultKey, TResultValue>> rangeActiveExpression;
            var sourceKeyToResultKey = source.CreateSimilarDictionary<TSourceKey, TSourceValue, TResultKey>();
            ISynchronizedObservableRangeDictionary<TResultKey, TResultValue> rangeObservableDictionary;
            ActiveDictionary<TResultKey, TResultValue>? activeDictionary = null;

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
                        activeDictionary!.OperationFault = null;
                    }
                }
            }

            void rangeActiveExpressionChanged(object sender, NotifyDictionaryChangedEventArgs<TSourceKey, KeyValuePair<TResultKey, TResultValue>> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Action == NotifyDictionaryChangedAction.Reset)
                    {
                        IDictionary<TResultKey, TResultValue> replacementDictionary;
                        switch (indexingStrategy)
                        {
                            case IndexingStrategy.SelfBalancingBinarySearchTree:
                                duplicateKeys = keyComparer is null ? new SortedDictionary<TResultKey, int>() : new SortedDictionary<TResultKey, int>(keyComparer);
                                replacementDictionary = keyComparer is null ? new SortedDictionary<TResultKey, TResultValue>() : new SortedDictionary<TResultKey, TResultValue>(keyComparer);
                                break;
                            default:
                                duplicateKeys = keyEqualityComparer is null ? new Dictionary<TResultKey, int>() : new Dictionary<TResultKey, int>(keyEqualityComparer);
                                replacementDictionary = keyEqualityComparer is null ? new Dictionary<TResultKey, TResultValue>() : new Dictionary<TResultKey, TResultValue>(keyEqualityComparer);
                                break;
                        }
                        var resultsAndFaults = rangeActiveExpression.GetResultsAndFaults();
                        nullKeys = resultsAndFaults.Count(rfc => rfc.result.Key is null);
                        var distinctResultsAndFaults = resultsAndFaults.Where(rfc => rfc.result.Key is { }).GroupBy(rfc => rfc.result.Key).ToList();
                        foreach (var keyValuePair in distinctResultsAndFaults.Select(g => g.First().result))
                            replacementDictionary.Add(keyValuePair);
                        rangeObservableDictionary.Reset(replacementDictionary);
                        foreach (var (key, duplicateCount) in distinctResultsAndFaults.Select(g => (key: g.Key, duplicateCount: g.Count() - 1)).Where(kc => kc.duplicateCount > 0))
                            duplicateKeys.Add(key, duplicateCount);
                        checkOperationFault();
                    }
                    else
                    {
                        if (e.OldItems is { } && e.OldItems.Count > 0)
                        {
                            foreach (var kv in e.OldItems)
                            {
                                var key = kv.Value.Key;
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
                            foreach (var kv in e.NewItems)
                            {
                                var resultKv = kv.Value;
                                var key = resultKv.Key;
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
                                    rangeObservableDictionary.Add(resultKv);
                            }
                            checkOperationFault();
                        }
                    }
                });

            void valueResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSourceKey, KeyValuePair<TResultKey, TResultValue>> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var resultKv = e.Result;
                    var key = resultKv.Key;
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
                        rangeObservableDictionary.Add(resultKv);
                    checkOperationFault();
                });

            void valueResultChanging(object sender, RangeActiveExpressionResultChangeEventArgs<TSourceKey, KeyValuePair<TResultKey, TResultValue>> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var key = e.Result.Key;
                    if (key is null)
                        --nullKeys;
                    else if (duplicateKeys.TryGetValue(key, out var duplicates))
                    {
                        if (duplicates <= 1)
                            duplicateKeys.Remove(key);
                        else
                            duplicateKeys[key] = duplicates - 1;
                    }
                    else
                        rangeObservableDictionary.Remove(key);
                    checkOperationFault();
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                switch (indexingStrategy)
                {
                    case IndexingStrategy.SelfBalancingBinarySearchTree:
                        duplicateKeys = keyComparer is null ? new SortedDictionary<TResultKey, int>() : new SortedDictionary<TResultKey, int>(keyComparer);
                        rangeObservableDictionary = keyComparer is null ? new SynchronizedObservableSortedDictionary<TResultKey, TResultValue>() : new SynchronizedObservableSortedDictionary<TResultKey, TResultValue>(keyComparer);
                        break;
                    default:
                        duplicateKeys = keyEqualityComparer is null ? new Dictionary<TResultKey, int>() : new Dictionary<TResultKey, int>(keyEqualityComparer);
                        rangeObservableDictionary = keyEqualityComparer is null ? new SynchronizedObservableDictionary<TResultKey, TResultValue>() : new SynchronizedObservableDictionary<TResultKey, TResultValue>(keyEqualityComparer);
                        break;
                }

                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                var resultsAndFaults = rangeActiveExpression.GetResultsAndFaults();
                nullKeys = resultsAndFaults.Count(rfc => rfc.result.Key is null);
                var distinctResultsAndFaults = resultsAndFaults.Where(rfc => rfc.result.Key is { }).GroupBy(rfc => rfc.result.Key).ToList();
                rangeObservableDictionary.AddRange(distinctResultsAndFaults.Select(g => g.First().result));
                foreach (var (key, duplicateCount) in distinctResultsAndFaults.Select(g => (key: g.Key, duplicateCount: g.Count() - 1)).Where(kc => kc.duplicateCount > 0))
                    duplicateKeys.Add(key, duplicateCount);
                activeDictionary = new ActiveDictionary<TResultKey, TResultValue>(rangeObservableDictionary, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.DictionaryChanged -= rangeActiveExpressionChanged;
                    rangeActiveExpression.ValueResultChanged -= valueResultChanged;
                    rangeActiveExpression.ValueResultChanging -= valueResultChanging;
                    rangeActiveExpression.Dispose();
                });
                rangeActiveExpression.DictionaryChanged += rangeActiveExpressionChanged;
                rangeActiveExpression.ValueResultChanged += valueResultChanged;
                rangeActiveExpression.ValueResultChanging += valueResultChanging;
                checkOperationFault();
                return activeDictionary;
            });
        }

        #endregion ToActiveDictionary

        #region ToActiveEnumerable

        /// <summary>
        /// Converts the values of an <see cref="IReadOnlyDictionary{TKey, TValue}"/> into an <see cref="IActiveEnumerable{TElement}"/>
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IReadOnlyDictionary{TKey, TValue}"/> to convert</param>
        /// <returns>An <see cref="IActiveEnumerable{TElement}"/> equivalent to the values of <paramref name="source"/> (and mutates with it so long as <paramref name="source"/> implements <see cref="INotifyDictionaryChanged{TKey, TValue}"/>)</returns>
        public static IActiveEnumerable<TValue> ToActiveEnumerable<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
            ActiveSelect(source, (key, value) => value);

        #endregion ToActiveEnumerable

        #region ValueFor

        /// <summary>
        /// Actively returns the value for a specified key in a dictionary
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">>An <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return a value from</param>
        /// <param name="key">The key of the value to retrieve</param>
        /// <returns>>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is the value for the specified key in the source dictionary</returns>
        public static IActiveValue<TValue> ActiveValueFor<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, TKey key)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                ActiveValue<TValue>? activeValue = null;

                Func<TKey, bool> equalsKey;
                var keyComparer = source.GetKeyComparer();
                if (keyComparer is { })
                    equalsKey = otherKey => keyComparer.Compare(otherKey, key) == 0;
                else 
                {
                    var keyEqualityComparer = source.GetKeyEqualityComparer() ?? EqualityComparer<TKey>.Default;
                    equalsKey = otherKey => keyEqualityComparer.Equals(otherKey, key);
                }

                void dispose() => changingSource.DictionaryChanged -= sourceChanged;

                void sourceChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        if (e.Action == NotifyDictionaryChangedAction.Reset)
                        {
                            try
                            {
                                activeValue!.OperationFault = null;
                                activeValue!.Value = source[key];
                            }
                            catch (Exception ex)
                            {
                                activeValue!.OperationFault = ex;
                            }
                        }
                        else
                        {
                            if (e.OldItems?.Any(kv => equalsKey(kv.Key)) ?? false)
                            {
                                activeValue!.OperationFault = ExceptionHelper.KeyNotFound;
                                activeValue!.Value = default;
                            }
                            if ((e.NewItems?.Count ?? 0) > 0)
                            {
                                var matchingValues = e.NewItems.Where(kv => equalsKey(kv.Key)).Select(kv => kv.Value).ToList();
                                if (matchingValues.Count > 0)
                                {
                                    activeValue!.OperationFault = null;
                                    activeValue!.Value = matchingValues[0];
                                }
                            }
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    try
                    {
                        activeValue = new ActiveValue<TValue>(source[key], elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                        changingSource.DictionaryChanged += sourceChanged;
                        return activeValue;
                    }
                    catch (Exception ex)
                    {
                        activeValue = new ActiveValue<TValue>(default!, ex, elementFaultChangeNotifier, dispose);
                        changingSource.DictionaryChanged += sourceChanged;
                        return activeValue;
                    }
                });
            }
            try
            {
                return new ActiveValue<TValue>(source[key], elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<TValue>(default!, ex, elementFaultChangeNotifier);
            }
        }

        #endregion ValueFor

        #region ValueForOrDefault

        /// <summary>
        /// Actively returns the value for a specified key in a dictionary or a default value if the key is not in the dictionary
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">>An <see cref="IReadOnlyDictionary{TKey, TValue}"/> to return a value from</param>
        /// <param name="key">The key of the value to retrieve</param>
        /// <returns>>An <see cref="IActiveValue{TValue}"/> the <see cref="IActiveValue{TValue}.Value"/> of which is <c>default</c>(<typeparamref name="TValue"/>) if the key is not in the source dictionary; otherwise, the value for the specified key in the source dictionary</returns>
        public static IActiveValue<TValue> ActiveValueForOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, TKey key)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                ActiveValue<TValue>? activeValue = null;

                Func<TKey, bool> equalsKey;
                var keyComparer = source.GetKeyComparer();
                if (keyComparer is { })
                    equalsKey = otherKey => keyComparer.Compare(otherKey, key) == 0;
                else
                {
                    var keyEqualityComparer = source.GetKeyEqualityComparer() ?? EqualityComparer<TKey>.Default;
                    equalsKey = otherKey => keyEqualityComparer.Equals(otherKey, key);
                }

                void dispose() => changingSource.DictionaryChanged -= sourceChanged;

                void sourceChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        if (e.Action == NotifyDictionaryChangedAction.Reset)
                            activeValue!.Value = source.TryGetValue(key, out var value) ? value : default;
                        else
                        {
                            if (e.OldItems?.Any(kv => equalsKey(kv.Key)) ?? false)
                                activeValue!.Value = default;
                            if ((e.NewItems?.Count ?? 0) > 0)
                            {
                                var matchingValues = e.NewItems.Where(kv => equalsKey(kv.Key)).Select(kv => kv.Value).ToList();
                                if (matchingValues.Count > 0)
                                    activeValue!.Value = matchingValues[0];
                            }
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    activeValue = new ActiveValue<TValue>(source.TryGetValue(key, out var value) ? value : default, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                    changingSource.DictionaryChanged += sourceChanged;
                    return activeValue;
                });
            }
            else
                return new ActiveValue<TValue>(source.TryGetValue(key, out var value) ? value : default, elementFaultChangeNotifier: elementFaultChangeNotifier);
        }

        #endregion ValueForOrDefault

        #region Where

        /// <summary>
        /// Actively filters a dictionary based on a predicate
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IReadOnlyDictionary{TKey, TValue}"/> to filter</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> that contains elements from the input dictionary that satisfy the condition</returns>
        public static IActiveDictionary<TKey, TValue> ActiveWhere<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate) =>
            ActiveWhere(source, predicate, null);

        /// <summary>
        /// Actively filters a dictionary based on a predicate
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values in <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IReadOnlyDictionary{TKey, TValue}"/> to filter</param>
        /// <param name="predicate">A function to test each key/value pair for a condition</param>
        /// <param name="predicateOptions">Options governing the behavior of active expressions created using <paramref name="predicate"/></param>
        /// <returns>An <see cref="ActiveDictionary{TKey, TValue}"/> that contains elements from the input dictionary that satisfy the condition</returns>
        public static IActiveDictionary<TKey, TValue> ActiveWhere<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, ActiveExpressionOptions? predicateOptions)
        {
            ActiveQueryOptions.Optimize(ref predicate);

            var synchronizedSource = source as ISynchronized;
            ReadOnlyDictionaryRangeActiveExpression<TKey, TValue, bool> rangeActiveExpression;
            ISynchronizedObservableRangeDictionary<TKey, TValue>? rangeObservableDictionary = null;

            void rangeActiveExpressionChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, bool> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Action == NotifyDictionaryChangedAction.Reset)
                    {
                        var newDictionary = source.CreateSimilarDictionary();
                        foreach (var result in rangeActiveExpression.GetResults().Where(r => r.result))
                            newDictionary.Add(result.key, source[result.key]);
                        rangeObservableDictionary!.Reset(newDictionary);
                    }
                    else
                    {
                        if ((e.OldItems?.Count ?? 0) > 0)
                            rangeObservableDictionary!.RemoveRange(e.OldItems.Where(kv => kv.Value).Select(kv => kv.Key));
                        if ((e.NewItems?.Count ?? 0) > 0)
                            rangeObservableDictionary!.AddRange(e.NewItems.Where(kv => kv.Value).Select(kv =>
                            {
                                var key = kv.Key;
                                return new KeyValuePair<TKey, TValue>(key, source[key]);
                            }));
                    }
                });

            void valueResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TKey, bool> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Element is TKey key)
                    {
                        if (e.Result)
                            rangeObservableDictionary!.Add(key, source[key]);
                        else
                            rangeObservableDictionary!.Remove(key);
                    }
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, predicate, predicateOptions);
                rangeObservableDictionary = source.CreateSimilarSynchronizedObservableDictionary();
                rangeObservableDictionary.AddRange(rangeActiveExpression.GetResults().Where(r => r.result).Select(r => new KeyValuePair<TKey, TValue>(r.key, source[r.key])));
                rangeActiveExpression.DictionaryChanged += rangeActiveExpressionChanged;
                rangeActiveExpression.ValueResultChanged += valueResultChanged;
                return new ActiveDictionary<TKey, TValue>(rangeObservableDictionary, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.DictionaryChanged -= rangeActiveExpressionChanged;
                    rangeActiveExpression.ValueResultChanged -= valueResultChanged;
                    rangeActiveExpression.Dispose();
                });
            });
        }

        #endregion Where
    }
}
