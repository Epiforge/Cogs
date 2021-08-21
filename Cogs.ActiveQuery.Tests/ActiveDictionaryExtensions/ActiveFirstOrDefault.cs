namespace Cogs.ActiveQuery.Tests.ActiveDictionaryExtensions;

[TestClass]
public class ActiveFirstOrDefault
{
    [TestMethod]
    public void ExpressionlessEmptySource()
    {
        var numbers = new ObservableDictionary<int, int>();
        using var query = numbers.ActiveFirstOrDefault();
        Assert.IsNull(query.OperationFault);
        Assert.AreEqual(0, query.Value.Value);
    }

    [TestMethod]
    public void ExpressionlessNonNotifier()
    {
        var numbers = Enumerable.Range(0, 10).ToDictionary(i => i);
        using var query = numbers.ActiveFirstOrDefault();
        Assert.IsNull(query.OperationFault);
        Assert.AreEqual(0, query.Value.Value);
    }

    [TestMethod]
    public void ExpressionlessSourceManipulation()
    {
        var numbers = new ObservableDictionary<int, int>(System.Linq.Enumerable.Range(0, 10).ToDictionary(i => i));
        using var query = numbers.ActiveFirstOrDefault();
        Assert.IsNull(query.OperationFault);
        Assert.AreEqual(0, query.Value.Value);
        numbers.Remove(0);
        Assert.AreEqual(1, query.Value.Value);
        numbers.Clear();
        Assert.IsNull(query.OperationFault);
        Assert.AreEqual(0, query.Value.Value);
        numbers.Add(30, 30);
        Assert.IsNull(query.OperationFault);
        Assert.AreEqual(30, query.Value.Value);
    }

    [TestMethod]
    public void SourceManipulation()
    {
        var numbers = new ObservableDictionary<int, int>(System.Linq.Enumerable.Range(0, 10).ToDictionary(i => i));
        using var query = numbers.ActiveFirstOrDefault((key, value) => value % 3 == 0);
        Assert.IsNull(query.OperationFault);
        Assert.AreEqual(0, query.Value.Value);
        numbers.Remove(0);
        Assert.AreEqual(3, query.Value.Value);
        numbers.RemoveAll((key, value) => value % 3 == 0);
        Assert.IsNull(query.OperationFault);
        Assert.AreEqual(0, query.Value.Value);
        numbers.Add(30, 30);
        Assert.IsNull(query.OperationFault);
        Assert.AreEqual(30, query.Value.Value);
    }
}
