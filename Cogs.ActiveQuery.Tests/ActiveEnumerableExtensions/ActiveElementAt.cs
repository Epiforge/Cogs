using Cogs.Collections.Synchronized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions
{
    [TestClass]
    public class ActiveElementAt
    {
        [TestMethod]
        public void EnumerableOutOfRange()
        {
            var numbers = new SynchronizedRangeObservableCollection<int>(System.Linq.Enumerable.Range(0, 5));
            var enumerable = (IEnumerable<int>)numbers;
            using var query = enumerable.ActiveElementAt(9);
            Assert.IsNotNull(query.OperationFault);
            Assert.AreEqual(0, query.Value);
        }

        [TestMethod]
        public void EnumerableSourceManipulation()
        {
            var numbers = new SynchronizedRangeObservableCollection<int>(System.Linq.Enumerable.Range(0, 10));
            var enumerable = (IEnumerable<int>)numbers;
            using var query = enumerable.ActiveElementAt(9);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(9, query.Value);
            numbers.Remove(9);
            Assert.IsNotNull(query.OperationFault);
            Assert.AreEqual(0, query.Value);
            numbers.Add(30);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(30, query.Value);
            numbers.Insert(9, 15);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(15, query.Value);
        }

        [TestMethod]
        public void NonNotifier()
        {
            var numbers = System.Linq.Enumerable.Range(0, 10);
            using var query = numbers.ActiveElementAt(9);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(9, query.Value);
        }

        [TestMethod]
        public void NonNotifierOutOfRange()
        {
            var numbers = System.Linq.Enumerable.Range(0, 5);
            using var query = numbers.ActiveElementAt(9);
            Assert.IsNotNull(query.OperationFault);
            Assert.AreEqual(0, query.Value);
        }

        [TestMethod]
        public void SourceManipulation()
        {
            var numbers = new SynchronizedRangeObservableCollection<int>(System.Linq.Enumerable.Range(0, 10));
            using var query = numbers.ActiveElementAt(9);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(9, query.Value);
            numbers.Remove(9);
            Assert.IsNotNull(query.OperationFault);
            Assert.AreEqual(0, query.Value);
            numbers.Add(30);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(30, query.Value);
            numbers.Insert(9, 15);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(15, query.Value);
        }
    }
}
