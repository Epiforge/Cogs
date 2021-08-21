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
    public void NonGenericElementFaults()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var query = ((IEnumerable)people).ActiveSelect(person => 3 / (person as TestPerson)!.Name!.Length);
        var changing = false;

        void elementFaultChanging(object? sender, ElementFaultChangeEventArgs e)
        {
            Assert.IsFalse(changing);
            Assert.AreSame(people[0], e.Element);
            Assert.AreEqual(1, e.Count);
            Assert.IsNull(e.Fault);
            changing = true;
        }

        void elementFaultChanged(object? sender, ElementFaultChangeEventArgs e)
        {
            Assert.IsTrue(changing);
            Assert.AreSame(people[0], e.Element);
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
