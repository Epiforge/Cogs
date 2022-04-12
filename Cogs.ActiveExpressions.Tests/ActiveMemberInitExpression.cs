namespace Cogs.ActiveExpressions.Tests;

[TestClass]
public class ActiveMemberInitExpression
{
    [TestMethod]
    public void ClassFields()
    {
        var emily = TestPerson.CreateEmily();
        using var expr = ActiveExpression.Create(() => new FieldyTestPerson { Name = emily.Name! });
        Assert.IsNull(expr.Fault);
        Assert.IsNotNull(expr.Value);
        Assert.AreEqual("Emily", expr.Value!.Name);
        emily.Name = "Em";
        Assert.IsNull(expr.Fault);
        Assert.IsNotNull(expr.Value);
        Assert.AreEqual("Em", expr.Value!.Name);
    }

    [TestMethod]
    public void ClassProperties()
    {
        var emily = TestPerson.CreateEmily();
        using var expr = ActiveExpression.Create(() => new TestPerson { Name = emily.Name! });
        Assert.IsNull(expr.Fault);
        Assert.IsNotNull(expr.Value);
        Assert.AreEqual("Emily", expr.Value!.Name);
        emily.Name = "Em";
        Assert.IsNull(expr.Fault);
        Assert.IsNotNull(expr.Value);
        Assert.AreEqual("Em", expr.Value!.Name);
    }

    [TestMethod]
    public void ConsistentHashCode()
    {
        int hashCode1, hashCode2;
        using (var expr = ActiveExpression.Create(() => new TestPerson { Name = "Emily" }))
            hashCode1 = expr.GetHashCode();
        using (var expr = ActiveExpression.Create(() => new TestPerson { Name = "Emily" }))
            hashCode2 = expr.GetHashCode();
        Assert.IsTrue(hashCode1 == hashCode2);
    }

    [TestMethod]
    public void Equality()
    {
        using var expr1 = ActiveExpression.Create(() => new TestPerson { Name = "Emily" });
        using var expr2 = ActiveExpression.Create(() => new TestPerson { Name = "Emily" });
        using var expr3 = ActiveExpression.Create(() => new TestPerson { Name = "Charles" });
        Assert.IsTrue(expr1 == expr2);
        Assert.IsFalse(expr1 == expr3);
    }

    [TestMethod]
    public void Equals()
    {
        using var expr1 = ActiveExpression.Create(() => new TestPerson { Name = "Emily" });
        using var expr2 = ActiveExpression.Create(() => new TestPerson { Name = "Emily" });
        using var expr3 = ActiveExpression.Create(() => new TestPerson { Name = "Charles" });
        Assert.IsTrue(expr1.Equals(expr2));
        Assert.IsFalse(expr1.Equals(expr3));
    }

    [TestMethod]
    public void Inequality()
    {
        using var expr1 = ActiveExpression.Create(() => new TestPerson { Name = "Emily" });
        using var expr2 = ActiveExpression.Create(() => new TestPerson { Name = "Emily" });
        using var expr3 = ActiveExpression.Create(() => new TestPerson { Name = "Charles" });
        Assert.IsFalse(expr1 != expr2);
        Assert.IsTrue(expr1 != expr3);
    }

    [TestMethod]
    public void MemberAssignmentFaultPropagation()
    {
        var emily = TestPerson.CreateEmily();
        using var expr = ActiveExpression.Create(() => new ThrowyTestPerson("Emily") { Name = emily.Name! });
        Assert.IsNull(expr.Fault);
        Assert.IsInstanceOfType(expr.Value, typeof(ThrowyTestPerson));
        emily.Name = null;
        Assert.IsNotNull(expr.Fault);
        Assert.IsNull(expr.Value);
        emily.Name = "Emily";
        Assert.IsNull(expr.Fault);
        Assert.IsInstanceOfType(expr.Value, typeof(ThrowyTestPerson));
        Assert.AreEqual(emily.Name, expr.Value!.Name);
    }

    [TestMethod]
    public void NewFaultPropagation()
    {
        var emily = TestPerson.CreateEmily();
        using var expr = ActiveExpression.Create(() => new ThrowyTestPerson(emily.Name!) { Name = "Emily" });
        Assert.IsNull(expr.Fault);
        Assert.IsInstanceOfType(expr.Value, typeof(ThrowyTestPerson));
        emily.Name = null;
        Assert.IsNotNull(expr.Fault);
        Assert.IsNull(expr.Value);
        emily.Name = "Emily";
        Assert.IsNull(expr.Fault);
        Assert.IsInstanceOfType(expr.Value, typeof(ThrowyTestPerson));
        Assert.AreEqual(emily.Name, expr.Value!.Name);
    }

    [TestMethod]
    public void StringConversion()
    {
        using var expr = ActiveExpression.Create(() => new TestPerson { Name = "Emily" });
        Assert.AreEqual($"new Cogs.ActiveExpressions.Tests.TestPerson() /* {expr.Value} */ {{ Name = {{C}} /* \"Emily\" */ }} /* {expr.Value} */", expr.ToString());
    }

    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void StructFields()
    {
        var emily = TestPerson.CreateEmily();
        using var expr = ActiveExpression.Create(() => new StructyTestPerson { Name = emily.Name! });
        Assert.IsNull(expr.Fault);
        Assert.IsNotNull(expr.Value);
        Assert.AreEqual("Emily", expr.Value!.Name);
        emily.Name = "Em";
        Assert.IsNull(expr.Fault);
        Assert.IsNotNull(expr.Value);
        Assert.AreEqual("Em", expr.Value!.Name);
    }

    [TestMethod]
    public async Task ValueAsyncDisposalAsync()
    {
        var options = new ActiveExpressionOptions();
        options.AddExpressionValueDisposal(() => new AsyncDisposableTestPerson());
        AsyncDisposableTestPerson? emily;
        var disposedTcs = new TaskCompletionSource<object?>();
        using (var expr = ActiveExpression.Create(() => new AsyncDisposableTestPerson { Name = "Emily" }, options))
        {
            Assert.IsNull(expr.Fault);
            emily = expr.Value;
            Assert.IsFalse(emily!.IsDisposed);
            emily.Disposed += (sender, e) => disposedTcs.SetResult(null);
        }
        await Task.WhenAny(disposedTcs.Task, Task.Delay(TimeSpan.FromSeconds(1)));
        Assert.IsTrue(emily.IsDisposed);
    }

    [TestMethod]
    public void ValueDisposal()
    {
        var options = new ActiveExpressionOptions();
        options.AddExpressionValueDisposal(() => new SyncDisposableTestPerson());
        SyncDisposableTestPerson? emily;
        using (var expr = ActiveExpression.Create(() => new SyncDisposableTestPerson { Name = "Emily" }, options))
        {
            Assert.IsNull(expr.Fault);
            emily = expr.Value;
            Assert.IsFalse(emily!.IsDisposed);
        }
        Assert.IsTrue(emily.IsDisposed);
    }
}
