namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions;

[TestClass]
public class ActiveSelect
{
    [TestMethod]
    public void EnumerableSorted()
    {
        var argumentOutOfRangeThrown = false;
        try
        {
            ((IEnumerable)Array.Empty<object>()).ActiveSelect(obj => obj!.GetHashCode(), IndexingStrategy.SelfBalancingBinarySearchTree);
        }
        catch (ArgumentOutOfRangeException)
        {
            argumentOutOfRangeThrown = true;
        }
        Assert.IsTrue(argumentOutOfRangeThrown);
    }

    [TestMethod]
    public void EnumerableSourceManipulation()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var expr = ((IEnumerable)people).ActiveSelect(person => (person as TestPerson)!.Name!.Length);
        void checkValues(params int[] values) => Assert.IsTrue(values.SequenceEqual(expr));
        checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Add(people.First());
        checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 4);
        people[0].Name = "Johnny";
        checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 6);
        people.RemoveAt(people.Count - 1);
        checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Move(0, 1);
        checkValues(5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Insert(0, people[0]);
        checkValues(5, 5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.RemoveAt(1);
        checkValues(5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Move(1, 0);
        checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.RemoveAt(0);
        checkValues(5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
    }

    [TestMethod]
    public void EnumerableSourceManipulationUnindexed()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var expr = ((IEnumerable)people).ActiveSelect(person => (person as TestPerson)!.Name!.Length, IndexingStrategy.NoneOrInherit);
        void checkValues(params int[] values) => Assert.IsTrue(values.SequenceEqual(expr));
        checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Add(people.First());
        checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 4);
        people[0].Name = "Johnny";
        checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 6);
        people.RemoveAt(people.Count - 1);
        checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Move(0, 1);
        checkValues(5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
    }

    [TestMethod]
    public void SourceManipulation()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var expr = people.ActiveSelect(person => person.Name!.Length);
        void checkValues(params int[] values) => Assert.IsTrue(values.SequenceEqual(expr));
        checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Add(people.First());
        checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 4);
        people[0].Name = "Johnny";
        checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 6);
        people.RemoveAt(people.Count - 1);
        checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Move(0, 1);
        checkValues(5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Insert(0, people[0]);
        checkValues(5, 5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.RemoveAt(1);
        checkValues(5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Move(1, 0);
        checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.RemoveAt(0);
        checkValues(5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
    }

    [TestMethod]
    public void SourceManipulationSorted()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var expr = people.ActiveSelect(person => person.Name!.Length, IndexingStrategy.SelfBalancingBinarySearchTree);
        void checkValues(params int[] values) => Assert.IsTrue(values.SequenceEqual(expr));
        checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Add(people.First());
        checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 4);
        people[0].Name = "Johnny";
        checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 6);
        people.RemoveAt(people.Count - 1);
        checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Move(0, 1);
        checkValues(5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Insert(0, people[0]);
        checkValues(5, 5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.RemoveAt(1);
        checkValues(5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Move(1, 0);
        checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.RemoveAt(0);
        checkValues(5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
    }

    [TestMethod]
    public void SourceManipulationUnindexed()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var expr = people.ActiveSelect(person => person.Name!.Length, IndexingStrategy.NoneOrInherit);
        void checkValues(params int[] values) => Assert.IsTrue(values.SequenceEqual(expr));
        checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Add(people.First());
        checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 4);
        people[0].Name = "Johnny";
        checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 6);
        people.RemoveAt(people.Count - 1);
        checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
        people.Move(0, 1);
        checkValues(5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
    }
}
