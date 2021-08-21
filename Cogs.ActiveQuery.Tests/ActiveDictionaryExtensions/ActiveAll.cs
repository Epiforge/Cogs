namespace Cogs.ActiveQuery.Tests.ActiveDictionaryExtensions;

[TestClass]
public class ActiveAll
{
    [TestMethod]
    public void SourceManipulation()
    {
        var numbers = new ObservableDictionary<int, int>(Enumerable.Range(1, 10).ToDictionary(i => i, i => i * 3));
        using var query = numbers.ActiveAll((key, value) => value % 3 == 0);
        Assert.IsNull(query.OperationFault);
        Assert.IsTrue(query.Value);
        numbers[1] = 2;
        Assert.IsFalse(query.Value);
        numbers.Remove(1);
        Assert.IsTrue(query.Value);
        --numbers[2];
        Assert.IsFalse(query.Value);
        numbers.Clear();
        Assert.IsTrue(query.Value);
        numbers.Add(11, 7);
        Assert.IsFalse(query.Value);
    }
}
