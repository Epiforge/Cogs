namespace Cogs.ActiveQuery.Tests.ActiveDictionaryExtensions;

[TestClass]
public class ActiveWhere
{
    [TestMethod]
    public void ElementResultChanges()
    {
        var people = TestPerson.CreatePeopleDictionary();
        var counts = new BlockingCollection<int>();
        using (var query = people.ActiveWhere((key, value) => value.Name!.Length == 4))
        {
            counts.Add(query.Count);
            people[0].Name = "Johnny";
            counts.Add(query.Count);
            people[1].Name = "Emilia";
            counts.Add(query.Count);
            people[12].Name = "Jack";
            counts.Add(query.Count);
        }
        Assert.IsTrue(new int[] { 2, 1, 1, 2 }.SequenceEqual(counts));
    }

    [TestMethod]
    public void ElementsAdded()
    {
        var people = TestPerson.CreatePeopleDictionary();
        var counts = new BlockingCollection<int>();
        using (var query = people.ActiveWhere((key, value) => value.Name!.Length == 4))
        {
            counts.Add(query.Count);
            people.Add(14, new TestPerson("Jack"));
            counts.Add(query.Count);
            people.Add(15, new TestPerson("Chuck"));
            counts.Add(query.Count);
            people.AddRange(new KeyValuePair<int, TestPerson>[]
            {
                new KeyValuePair<int, TestPerson>(16, new TestPerson("Jill")),
                new KeyValuePair<int, TestPerson>(17, new TestPerson("Nick"))
            });
            counts.Add(query.Count);
            people.AddRange(new KeyValuePair<int, TestPerson>[]
            {
                new KeyValuePair<int, TestPerson>(18, new TestPerson("Clint")),
                new KeyValuePair<int, TestPerson>(19, new TestPerson("Harry"))
            });
            counts.Add(query.Count);
            people.AddRange(new KeyValuePair<int, TestPerson>[]
            {
                new KeyValuePair<int, TestPerson>(20, new TestPerson("Dana")),
                new KeyValuePair<int, TestPerson>(21, new TestPerson("Ray"))
            });
            counts.Add(query.Count);
            people[7] = new TestPerson("Tony");
            counts.Add(query.Count);
        }
        Assert.IsTrue(new int[] { 2, 3, 3, 5, 5, 6, 7 }.SequenceEqual(counts));
    }

    [TestMethod]
    public void ElementsRemoved()
    {
        var people = TestPerson.CreatePeopleDictionary();
        var counts = new BlockingCollection<int>();
        using (var query = people.ActiveWhere((key, value) => value.Name!.Length == 5))
        {
            counts.Add(query.Count);
            people.Remove(1);
            counts.Add(query.Count);
            people.Remove(0);
            counts.Add(query.Count);
            people.RemoveRange(new int[] { 11, 12 });
            counts.Add(query.Count);
            people.RemoveRange(new int[] { 10, 13 });
            counts.Add(query.Count);
        }
        Assert.IsTrue(new int[] { 6, 5, 5, 3, 2 }.SequenceEqual(counts));
    }
}
