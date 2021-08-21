namespace Cogs.ActiveQuery;

/// <summary>
/// Represents a generic read-only collection of key-value pairs that is the result of an active query and that cannot be disposed by callers
/// </summary>
/// <typeparam name="TKey">The type of keys</typeparam>
/// <typeparam name="TValue">The type of values</typeparam>
public interface INondisposableActiveDictionary<TKey, TValue> : IDisposalStatus, INotifyDictionaryChanged, INotifyDictionaryChanged<TKey, TValue>, INotifyDisposed, INotifyDisposing, INotifyElementFaultChanges, IReadOnlyDictionary<TKey, TValue>, ISynchronized
{
    /// <summary>
    /// Occurs when the dictionary changes
    /// </summary>
    new event EventHandler<NotifyDictionaryChangedEventArgs<TKey, TValue>>? DictionaryChanged;

    /// <summary>
    /// Gets the <see cref="IComparer{T}"/> in use by the <see cref="IActiveDictionary{TKey, TValue}"/> in order to order keys
    /// </summary>
    IComparer<TKey>? Comparer { get; }

    /// <summary>
    /// Gets the <see cref="IEqualityComparer{T}"/> in use by the <see cref="IActiveDictionary{TKey, TValue}"/> in order to hash and to test keys for equality
    /// </summary>
    IEqualityComparer<TKey>? EqualityComparer { get; }

    /// <summary>
    /// Gets the <see cref="ActiveQuery.IndexingStrategy"/> in use by the <see cref="IActiveDictionary{TKey, TValue}"/>
    /// </summary>
    IndexingStrategy? IndexingStrategy { get; }

    /// <summary>
    /// Gets the exception that occured the most recent time the query updated
    /// </summary>
    Exception? OperationFault { get; }
}
