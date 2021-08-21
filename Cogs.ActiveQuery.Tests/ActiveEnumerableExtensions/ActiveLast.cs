namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions;

[TestClass]
public class ActiveLast
{
    [TestMethod]
    public void ExpressionlessEmptyNonNotifier()
    {
        var numbers = System.Linq.Enumerable.Empty<int>();
        using var query = numbers.ActiveLast();
        Assert.IsNotNull(query.OperationFault);
        Assert.AreEqual(0, query.Value);
    }

    [TestMethod]
    public void ExpressionlessEmptySource()
    {
        var numbers = new SynchronizedRangeObservableCollection<int>();
        using var query = numbers.ActiveLast();
        Assert.IsNotNull(query.OperationFault);
        Assert.AreEqual(0, query.Value);
    }

    [TestMethod]
    public void ExpressionlessNonNotifier()
    {
        var numbers = System.Linq.Enumerable.Range(0, 10);
        using var query = numbers.ActiveLast();
        Assert.IsNull(query.OperationFault);
        Assert.AreEqual(9, query.Value);
    }

    [TestMethod]
    public void ExpressionlessSourceManipulation()
    {
        var numbers = new SynchronizedRangeObservableCollection<int>(System.Linq.Enumerable.Range(0, 10));
        using var query = numbers.ActiveLast();
        Assert.IsNull(query.OperationFault);
        Assert.AreEqual(9, query.Value);
        numbers.Remove(9);
        Assert.AreEqual(8, query.Value);
        numbers.Clear();
        Assert.IsNotNull(query.OperationFault);
        Assert.AreEqual(0, query.Value);
        numbers.Add(30);
        Assert.IsNull(query.OperationFault);
        Assert.AreEqual(30, query.Value);
    }

    [TestMethod]
    public void SourceManipulation()
    {
        var numbers = new SynchronizedRangeObservableCollection<int>(System.Linq.Enumerable.Range(0, 10));
        using var query = numbers.ActiveLast(i => i % 3 == 0);
        Assert.IsNull(query.OperationFault);
        Assert.AreEqual(9, query.Value);
        numbers.Remove(9);
        Assert.AreEqual(6, query.Value);
        numbers.RemoveAll(i => i % 3 == 0);
        Assert.IsNotNull(query.OperationFault);
        Assert.AreEqual(0, query.Value);
        numbers.Add(30);
        Assert.IsNull(query.OperationFault);
        Assert.AreEqual(30, query.Value);
    }
}
