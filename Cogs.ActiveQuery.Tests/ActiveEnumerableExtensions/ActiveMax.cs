using Cogs.Collections.Synchronized;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions
{
    [TestClass]
    public class ActiveMax
    {
        [TestMethod]
        public void ExpressionlessSourceManipulation()
        {
            var numbers = new SynchronizedRangeObservableCollection<int>();
            using var aggregate = numbers.ActiveMax();
            Assert.IsNotNull(aggregate.OperationFault);
            Assert.AreEqual(0, aggregate.Value);
            numbers.Add(1);
            Assert.IsNull(aggregate.OperationFault);
            Assert.AreEqual(1, aggregate.Value);
            numbers.AddRange(System.Linq.Enumerable.Range(2, 3));
            Assert.AreEqual(4, aggregate.Value);
            numbers.RemoveRange(0, 2);
            Assert.AreEqual(4, aggregate.Value);
            numbers.RemoveAt(1);
            Assert.AreEqual(3, aggregate.Value);
            numbers.Reset(System.Linq.Enumerable.Range(2, 3));
            Assert.AreEqual(4, aggregate.Value);
        }

        [TestMethod]
        public void SourceManipulation()
        {
            var people = TestPerson.CreatePeopleCollection();
            using var aggregate = people.ActiveMax(p => p.Name.Length);
            Assert.IsNull(aggregate.OperationFault);
            Assert.AreEqual(7, aggregate.Value);
            people.Add(people[0]);
            Assert.AreEqual(7, aggregate.Value);
            people[0].Name = "Johnathon";
            Assert.AreEqual(9, aggregate.Value);
            people[0].Name = "John";
            Assert.AreEqual(7, aggregate.Value);
        }
    }
}
