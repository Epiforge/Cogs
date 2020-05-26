using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using System.Linq;

namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions
{
    [TestClass]
    public class ActiveWhere
    {
        [TestMethod]
        public void ElementResultChanges()
        {
            var people = TestPerson.CreatePeopleCollection();
            var counts = new BlockingCollection<int>();
            using (var query = people.ActiveWhere(p => p.Name.Length == 4))
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
            var people = TestPerson.CreatePeopleCollection();
            var counts = new BlockingCollection<int>();
            using (var query = people.ActiveWhere(p => p.Name.Length == 4))
            {
                counts.Add(query.Count);
                people.Add(new TestPerson("Jack"));
                counts.Add(query.Count);
                people.Add(new TestPerson("Chuck"));
                counts.Add(query.Count);
                people.AddRange(new TestPerson[] { new TestPerson("Jill"), new TestPerson("Nick") });
                counts.Add(query.Count);
                people.AddRange(new TestPerson[] { new TestPerson("Clint"), new TestPerson("Harry") });
                counts.Add(query.Count);
                people.AddRange(new TestPerson[] { new TestPerson("Dana"), new TestPerson("Ray") });
                counts.Add(query.Count);
                people[7] = new TestPerson("Tony");
                counts.Add(query.Count);
            }
            Assert.IsTrue(new int[] { 2, 3, 3, 5, 5, 6, 7 }.SequenceEqual(counts));
        }

        [TestMethod]
        public void ElementsRemoved()
        {
            var people = TestPerson.CreatePeopleCollection();
            var counts = new BlockingCollection<int>();
            using (var query = people.ActiveWhere(p => p.Name.Length == 5))
            {
                counts.Add(query.Count);
                people.RemoveAt(1);
                counts.Add(query.Count);
                people.RemoveAt(0);
                counts.Add(query.Count);
                people.RemoveRange(9, 2);
                counts.Add(query.Count);
                people.RemoveRange(8, 2);
                counts.Add(query.Count);
            }
            Assert.IsTrue(new int[] { 6, 5, 5, 3, 2 }.SequenceEqual(counts));
        }
    }
}
