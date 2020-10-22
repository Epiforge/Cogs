using System.Collections.Generic;

namespace Cogs.Collections
{
    /// <summary>
    /// Represents a keyed data structure that uses an <see cref="IEqualityComparer{T}"/> to determine equality of keys
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the keyed data structure</typeparam>
    public interface IHashKeys<TKey>
    {
        /// <summary>
        /// Gets the <see cref="IEqualityComparer{TKey}"/> that is used to determine equality of keys
        /// </summary>
        IEqualityComparer<TKey> Comparer { get; }
    }
}
