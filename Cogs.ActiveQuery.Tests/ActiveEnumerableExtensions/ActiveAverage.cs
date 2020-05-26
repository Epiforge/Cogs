using Cogs.Collections.Synchronized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions
{
    [TestClass]
    public class ActiveAverage
    {
        [TestMethod]
        public void ExpressionlessSourceManipulation()
        {
            var numbers = new SynchronizedRangeObservableCollection<decimal>();
            using var aggregate = numbers.ActiveAverage();
            Assert.IsNotNull(aggregate.OperationFault);
            Assert.AreEqual(0, aggregate.Value);
            numbers.Add(1m);
            Assert.IsNull(aggregate.OperationFault);
            Assert.AreEqual(1m, aggregate.Value);
            numbers.AddRange(System.Linq.Enumerable.Range(2, 3).Select(i => Convert.ToDecimal(i)));
            Assert.AreEqual(2.5m, aggregate.Value);
            numbers.RemoveRange(0, 2);
            Assert.AreEqual(3.5m, aggregate.Value);
            numbers.RemoveAt(0);
            Assert.AreEqual(4m, aggregate.Value);
            numbers.RemoveAt(0);
            Assert.IsNotNull(aggregate.OperationFault);
            Assert.AreEqual(0m, aggregate.Value);
            numbers.Reset(System.Linq.Enumerable.Range(2, 3).Select(i => Convert.ToDecimal(i)));
            Assert.IsNull(aggregate.OperationFault);
            Assert.AreEqual(3m, aggregate.Value);
        }
    }
}
