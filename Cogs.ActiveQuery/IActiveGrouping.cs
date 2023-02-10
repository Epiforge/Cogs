namespace Cogs.ActiveQuery;

/// <summary>
/// Represents a collection of objects that have a common key
/// </summary>
/// <typeparam name="TKey">The type of the key of the <see cref="IActiveGrouping{TKey, TElement}"/></typeparam>
/// <typeparam name="TElement">The type of the values of the <see cref="IActiveGrouping{TKey, TElement}"/></typeparam>
public interface IActiveGrouping<out TKey, out TElement> :
    INondisposableActiveEnumerable<TElement>
{
    /// <summary>
    /// The key of the <see cref="IActiveGrouping{TKey, TElement}"/>
    /// </summary>
    TKey Key { get; }

    internal void DisposeInternal();
}