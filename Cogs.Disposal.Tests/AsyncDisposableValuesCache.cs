namespace Cogs.Disposal.Tests;

[TestClass]
public class AsyncDisposableValuesCache
{
    [TestMethod]
    public async Task PrimesAsync()
    {
        var cache = new AsyncDisposableValuesCache<int, AsyncPrimesToInt>();
        var itemTasks = Enumerable.Range(0, 1000).Select(i => Task.Run(async () => await cache.GetAsync(i).ConfigureAwait(false))).ToList();
        await Task.WhenAll(itemTasks);
        var items = itemTasks.Select(itemTask => itemTask.Result).ToList();
        var disposalTasks = items.Select(item => Task.Run(async () => await item.DisposeAsync().ConfigureAwait(false)));
        await Task.WhenAll(disposalTasks);
        Assert.AreEqual(0, cache.Count);
    }
}
