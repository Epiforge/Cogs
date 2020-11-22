using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;

namespace Cogs.ActiveExpressions.Tests
{
    [TestClass]
    public class ActiveConditionalExpression
    {
        [TestMethod]
        public void ConsistentHashCode()
        {
            int hashCode1, hashCode2;
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name!.Length > 0 ? p1.Name : p2.Name, john, emily))
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                hashCode1 = expr.GetHashCode();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name!.Length > 0 ? p1.Name : p2.Name, john, emily))
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                hashCode2 = expr.GetHashCode();
            Assert.IsTrue(hashCode1 == hashCode2);
        }

        [TestMethod]
        public void Equality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using var expr1 = ActiveExpression.Create((p1, p2) => p1.Name!.Length > 0 ? p1.Name : p2.Name, john, emily);
            using var expr2 = ActiveExpression.Create((p1, p2) => p1.Name!.Length > 0 ? p1.Name : p2.Name, john, emily);
            using var expr3 = ActiveExpression.Create((p1, p2) => p2.Name!.Length > 0 ? p2.Name : p1.Name, john, emily);
            using var expr4 = ActiveExpression.Create((p1, p2) => p1.Name!.Length > 0 ? p1.Name : p2.Name, emily, john);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.IsTrue(expr1 == expr2);
            Assert.IsFalse(expr1 == expr3);
            Assert.IsFalse(expr1 == expr4);
        }

        [TestMethod]
        public void Equals()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using var expr1 = ActiveExpression.Create((p1, p2) => p1.Name!.Length > 0 ? p1.Name : p2.Name, john, emily);
            using var expr2 = ActiveExpression.Create((p1, p2) => p1.Name!.Length > 0 ? p1.Name : p2.Name, john, emily);
            using var expr3 = ActiveExpression.Create((p1, p2) => p2.Name!.Length > 0 ? p2.Name : p1.Name, john, emily);
            using var expr4 = ActiveExpression.Create((p1, p2) => p1.Name!.Length > 0 ? p1.Name : p2.Name, emily, john);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.IsTrue(expr1.Equals(expr2));
            Assert.IsFalse(expr1.Equals(expr3));
            Assert.IsFalse(expr1.Equals(expr4));
        }

        [TestMethod]
        public void FaultPropagationIfFalse()
        {
            var john = TestPerson.CreateJohn();
            john.Name = null;
            var emily = TestPerson.CreateEmily();
            emily.Name = null;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using var expr = ActiveExpression.Create((p1, p2) => p1.Name != null ? p1.Name.Length : p2.Name!.Length, john, emily);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.IsNotNull(expr.Fault);
            john.Name = "John";
            Assert.IsNull(expr.Fault);
            john.Name = null;
            Assert.IsNotNull(expr.Fault);
            emily.Name = "Emily";
            Assert.IsNull(expr.Fault);
        }

        [TestMethod]
        public void FaultPropagationIfTrue()
        {
            var john = TestPerson.CreateJohn();
            john.Name = null;
            var emily = TestPerson.CreateEmily();
            emily.Name = null;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using var expr = ActiveExpression.Create((p1, p2) => p2.Name == null ? p1.Name!.Length : p2.Name.Length, john, emily);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.IsNotNull(expr.Fault);
            emily.Name = "Emily";
            Assert.IsNull(expr.Fault);
            emily.Name = null;
            Assert.IsNotNull(expr.Fault);
            john.Name = "John";
            Assert.IsNull(expr.Fault);
        }

        [TestMethod]
        public void FaultPropagationTest()
        {
            var john = TestPerson.CreateJohn();
            john.Name = null;
            var emily = TestPerson.CreateEmily();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using var expr = ActiveExpression.Create((p1, p2) => p1.Name!.Length > 0 ? p1.Name : p2.Name, john, emily);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.IsNotNull(expr.Fault);
            john.Name = "John";
            Assert.IsNull(expr.Fault);
        }

        [TestMethod]
        public void FaultShortCircuiting()
        {
            var john = TestPerson.CreateJohn();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using var expr = ActiveExpression.Create<TestPerson, TestPerson, string>((p1, p2) => p1.Name!.Length > 0 ? p1.Name! : p2.Name!, john, null);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.AreEqual(john.Name, expr.Value);
            Assert.IsNull(expr.Fault);
        }

        [TestMethod]
        public void Inequality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using var expr1 = ActiveExpression.Create((p1, p2) => p1.Name!.Length > 0 ? p1.Name : p2.Name, john, emily);
            using var expr2 = ActiveExpression.Create((p1, p2) => p1.Name!.Length > 0 ? p1.Name : p2.Name, john, emily);
            using var expr3 = ActiveExpression.Create((p1, p2) => p2.Name!.Length > 0 ? p2.Name : p1.Name, john, emily);
            using var expr4 = ActiveExpression.Create((p1, p2) => p1.Name!.Length > 0 ? p1.Name : p2.Name, emily, john);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.IsFalse(expr1 != expr2);
            Assert.IsTrue(expr1 != expr3);
            Assert.IsTrue(expr1 != expr4);
        }

        [TestMethod]
        public void PropertyChanges()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var values = new BlockingCollection<string>();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using (var expr = ActiveExpression.Create((p1, p2) => string.IsNullOrEmpty(p1.Name) ? p2.Name : p1.Name, john, emily))
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            {
                void propertyChanged(object? sender, PropertyChangedEventArgs e) => values.Add(expr.Value!);
                expr.PropertyChanged += propertyChanged;
                values.Add(expr.Value!);
                john.Name = "J";
                john.Name = "John";
                john.Name = null;
                emily.Name = "E";
                emily.Name = "Emily";
                emily.Name = null;
                emily.Name = "Emily";
                john.Name = "John";
                expr.PropertyChanged -= propertyChanged;
            }
            Assert.IsTrue(new string[] { "John", "J", "John", "Emily", "E", "Emily", null!, "Emily", "John" }.SequenceEqual(values));
        }

        [TestMethod]
        public void ValueShortCircuiting()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name!.Length > 0 ? p1.Name : p2.Name, john, emily))
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                Assert.AreEqual(john.Name, expr.Value);
            Assert.AreEqual(0, emily.NameGets);
        }

        [TestMethod]
        public void StringConversion()
        {
            var john = TestPerson.CreateJohn();
            john.Name = "X";
            var emily = TestPerson.CreateEmily();
            emily.Name = "Y";
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using var expr = ActiveExpression.Create((p1, p2) => p1.Name == null ? p1 : p2, john, emily);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.AreEqual($"(({{C}} /* {john} */.Name /* \"X\" */ == {{C}} /* null */) /* False */ ? {{C}} /* {john} */ : {{C}} /* {emily} */) /* {emily} */", expr.ToString());
        }
    }
}
