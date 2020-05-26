using Cogs.Collections.Synchronized;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions
{
    [TestClass]
    public class ActiveFirst
    {
        [TestMethod]
        public void ExpressionlessEmptyNonNotifier()
        {
            var numbers = System.Linq.Enumerable.Empty<int>();
            using var query = numbers.ActiveFirst();
            Assert.IsNotNull(query.OperationFault);
            Assert.AreEqual(0, query.Value);
        }

        [TestMethod]
        public void ExpressionlessEmptySource()
        {
            var numbers = new SynchronizedRangeObservableCollection<int>();
            using var query = numbers.ActiveFirst();
            Assert.IsNotNull(query.OperationFault);
            Assert.AreEqual(0, query.Value);
        }

        [TestMethod]
        public void ExpressionlessNonNotifier()
        {
            var numbers = System.Linq.Enumerable.Range(0, 10);
            using var query = numbers.ActiveFirst();
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(0, query.Value);
        }

        [TestMethod]
        public void ExpressionlessSourceManipulation()
        {
            var numbers = new SynchronizedRangeObservableCollection<int>(System.Linq.Enumerable.Range(0, 10));
            using var query = numbers.ActiveFirst();
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(0, query.Value);
            numbers.Remove(0);
            Assert.AreEqual(1, query.Value);
            numbers.Clear();
            Assert.IsNotNull(query.OperationFault);
            Assert.AreEqual(0, query.Value);
            numbers.Add(30);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(30, query.Value);
        }

        [TestMethod]
        public void SourceManipulation()
        {
            var numbers = new SynchronizedRangeObservableCollection<int>(System.Linq.Enumerable.Range(0, 10));
            using var query = numbers.ActiveFirst(i => i % 3 == 0);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(0, query.Value);
            numbers.Remove(0);
            Assert.AreEqual(3, query.Value);
            numbers.RemoveAll(i => i % 3 == 0);
            Assert.IsNotNull(query.OperationFault);
            Assert.AreEqual(0, query.Value);
            numbers.Add(30);
            Assert.IsNull(query.OperationFault);
            Assert.AreEqual(30, query.Value);
        }
    }
}
