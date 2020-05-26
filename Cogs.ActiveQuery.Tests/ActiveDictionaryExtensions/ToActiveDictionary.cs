using Cogs.Collections.Synchronized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Cogs.ActiveQuery.Tests.ActiveDictionaryExtensions
{
    [TestClass]
    public class ToActiveDictionary
    {
        [TestMethod]
        public void DuplicateKeys()
        {
            using var query = TestPerson.CreatePeopleDictionary().ToActiveDictionary((key, value) => new KeyValuePair<int, TestPerson>(0, value));
            Assert.IsNotNull(query.OperationFault);
        }

        [TestMethod]
        public void NullKeys()
        {
            var john = new TestPerson(null);
            var people = new SynchronizedObservableDictionary<string, TestPerson>();
            using var query = people.ToActiveDictionary((key, value) => new KeyValuePair<string, TestPerson>(value.Name, value));
            Assert.IsNull(query.OperationFault);
            people.Add("John", john);
            Assert.IsNotNull(query.OperationFault);
            john.Name = "John";
            Assert.IsNull(query.OperationFault);
            john.Name = null;
            Assert.IsNotNull(query.OperationFault);
            people.Clear();
            Assert.IsNull(query.OperationFault);
        }

        [TestMethod]
        public void SourceManipulation()
        {
            foreach (var indexingStrategy in new IndexingStrategy[] { IndexingStrategy.HashTable, IndexingStrategy.SelfBalancingBinarySearchTree })
            {
                var people = TestPerson.CreatePeopleDictionary();
                using var query = people.ToActiveDictionary((key, value) => new KeyValuePair<string, string>(value.Name.Substring(0, 3), value.Name.Substring(3)));
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(string.Empty, query["Ben"]);
                people[6].Name = "Benjamin";
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual("jamin", query["Ben"]);
                people[6].Name = "Ben";
                var benny = new TestPerson("!!!TROUBLE");
                people.Add(people.Count, benny);
                Assert.IsNull(query.OperationFault);
                benny.Name = "Benny";
                Assert.IsNotNull(query.OperationFault);
                var benjamin = new TestPerson("@@@TROUBLE");
                people.Add(people.Count, benjamin);
                benjamin.Name = "Benjamin";
                Assert.IsNotNull(query.OperationFault);
                benny.Name = "!!!TROUBLE";
                Assert.IsNotNull(query.OperationFault);
                Assert.AreEqual("TROUBLE", query["!!!"]);
                benjamin.Name = "@@@TROUBLE";
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual("TROUBLE", query["@@@"]);
                people.Add(people.Count, benjamin);
                Assert.IsNotNull(query.OperationFault);
                Assert.AreEqual("TROUBLE", query["@@@"]);
                benjamin.Name = "###TROUBLE";
                Assert.IsNotNull(query.OperationFault);
                Assert.AreEqual("TROUBLE", query["###"]);
                people.Add(people.Count, benjamin);
                Assert.IsNotNull(query.OperationFault);
                people.Remove(people.Count - 1);
                Assert.IsNotNull(query.OperationFault);
                people.Remove(people.Count - 1);
                Assert.IsNull(query.OperationFault);
                people.Remove(people.Count - 1);
                Assert.IsNull(query.OperationFault);
                people.Remove(people.Count - 1);
                Assert.IsNull(query.OperationFault);
            }
        }
    }
}
