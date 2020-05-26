using Cogs.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Cogs.ActiveQuery.Tests.ActiveDictionaryExtensions
{
    [TestClass]
    public class ActiveAverage
    {
        [TestMethod]
        public void ExpressionlessSourceManipulation()
        {
            var numbers = new ObservableDictionary<string, decimal>();
            using var aggregate = numbers.ActiveAverage();
            Assert.IsNotNull(aggregate.OperationFault);
            Assert.AreEqual(0, aggregate.Value);
            numbers.Add("1", 1m);
            Assert.IsNull(aggregate.OperationFault);
            Assert.AreEqual(1m, aggregate.Value);
            numbers.AddRange(System.Linq.Enumerable.Range(2, 3).ToDictionary(i => i.ToString(), i => Convert.ToDecimal(i)));
            Assert.AreEqual(2.5m, aggregate.Value);
            numbers.RemoveRange(new string[] { "1", "2" });
            Assert.AreEqual(3.5m, aggregate.Value);
            numbers.Remove("3");
            Assert.AreEqual(4m, aggregate.Value);
            numbers.Remove("4");
            Assert.IsNotNull(aggregate.OperationFault);
            Assert.AreEqual(0m, aggregate.Value);
            numbers.Reset(System.Linq.Enumerable.Range(2, 3).ToDictionary(i => i.ToString(), i => Convert.ToDecimal(i)));
            Assert.IsNull(aggregate.OperationFault);
            Assert.AreEqual(3m, aggregate.Value);
        }
    }
}
