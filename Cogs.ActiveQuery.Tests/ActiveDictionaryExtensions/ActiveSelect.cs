namespace Cogs.ActiveQuery.Tests.ActiveDictionaryExtensions;

[TestClass]
public class ActiveSelect
{
    [TestMethod]
    public void SourceManipulation()
    {
        var people = TestPerson.CreatePeopleDictionary();
        using var expr = people.ActiveSelect((key, value) => Tuple.Create(key, value.Name!.Length));
        void checkValues(params int[] values) => Assert.IsTrue(values.SequenceEqual(expr.OrderBy(t => t!.Item1).Select(t => t!.Item2)));
        checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Add(people.Count, people[0]);
        checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 4);
        people[0].Name = "Johnny";
        checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 6);
        people.Remove(people.Count - 1);
        checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        var temp = people[1];
        people[1] = people[0];
        people[0] = temp;
        checkValues(5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Add(-1, people[0]);
        checkValues(5, 5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Remove(0);
        checkValues(5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        temp = people[-1];
        people[-1] = people[1];
        people[1] = temp;
        checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Remove(-1);
        checkValues(5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
    }
}
