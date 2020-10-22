using System.Collections.Generic;

namespace Cogs.Collections
{
    /// <summary>
    /// Represents a keyed data structure that uses an <see cref="IComparer{T}"/> to sort keys
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the keyed data structure</typeparam>
    public interface ISortKeys<TKey>
    {
        /// <summary>
        /// Gets the <see cref="IComparer{TKey}"/> that is used to sort keys
        /// </summary>
        IComparer<TKey> Comparer { get; }
    }
}
