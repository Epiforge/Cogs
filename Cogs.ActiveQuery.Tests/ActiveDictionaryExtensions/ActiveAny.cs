using Cogs.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Cogs.ActiveQuery.Tests.ActiveDictionaryExtensions
{
    [TestClass]
    public class ActiveAny
    {
        [TestMethod]
        public void ExpressionlessNonNotifying()
        {
            var numbers = Enumerable.Range(1, 10).ToDictionary(i => i, i => i * 3);
            using var query = numbers.ActiveAny();
            Assert.IsNull(query.OperationFault);
            Assert.IsTrue(query.Value);
        }

        [TestMethod]
        public void ExpressionlessNull()
        {
            using var query = ((IReadOnlyDictionary<object, object>)null!).ActiveAny();
            Assert.IsNotNull(query.OperationFault);
            Assert.IsFalse(query.Value);
        }

        [TestMethod]
        public void ExpressionlessSourceManipulation()
        {
            var numbers = new ObservableDictionary<int, int>(Enumerable.Range(1, 10).ToDictionary(i => i, i => i * 3));
            using var query = numbers.ActiveAny();
            Assert.IsNull(query.OperationFault);
            Assert.IsTrue(query.Value);
            numbers[1] = 2;
            Assert.IsTrue(query.Value);
            numbers.Remove(1);
            Assert.IsTrue(query.Value);
            --numbers[2];
            Assert.IsTrue(query.Value);
            numbers.Clear();
            Assert.IsFalse(query.Value);
            numbers.Add(1, 7);
            Assert.IsTrue(query.Value);
        }

        [TestMethod]
        public void SourceManipulation()
        {
            var numbers = new ObservableDictionary<int, int>(Enumerable.Range(1, 10).ToDictionary(i => i, i => i * 3));
            using var query = numbers.ActiveAny((key, value) => value % 3 != 0);
            Assert.IsNull(query.OperationFault);
            Assert.IsFalse(query.Value);
            numbers[1] = 2;
            Assert.IsTrue(query.Value);
            numbers.Remove(1);
            Assert.IsFalse(query.Value);
            --numbers[2];
            Assert.IsTrue(query.Value);
            numbers.Clear();
            Assert.IsFalse(query.Value);
            numbers.Add(1, 7);
            Assert.IsTrue(query.Value);
        }
    }
}
