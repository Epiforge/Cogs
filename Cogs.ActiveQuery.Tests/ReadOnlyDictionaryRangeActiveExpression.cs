namespace Cogs.ActiveQuery.Tests;

[TestClass]
public class ReadOnlyDictionaryRangeActiveExpression
{
    [TestMethod]
    public void DisposalOverridden()
    {
        var people = TestPerson.CreatePeopleDictionary();
        using var queryA = people.ToActiveDictionary((key, value) => new KeyValuePair<int, int>(key, 3 / value.Name!.Length));
        using var queryB = people.ToActiveDictionary((key, value) => new KeyValuePair<int, int>(key, 3 / value.Name!.Length));
    }

    [TestMethod]
    public void ElementFaults()
    {
        var people = TestPerson.CreatePeopleDictionary();
        using var query = people.ToActiveDictionary((key, value) => new KeyValuePair<int, int>(key, 3 / value.Name!.Length));
        var changing = false;

        void elementFaultChanging(object? sender, ElementFaultChangeEventArgs e)
        {
            Assert.IsFalse(changing);
            Assert.AreEqual(0, e.Element);
            Assert.AreEqual(1, e.Count);
            Assert.IsNull(e.Fault);
            changing = true;
        }

        void elementFaultChanged(object? sender, ElementFaultChangeEventArgs e)
        {
            Assert.IsTrue(changing);
            Assert.AreEqual(0, e.Element);
            Assert.AreEqual(1, e.Count);
            Assert.IsInstanceOfType(e.Fault, typeof(DivideByZeroException));
            changing = false;
        }

        query.ElementFaultChanging += elementFaultChanging;
        query.ElementFaultChanged += elementFaultChanged;
        people[0].Name = string.Empty;
        Assert.IsFalse(changing);
        Assert.AreEqual(1, query.GetElementFaults().Count);
        query.ElementFaultChanging -= elementFaultChanging;
        query.ElementFaultChanged -= elementFaultChanged;
    }

    [TestMethod]
    public void SourceFaultNotifier()
    {
        var people = TestPerson.CreatePeopleDictionary();
        using var queryA = people.ToActiveDictionary((key, value) => new KeyValuePair<int, int>(key, 3 / value.Name!.Length));
        using var queryB = queryA.ToActiveDictionary((key, value) => new KeyValuePair<int, int>(key, value * -1));
        people[0].Name = string.Empty;
        people.Clear();
    }
}
