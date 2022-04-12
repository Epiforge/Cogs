namespace Cogs.ActiveQuery.Tests.ActiveDictionaryExtensions;

[TestClass]
public class SwitchContext
{
    [TestMethod]
    public async Task SourceManipulationAsync()
    {
        var context1 = new AsyncSynchronizationContext();
        var context2 = new AsyncSynchronizationContext();
        var people = TestPerson.CreatePeopleDictionary(context1);
        using var query = people.SwitchContext(context2);
        Assert.AreEqual(14, query.Count);
        people.Add(14, new TestPerson("Daniel"));
        await Task.Delay(250);
        Assert.AreEqual(15, query.Count);
        people[14] = new TestPerson("Javon");
        await Task.Delay(250);
        Assert.AreEqual("Javon", query[14].Name);
        people[0] = people[14];
        await Task.Delay(250);
        Assert.AreEqual("Javon", query[0].Name);
        people.Remove(0);
        await Task.Delay(250);
        Assert.AreEqual(14, query.Count);
        people.Reset(new Dictionary<int, TestPerson> { { 0, new TestPerson("Sarah") } });
        await Task.Delay(250);
        Assert.AreEqual(1, query.Count);
    }

    [TestMethod]
    public async Task SourceManipulationSortedAsync()
    {
        var context1 = new AsyncSynchronizationContext();
        var context2 = new AsyncSynchronizationContext();
        ISynchronizedObservableRangeDictionary<int, TestPerson> people = new SynchronizedObservableSortedDictionary<int, TestPerson>(context1);
        foreach (var kv in TestPerson.MakePeople().Select((person, index) => new KeyValuePair<int, TestPerson>(index, person)))
            people.Add(kv);
        using var query = people.SwitchContext(context2);
        Assert.AreEqual(14, query.Count);
        people.Add(14, new TestPerson("Daniel"));
        await Task.Delay(250);
        Assert.AreEqual(15, query.Count);
        people[14] = new TestPerson("Javon");
        await Task.Delay(250);
        Assert.AreEqual("Javon", query[14].Name);
        people[0] = people[14];
        await Task.Delay(250);
        Assert.AreEqual("Javon", query[0].Name);
        people.Remove(0);
        await Task.Delay(250);
        Assert.AreEqual(14, query.Count);
        people.Reset(new Dictionary<int, TestPerson> { { 0, new TestPerson("Sarah") } });
        await Task.Delay(250);
        Assert.AreEqual(1, query.Count);
    }
}
