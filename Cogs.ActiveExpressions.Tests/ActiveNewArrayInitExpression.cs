namespace Cogs.ActiveExpressions.Tests;

[TestClass]
public class ActiveNewArrayInitExpression
{
    [TestMethod]
    public void ConsistentHashCode()
    {
        int hashCode1, hashCode2;
        var john = TestPerson.CreateJohn();
        var emily = TestPerson.CreateEmily();
        using (var expr = ActiveExpression.Create((p1, p2) => new string?[] { p1.Name, p2.Name }, john, emily))
            hashCode1 = expr.GetHashCode();
        using (var expr = ActiveExpression.Create((p1, p2) => new string?[] { p1.Name, p2.Name }, john, emily))
            hashCode2 = expr.GetHashCode();
        Assert.IsTrue(hashCode1 == hashCode2);
    }

    [TestMethod]
    public void Equality()
    {
        var john = TestPerson.CreateJohn();
        var emily = TestPerson.CreateEmily();
        using var expr1 = ActiveExpression.Create((p1, p2) => new string?[] { p1.Name, p2.Name }, john, emily);
        using var expr2 = ActiveExpression.Create((p1, p2) => new string?[] { p1.Name, p2.Name }, john, emily);
        using var expr3 = ActiveExpression.Create((p1, p2) => new string?[] { p2.Name, p1.Name }, john, emily);
        using var expr4 = ActiveExpression.Create((p1, p2) => new string?[] { p1.Name, p2.Name }, emily, john);
        Assert.IsTrue(expr1 == expr2);
        Assert.IsFalse(expr1 == expr3);
        Assert.IsFalse(expr1 == expr4);
    }

    [TestMethod]
    public void Equals()
    {
        var john = TestPerson.CreateJohn();
        var emily = TestPerson.CreateEmily();
        using var expr1 = ActiveExpression.Create((p1, p2) => new string?[] { p1.Name, p2.Name }, john, emily);
        using var expr2 = ActiveExpression.Create((p1, p2) => new string?[] { p1.Name, p2.Name }, john, emily);
        using var expr3 = ActiveExpression.Create((p1, p2) => new string?[] { p2.Name, p1.Name }, john, emily);
        using var expr4 = ActiveExpression.Create((p1, p2) => new string?[] { p1.Name, p2.Name }, emily, john);
        Assert.IsTrue(expr1.Equals(expr2));
        Assert.IsFalse(expr1.Equals(expr3));
        Assert.IsFalse(expr1.Equals(expr4));
    }

    [TestMethod]
    public void Inequality()
    {
        var john = TestPerson.CreateJohn();
        var emily = TestPerson.CreateEmily();
        using var expr1 = ActiveExpression.Create((p1, p2) => new string?[] { p1.Name, p2.Name }, john, emily);
        using var expr2 = ActiveExpression.Create((p1, p2) => new string?[] { p1.Name, p2.Name }, john, emily);
        using var expr3 = ActiveExpression.Create((p1, p2) => new string?[] { p2.Name, p1.Name }, john, emily);
        using var expr4 = ActiveExpression.Create((p1, p2) => new string?[] { p1.Name, p2.Name }, emily, john);
        Assert.IsFalse(expr1 != expr2);
        Assert.IsTrue(expr1 != expr3);
        Assert.IsTrue(expr1 != expr4);
    }

    [TestMethod]
    public void InitializerFaultPropagation()
    {
        var john = TestPerson.CreateJohn();
        var emily = TestPerson.CreateEmily();
        using var expr = ActiveExpression.Create(() => string.Concat(new string?[] { john.Name!.Length.ToString(), emily.Name!.Length.ToString() }));
        Assert.IsNull(expr.Fault);
        john.Name = null;
        Assert.IsNotNull(expr.Fault);
        john.Name = "John";
        Assert.IsNull(expr.Fault);
    }


    [TestMethod]
    public void StringConversion()
    {
        var john = TestPerson.CreateJohn();
        var emily = TestPerson.CreateEmily();
        using var expr = ActiveExpression.Create((p1, p2) => new string?[] { p1.Name, p2.Name }, john, emily);
        Assert.AreEqual($"new System.String[] {{{{C}} /* {john} */.Name /* \"{john.Name}\" */, {{C}} /* {emily} */.Name /* \"{emily.Name}\" */}} /* System.String[] */", expr.ToString());
    }
}
