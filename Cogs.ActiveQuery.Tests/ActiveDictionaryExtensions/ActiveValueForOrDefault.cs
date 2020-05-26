using Cogs.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Cogs.ActiveQuery.Tests.ActiveDictionaryExtensions
{
    [TestClass]
    public class ActiveValueForOrDefault
    {
        [TestMethod]
        public void NonNotifier()
        {
            var numbers = Enumerable.Range(0, 10).ToDictionary(i => i);
            using var query = numbers.ActiveValueForOrDefault(9);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(9, query.Value);
        }

        [TestMethod]
        public void NonNotifierOutOfRange()
        {
            var numbers = Enumerable.Range(0, 5).ToDictionary(i => i);
            using var query = numbers.ActiveValueForOrDefault(9);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(0, query.Value);
        }

        [TestMethod]
        public void SourceManipulation()
        {
            var numbers = new ObservableDictionary<int, int>(Enumerable.Range(0, 10).ToDictionary(i => i));
            using var query = numbers.ActiveValueForOrDefault(9);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(9, query.Value);
            numbers.Remove(9);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(0, query.Value);
            numbers.Add(9, 30);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(30, query.Value);
            numbers[9] = 15;
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(15, query.Value);
        }

        [TestMethod]
        public void SourceManipulationSorted()
        {
            var numbers = new ObservableSortedDictionary<int, int>(Enumerable.Range(0, 10).ToDictionary(i => i));
            using var query = numbers.ActiveValueForOrDefault(9);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(9, query.Value);
            numbers.Remove(9);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(0, query.Value);
            numbers.Add(9, 30);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(30, query.Value);
            numbers[9] = 15;
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(15, query.Value);
        }
    }
}
