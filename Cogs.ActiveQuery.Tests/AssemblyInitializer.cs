using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;

namespace Cogs.ActiveQuery.Tests
{
    [TestClass]
    [SuppressMessage("Design", "CA1052:Static holder types should be Static or NotInheritable")]
    public class AssemblyInitializer
    {
        [AssemblyInitialize]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
        public static void AssemblyInit(TestContext context) => ActiveQueryOptions.Optimizer = ExpressionOptimizer.tryVisit;
    }
}
