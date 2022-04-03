namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions;

[TestClass]
public class ActiveWhere
{
    [TestMethod]
    public void ElementResultChanges()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var query = people.ActiveWhere(p => p.Name!.Length == 4);
        Assert.IsTrue(new string[] { "John", "Erin" }.SequenceEqual(query.Select(person => person.Name)));
        people[0].Name = "Johnny";
        Assert.IsTrue(new string[] { "Erin" }.SequenceEqual(query.Select(person => person.Name)));
        people[1].Name = "Emilia";
        Assert.IsTrue(new string[] { "Erin" }.SequenceEqual(query.Select(person => person.Name)));
        people[12].Name = "Jack";
        Assert.IsTrue(new string[] { "Erin", "Jack" }.SequenceEqual(query.Select(person => person.Name)));
    }

    [TestMethod]
    public void ElementsAdded()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var query = people.ActiveWhere(p => p.Name!.Length == 4);
        Assert.IsTrue(new string[] { "John", "Erin" }.SequenceEqual(query.Select(person => person.Name)));
        people.Add(new TestPerson("Jack"));
        Assert.IsTrue(new string[] { "John", "Erin", "Jack" }.SequenceEqual(query.Select(person => person.Name)));
        people.Add(new TestPerson("Chuck"));
        Assert.IsTrue(new string[] { "John", "Erin", "Jack" }.SequenceEqual(query.Select(person => person.Name)));
        people.AddRange(new TestPerson[] { new TestPerson("Jill"), new TestPerson("Nick") });
        Assert.IsTrue(new string[] { "John", "Erin", "Jack", "Jill", "Nick" }.SequenceEqual(query.Select(person => person.Name)));
        people.AddRange(new TestPerson[] { new TestPerson("Clint"), new TestPerson("Harry") });
        Assert.IsTrue(new string[] { "John", "Erin", "Jack", "Jill", "Nick" }.SequenceEqual(query.Select(person => person.Name)));
        people.AddRange(new TestPerson[] { new TestPerson("Dana"), new TestPerson("Ray") });
        Assert.IsTrue(new string[] { "John", "Erin", "Jack", "Jill", "Nick", "Dana" }.SequenceEqual(query.Select(person => person.Name)));
        people[7] = new TestPerson("Tony");
    }

    [TestMethod]
    public void ElementsRemoved()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var query = people.ActiveWhere(p => p.Name!.Length == 5);
        Assert.IsTrue(new string[] { "Emily", "Cliff", "Craig", "Bryan", "James", "Steve" }.SequenceEqual(query.Select(person => person.Name)));
        people.RemoveAt(1);
        Assert.IsTrue(new string[] { "Cliff", "Craig", "Bryan", "James", "Steve" }.SequenceEqual(query.Select(person => person.Name)));
        people.RemoveAt(0);
        Assert.IsTrue(new string[] { "Cliff", "Craig", "Bryan", "James", "Steve" }.SequenceEqual(query.Select(person => person.Name)));
        people.RemoveRange(9, 2);
        Assert.IsTrue(new string[] { "Cliff", "Craig", "Steve" }.SequenceEqual(query.Select(person => person.Name)));
        people.RemoveRange(8, 2);
        Assert.IsTrue(new string[] { "Cliff", "Craig" }.SequenceEqual(query.Select(person => person.Name)));
    }
}
