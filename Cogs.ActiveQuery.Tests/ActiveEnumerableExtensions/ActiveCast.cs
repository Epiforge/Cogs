using Cogs.Collections.Synchronized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions
{
    [TestClass]
    public class ActiveCast
    {
        [TestMethod]
        public void SourceManipulation()
        {
            var numbers = new SynchronizedRangeObservableCollection<int>(System.Linq.Enumerable.Range(1, 10));
            using var query = numbers.ActiveCast<object>();
            Assert.AreEqual(0, query.GetElementFaults().Count);
            Assert.AreEqual(55, query.Cast<int>().Sum());
            Assert.IsInstanceOfType(query[0], typeof(object));
            numbers[0] += 10;
            Assert.AreEqual(0, query.GetElementFaults().Count);
            Assert.AreEqual(65, query.Cast<int>().Sum());
        }
    }
}
