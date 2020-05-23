using Cogs.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Cogs.ActiveExpressions.Tests
{
    [TestClass]
    public class ActiveMemberExpression
    {
        #region TestMethod Classes

        class TestObject : PropertyChangeNotifier
        {
            AsyncDisposableTestPerson? asyncDisposable;
            SyncDisposableTestPerson? syncDisposable;

            public AsyncDisposableTestPerson? AsyncDisposable
            {
                get => asyncDisposable;
                set => SetBackedProperty(ref asyncDisposable, in value);
            }

            public SyncDisposableTestPerson? SyncDisposable
            {
                get => syncDisposable;
                set => SetBackedProperty(ref syncDisposable, in value);
            }
        }

        #endregion TestMethod Classes

        [TestMethod]
        public void Closure()
        {
            var x = 3;
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using var expr = ActiveExpression.Create(p1 => p1.Name == null ? x : emily.Name!.Length, john);
            Assert.AreEqual(5, expr.Value);
            john.Name = null;
            Assert.AreEqual(3, expr.Value);
        }

        [TestMethod]
        public void ConsistentHashCode()
        {
            int hashCode1, hashCode2;
            var john = TestPerson.CreateJohn();
            using (var expr = ActiveExpression.Create(p1 => p1.Name, john))
                hashCode1 = expr.GetHashCode();
            using (var expr = ActiveExpression.Create(p1 => p1.Name, john))
                hashCode2 = expr.GetHashCode();
            Assert.IsTrue(hashCode1 == hashCode2);
        }

        [TestMethod]
        public void Equality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using var expr1 = ActiveExpression.Create(p1 => p1.Name, john);
            using var expr2 = ActiveExpression.Create(p1 => p1.Name, john);
            using var expr3 = ActiveExpression.Create(p1 => p1.Placeholder, john);
            using var expr4 = ActiveExpression.Create(p1 => p1.Name, emily);
            Assert.IsTrue(expr1 == expr2);
            Assert.IsFalse(expr1 == expr3);
            Assert.IsFalse(expr1 == expr4);
        }

        [TestMethod]
        public void Equals()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using var expr1 = ActiveExpression.Create(p1 => p1.Name, john);
            using var expr2 = ActiveExpression.Create(p1 => p1.Name, john);
            using var expr3 = ActiveExpression.Create(p1 => p1.Placeholder, john);
            using var expr4 = ActiveExpression.Create(p1 => p1.Name, emily);
            Assert.IsTrue(expr1.Equals(expr2));
            Assert.IsFalse(expr1.Equals(expr3));
            Assert.IsFalse(expr1.Equals(expr4));
        }

        [TestMethod]
        public void FieldValue()
        {
            var team = (developer: TestPerson.CreateJohn(), artist: TestPerson.CreateEmily());
            using var expr = ActiveExpression.Create(p1 => p1.artist.Name, team);
            Assert.AreEqual("Emily", expr.Value);
        }

        [TestMethod]
        public void Inequality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using var expr1 = ActiveExpression.Create(p1 => p1.Name, john);
            using var expr2 = ActiveExpression.Create(p1 => p1.Name, john);
            using var expr3 = ActiveExpression.Create(p1 => p1.Placeholder, john);
            using var expr4 = ActiveExpression.Create(p1 => p1.Name, emily);
            Assert.IsFalse(expr1 != expr2);
            Assert.IsTrue(expr1 != expr3);
            Assert.IsTrue(expr1 != expr4);
        }

        [TestMethod]
        public void ObjectFaultPropagation()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using var expr = ActiveExpression.Create((p1, p2) => (p1.Name!.Length > 0 ? p1 : p2).Name, john, emily);
            Assert.IsNull(expr.Fault);
            john.Name = null;
            Assert.IsNotNull(expr.Fault);
            john.Name = "John";
            Assert.IsNull(expr.Fault);
        }

        [TestMethod]
        public void StaticPropertyValue()
        {
            using var expr = ActiveExpression.Create(() => Environment.UserName);
            Assert.AreEqual(Environment.UserName, expr.Value);
        }

        [TestMethod]
        public async Task ValueAsyncDisposal()
        {
            var john = AsyncDisposableTestPerson.CreateJohn();
            var emily = AsyncDisposableTestPerson.CreateEmily();
            var testObject = new TestObject { AsyncDisposable = john };
            var options = new ActiveExpressionOptions();
            options.AddExpressionValueDisposal(() => new TestObject().AsyncDisposable);
            var disposedTcs = new TaskCompletionSource<object?>();
            using (var expr = ActiveExpression.Create(p1 => p1.AsyncDisposable, testObject, options))
            {
                Assert.AreSame(john, expr.Value);
                Assert.IsFalse(john.IsDisposed);
                john.Disposed += (sender, e) => disposedTcs.SetResult(null);
                testObject.AsyncDisposable = emily;
                await Task.WhenAny(disposedTcs.Task, Task.Delay(TimeSpan.FromSeconds(1)));
                Assert.AreSame(emily, expr.Value);
                Assert.IsFalse(emily.IsDisposed);
                Assert.IsTrue(john.IsDisposed);
                disposedTcs = new TaskCompletionSource<object?>();
                emily.Disposed += (sender, e) => disposedTcs.SetResult(null);
            }
            await Task.WhenAny(disposedTcs.Task, Task.Delay(TimeSpan.FromSeconds(1)));
            Assert.IsTrue(emily.IsDisposed);
        }

        [TestMethod]
        public void ValueDisposal()
        {
            var john = SyncDisposableTestPerson.CreateJohn();
            var emily = SyncDisposableTestPerson.CreateEmily();
            var testObject = new TestObject { SyncDisposable = john };
            var options = new ActiveExpressionOptions();
            options.AddExpressionValueDisposal(() => new TestObject().SyncDisposable);
            using (var expr = ActiveExpression.Create(p1 => p1.SyncDisposable, testObject, options))
            {
                Assert.AreSame(john, expr.Value);
                Assert.IsFalse(john.IsDisposed);
                testObject.SyncDisposable = emily;
                Assert.AreSame(emily, expr.Value);
                Assert.IsFalse(emily.IsDisposed);
                Assert.IsTrue(john.IsDisposed);
            }
            Assert.IsTrue(emily.IsDisposed);
        }
    }
}
