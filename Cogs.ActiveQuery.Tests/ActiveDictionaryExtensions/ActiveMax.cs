using Cogs.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Cogs.ActiveQuery.Tests.ActiveDictionaryExtensions
{
    [TestClass]
    public class ActiveMax
    {
        [TestMethod]
        public void ExpressionlessSourceManipulation()
        {
            var numbers = new ObservableDictionary<int, int>();
            using var aggregate = numbers.ActiveMax();
            Assert.IsNotNull(aggregate.OperationFault);
            Assert.AreEqual(0, aggregate.Value);
            numbers.Add(1, 1);
            Assert.IsNull(aggregate.OperationFault);
            Assert.AreEqual(1, aggregate.Value);
            numbers.AddRange(System.Linq.Enumerable.Range(2, 3).Select(i => new KeyValuePair<int, int>(i, i)));
            Assert.AreEqual(4, aggregate.Value);
            numbers.RemoveRange(new int[] { 1, 2 });
            Assert.AreEqual(4, aggregate.Value);
            numbers.Remove(4);
            Assert.AreEqual(3, aggregate.Value);
            numbers.Reset(System.Linq.Enumerable.Range(2, 3).ToDictionary(i => i));
            Assert.AreEqual(4, aggregate.Value);
        }

        [TestMethod]
        public void SourceManipulation()
        {
            var people = new ObservableDictionary<string, TestPerson>(TestPerson.CreatePeopleCollection().ToDictionary(p => p.Name));
            using var aggregate = people.ActiveMax((key, value) => value.Name.Length);
            Assert.IsNull(aggregate.OperationFault);
            Assert.AreEqual(7, aggregate.Value);
            people.Add("John2", people["John"]);
            Assert.AreEqual(7, aggregate.Value);
            people["John"].Name = "Johnathon";
            Assert.AreEqual(9, aggregate.Value);
            people["John"].Name = "John";
            Assert.AreEqual(7, aggregate.Value);
        }
    }
}
