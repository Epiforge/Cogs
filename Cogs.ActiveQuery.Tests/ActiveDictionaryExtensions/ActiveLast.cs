namespace Cogs.ActiveQuery.Tests.ActiveDictionaryExtensions;

[TestClass]
public class ActiveLast
{
    [TestMethod]
    public void ExpressionlessEmptyNonNotifier()
    {
        var numbers = new Dictionary<int, int>();
        using var query = numbers.ActiveLast();
        Assert.IsNotNull(query.OperationFault);
        Assert.AreEqual(0, query.Value.Value);
    }

    [TestMethod]
    public void ExpressionlessEmptySource()
    {
        var numbers = new ObservableDictionary<int, int>();
        using var query = numbers.ActiveLast();
        Assert.IsNotNull(query.OperationFault);
        Assert.AreEqual(0, query.Value.Value);
    }

    [TestMethod]
    public void ExpressionlessNonNotifier()
    {
        var numbers = Enumerable.Range(0, 10).ToDictionary(i => i);
        using var query = numbers.ActiveLast();
        Assert.IsNull(query.OperationFault);
        Assert.AreEqual(9, query.Value.Value);
    }

    [TestMethod]
    public void ExpressionlessSourceManipulation()
    {
        var numbers = new ObservableDictionary<int, int>(Enumerable.Range(0, 10).ToDictionary(i => i));
        using var query = numbers.ActiveLast();
        Assert.IsNull(query.OperationFault);
        Assert.AreEqual(9, query.Value.Value);
        numbers.Remove(9);
        Assert.AreEqual(8, query.Value.Value);
        numbers.Clear();
        Assert.IsNotNull(query.OperationFault);
        Assert.AreEqual(0, query.Value.Value);
        numbers.Add(30, 30);
        Assert.IsNull(query.OperationFault);
        Assert.AreEqual(30, query.Value.Value);
    }

    [TestMethod]
    public void SourceManipulation()
    {
        var numbers = new ObservableDictionary<int, int>(Enumerable.Range(0, 10).ToDictionary(i => i));
        using var query = numbers.ActiveLast((key, value) => value % 3 == 0);
        Assert.IsNull(query.OperationFault);
        Assert.AreEqual(9, query.Value.Value);
        numbers.Remove(9);
        Assert.AreEqual(6, query.Value.Value);
        numbers.RemoveAll((key, value) => value % 3 == 0);
        Assert.IsNotNull(query.OperationFault);
        Assert.AreEqual(0, query.Value.Value);
        numbers.Add(30, 30);
        Assert.IsNull(query.OperationFault);
        Assert.AreEqual(30, query.Value.Value);
    }
}
