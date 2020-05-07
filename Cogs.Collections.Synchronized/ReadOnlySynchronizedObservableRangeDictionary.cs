using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cogs.Collections.Synchronized
{
    /// <summary>
    /// Read-only wrapper around a <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/>
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the read-only dictionary</typeparam>
    /// <typeparam name="TValue">The type of values in the read-only dictionary</typeparam>
    public class ReadOnlySynchronizedObservableRangeDictionary<TKey, TValue> : ReadOnlyObservableRangeDictionary<TKey, TValue>, ISynchronizedObservableRangeDictionary<TKey, TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlySynchronizedObservableRangeDictionary{TKey, TValue}"/> class
        /// </summary>
        /// <param name="synchronizedObservableRangeDictionary">The <see cref="ISynchronizedObservableRangeDictionary{TKey, TValue}"/> around which to wrap</param>
        public ReadOnlySynchronizedObservableRangeDictionary(ISynchronizedObservableRangeDictionary<TKey, TValue> synchronizedObservableRangeDictionary) : base(synchronizedObservableRangeDictionary) => this.synchronizedObservableRangeDictionary = synchronizedObservableRangeDictionary;

        readonly ISynchronizedObservableRangeDictionary<TKey, TValue> synchronizedObservableRangeDictionary;

        /// <summary>
        /// Gets the number of key-value pairs contained in the read-only synchronized observable range dictionary
        /// </summary>
        public Task<int> CountAsync => synchronizedObservableRangeDictionary.CountAsync;

        /// <summary>
        /// Gets the <see cref="System.Threading.SynchronizationContext"/> on which this object's operations occur
        /// </summary>
        public SynchronizationContext? SynchronizationContext => synchronizedObservableRangeDictionary.SynchronizationContext;

        Task ISynchronizedObservableRangeDictionary<TKey, TValue>.AddAsync(TKey key, TValue value) => Task.FromException(new NotSupportedException());

        Task ISynchronizedObservableRangeDictionary<TKey, TValue>.AddRangeAsync(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs) => Task.FromException(new NotSupportedException());

        Task ISynchronizedObservableRangeDictionary<TKey, TValue>.AddRangeAsync(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs) => Task.FromException(new NotSupportedException());

        Task ISynchronizedObservableRangeDictionary<TKey, TValue>.ClearAsync() => Task.FromException(new NotSupportedException());

        /// <summary>
        /// Determines whether the read-only synchronized observable range dictionary contains an element with the specified key
        /// </summary>
        /// <param name="key">The key to locate in the read-only synchronized observable range dictionary</param>
        /// <returns><c>true</c> if the read-only synchronized observable range dictionary contains an element with the key; otherwise, <c>false</c></returns>
        public Task<bool> ContainsKeyAsync(TKey key) => synchronizedObservableRangeDictionary.ContainsKeyAsync(key);

        /// <summary>
        /// Determines whether the read-only synchronized observable range dictionary contains an element with the specified value
        /// </summary>
        /// <param name="value">The value to locate in the read-only synchronized observable range dictionary</param>
        /// <returns><c>true</c> if the read-only synchronized observable range dictionary contains an element with the value; otherwise, <c>false</c></returns>
        public Task<bool> ContainsValueAsync(TValue value) => synchronizedObservableRangeDictionary.ContainsValueAsync(value);

        /// <summary>
        /// Copies all keys in the read-only synchronized observable range dictionary into a <see cref="IReadOnlyList{T}"/>
        /// </summary>
        public IReadOnlyList<TKey> GetAllKeys() => synchronizedObservableRangeDictionary.GetAllKeys();

        /// <summary>
        /// Copies all keys in the read-only synchronized observable range dictionary into a <see cref="IReadOnlyList{T}"/>
        /// </summary>
        public Task<IReadOnlyList<TKey>> GetAllKeysAsync() => synchronizedObservableRangeDictionary.GetAllKeysAsync();

        /// <summary>
        /// Gets the elements with the specified keys
        /// </summary>
        /// <param name="keys">The keys of the elements to get</param>
        /// <returns>The elements with the specified keys</returns>
        public Task<IReadOnlyList<KeyValuePair<TKey, TValue>>> GetRangeAsync(IEnumerable<TKey> keys) => synchronizedObservableRangeDictionary.GetRangeAsync(keys);

        /// <summary>
        /// Gets the element with the specified key
        /// </summary>
        /// <param name="key">The key of the element to get</param>
        /// <returns>The element with the specified key</returns>
        public Task<TValue> GetValueAsync(TKey key) => synchronizedObservableRangeDictionary.GetValueAsync(key);

        Task<IReadOnlyList<KeyValuePair<TKey, TValue>>> ISynchronizedObservableRangeDictionary<TKey, TValue>.RemoveAllAsync(Func<TKey, TValue, bool> predicate) => Task.FromException<IReadOnlyList<KeyValuePair<TKey, TValue>>>(new NotSupportedException());

        Task<bool> ISynchronizedObservableRangeDictionary<TKey, TValue>.RemoveAsync(TKey key) => Task.FromException<bool>(new NotSupportedException());

        Task<IReadOnlyList<TKey>> ISynchronizedObservableRangeDictionary<TKey, TValue>.RemoveRangeAsync(IEnumerable<TKey> keys) => Task.FromException<IReadOnlyList<TKey>>(new NotSupportedException());

        Task ISynchronizedObservableRangeDictionary<TKey, TValue>.ReplaceRangeAsync(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs) => Task.FromException(new NotSupportedException());

        Task<IReadOnlyList<TKey>> ISynchronizedObservableRangeDictionary<TKey, TValue>.ReplaceRangeAsync(IEnumerable<TKey> removeKeys, IEnumerable<KeyValuePair<TKey, TValue>> newKeyValuePairs) => Task.FromException<IReadOnlyList<TKey>>(new NotSupportedException());

        Task ISynchronizedObservableRangeDictionary<TKey, TValue>.ResetAsync() => Task.FromException(new NotSupportedException());

        Task ISynchronizedObservableRangeDictionary<TKey, TValue>.ResetAsync(IDictionary<TKey, TValue> dictionary) => Task.FromException(new NotSupportedException());

        Task ISynchronizedObservableRangeDictionary<TKey, TValue>.SetValueAsync(TKey key, TValue value) => Task.FromException(new NotSupportedException());

        /// <summary>
        /// Gets the value associated with the specified key
        /// </summary>
        /// <param name="key">The key the value of which to get</param>
        /// <returns><c>true</c> if the read-only synchronized observable range dictionary contains an element with the specified key and the value that was retrieved; otherwise, <c>false</c> and the default <typeparamref name="TValue"/></returns>
        public Task<(bool valueRetrieved, TValue value)> TryGetValueAsync(TKey key) => synchronizedObservableRangeDictionary.TryGetValueAsync(key);
    }
}
