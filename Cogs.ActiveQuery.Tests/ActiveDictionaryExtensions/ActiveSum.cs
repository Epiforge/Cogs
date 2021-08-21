namespace Cogs.ActiveQuery.Tests.ActiveDictionaryExtensions;

[TestClass]
public class ActiveSum
{
    [TestMethod]
    public void ExpressionlessSourceManipulation()
    {
        var numbers = new ObservableDictionary<int, int>();
        using var aggregate = numbers.ActiveSum();
        Assert.IsNull(aggregate.OperationFault);
        Assert.AreEqual(0, aggregate.Value);
        numbers.Add(1, 1);
        Assert.IsNull(aggregate.OperationFault);
        Assert.AreEqual(1, aggregate.Value);
        numbers.AddRange(System.Linq.Enumerable.Range(2, 3).ToDictionary(i => i));
        Assert.AreEqual(10, aggregate.Value);
        numbers.RemoveRange(new int[] { 1, 2 });
        Assert.AreEqual(7, aggregate.Value);
        numbers.Remove(3);
        Assert.AreEqual(4, aggregate.Value);
        numbers.Reset(System.Linq.Enumerable.Range(1, 3).ToDictionary(i => i));
        Assert.AreEqual(6, aggregate.Value);
    }

    [TestMethod]
    public void SourceManipulation()
    {
        var people = TestPerson.CreatePeopleDictionary();
        using var aggregate = people.ActiveSum((key, value) => value.Name!.Length);
        Assert.IsNull(aggregate.OperationFault);
        Assert.AreEqual(74, aggregate.Value);
        people.Add(people.Count, people[0]);
        Assert.AreEqual(78, aggregate.Value);
        people[0].Name = "Johnny";
        Assert.AreEqual(82, aggregate.Value);
    }
}
