namespace Cogs.Threading;

/// <summary>
/// Provides a set of static methods for quickly leveraging the Microsoft Dataflow library
/// </summary>
public static class DataflowExtensions
{
    static readonly ExecutionDataflowBlockOptions cpuBoundBlock = new() { MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded };
    static readonly DataflowLinkOptions propagateLink = new() { PropagateCompletion = true };
    static readonly ExecutionDataflowBlockOptions singleThreadBlock = new() { MaxDegreeOfParallelism = 1 };

    /// <summary>
    /// Performs a specified action for each element of a sequence in unbounded parallel
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the sequence</typeparam>
    /// <param name="source">The sequence</param>
    /// <param name="action">The action</param>
    public static Task DataflowForAllAsync<TSource>(this IEnumerable<TSource> source, Action<TSource> action) =>
        DataflowForAllAsync(source, action, cpuBoundBlock);

    /// <summary>
    /// Performs a specified asynchronous action for each element of a sequence in unbounded parallel
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the sequence</typeparam>
    /// <param name="source">The sequence</param>
    /// <param name="asyncAction">The asynchronous action</param>
    public static Task DataflowForAllAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, Task> asyncAction) =>
        DataflowForAllAsync(source, asyncAction, cpuBoundBlock);

    /// <summary>
    /// Performs a specified action for each element of a sequence in parallel
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the sequence</typeparam>
    /// <param name="source">The sequence</param>
    /// <param name="action">The action</param>
    /// <param name="options">Manual Dataflow options</param>
    public static Task DataflowForAllAsync<TSource>(this IEnumerable<TSource> source, Action<TSource> action, ExecutionDataflowBlockOptions options)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        var block = new ActionBlock<TSource>(action, options);
        if (source is IList<TSource> sourceList)
            for (int i = 0, ii = sourceList.Count; i < ii; ++i)
                block.Post(sourceList[i]);
        else
            foreach (var element in source)
                block.Post(element);
        block.Complete();
        return block.Completion;
    }

    /// <summary>
    /// Performs a specified asynchronous action for each element of a sequence in parallel
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the sequence</typeparam>
    /// <param name="source">The sequence</param>
    /// <param name="asyncAction">The asynchronous action</param>
    /// <param name="options">Manual Dataflow options</param>
    public static Task DataflowForAllAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, Task> asyncAction, ExecutionDataflowBlockOptions options)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        var block = new ActionBlock<TSource>(asyncAction, options);
        if (source is IList<TSource> sourceList)
            for (int i = 0, ii = sourceList.Count; i < ii; ++i)
                block.Post(sourceList[i]);
        else
            foreach (var element in source)
                block.Post(element);
        block.Complete();
        return block.Completion;
    }

    /// <summary>
    /// Performs a specified transform on each element of a sequence in unbounded parallel
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the sequence</typeparam>
    /// <typeparam name="TResult">The type rendered by the transform</typeparam>
    /// <param name="source">The sequence</param>
    /// <param name="selector">The transform</param>
    /// <returns>The results of the transform on each element</returns>
    public static Task<IReadOnlyList<TResult>> DataflowSelectAsync<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) =>
        DataflowSelectAsync(source, selector, cpuBoundBlock);

    /// <summary>
    /// Performs a specified asynchronous transform on each element of a sequence in unbounded parallel
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the sequence</typeparam>
    /// <typeparam name="TResult">The type rendered by the asynchronous transform</typeparam>
    /// <param name="source">The sequence</param>
    /// <param name="asyncSelector">The asynchronous transform</param>
    /// <returns>The results of the asynchronous transform on each element</returns>
    public static Task<IReadOnlyList<TResult>> DataflowSelectAsync<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Task<TResult>> asyncSelector) =>
        DataflowSelectAsync(source, asyncSelector, cpuBoundBlock);

    /// <summary>
    /// Performs a specified transform on each element of a sequence in parallel
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the sequence</typeparam>
    /// <typeparam name="TResult">The type rendered by the transform</typeparam>
    /// <param name="source">The sequence</param>
    /// <param name="selector">The transform</param>
    /// <param name="options">Manual Dataflow options</param>
    /// <returns>The results of the transform on each element</returns>
    public static async Task<IReadOnlyList<TResult>> DataflowSelectAsync<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, ExecutionDataflowBlockOptions options)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        var results = new List<TResult>();
        var transformBlock = new TransformBlock<TSource, TResult>(selector, options);
        var actionBlock = new ActionBlock<TResult>(result => results.Add(result), singleThreadBlock);
        transformBlock.LinkTo(actionBlock, propagateLink);
        if (source is IList<TSource> sourceList)
            for (int i = 0, ii = sourceList.Count; i < ii; ++i)
                transformBlock.Post(sourceList[i]);
        else
            foreach (var element in source)
                transformBlock.Post(element);
        transformBlock.Complete();
        await actionBlock.Completion.ConfigureAwait(false);
        return results.ToImmutableArray();
    }

    /// <summary>
    /// Performs a specified asynchronous transform on each element of a sequence in parallel
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the sequence</typeparam>
    /// <typeparam name="TResult">The type rendered by the asynchronous transform</typeparam>
    /// <param name="source">The sequence</param>
    /// <param name="asyncSelector">The asynchronous transform</param>
    /// <param name="options">Manual Dataflow options</param>
    /// <returns>The results of the asynchronous transform on each element</returns>
    public static async Task<IReadOnlyList<TResult>> DataflowSelectAsync<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Task<TResult>> asyncSelector, ExecutionDataflowBlockOptions options)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        var results = new List<TResult>();
        var transformBlock = new TransformBlock<TSource, TResult>(asyncSelector, options);
        var actionBlock = new ActionBlock<TResult>(result => results.Add(result), singleThreadBlock);
        transformBlock.LinkTo(actionBlock, propagateLink);
        if (source is IList<TSource> sourceList)
            for (int i = 0, ii = sourceList.Count; i < ii; ++i)
                transformBlock.Post(sourceList[i]);
        else
            foreach (var element in source)
                transformBlock.Post(element);
        transformBlock.Complete();
        await actionBlock.Completion.ConfigureAwait(false);
        return results.ToImmutableArray();
    }
}
