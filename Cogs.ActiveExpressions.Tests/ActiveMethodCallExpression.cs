using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Cogs.ActiveExpressions.Tests
{
    [TestClass]
    public class ActiveMethodCallExpression
    {
        #region TestMethod Methods

        AsyncDisposableTestPerson CombineAsyncDisposablePeople(AsyncDisposableTestPerson a, AsyncDisposableTestPerson b) => new AsyncDisposableTestPerson { Name = $"{a.Name} {b.Name}" };

        TestPerson CombinePeople(TestPerson a, TestPerson b) => new TestPerson { Name = $"{a.Name} {b.Name}" };

        SyncDisposableTestPerson CombineSyncDisposablePeople(SyncDisposableTestPerson a, SyncDisposableTestPerson b) => new SyncDisposableTestPerson { Name = $"{a.Name} {b.Name}" };

        TestPerson ReversedCombinePeople(TestPerson a, TestPerson b) => new TestPerson { Name = $"{b.Name} {a.Name}" };

        #endregion TestMethod Methods

        [TestMethod]
        public void ActuallyAProperty()
        {
            var emily = TestPerson.CreateEmily();
            using var expr = ActiveExpression.Create(Expression.Lambda<Func<string>>(Expression.Call(Expression.Constant(emily), typeof(TestPerson).GetProperty(nameof(TestPerson.Name))!.GetMethod)));
            Assert.IsNull(expr.Fault);
            Assert.AreEqual("Emily", expr.Value);
            emily.Name = "E";
            Assert.IsNull(expr.Fault);
            Assert.AreEqual("E", expr.Value);
        }

        [TestMethod]
        public void ArgumentFaultPropagation()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using var expr = ActiveExpression.Create(() => CombinePeople(john.Name!.Length > 3 ? john : null!, emily));
            Assert.IsNull(expr.Fault);
            john.Name = null;
            Assert.IsNotNull(expr.Fault);
            john.Name = "John";
            Assert.IsNull(expr.Fault);
        }

        [TestMethod]
        public void ConsistentHashCode()
        {
            int hashCode1, hashCode2;
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), john, emily))
                hashCode1 = expr.GetHashCode();
            using (var expr = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), john, emily))
                hashCode2 = expr.GetHashCode();
            Assert.IsTrue(hashCode1 == hashCode2);
        }

        [TestMethod]
        public void Equality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using var expr1 = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), john, emily);
            using var expr2 = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), john, emily);
            using var expr3 = ActiveExpression.Create((p1, p2) => ReversedCombinePeople(p1, p2), john, emily);
            using var expr4 = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), emily, john);
            Assert.IsTrue(expr1 == expr2);
            Assert.IsFalse(expr1 == expr3);
            Assert.IsFalse(expr1 == expr4);
        }

        [TestMethod]
        public void Equals()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using var expr1 = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), john, emily);
            using var expr2 = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), john, emily);
            using var expr3 = ActiveExpression.Create((p1, p2) => ReversedCombinePeople(p1, p2), john, emily);
            using var expr4 = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), emily, john);
            Assert.IsTrue(expr1.Equals(expr2));
            Assert.IsFalse(expr1.Equals(expr3));
            Assert.IsFalse(expr1.Equals(expr4));
        }

        [TestMethod]
        public void Inequality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using var expr1 = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), john, emily);
            using var expr2 = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), john, emily);
            using var expr3 = ActiveExpression.Create((p1, p2) => ReversedCombinePeople(p1, p2), john, emily);
            using var expr4 = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), emily, john);
            Assert.IsFalse(expr1 != expr2);
            Assert.IsTrue(expr1 != expr3);
            Assert.IsTrue(expr1 != expr4);
        }

        [TestMethod]
        public void ObjectFaultPropagation()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using var expr = ActiveExpression.Create(() => (john.Name!.Length > 3 ? this : null!).CombinePeople(john, emily));
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
            using var expr = ActiveExpression.Create((p1, p2) => CombinePeople(p1, p2), john, emily);
            Assert.AreEqual($"{{C}} /* {this} */.CombinePeople({{C}} /* {john} */, {{C}} /* {emily} */) /* {expr.Value} */", expr.ToString());
        }

        [TestMethod]
        public async Task ValueAsyncDisposal()
        {
            var john = AsyncDisposableTestPerson.CreateJohn();
            var emily = AsyncDisposableTestPerson.CreateEmily();
            var options = new ActiveExpressionOptions();
            options.AddExpressionValueDisposal(() => CombineAsyncDisposablePeople(null!, null!));
            AsyncDisposableTestPerson? first, second;
            var disposedTcs = new TaskCompletionSource<object?>();
            using (var expr = ActiveExpression.Create(() => CombineAsyncDisposablePeople(john.Name!.Length > 3 ? john : emily, emily), options))
            {
                Assert.IsNull(expr.Fault);
                first = expr.Value;
                Assert.IsFalse(first!.IsDisposed);
                first.Disposed += (sender, e) => disposedTcs.SetResult(null);
                john.Name = string.Empty;
                await Task.WhenAny(disposedTcs.Task, Task.Delay(TimeSpan.FromSeconds(1)));
                Assert.IsNull(expr.Fault);
                second = expr.Value;
                Assert.IsFalse(second!.IsDisposed);
                Assert.IsTrue(first.IsDisposed);
                disposedTcs = new TaskCompletionSource<object?>();
                second.Disposed += (sender, e) => disposedTcs.SetResult(null);
            }
            await Task.WhenAny(disposedTcs.Task, Task.Delay(TimeSpan.FromSeconds(1)));
            Assert.IsTrue(second.IsDisposed);
        }

        [TestMethod]
        public void ValueDisposal()
        {
            var john = SyncDisposableTestPerson.CreateJohn();
            var emily = SyncDisposableTestPerson.CreateEmily();
            var options = new ActiveExpressionOptions();
            options.AddExpressionValueDisposal(() => CombineSyncDisposablePeople(null!, null!));
            SyncDisposableTestPerson? first, second;
            using (var expr = ActiveExpression.Create(() => CombineSyncDisposablePeople(john.Name!.Length > 3 ? john : emily, emily), options))
            {
                Assert.IsNull(expr.Fault);
                first = expr.Value;
                Assert.IsFalse(first!.IsDisposed);
                john.Name = string.Empty;
                Assert.IsNull(expr.Fault);
                second = expr.Value;
                Assert.IsFalse(second!.IsDisposed);
                Assert.IsTrue(first.IsDisposed);
            }
            Assert.IsTrue(second.IsDisposed);
        }
    }
}
