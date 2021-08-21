namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions;

[TestClass]
public class ActiveSingleOrDefault
{
    [TestMethod]
    public void EmptySource()
    {
        var numbers = new SynchronizedRangeObservableCollection<int>();
        using var expr = numbers.ActiveSingleOrDefault(i => i % 3 == 0);
        Assert.IsNull(expr.OperationFault);
        Assert.AreEqual(0, expr.Value);
    }

    [TestMethod]
    public void ExpressionlessEmptyNonNotifier()
    {
        var numbers = System.Array.Empty<int>();
        using var expr = numbers.ActiveSingleOrDefault();
        Assert.IsNull(expr.OperationFault);
        Assert.AreEqual(0, expr.Value);
    }

    [TestMethod]
    public void ExpressionlessEmptySource()
    {
        var numbers = new SynchronizedRangeObservableCollection<int>();
        using var expr = numbers.ActiveSingleOrDefault();
        Assert.IsNull(expr.OperationFault);
        Assert.AreEqual(0, expr.Value);
    }

    [TestMethod]
    public void ExpressionlessMultiple()
    {
        var numbers = new SynchronizedRangeObservableCollection<int>(new int[] { 1, 1 });
        using var expr = numbers.ActiveSingleOrDefault();
        Assert.IsNotNull(expr.OperationFault);
        Assert.AreEqual(0, expr.Value);
    }

    [TestMethod]
    public void ExpressionlessNonNotifier()
    {
        var numbers = new int[] { 1 };
        using var expr = numbers.ActiveSingleOrDefault();
        Assert.IsNull(expr.OperationFault);
        Assert.AreEqual(1, expr.Value);
    }

    [TestMethod]
    public void ExpressionlessNonNotifierMultiple()
    {
        var numbers = new int[] { 1, 1 };
        using var expr = numbers.ActiveSingleOrDefault();
        Assert.IsNotNull(expr.OperationFault);
        Assert.AreEqual(0, expr.Value);
    }

    [TestMethod]
    public void ExpressionlessSourceManipulation()
    {
        var numbers = new SynchronizedRangeObservableCollection<int>(new int[] { 1 });
        using var expr = numbers.ActiveSingleOrDefault();
        Assert.IsNull(expr.OperationFault);
        Assert.AreEqual(1, expr.Value);
        numbers.Add(2);
        Assert.IsNotNull(expr.OperationFault);
        Assert.AreEqual(0, expr.Value);
        numbers.RemoveAt(0);
        Assert.IsNull(expr.OperationFault);
        Assert.AreEqual(2, expr.Value);
        numbers.Clear();
        Assert.IsNull(expr.OperationFault);
        Assert.AreEqual(0, expr.Value);
    }

    [TestMethod]
    public void Multiple()
    {
        var numbers = new SynchronizedRangeObservableCollection<int>(Enumerable.Range(1, 3).Select(i => i * 3));
        using var expr = numbers.ActiveSingleOrDefault(i => i % 3 == 0);
        Assert.IsNotNull(expr.OperationFault);
        Assert.AreEqual(0, expr.Value);
    }

    [TestMethod]
    public void SourceManipulation()
    {
        var numbers = new SynchronizedRangeObservableCollection<int>(Enumerable.Range(1, 3));
        using var expr = numbers.ActiveSingleOrDefault(i => i % 3 == 0);
        Assert.IsNull(expr.OperationFault);
        Assert.AreEqual(3, expr.Value);
        numbers.RemoveAt(2);
        Assert.IsNull(expr.OperationFault);
        Assert.AreEqual(0, expr.Value);
        numbers.Add(3);
        Assert.IsNull(expr.OperationFault);
        Assert.AreEqual(3, expr.Value);
        numbers.Add(5);
        Assert.IsNull(expr.OperationFault);
        Assert.AreEqual(3, expr.Value);
        numbers.Add(6);
        Assert.IsNotNull(expr.OperationFault);
        Assert.AreEqual(0, expr.Value);
        numbers.Clear();
        Assert.IsNull(expr.OperationFault);
        Assert.AreEqual(0, expr.Value);
    }
}
