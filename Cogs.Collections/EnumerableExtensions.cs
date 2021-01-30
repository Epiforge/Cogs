using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Cogs.Collections
{
    /// <summary>
    /// Provides extensions for dealing with <see cref="IEnumerable"/> and <see cref="IEnumerable{T}"/>
    /// </summary>
    public static class EnumerableExtensions
    {
        #region Indicies

        /// <summary>
        /// Finds the index of the first element in the source that satisfies the specified predicate
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source</typeparam>
        /// <param name="source">The source</param>
        /// <param name="predicate">The predicate</param>
        /// <returns>The index of the first element that satisfies the predicate; or, <c>-1</c> if none did</returns>
        public static int FindIndex<TSource>(this IEnumerable<TSource> source, Predicate<TSource> predicate)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));
            if (source is TSource[] typedArray)
                return Array.FindIndex(typedArray, predicate);
            if (source is List<TSource> genericList)
                return genericList.FindIndex(predicate);
            var index = -1;
            foreach (var element in source)
            {
                ++index;
                if (predicate(element))
                    return index;
            }
            return -1;
        }

        /// <summary>
        /// Finds the index of the last element in the source that satisfies the specified predicate
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source</typeparam>
        /// <param name="source">The source</param>
        /// <param name="predicate">The predicate</param>
        /// <returns>The index of the last element that satisfies the predicate; or, <c>-1</c> if none did</returns>
        public static int FindLastIndex<TSource>(this IEnumerable<TSource> source, Predicate<TSource> predicate)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));
            if (source is TSource[] typedArray)
                return Array.FindLastIndex(typedArray, predicate);
            if (source is List<TSource> genericList)
                return genericList.FindLastIndex(predicate);
            var index = source.Count();
            foreach (var element in source.Reverse())
            {
                --index;
                if (predicate(element))
                    return index;
            }
            return -1;
        }

        static IEnumerable<int> FindEnumerableIndicies<TSource>(IEnumerable<TSource> source, Predicate<TSource> predicate)
        {
            var index = -1;
            foreach (var element in source)
            {
                ++index;
                if (predicate(element))
                    yield return index;
            }
        }

        static IEnumerable<int> FindTypedIndicies<TSource>(TSource[] typedArray, Predicate<TSource> predicate)
        {
            var index = Array.IndexOf(typedArray, predicate);
            while (index >= 0)
            {
                yield return index;
                index = Array.IndexOf(typedArray, predicate, index + 1);
            }
        }

        static IEnumerable<int> FindTypedIndicies<TSource>(List<TSource> genericList, Predicate<TSource> predicate)
        {
            var index = genericList.FindIndex(predicate);
            while (index >= 0)
            {
                yield return index;
                index = genericList.FindIndex(index + 1, predicate);
            }
        }

        /// <summary>
        /// Finds the indicies of the elements in the source that satisfy the specified predicate
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source</typeparam>
        /// <param name="source">The source</param>
        /// <param name="predicate">The predicate</param>
        /// <returns>The indicies of the elements that satisfy the predicate</returns>
        public static IEnumerable<int> FindIndicies<TSource>(this IEnumerable<TSource> source, Predicate<TSource> predicate)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));
            if (source is TSource[] typedArray)
                return FindTypedIndicies(typedArray, predicate);
            if (source is List<TSource> genericList)
                return FindTypedIndicies(genericList, predicate);
            var equalityComparer = EqualityComparer<TSource>.Default;
            return FindEnumerableIndicies(source, predicate);
        }

        /// <summary>
        /// Finds the first index of the specified item in the source
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source</typeparam>
        /// <param name="source">The source</param>
        /// <param name="item">The item</param>
        /// <returns>The first index of the item; or, <c>-1</c> if it was not found</returns>
        public static int IndexOf<TSource>(this IEnumerable<TSource> source, TSource item)
        {
            if (source is TSource[] typedArray)
                return Array.IndexOf(typedArray, item);
            if (source is IList<TSource> genericListInterface)
                return genericListInterface.IndexOf(item);
            if (source is Array array)
                return Array.IndexOf(array, item);
            if (source is IList listInterface)
                return listInterface.IndexOf(item);
            var equalityComparer = EqualityComparer<TSource>.Default;
            return FindIndex(source, element => equalityComparer.Equals(element, item));
        }

        /// <summary>
        /// Finds the last index of the specified item in the source
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source</typeparam>
        /// <param name="source">The source</param>
        /// <param name="item">The item</param>
        /// <returns>The last index of the item; or, <c>-1</c> if it was not found</returns>
        public static int LastIndexOf<TSource>(this IEnumerable<TSource> source, TSource item)
        {
            if (source is TSource[] typedArray)
                return Array.LastIndexOf(typedArray, item);
            if (source is IList<TSource> genericListInterface)
                return genericListInterface.LastIndexOf(item);
            if (source is Array array)
                return Array.LastIndexOf(array, item);
            var equalityComparer = EqualityComparer<TSource>.Default;
            return FindLastIndex(source, element => equalityComparer.Equals(element, item));
        }

        static IEnumerable<int> TypedIndiciesOf<TSource>(TSource[] typedArray, TSource item)
        {
            var index = Array.IndexOf(typedArray, item);
            while (index >= 0)
            {
                yield return index;
                index = Array.IndexOf(typedArray, item, index + 1);
            }
        }

        static IEnumerable<int> TypedIndiciesOf<TSource>(List<TSource> genericList, TSource item)
        {
            var index = genericList.IndexOf(item);
            while (index >= 0)
            {
                yield return index;
                index = genericList.IndexOf(item, index + 1);
            }
        }

        static IEnumerable<int> TypedIndiciesOf(Array array, object? item)
        {
            var index = Array.IndexOf(array, item);
            while (index >= 0)
            {
                yield return index;
                index = Array.IndexOf(array, item, index + 1);
            }
        }

        /// <summary>
        /// Finds the indicies of the specified item in the source
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source</typeparam>
        /// <param name="source">The source</param>
        /// <param name="item">The item</param>
        /// <returns>The indicies of the item</returns>
        public static IEnumerable<int> IndiciesOf<TSource>(this IEnumerable<TSource> source, TSource item)
        {
            if (source is TSource[] typedArray)
                return TypedIndiciesOf(typedArray, item);
            if (source is List<TSource> genericList)
                return TypedIndiciesOf(genericList, item);
            if (source is Array array)
                return TypedIndiciesOf(array, item);
            var equalityComparer = EqualityComparer<TSource>.Default;
            return FindIndicies(source, element => equalityComparer.Equals(element, item));
        }

        #endregion Indicies

        /// <summary>
        /// Creates an <see cref="IEnumerable{T}"/> that contains a specified number of instances of the item
        /// </summary>
        /// <typeparam name="T">The type of the item</typeparam>
        /// <param name="item">The item</param>
        /// <param name="count">The number of instances</param>
        /// <returns>The <see cref="IEnumerable{T}"/></returns>
        public static IEnumerable<T> Repeat<T>(this T item, int count)
        {
            for (var i = 0; i < count; ++i)
                yield return item;
        }
    }
}
