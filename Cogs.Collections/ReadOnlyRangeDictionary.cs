using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Cogs.Collections
{
    /// <summary>
    /// Read-only wrapper around an <see cref="IRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the read-only dictionary</typeparam>
    /// <typeparam name="TValue">The type of values in the read-only dictionary</typeparam>
    [SuppressMessage("Code Analysis", "CA1033: Interface methods should be callable by child types")]
    public class ReadOnlyRangeDictionary<TKey, TValue> : ReadOnlyDictionary<TKey, TValue>, IRangeDictionary<TKey, TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyRangeDictionary{TKey, TValue}"/> class
        /// </summary>
        /// <param name="rangeDictionary">The <see cref="IRangeDictionary{TKey, TValue}"/> around which to wrap</param>
        public ReadOnlyRangeDictionary(IRangeDictionary<TKey, TValue> rangeDictionary) : base(rangeDictionary) => this.rangeDictionary = rangeDictionary;

        readonly IRangeDictionary<TKey, TValue> rangeDictionary;

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => throw new NotSupportedException();

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException();
        
        void IRangeDictionary<TKey, TValue>.AddRange(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs) => throw new NotSupportedException();

        void IRangeDictionary<TKey, TValue>.AddRange(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs) => throw new NotSupportedException();

        void ICollection<KeyValuePair<TKey, TValue>>.Clear() => throw new NotSupportedException();

        /// <summary>
        /// Determines whether the read-only range dictionary contains a key/value pair
        /// </summary>
        /// <param name="item">The key/value pair to locate in the read-only range dictionary</param>
        /// <returns><c>true</c> if the item is found in the read-only range dictionary; otherwise, <c>false</c></returns>
        public bool Contains(KeyValuePair<TKey, TValue> item) => rangeDictionary.Contains(item);

        /// <summary>
        /// Copies the elements of the read-only range dictionary to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from read-only range dictionary (the <see cref="Array"/> must have zero-based indexing)</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => rangeDictionary.CopyTo(array, arrayIndex);

        /// <summary>
        /// Gets the elements with the specified keys
        /// </summary>
        /// <param name="keys">The keys of the elements to get</param>
        /// <returns>The elements with the specified keys</returns>
        public IReadOnlyList<KeyValuePair<TKey, TValue>> GetRange(IEnumerable<TKey> keys) => rangeDictionary.GetRange(keys);

        bool IDictionary<TKey, TValue>.Remove(TKey key) => throw new NotSupportedException();

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException();

        IReadOnlyList<KeyValuePair<TKey, TValue>> IRangeDictionary<TKey, TValue>.RemoveAll(Func<TKey, TValue, bool> predicate) => throw new NotSupportedException();

        IReadOnlyList<TKey> IRangeDictionary<TKey, TValue>.RemoveRange(IEnumerable<TKey> keys) => throw new NotSupportedException();

        void IRangeDictionary<TKey, TValue>.ReplaceRange(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs) => throw new NotSupportedException();

        IReadOnlyList<TKey> IRangeDictionary<TKey, TValue>.ReplaceRange(IEnumerable<TKey> removeKeys, IEnumerable<KeyValuePair<TKey, TValue>> newKeyValuePairs) => throw new NotSupportedException();

        void IRangeDictionary<TKey, TValue>.Reset() => throw new NotSupportedException();

        void IRangeDictionary<TKey, TValue>.Reset(IDictionary<TKey, TValue> dictionary) => throw new NotSupportedException();

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => throw new NotSupportedException();

        ICollection<TValue> IDictionary<TKey, TValue>.Values => throw new NotSupportedException();

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => true;

        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get => rangeDictionary[key];
            set => throw new NotSupportedException();
        }

        TValue IRangeDictionary<TKey, TValue>.this[TKey key]
        {
            get => rangeDictionary[key];
            set => throw new NotSupportedException();
        }
    }
}
