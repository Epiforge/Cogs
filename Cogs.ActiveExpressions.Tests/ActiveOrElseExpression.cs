using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;

namespace Cogs.ActiveExpressions.Tests
{
    [TestClass]
    public class ActiveOrElseExpression
    {
        [TestMethod]
        public void ConsistentHashCode()
        {
            int hashCode1, hashCode2;
            var john = TestPerson.CreateJohn();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using (var expr = ActiveExpression.Create(p1 => p1.Name != null || p1.Name!.Length > 0, john))
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                hashCode1 = expr.GetHashCode();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using (var expr = ActiveExpression.Create(p1 => p1.Name != null || p1.Name!.Length > 0, john))
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
            using var expr1 = ActiveExpression.Create(p1 => p1.Name != null || p1.Name!.Length > 0, john);
            using var expr2 = ActiveExpression.Create(p1 => p1.Name != null || p1.Name!.Length > 0, john);
            using var expr3 = ActiveExpression.Create(p1 => p1.Name == null && p1.Name!.Length == 0, john);
            using var expr4 = ActiveExpression.Create(p1 => p1.Name != null || p1.Name!.Length > 0, emily);
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
            using var expr1 = ActiveExpression.Create(p1 => p1.Name != null || p1.Name!.Length > 0, john);
            using var expr2 = ActiveExpression.Create(p1 => p1.Name != null || p1.Name!.Length > 0, john);
            using var expr3 = ActiveExpression.Create(p1 => p1.Name == null && p1.Name!.Length == 0, john);
            using var expr4 = ActiveExpression.Create(p1 => p1.Name != null || p1.Name!.Length > 0, emily);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.IsTrue(expr1.Equals(expr2));
            Assert.IsFalse(expr1.Equals(expr3));
            Assert.IsFalse(expr1.Equals(expr4));
        }

        [TestMethod]
        public void FaultPropagation()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using var expr = ActiveExpression.Create((p1, p2) => p1.Name!.Length > 0 || p2.Name!.Length > 0, john, emily);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
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
        public void FaultShortCircuiting()
        {
            var john = TestPerson.CreateJohn();
            TestPerson? noOne = null;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using var expr = ActiveExpression.Create((p1, p2) => !string.IsNullOrEmpty(p1.Name) || !string.IsNullOrEmpty(p2!.Name), john, noOne);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.IsTrue(expr.Value);
            Assert.IsNull(expr.Fault);
        }

        [TestMethod]
        public void Inequality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using var expr1 = ActiveExpression.Create(p1 => p1.Name != null || p1.Name!.Length > 0, john);
            using var expr2 = ActiveExpression.Create(p1 => p1.Name != null || p1.Name!.Length > 0, john);
            using var expr3 = ActiveExpression.Create(p1 => p1.Name == null && p1.Name!.Length == 0, john);
            using var expr4 = ActiveExpression.Create(p1 => p1.Name != null || p1.Name!.Length > 0, emily);
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
            var values = new BlockingCollection<bool>();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name!.Length == 1 || p2.Name!.Length == 1, john, emily))
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            {
                void propertyChanged(object? sender, PropertyChangedEventArgs e) => values.Add(expr.Value);
                expr.PropertyChanged += propertyChanged;
                values.Add(expr.Value);
                john.Name = "J";
                john.Name = "John";
                emily.Name = "E";
                emily.Name = "Emily";
                john.Name = "J";
                emily.Name = "E";
                john.Name = "John";
                expr.PropertyChanged -= propertyChanged;
            }
            Assert.IsTrue(new bool[] { false, true, false, true, false, true }.SequenceEqual(values));
        }

        [TestMethod]
        public void StringConversion()
        {
            var emily = TestPerson.CreateEmily();
            emily.Name = "X";
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using var expr = ActiveExpression.Create(p1 => p1.Name == "X" || p1.Name!.Length == 2, emily);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.AreEqual("(({C} /* {X} */.Name /* \"X\" */ == {C} /* \"X\" */) /* True */ || ({C} /* {X} */.Name /* \"X\" */.Length /* ? */ == {C} /* 2 */) /* ? */) /* True */", expr.ToString());
        }

        [TestMethod]
        public void ValueShortCircuiting()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using (var expr = ActiveExpression.Create((p1, p2) => p1.Name!.Length > 1 || p2.Name!.Length > 3, john, emily))
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                Assert.IsTrue(expr.Value);
            Assert.AreEqual(0, emily.NameGets);
        }
    }
}
