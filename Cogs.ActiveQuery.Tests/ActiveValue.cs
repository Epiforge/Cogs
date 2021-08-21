namespace Cogs.ActiveQuery.Tests;

[TestClass]
public class ActiveValue
{
    [TestMethod]
    public void ElementFaults()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var query = people.ActiveAverage(person => 1 / person.Name!.Length);
        people[0].Name = string.Empty;
        Assert.AreEqual(1, query.GetElementFaults().Count);
    }
}
