namespace Cogs.ActiveQuery.Tests;

[TestClass]
public class EnumerableRangeActiveExpression
{
    [TestMethod]
    public void GenericDisposalOverridden()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var queryA = people.ActiveSelect(person => 3 / person.Name!.Length);
        using var queryB = people.ActiveSelect(person => 3 / person.Name!.Length);
        people.Clear();
    }

    [TestMethod]
    public void GenericSourceFaultNotifier()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var queryA = people.ActiveSelect(person => 3 / person.Name!.Length);
        using var queryB = queryA.ActiveSelect(num => num * -1);
        people[0].Name = string.Empty;
        people.Clear();
    }

    [TestMethod]
    public void NonGenericCount()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var query = ((IEnumerable)people).ActiveSelect(person => 3 / (person as TestPerson)!.Name!.Length);
        Assert.AreEqual(14, query.Count);
    }

    [TestMethod]
    public void NonGenericDisposalOverridden()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var queryA = ((IEnumerable)people).ActiveSelect(person => 3 / (person as TestPerson)!.Name!.Length);
        using var queryB = ((IEnumerable)people).ActiveSelect(person => 3 / (person as TestPerson)!.Name!.Length);
        people.Clear();
    }

    [TestMethod]
    public void NonGenericEmpty()
    {
        var people = new SynchronizedRangeObservableCollection<TestPerson>();
        using var query = ((IEnumerable)people).ActiveSelect(person => 3 / (person as TestPerson)!.Name!.Length);
    }

    [TestMethod]
    public void NonGenericReset()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var query = ((IEnumerable)people).ActiveSelect(person => 3 / (person as TestPerson)!.Name!.Length);
        people.Clear();
    }

    [TestMethod]
    public void NonGenericSourceFaultNotifier()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var queryA = ((IEnumerable)people).ActiveSelect(person => 3 / (person as TestPerson)!.Name!.Length);
        using var queryB = ((IEnumerable)queryA).ActiveSelect(num => (int)num! * -1);
        people[0].Name = string.Empty;
        people.Clear();
    }
}
