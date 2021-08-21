namespace Cogs.ActiveExpressions.Tests;

[TestClass]
public class ActiveBinaryExpression
{
    [TestMethod]
    public void ConsistentHashCode()
    {
        int hashCode1, hashCode2;
        var john = TestPerson.CreateJohn();
        using (var expr = ActiveExpression.Create(p1 => p1.Name!.Length + 2, john))
            hashCode1 = expr.GetHashCode();
        using (var expr = ActiveExpression.Create(p1 => p1.Name!.Length + 2, john))
            hashCode2 = expr.GetHashCode();
        Assert.IsTrue(hashCode1 == hashCode2);
    }

    [TestMethod]
    public void Equality()
    {
        var john = TestPerson.CreateJohn();
        var emily = TestPerson.CreateEmily();
        using var expr1 = ActiveExpression.Create(p1 => p1.Name!.Length + 2, john);
        using var expr2 = ActiveExpression.Create(p1 => p1.Name!.Length + 2, john);
        using var expr3 = ActiveExpression.Create(p1 => p1.Name!.Length - 2, john);
        using var expr4 = ActiveExpression.Create(p1 => p1.Name!.Length + 2, emily);
        Assert.IsTrue(expr1 == expr2);
        Assert.IsFalse(expr1 == expr3);
        Assert.IsFalse(expr1 == expr4);
    }

    [TestMethod]
    public void Equals()
    {
        var john = TestPerson.CreateJohn();
        var emily = TestPerson.CreateEmily();
        using var expr1 = ActiveExpression.Create(p1 => p1.Name!.Length + 2, john);
        using var expr2 = ActiveExpression.Create(p1 => p1.Name!.Length + 2, john);
        using var expr3 = ActiveExpression.Create(p1 => p1.Name!.Length - 2, john);
        using var expr4 = ActiveExpression.Create(p1 => p1.Name!.Length + 2, emily);
        Assert.IsTrue(expr1.Equals(expr2));
        Assert.IsFalse(expr1.Equals(expr3));
        Assert.IsFalse(expr1.Equals(expr4));
    }

    [TestMethod]
    public void EvaluationFault()
    {
        var john = TestPerson.CreateJohn();
        TestPerson? noOne = null;
#pragma warning disable CS8604 // Possible null reference argument.
        using var expr = ActiveExpression.Create(() => john + noOne);
#pragma warning restore CS8604 // Possible null reference argument.
        Assert.IsNotNull(expr.Fault);
    }

    [TestMethod]
    public void FaultPropagation()
    {
        var john = TestPerson.CreateJohn();
        var emily = TestPerson.CreateEmily();
        using var expr = ActiveExpression.Create((p1, p2) => p1.Name!.Length + p2.Name!.Length, john, emily);
        Assert.IsNull(expr.Fault);
        john.Name = null;
        Assert.IsNotNull(expr.Fault);
        emily.Name = null;
        john.Name = string.Empty;
        Assert.IsNotNull(expr.Fault);
        emily.Name = "Emily";
        Assert.IsNull(expr.Fault);
    }

    [TestMethod]
    public void Inequality()
    {
        var john = TestPerson.CreateJohn();
        var emily = TestPerson.CreateEmily();
        using var expr1 = ActiveExpression.Create(p1 => p1.Name!.Length + 2, john);
        using var expr2 = ActiveExpression.Create(p1 => p1.Name!.Length + 2, john);
        using var expr3 = ActiveExpression.Create(p1 => p1.Name!.Length - 2, john);
        using var expr4 = ActiveExpression.Create(p1 => p1.Name!.Length + 2, emily);
        Assert.IsFalse(expr1 != expr2);
        Assert.IsTrue(expr1 != expr3);
        Assert.IsTrue(expr1 != expr4);
    }

    [TestMethod]
    public void PropertyChanges()
    {
        var john = TestPerson.CreateJohn();
        var emily = TestPerson.CreateEmily();
        var values = new BlockingCollection<int>();
        using (var expr = ActiveExpression.Create((p1, p2) => p1.Name!.Length + p2.Name!.Length, john, emily))
        {
            void propertyChanged(object? sender, PropertyChangedEventArgs e) => values.Add(expr.Value);
            expr.PropertyChanged += propertyChanged;
            values.Add(expr.Value);
            john.Name = "J";
            emily.Name = "E";
            john.Name = "John";
            john.Name = "J";
            emily.Name = "Emily";
            emily.Name = "E";
            expr.PropertyChanged -= propertyChanged;
        }
        Assert.IsTrue(new int[] { 9, 6, 2, 5, 2, 6, 2 }.SequenceEqual(values));
    }

    [TestMethod]
    public void StringConversion()
    {
        var emily = TestPerson.CreateEmily();
        emily.Name = "X";
        using var expr = ActiveExpression.Create(p1 => p1.Name!.Length + 1, emily);
        Assert.AreEqual("({C} /* {X} */.Name /* \"X\" */.Length /* 1 */ + {C} /* 1 */) /* 2 */", expr.ToString());
    }

    [TestMethod]
    public async Task ValueAsyncDisposalAsync()
    {
        var people = new ObservableCollection<AsyncDisposableTestPerson>
        {
            AsyncDisposableTestPerson.CreateJohn(),
            AsyncDisposableTestPerson.CreateEmily()
        };
        var disposedTcs = new TaskCompletionSource<object?>();
        AsyncDisposableTestPerson? newPerson;
        using (var expr = ActiveExpression.Create(p => p[0] + p[1], people))
        {
            newPerson = expr.Value;
            Assert.IsFalse(newPerson!.IsDisposed);
            newPerson.Disposed += (sender, e) => disposedTcs.SetResult(null);
            people[0] = AsyncDisposableTestPerson.CreateJohn();
            await Task.WhenAny(disposedTcs.Task, Task.Delay(TimeSpan.FromSeconds(1)));
            Assert.IsTrue(newPerson.IsDisposed);
            newPerson = expr.Value;
            Assert.IsFalse(newPerson!.IsDisposed);
            disposedTcs = new TaskCompletionSource<object?>();
            newPerson.Disposed += (sender, e) => disposedTcs.SetResult(null);
        }
        await Task.WhenAny(disposedTcs.Task, Task.Delay(TimeSpan.FromSeconds(1)));
        Assert.IsTrue(newPerson.IsDisposed);
    }

    [TestMethod]
    public void ValueDisposal()
    {
        var people = new ObservableCollection<SyncDisposableTestPerson>
        {
            SyncDisposableTestPerson.CreateJohn(),
            SyncDisposableTestPerson.CreateEmily()
        };
        SyncDisposableTestPerson? newPerson;
        using (var expr = ActiveExpression.Create(p => p[0] + p[1], people))
        {
            newPerson = expr.Value;
            Assert.IsFalse(newPerson!.IsDisposed);
            people[0] = SyncDisposableTestPerson.CreateJohn();
            Assert.IsTrue(newPerson.IsDisposed);
            newPerson = expr.Value;
            Assert.IsFalse(newPerson!.IsDisposed);
        }
        Assert.IsTrue(newPerson.IsDisposed);
    }
}
