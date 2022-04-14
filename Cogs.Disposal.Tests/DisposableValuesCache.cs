
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
        Assert.AreEqual(0, cache.Count);
    }
}
