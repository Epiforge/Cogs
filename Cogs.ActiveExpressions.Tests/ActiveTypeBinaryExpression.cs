using Cogs.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;

namespace Cogs.ActiveExpressions.Tests
{
    [TestClass]
    public class ActiveTypeBinaryExpression
    {
        #region Helper Class

        class SomeObject : PropertyChangeNotifier
        {
            object? field;

            public object? Property
            {
                get => field;
                set => SetBackedProperty(ref field, in value);
            }
        }

        #endregion Helper Class

        [TestMethod]
        public void ConsistentHashCode()
        {
            int hashCode1, hashCode2;
            var john = TestPerson.CreateJohn();
            using (var expr = ActiveExpression.Create(p1 => p1 is TestPerson, (object)john))
                hashCode1 = expr.GetHashCode();
            using (var expr = ActiveExpression.Create(p1 => p1 is TestPerson, (object)john))
                hashCode2 = expr.GetHashCode();
            Assert.IsTrue(hashCode1 == hashCode2);
        }

        [TestMethod]
        public void Equality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using var expr1 = ActiveExpression.Create(p1 => p1 is TestPerson, (object)john);
            using var expr2 = ActiveExpression.Create(p1 => p1 is TestPerson, (object)john);
            using var expr3 = ActiveExpression.Create(p1 => p1 is DisposableTestPerson, (object)john);
            using var expr4 = ActiveExpression.Create(p1 => p1 is TestPerson, (object)emily);
            Assert.IsTrue(expr1 == expr2);
            Assert.IsFalse(expr1 == expr3);
            Assert.IsFalse(expr1 == expr4);
        }

        [TestMethod]
        public void Equals()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using var expr1 = ActiveExpression.Create(p1 => p1 is TestPerson, (object)john);
            using var expr2 = ActiveExpression.Create(p1 => p1 is TestPerson, (object)john);
            using var expr3 = ActiveExpression.Create(p1 => p1 is DisposableTestPerson, (object)john);
            using var expr4 = ActiveExpression.Create(p1 => p1 is TestPerson, (object)emily);
            Assert.IsTrue(expr1.Equals(expr2));
            Assert.IsFalse(expr1.Equals(expr3));
            Assert.IsFalse(expr1.Equals(expr4));
        }

        [TestMethod]
        public void FaultPropagation()
        {
            var john = TestPerson.CreateJohn();
#pragma warning disable CS0183, CS8602 // 'is' expression's given expression is always of the provided type, Dereference of a possibly null reference.
            using var expr = ActiveExpression.Create(p1 => p1.Name!.Length is int, john);
#pragma warning restore CS0183, CS8602 // 'is' expression's given expression is always of the provided type, Dereference of a possibly null reference.
            Assert.IsNull(expr.Fault);
            john.Name = null;
            Assert.IsNotNull(expr.Fault);
            john.Name = string.Empty;
            Assert.IsNull(expr.Fault);
        }

        [TestMethod]
        public void Inequality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using var expr1 = ActiveExpression.Create(p1 => p1 is TestPerson, (object)john);
            using var expr2 = ActiveExpression.Create(p1 => p1 is TestPerson, (object)john);
            using var expr3 = ActiveExpression.Create(p1 => p1 is DisposableTestPerson, (object)john);
            using var expr4 = ActiveExpression.Create(p1 => p1 is TestPerson, (object)emily);
            Assert.IsFalse(expr1 != expr2);
            Assert.IsTrue(expr1 != expr3);
            Assert.IsTrue(expr1 != expr4);
        }

        [TestMethod]
        public void PropertyChanges()
        {
            var john = TestPerson.CreateJohn();
            var someObject = new SomeObject();
            var values = new BlockingCollection<bool>();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using (var expr = ActiveExpression.Create(p1 => p1.Property is TestPerson, someObject))
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            {
                void propertyChanged(object? sender, PropertyChangedEventArgs e) => values.Add(expr.Value);
                expr.PropertyChanged += propertyChanged;
                values.Add(expr.Value);
                someObject.Property = john;
                someObject.Property = "John";
                someObject.Property = john;
                someObject.Property = null;
                expr.PropertyChanged -= propertyChanged;
            }
            Assert.IsTrue(new bool[] { false, true, false, true, false }.SequenceEqual(values));
        }

        [TestMethod]
        public void StringConversion()
        {
            var emily = TestPerson.CreateEmily();
            emily.Name = "X";
            using var expr = ActiveExpression.Create(p1 => p1 is TestPerson, (object)emily);
            Assert.AreEqual("({C} /* {X} */ is Cogs.ActiveExpressions.Tests.TestPerson) /* True */", expr.ToString());
        }
    }
}
