using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq.Expressions;

namespace Cogs.ActiveExpressions.Tests
{
    [TestClass]
    public class Comparer
    {
        [TestMethod]
        public void Equals()
        {
            Expression<Func<int, int, int>> expr1 = (a, b) => a + b;
            Expression<Func<int, int, int>> expr2 = (x, y) => x + y;
            Assert.IsTrue(ExpressionEqualityComparer.Default.Equals(expr1, expr2));
        }

        [TestMethod]
        public new void GetHashCode()
        {
            Expression<Func<int, int, int>> expr1 = (a, b) => a + b;
            Expression<Func<int, int, int>> expr2 = (x, y) => x + y;
            Assert.IsTrue(ExpressionEqualityComparer.Default.GetHashCode(expr1) == ExpressionEqualityComparer.Default.GetHashCode(expr2));
        }
    }
}
