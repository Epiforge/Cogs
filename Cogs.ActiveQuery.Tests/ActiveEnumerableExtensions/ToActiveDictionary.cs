namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions;

[TestClass]
public class ToActiveDictionary
{
    [TestMethod]
    public void DuplicateKeys()
    {
        using var query = System.Linq.Enumerable.Range(0, 10).ToActiveDictionary(i => new KeyValuePair<int, int>(0, i));
        Assert.IsNotNull(query.OperationFault);
    }

    [TestMethod]
    public void NullKeys()
    {
        var john = new TestPerson(null);
        var people = new SynchronizedRangeObservableCollection<TestPerson>();
        using var query = people.ToActiveDictionary(p => new KeyValuePair<string, TestPerson>(p.Name!, p));
        Assert.IsNull(query.OperationFault);
        people.Add(john);
        Assert.IsNotNull(query.OperationFault);
        john.Name = "John";
        Assert.IsNull(query.OperationFault);
        john.Name = null;
        Assert.IsNotNull(query.OperationFault);
        people.Clear();
        Assert.IsNull(query.OperationFault);
    }

    [TestMethod]
    [SuppressMessage("Style", "IDE0057: Use range operator")]
    public void SourceManipulation()
    {
        foreach (var indexingStrategy in new IndexingStrategy[] { IndexingStrategy.HashTable, IndexingStrategy.SelfBalancingBinarySearchTree })
        {
            var people = TestPerson.CreatePeopleCollection();
            using var query = people.ToActiveDictionary(p => new KeyValuePair<string, string>(p.Name!.Substring(0, 3), p.Name.Substring(3)), indexingStrategy);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(string.Empty, query["Ben"]);
            people[6].Name = "Benjamin";
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual("jamin", query["Ben"]);
            people[6].Name = "Ben";
            var benny = new TestPerson("!!!TROUBLE");
            people.Add(benny);
            Assert.IsNull(query.OperationFault);
            benny.Name = "Benny";
            Assert.IsNotNull(query.OperationFault);
            var benjamin = new TestPerson("@@@TROUBLE");
            people.Add(benjamin);
            benjamin.Name = "Benjamin";
            Assert.IsNotNull(query.OperationFault);
            benny.Name = "!!!TROUBLE";
            Assert.IsNotNull(query.OperationFault);
            Assert.AreEqual("TROUBLE", query["!!!"]);
            benjamin.Name = "@@@TROUBLE";
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual("TROUBLE", query["@@@"]);
            people.Add(benjamin);
            Assert.IsNotNull(query.OperationFault);
            Assert.AreEqual("TROUBLE", query["@@@"]);
            benjamin.Name = "###TROUBLE";
            Assert.IsNotNull(query.OperationFault);
            Assert.AreEqual("TROUBLE", query["###"]);
            people.Add(benjamin);
            Assert.IsNotNull(query.OperationFault);
            people.Remove(benjamin);
            Assert.IsNotNull(query.OperationFault);
            people.Remove(benjamin);
            Assert.IsNull(query.OperationFault);
            people.Remove(benjamin);
            Assert.IsNull(query.OperationFault);
            people.Remove(benny);
            Assert.IsNull(query.OperationFault);
        }
    }
}
