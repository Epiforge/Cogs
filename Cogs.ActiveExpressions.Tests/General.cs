using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace Cogs.ActiveExpressions.Tests
{
    [TestClass]
    public class General
    {
        #region Helper Classes

        class DummyActiveExpression : ActiveExpression
        {
            public DummyActiveExpression() : base(typeof(bool), ExpressionType.Constant, null, false) => EvaluateIfNotDeferred();

            protected override bool Dispose(bool disposing) => false;
        }

        #endregion Helper Classes

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void BaseEquals()
        {
            using var dummy = new DummyActiveExpression();
            dummy.Equals(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void BaseGetHashCode()
        {
            using var dummy = new DummyActiveExpression();
            dummy.GetHashCode();
        }

        [TestMethod]
        public void CharStringConversion()
        {
            var person = new TestPerson("\\");
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using var expr = ActiveExpression.Create(p1 => p1.Name![0], person);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.AreEqual("{C} /* {\\} */.Name /* \"\\\\\" */[{C} /* 0 */] /* '\\\\' */", expr.ToString());
            person.Name = "\0";
            Assert.AreEqual("{C} /* {\0} */.Name /* \"\\0\" */[{C} /* 0 */] /* '\\0' */", expr.ToString());
            person.Name = "\a";
            Assert.AreEqual("{C} /* {\a} */.Name /* \"\\a\" */[{C} /* 0 */] /* '\\a' */", expr.ToString());
            person.Name = "\b";
            Assert.AreEqual("{C} /* {\b} */.Name /* \"\\b\" */[{C} /* 0 */] /* '\\b' */", expr.ToString());
            person.Name = "\f";
            Assert.AreEqual("{C} /* {\f} */.Name /* \"\\f\" */[{C} /* 0 */] /* '\\f' */", expr.ToString());
            person.Name = "\n";
            Assert.AreEqual("{C} /* {\n} */.Name /* \"\\n\" */[{C} /* 0 */] /* '\\n' */", expr.ToString());
            person.Name = "\r";
            Assert.AreEqual("{C} /* {\r} */.Name /* \"\\r\" */[{C} /* 0 */] /* '\\r' */", expr.ToString());
            person.Name = "\t";
            Assert.AreEqual("{C} /* {\t} */.Name /* \"\\t\" */[{C} /* 0 */] /* '\\t' */", expr.ToString());
            person.Name = "\v";
            Assert.AreEqual("{C} /* {\v} */.Name /* \"\\v\" */[{C} /* 0 */] /* '\\v' */", expr.ToString());
            person.Name = "x";
            Assert.AreEqual("{C} /* {x} */.Name /* \"x\" */[{C} /* 0 */] /* 'x' */", expr.ToString());
        }

        [TestMethod]
        public void CreateFromLambda()
        {
            using var expr = ActiveExpression.Create<int>(Expression.Lambda(Expression.Negate(Expression.Constant(3))));
            Assert.IsNull(expr.Fault);
            Assert.AreEqual(-3, expr.Value);
        }

        [TestMethod]
        public void CreateWithOptions()
        {
            using var expr = ActiveExpression.CreateWithOptions<int>(Expression.Lambda(Expression.Negate(Expression.Constant(3))), new ActiveExpressionOptions());
            Assert.IsNull(expr.Fault);
            Assert.AreEqual(-3, expr.Value);
        }

        [TestMethod]
        public void DateTimeStringConversion()
        {
            var now = DateTime.UtcNow;
            using var expr = ActiveExpression.Create(p1 => p1, now);
            Assert.AreEqual($"{{C}} /* new System.DateTime({now.Ticks}, System.DateTimeKind.Utc) */", expr.ToString());
        }

        [TestMethod]
        public void FaultedStringConversion()
        {
            TestPerson? noOne = null;
            using var expr = ActiveExpression.Create(p1 => p1!.Name, noOne);
            Assert.AreEqual($"{{C}} /* null */.Name /* [{typeof(NullReferenceException).Name}: {new NullReferenceException().Message}] */", expr.ToString());
        }

        [TestMethod]
        public void GuidStringConversion()
        {
            var guid = Guid.NewGuid();
            using var expr = ActiveExpression.Create(p1 => p1, guid);
            Assert.AreEqual($"{{C}} /* new System.Guid(\"{guid}\") */", expr.ToString());
        }

        [TestMethod]
        public void LambdaConsistentHashCode()
        {
            int hashCode1, hashCode2;
            using (var expr = ActiveExpression.Create<int>(Expression.Lambda(Expression.Negate(Expression.Constant(3)))))
                hashCode1 = expr.GetHashCode();
            using (var expr = ActiveExpression.Create<int>(Expression.Lambda(Expression.Negate(Expression.Constant(3)))))
                hashCode2 = expr.GetHashCode();
            Assert.IsTrue(hashCode1 == hashCode2);
        }

        [TestMethod]
        public void LambdaArguments()
        {
            using var expr = ActiveExpression.Create<int>(Expression.Lambda(Expression.Negate(Expression.Constant(3))));
            Assert.AreEqual(0, expr.Arguments.Count);
        }

        [TestMethod]
        public void LambdaOptions()
        {
            using var expr = ActiveExpression.Create<int>(Expression.Lambda(Expression.Negate(Expression.Constant(3))));
            Assert.IsNull(expr.Options);
        }

        [TestMethod]
        public void OneArgumentObjectEquals()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using var expr1 = ActiveExpression.Create(p => p.Name!.Length, john);
            using var expr2 = ActiveExpression.Create(p => p.Name!.Length, john);
            using var expr3 = ActiveExpression.Create(p => p.Name!.Length, emily);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.IsTrue(expr1.Equals((object?)expr2));
            Assert.IsFalse(expr1.Equals((object?)expr3));
            Assert.IsFalse(expr1.Equals((object?)null));
        }

        [TestMethod]
        public void OneArgumentOptions()
        {
            using var expr = ActiveExpression.Create(a => a, 1);
            Assert.IsNull(expr.Options);
        }

        [TestMethod]
        public void OperatorExpressionSyntax()
        {
            Assert.AreEqual("(1 + 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Add, typeof(int), 1, 2));
            Assert.AreEqual("checked(1 + 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.AddChecked, typeof(int), 1, 2));
            Assert.AreEqual("(1 & 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.And, typeof(int), 1, 2));
            Assert.AreEqual("((System.Object)1)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Convert, typeof(object), 1));
            Assert.AreEqual("checked((System.Int32)1)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.ConvertChecked, typeof(int), 1L));
            Assert.AreEqual("(1 - 1)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Decrement, typeof(int), 1));
            Assert.AreEqual("(1 / 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Divide, typeof(int), 1, 2));
            Assert.AreEqual("(1 == 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Equal, typeof(int), 1, 2));
            Assert.AreEqual("(1 ^ 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.ExclusiveOr, typeof(int), 1, 2));
            Assert.AreEqual("(1 > 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.GreaterThan, typeof(int), 1, 2));
            Assert.AreEqual("(1 >= 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.GreaterThanOrEqual, typeof(int), 1, 2));
            Assert.AreEqual("(1 + 1)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Increment, typeof(int), 1));
            Assert.AreEqual("(1 << 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.LeftShift, typeof(int), 1, 2));
            Assert.AreEqual("(1 < 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.LessThan, typeof(int), 1, 2));
            Assert.AreEqual("(1 <= 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.LessThanOrEqual, typeof(int), 1, 2));
            Assert.AreEqual("(1 % 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Modulo, typeof(int), 1, 2));
            Assert.AreEqual("(1 * 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Multiply, typeof(int), 1, 2));
            Assert.AreEqual("checked(1 * 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.MultiplyChecked, typeof(int), 1, 2));
            Assert.AreEqual("(-1)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Negate, typeof(int), 1));
            Assert.AreEqual("checked(-1)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.NegateChecked, typeof(int), 1));
            Assert.AreEqual("(!True)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Not, typeof(bool), true));
            Assert.AreEqual("(~1)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Not, typeof(int), 1));
            Assert.AreEqual("(1 != 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.NotEqual, typeof(int), 1, 2));
            Assert.AreEqual("(~1)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.OnesComplement, typeof(int), 1));
            Assert.AreEqual("(1 | 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Or, typeof(int), 1, 2));
            Assert.AreEqual("Math.Pow(1, 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Power, typeof(int), 1, 2));
            Assert.AreEqual("(1 >> 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.RightShift, typeof(int), 1, 2));
            Assert.AreEqual("(1 - 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Subtract, typeof(int), 1, 2));
            Assert.AreEqual("checked(1 - 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.SubtractChecked, typeof(int), 1, 2));
            Assert.AreEqual("(+1)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.UnaryPlus, typeof(int), 1));
            var outOfRangeThrown = false;
            try
            {
                ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.AddAssign, typeof(int), 1, 2);
            }
            catch (ArgumentOutOfRangeException)
            {
                outOfRangeThrown = true;
            }
            Assert.IsTrue(outOfRangeThrown);
        }

        [TestMethod]
        public void OptimizerAppliedDeMorgan()
        {
            var a = Expression.Parameter(typeof(bool));
            var b = Expression.Parameter(typeof(bool));
            using var expr = ActiveExpression.Create<bool>(Expression.Lambda<Func<bool, bool, bool>>(Expression.AndAlso(Expression.Not(a), Expression.Not(b)), a, b), false, false);
            Assert.AreEqual("(!({C} /* False */ || {C} /* False */) /* False */) /* True */", expr.ToString());
        }

        [TestMethod]
        public void NoArgumentObjectEquals()
        {
            using var expr1 = ActiveExpression.Create(() => 1);
            using var expr2 = ActiveExpression.Create(() => 1);
            using var expr3 = ActiveExpression.Create(() => 2);
            Assert.IsTrue(expr1.Equals((object?)expr2));
            Assert.IsFalse(expr1.Equals((object?)expr3));
            Assert.IsFalse(expr1.Equals((object?)null));
        }

        [TestMethod]
        public void QuotedExpressions()
        {
            using var expr = ActiveExpression.Create(p1 => p1 ? (Expression<Func<int>>)(() => 1) : () => 2, true);
            Assert.AreEqual(1, expr.Value!.Compile()());
        }

        [TestMethod]
        public void ThreeArgumentConsistentHashCode()
        {
            int hashCode1, hashCode2;
            using (var expr = ActiveExpression.Create((a, b, c) => a + b + c, 1, 2, 3))
                hashCode1 = expr.GetHashCode();
            using (var expr = ActiveExpression.Create((a, b, c) => a + b + c, 1, 2, 3))
                hashCode2 = expr.GetHashCode();
            Assert.IsTrue(hashCode1 == hashCode2);
        }

        [TestMethod]
        public void ThreeArgumentEquality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var charles = new TestPerson("Charles");
#pragma warning disable CS8604 // Possible null reference argument.
            using var expr1 = ActiveExpression.Create((a, b, c) => a + b + c, john, emily, charles);
            using var expr2 = ActiveExpression.Create((a, b, c) => a + b + c, john, emily, charles);
            using var expr3 = ActiveExpression.Create((a, b, c) => a + c + b, john, emily, charles);
            using var expr4 = ActiveExpression.Create((a, b, c) => a + b + c, charles, emily, john);
#pragma warning restore CS8604 // Possible null reference argument.
            Assert.IsTrue(expr1 == expr2);
            Assert.IsFalse(expr1 == expr3);
            Assert.IsFalse(expr1 == expr4);
        }

        [TestMethod]
        public void ThreeArgumentEquals()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var charles = new TestPerson("Charles");
#pragma warning disable CS8604 // Possible null reference argument.
            using var expr1 = ActiveExpression.Create((a, b, c) => a + b + c, john, emily, charles);
            using var expr2 = ActiveExpression.Create((a, b, c) => a + b + c, john, emily, charles);
            using var expr3 = ActiveExpression.Create((a, b, c) => a + c + b, john, emily, charles);
            using var expr4 = ActiveExpression.Create((a, b, c) => a + b + c, charles, emily, john);
#pragma warning restore CS8604 // Possible null reference argument.
            Assert.IsTrue(expr1.Equals((object)expr2));
            Assert.IsFalse(expr1.Equals((object)expr3));
            Assert.IsFalse(expr1.Equals((object)expr4));
        }

        [TestMethod]
        public void ThreeArgumentInequality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var charles = new TestPerson("Charles");
#pragma warning disable CS8604 // Possible null reference argument.
            using var expr1 = ActiveExpression.Create((a, b, c) => a + b + c, john, emily, charles);
            using var expr2 = ActiveExpression.Create((a, b, c) => a + b + c, john, emily, charles);
            using var expr3 = ActiveExpression.Create((a, b, c) => a + c + b, john, emily, charles);
            using var expr4 = ActiveExpression.Create((a, b, c) => a + b + c, charles, emily, john);
#pragma warning restore CS8604 // Possible null reference argument.
            Assert.IsFalse(expr1 != expr2);
            Assert.IsTrue(expr1 != expr3);
            Assert.IsTrue(expr1 != expr4);
        }

        [TestMethod]
        public void ThreeArgumentStringConversion()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var charles = new TestPerson("Charles");
#pragma warning disable CS8604 // Possible null reference argument.
            using var expr = ActiveExpression.Create((a, b, c) => a + b + c, john, emily, charles);
#pragma warning restore CS8604 // Possible null reference argument.
            Assert.AreEqual("(({C} /* {John} */ + {C} /* {Emily} */) /* {John Emily} */ + {C} /* {Charles} */) /* {John Emily Charles} */", expr.ToString());
        }

        [TestMethod]
        public void ThreeArgumentValueChanges()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var charles = new TestPerson("Charles");
            var values = new BlockingCollection<string>();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using (var expr = ActiveExpression.Create((a, b, c) => $"{a.Name} {b.Name} {c.Name}", john, emily, charles))
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            {
                void propertyChanged(object? sender, PropertyChangedEventArgs e) => values.Add(expr.Value!);
                expr.PropertyChanged += propertyChanged;
                values.Add(expr.Value!);
                john.Name = "J";
                emily.Name = "E";
                charles.Name = "C";
                expr.PropertyChanged -= propertyChanged;
            }
            Assert.IsTrue(new string[]
            {
                "John Emily Charles",
                "J Emily Charles",
                "J E Charles",
                "J E C"
            }.SequenceEqual(values));
        }

        [TestMethod]
        public void ThreeArgumentOptions()
        {
            using var expr = ActiveExpression.Create((a, b, c) => a + b + c, 1, 1, 1);
            Assert.IsNull(expr.Options);
        }

        [TestMethod]
        public void TimeSpanStringConversion()
        {
            var threeMinutes = TimeSpan.FromMinutes(3);
            using var expr = ActiveExpression.Create(p1 => p1, threeMinutes);
            Assert.AreEqual($"{{C}} /* new System.TimeSpan({threeMinutes.Ticks}) */", expr.ToString());
        }

        [TestMethod]
        public void TwoArgumentConsistentHashCode()
        {
            int hashCode1, hashCode2;
            using (var expr = ActiveExpression.Create((a, b) => a + b, 1, 2))
                hashCode1 = expr.GetHashCode();
            using (var expr = ActiveExpression.Create((a, b) => a + b, 1, 2))
                hashCode2 = expr.GetHashCode();
            Assert.IsTrue(hashCode1 == hashCode2);
        }

        [TestMethod]
        public void TwoArgumentObjectEquals()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using var expr1 = ActiveExpression.Create((p1, p2) => p1.Name!.Length + p2.Name!.Length, john, emily);
            using var expr2 = ActiveExpression.Create((p1, p2) => p1.Name!.Length + p2.Name!.Length, john, emily);
            using var expr3 = ActiveExpression.Create((p1, p2) => p1.Name!.Length + p2.Name!.Length, emily, john);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Assert.IsTrue(expr1.Equals((object?)expr2));
            Assert.IsFalse(expr1.Equals((object?)expr3));
            Assert.IsFalse(expr1.Equals((object?)null));
        }

        [TestMethod]
        public void TwoArgumentOptions()
        {
            using var expr = ActiveExpression.Create((a, b) => a + b, 1, 1);
            Assert.IsNull(expr.Options);
        }

        [TestMethod]
        public void UnsupportedExpressionType()
        {
            var expr = Expression.Lambda<Func<int>>(Expression.Block(Expression.Constant(3)));
            Assert.AreEqual(3, expr.Compile()());
            var notSupportedThrown = false;
            try
            {
                using var ae = ActiveExpression.Create(expr);
            }
            catch (NotSupportedException)
            {
                notSupportedThrown = true;
            }
            Assert.IsTrue(notSupportedThrown);
        }
    }
}
