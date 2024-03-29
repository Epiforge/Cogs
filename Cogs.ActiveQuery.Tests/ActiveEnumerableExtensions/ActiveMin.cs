namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions;

[TestClass]
public class ActiveMin
{
    [TestMethod]
    public void ExpressionlessSourceManipulation()
    {
        var numbers = new SynchronizedRangeObservableCollection<int>();
        using var aggregate = numbers.ActiveMin();
        Assert.IsNotNull(aggregate.OperationFault);
        Assert.AreEqual(0, aggregate.Value);
        numbers.Add(1);
        Assert.IsNull(aggregate.OperationFault);
        Assert.AreEqual(1, aggregate.Value);
        numbers.AddRange(System.Linq.Enumerable.Range(2, 3));
        Assert.AreEqual(1, aggregate.Value);
        numbers.RemoveRange(0, 2);
        Assert.AreEqual(3, aggregate.Value);
        numbers.RemoveAt(0);
        Assert.AreEqual(4, aggregate.Value);
        numbers.Reset(System.Linq.Enumerable.Range(1, 3));
        Assert.AreEqual(1, aggregate.Value);
    }

    [TestMethod]
    public void SourceManipulation()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var aggregate = people.ActiveMin(p => p.Name!.Length);
        Assert.IsNull(aggregate.OperationFault);
        Assert.AreEqual(3, aggregate.Value);
        people.Add(people[0]);
        Assert.AreEqual(3, aggregate.Value);
        people[0].Name = "J";
        Assert.AreEqual(1, aggregate.Value);
        people[0].Name = "John";
        Assert.AreEqual(3, aggregate.Value);
    }
}
