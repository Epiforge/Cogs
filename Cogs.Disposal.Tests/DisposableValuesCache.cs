
namespace Cogs.Disposal.Tests;

[TestClass]
public class DisposableValuesCache
{
    [TestMethod]
    public async Task PrimesAsync()
    {
        var cache = new DisposableValuesCache<int, PrimesToInt>();
        var itemTasks = Enumerable.Range(0, 1000).Select(i => Task.Run(() => cache[i])).ToList();
        await Task.WhenAll(itemTasks);
        var items = itemTasks.Select(itemTask => itemTask.Result).ToList();
        var disposalTasks = items.Select(item => Task.Run(() => item.Dispose()));
        await Task.WhenAll(disposalTasks);
        var cacheCount = cache.Count;
        while (cacheCount > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(0.1));
            cacheCount = cache.Count;
        }
    }
}
