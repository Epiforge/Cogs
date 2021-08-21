namespace Cogs.ActiveExpressions.Tests;

[TestClass]
public class AssemblyInitializer
{
    [AssemblyInitialize]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
    public static void AssemblyInit(TestContext context) => ActiveExpression.Optimizer = ExpressionOptimizer.tryVisit;
}
