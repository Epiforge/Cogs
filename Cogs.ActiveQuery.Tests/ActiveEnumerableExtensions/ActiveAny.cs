namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions;

[TestClass]
public class ActiveAny
{
    [TestMethod]
    public void ExpressionlessNonNotifying()
    {
        var numbers = Enumerable.Range(1, 10).Select(i => i * 3);
        using var query = numbers.ActiveAny();
        Assert.IsNull(query.OperationFault);
        Assert.IsTrue(query.Value);
    }

    [TestMethod]
    public void ExpressionlessNull()
    {
        using var query = ((IEnumerable<object>)null!).ActiveAny();
        Assert.IsNotNull(query.OperationFault);
        Assert.IsFalse(query.Value);
    }

    [TestMethod]
    public void ExpressionlessSourceManipulation()
    {
        var numbers = new SynchronizedRangeObservableCollection<int>(Enumerable.Range(1, 10).Select(i => i * 3));
        using var query = numbers.ActiveAny();
        Assert.IsNull(query.OperationFault);
        Assert.IsTrue(query.Value);
        numbers[0] = 2;
        Assert.IsTrue(query.Value);
        numbers.RemoveAt(0);
        Assert.IsTrue(query.Value);
        --numbers[0];
        Assert.IsTrue(query.Value);
        numbers.Clear();
        Assert.IsFalse(query.Value);
        numbers.Add(7);
        Assert.IsTrue(query.Value);
    }

    [TestMethod]
    public void SourceManipulation()
    {
        var numbers = new SynchronizedRangeObservableCollection<int>(Enumerable.Range(1, 10).Select(i => i * 3));
        using var query = numbers.ActiveAny(i => i % 3 != 0);
        Assert.IsNull(query.OperationFault);
        Assert.IsFalse(query.Value);
        numbers[0] = 2;
        Assert.IsTrue(query.Value);
        numbers.RemoveAt(0);
        Assert.IsFalse(query.Value);
        --numbers[0];
        Assert.IsTrue(query.Value);
        numbers.Clear();
        Assert.IsFalse(query.Value);
        numbers.Add(7);
        Assert.IsTrue(query.Value);
    }
}
