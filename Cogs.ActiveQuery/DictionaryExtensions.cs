using Cogs.Collections;
using Cogs.Collections.Synchronized;
using System.Collections.Generic;
using System.Threading;

namespace Gear.ActiveQuery
{
    static class DictionaryExtensions
    {
        static IDictionary<TKey, TResultValue> CreateDictionary<TKey, TSourceValue, TResultValue>(IndexingStrategy? indexingStrategy = null, IEqualityComparer<TKey>? keyEqualityComparer = null, IComparer<TKey>? keyComparer = null) =>
            indexingStrategy switch
            {
                IndexingStrategy.SelfBalancingBinarySearchTree => keyComparer != null ? new SortedDictionary<TKey, TResultValue>(keyComparer) : new SortedDictionary<TKey, TResultValue>(),
                _ => keyEqualityComparer != null ? new Dictionary<TKey, TResultValue>(keyEqualityComparer) : new Dictionary<TKey, TResultValue>(),
            };

        public static IDictionary<TKey, TValue> CreateSimilarDictionary<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnlyDictionary) => CreateSimilarDictionary<TKey, TValue, TValue>(readOnlyDictionary);

        public static IDictionary<TKey, TResultValue> CreateSimilarDictionary<TKey, TSourceValue, TResultValue>(this IReadOnlyDictionary<TKey, TSourceValue> readOnlyDictionary)
        {
            var indexingStrategy = GetIndexingStrategy(readOnlyDictionary);
            return indexingStrategy switch
            {
                IndexingStrategy.SelfBalancingBinarySearchTree => CreateDictionary<TKey, TSourceValue, TResultValue>(indexingStrategy, keyComparer: GetKeyComparer(readOnlyDictionary)),
                _ => CreateDictionary<TKey, TSourceValue, TResultValue>(indexingStrategy, keyEqualityComparer: GetKeyEqualityComparer(readOnlyDictionary)),
            };
        }

        public static ISynchronizedObservableRangeDictionary<TKey, TValue> CreateSimilarSynchronizedObservableDictionary<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnlyDictionary) => CreateSimilarSynchronizedObservableDictionary(readOnlyDictionary, SynchronizationContext.Current);

        public static ISynchronizedObservableRangeDictionary<TKey, TValue> CreateSimilarSynchronizedObservableDictionary<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnlyDictionary, SynchronizationContext synchronizationContext) => CreateSimilarSynchronizedObservableDictionary<TKey, TValue, TValue>(readOnlyDictionary, synchronizationContext);

        public static ISynchronizedObservableRangeDictionary<TKey, TResultValue> CreateSimilarSynchronizedObservableDictionary<TKey, TSourceValue, TResultValue>(this IReadOnlyDictionary<TKey, TSourceValue> readOnlyDictionary, SynchronizationContext synchronizationContext)
        {
            var indexingStrategy = GetIndexingStrategy(readOnlyDictionary);
            return indexingStrategy switch
            {
                IndexingStrategy.SelfBalancingBinarySearchTree => CreateSynchronizedObservableDictionary<TKey, TSourceValue, TResultValue>(synchronizationContext, indexingStrategy, keyComparer: GetKeyComparer(readOnlyDictionary)),
                _ => CreateSynchronizedObservableDictionary<TKey, TSourceValue, TResultValue>(synchronizationContext, indexingStrategy, keyEqualityComparer: GetKeyEqualityComparer(readOnlyDictionary)),
            };
        }

        static ISynchronizedObservableRangeDictionary<TKey, TResultValue> CreateSynchronizedObservableDictionary<TKey, TSourceValue, TResultValue>(SynchronizationContext synchronizationContext, IndexingStrategy? indexingStrategy = null, IEqualityComparer<TKey>? keyEqualityComparer = null, IComparer<TKey>? keyComparer = null) =>
            indexingStrategy switch
            {
                IndexingStrategy.SelfBalancingBinarySearchTree when keyComparer != null => keyComparer != null ? new SynchronizedObservableSortedDictionary<TKey, TResultValue>(synchronizationContext, keyComparer) : new SynchronizedObservableSortedDictionary<TKey, TResultValue>(synchronizationContext),
                _ => keyEqualityComparer != null ? new SynchronizedObservableDictionary<TKey, TResultValue>(synchronizationContext, keyEqualityComparer) : new SynchronizedObservableDictionary<TKey, TResultValue>(synchronizationContext),
            };

        public static IndexingStrategy? GetIndexingStrategy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnlyDictionary) =>
            readOnlyDictionary switch
            {
                ActiveDictionary<TKey, TValue> activeDictionary => activeDictionary.IndexingStrategy,
                Dictionary<TKey, TValue> _ => IndexingStrategy.HashTable,
                ObservableDictionary<TKey, TValue> _ => IndexingStrategy.HashTable,
                SortedDictionary<TKey, TValue> _ => IndexingStrategy.SelfBalancingBinarySearchTree,
                ObservableSortedDictionary<TKey, TValue> _ => IndexingStrategy.SelfBalancingBinarySearchTree,
                _ => null
            };

        public static IEqualityComparer<TKey>? GetKeyEqualityComparer<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnlyDictionary, bool attemptCoercion = true) =>
            readOnlyDictionary switch
            {
                ActiveDictionary<TKey, TValue> activeDictionary when activeDictionary.IndexingStrategy == IndexingStrategy.HashTable => activeDictionary.EqualityComparer,
                SynchronizedObservableDictionary<TKey, TValue> synchronizedObservable => synchronizedObservable.Comparer,
                ObservableDictionary<TKey, TValue> observable => observable.Comparer,
                Dictionary<TKey, TValue> standard => standard.Comparer,
                _ => null
            } ?? (attemptCoercion && GetKeyComparer(readOnlyDictionary, false) is IEqualityComparer<TKey> coercedEqualityComparer ? coercedEqualityComparer : null);

        public static IComparer<TKey>? GetKeyComparer<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnlyDictionary, bool attemptCoercion = true) =>
            readOnlyDictionary switch
            {
                ActiveDictionary<TKey, TValue> activeDictionary when activeDictionary.IndexingStrategy == IndexingStrategy.SelfBalancingBinarySearchTree => activeDictionary.Comparer,
                SynchronizedObservableSortedDictionary<TKey, TValue> synchronizedObservable => synchronizedObservable.Comparer,
                ObservableSortedDictionary<TKey, TValue> observable => observable.Comparer,
                SortedDictionary<TKey, TValue> standard => standard.Comparer,
                _ => attemptCoercion && GetKeyEqualityComparer(readOnlyDictionary, false) is IComparer<TKey> coercedComparer ? coercedComparer : null,
            };
    }
}
