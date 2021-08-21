namespace Cogs.ActiveQuery.Tests;

[TestClass]
public class DictionaryExtensions
{
    [TestMethod]
    public void CreateSimilarSynchronizedObservableDictionaryForSelfBalancingBinarySearchTree()
    {
        var people = TestPerson.CreatePeopleDictionary();
        using var sortedPeople = people.ToActiveDictionary((key, value) => new KeyValuePair<int, TestPerson>(key, value), IndexingStrategy.SelfBalancingBinarySearchTree);
        using var sortedPeopleFiltered = sortedPeople.ActiveWhere((key, value) => key % 2 == 0);
    }

    [TestMethod]
    public void GetKeyComparerForSortedDictionary()
    {
        var dict = new SortedDictionary<string, object>() { { "1", 1 } };
        using var query = dict.ActiveOfType<string, object, int>();
        Assert.AreSame(Comparer<string>.Default, query.Comparer);
    }
}
