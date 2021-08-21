namespace Cogs.ActiveExpressions.Tests;

[TestClass]
public class Visitor
{
    [TestMethod]
    public void VisitBinary()
    {
        var visitor = new ExpressionDiagramVisitor();
        visitor.Visit((Expression<Func<int, int, int>>)((a, b) => a + b));
        Assert.IsTrue(visitor.Elements.SequenceEqual(new object?[]
        {
            false,
            ExpressionType.Lambda,
            typeof(Func<int, int, int>),
            typeof(int),
            false,

            false,
            ExpressionType.Add,
            typeof(int),
            false,
            false,
            null,

            false,
            ExpressionType.Parameter,
            typeof(int),
            0,
            0,

            false,
            ExpressionType.Parameter,
            typeof(int),
            0,
            1,

            false,
            ExpressionType.Parameter,
            typeof(int),
            0,
            0,

            false,
            ExpressionType.Parameter,
            typeof(int),
            0,
            1
        }));
    }

    [TestMethod]
    public void VisitConstant()
    {
        var visitor = new ExpressionDiagramVisitor();
        visitor.Visit((Expression<Func<int>>)(() => 1));
        Assert.IsTrue(visitor.Elements.SequenceEqual(new object?[]
        {
            false,
            ExpressionType.Lambda,
            typeof(Func<int>),
            typeof(int),
            false,

            false,
            ExpressionType.Constant,
            typeof(int),
            1
        }));
    }
}
