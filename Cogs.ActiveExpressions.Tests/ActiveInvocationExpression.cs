using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq.Expressions;

namespace Cogs.ActiveExpressions.Tests
{
    [TestClass]
    public class ActiveInvocationExpression
    {
        #region TestMethod Methods

        TestPerson CombinePeople(TestPerson a, TestPerson b) => new TestPerson { Name = $"{a.Name} {b.Name}" };

        TestPerson ReversedCombinePeople(TestPerson a, TestPerson b) => new TestPerson { Name = $"{b.Name} {a.Name}" };

        #endregion TestMethod Methods

        [TestMethod]
        public void ArgumentChangePropagation()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var firstParameter = Expression.Parameter(typeof(TestPerson));
            var secondParameter = Expression.Parameter(typeof(TestPerson));
            var testPersonNamePropertyInfo = typeof(TestPerson).GetProperty(nameof(TestPerson.Name));
            using var expr = ActiveExpression.Create<int>(Expression.Lambda(Expression.Invoke((Expression<Func<string, string, int>>)((p1, p2) => p1.Length + p2.Length), Expression.MakeMemberAccess(firstParameter, testPersonNamePropertyInfo), Expression.MakeMemberAccess(secondParameter, testPersonNamePropertyInfo)), firstParameter, secondParameter), john, emily);
            Assert.AreEqual(9, expr.Value);
            emily.Name = "Arya";
            Assert.AreEqual(8, expr.Value);
        }

        [TestMethod]
        public void ArgumentFaultPropagation()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var firstParameter = Expression.Parameter(typeof(TestPerson));
            var secondParameter = Expression.Parameter(typeof(TestPerson));
            var testPersonNamePropertyInfo = typeof(TestPerson).GetProperty(nameof(TestPerson.Name));
            var stringLengthPropertyInfo = typeof(string).GetProperty(nameof(string.Length));
            using var expr = ActiveExpression.Create<int>(Expression.Lambda(Expression.Invoke((Expression<Func<int, int, int>>)((p1, p2) => p1 + p2), Expression.MakeMemberAccess(Expression.MakeMemberAccess(firstParameter, testPersonNamePropertyInfo), stringLengthPropertyInfo), Expression.MakeMemberAccess(Expression.MakeMemberAccess(secondParameter, testPersonNamePropertyInfo), stringLengthPropertyInfo)), firstParameter, secondParameter), john, emily);
            Assert.IsNull(expr.Fault);
            emily.Name = null;
            Assert.IsNotNull(expr.Fault);
            emily.Name = "Emily";
            Assert.IsNull(expr.Fault);
        }

        [TestMethod]
        public void ConsistentHashCode()
        {
            int hashCode1, hashCode2;
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var firstParameter1 = Expression.Parameter(typeof(TestPerson));
            var secondParameter1 = Expression.Parameter(typeof(TestPerson));
            using (var expr = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke((Expression<Func<TestPerson, TestPerson, TestPerson>>)((p1, p2) => CombinePeople(p1, p2)), firstParameter1, secondParameter1), firstParameter1, secondParameter1), john, emily))
                hashCode1 = expr.GetHashCode();
            var firstParameter2 = Expression.Parameter(typeof(TestPerson));
            var secondParameter2 = Expression.Parameter(typeof(TestPerson));
            using (var expr = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke((Expression<Func<TestPerson, TestPerson, TestPerson>>)((p1, p2) => CombinePeople(p1, p2)), firstParameter2, secondParameter2), firstParameter2, secondParameter2), john, emily))
                hashCode2 = expr.GetHashCode();
            Assert.IsTrue(hashCode1 == hashCode2);
        }

        [TestMethod]
        public void Equality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var firstParameter1 = Expression.Parameter(typeof(TestPerson));
            var secondParameter1 = Expression.Parameter(typeof(TestPerson));
            using var expr1 = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke((Expression<Func<TestPerson, TestPerson, TestPerson>>)((p1, p2) => CombinePeople(p1, p2)), firstParameter1, secondParameter1), firstParameter1, secondParameter1), john, emily);
            var firstParameter2 = Expression.Parameter(typeof(TestPerson));
            var secondParameter2 = Expression.Parameter(typeof(TestPerson));
            using var expr2 = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke((Expression<Func<TestPerson, TestPerson, TestPerson>>)((p1, p2) => CombinePeople(p1, p2)), firstParameter2, secondParameter2), firstParameter2, secondParameter2), john, emily);
            var firstParameter3 = Expression.Parameter(typeof(TestPerson));
            var secondParameter3 = Expression.Parameter(typeof(TestPerson));
            using var expr3 = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke((Expression<Func<TestPerson, TestPerson, TestPerson>>)((p1, p2) => ReversedCombinePeople(p1, p2)), firstParameter3, secondParameter3), firstParameter3, secondParameter3), john, emily);
            var firstParameter4 = Expression.Parameter(typeof(TestPerson));
            var secondParameter4 = Expression.Parameter(typeof(TestPerson));
            using var expr4 = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke((Expression<Func<TestPerson, TestPerson, TestPerson>>)((p1, p2) => CombinePeople(p1, p2)), firstParameter4, secondParameter4), firstParameter4, secondParameter4), emily, john);
            Assert.IsTrue(expr1 == expr2);
            Assert.IsFalse(expr1 == expr3);
            Assert.IsFalse(expr1 == expr4);
        }

        [TestMethod]
        public void Equals()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var firstParameter1 = Expression.Parameter(typeof(TestPerson));
            var secondParameter1 = Expression.Parameter(typeof(TestPerson));
            using var expr1 = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke((Expression<Func<TestPerson, TestPerson, TestPerson>>)((p1, p2) => CombinePeople(p1, p2)), firstParameter1, secondParameter1), firstParameter1, secondParameter1), john, emily);
            var firstParameter2 = Expression.Parameter(typeof(TestPerson));
            var secondParameter2 = Expression.Parameter(typeof(TestPerson));
            using var expr2 = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke((Expression<Func<TestPerson, TestPerson, TestPerson>>)((p1, p2) => CombinePeople(p1, p2)), firstParameter2, secondParameter2), firstParameter2, secondParameter2), john, emily);
            var firstParameter3 = Expression.Parameter(typeof(TestPerson));
            var secondParameter3 = Expression.Parameter(typeof(TestPerson));
            using var expr3 = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke((Expression<Func<TestPerson, TestPerson, TestPerson>>)((p1, p2) => ReversedCombinePeople(p1, p2)), firstParameter3, secondParameter3), firstParameter3, secondParameter3), john, emily);
            var firstParameter4 = Expression.Parameter(typeof(TestPerson));
            var secondParameter4 = Expression.Parameter(typeof(TestPerson));
            using var expr4 = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke((Expression<Func<TestPerson, TestPerson, TestPerson>>)((p1, p2) => CombinePeople(p1, p2)), firstParameter4, secondParameter4), firstParameter4, secondParameter4), emily, john);
            Assert.IsTrue(expr1.Equals(expr2));
            Assert.IsFalse(expr1.Equals(expr3));
            Assert.IsFalse(expr1.Equals(expr4));
        }

        [TestMethod]
        public void ExpressionFaultPropagation()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var firstParameter = Expression.Parameter(typeof(TestPerson));
            var secondParameter = Expression.Parameter(typeof(TestPerson));
            var testPersonNamePropertyInfo = typeof(TestPerson).GetProperty(nameof(TestPerson.Name));
            using var expr = ActiveExpression.Create<int>(Expression.Lambda(Expression.Invoke((Expression<Func<string, string, int>>)((p1, p2) => p1.Length + p2.Length), Expression.MakeMemberAccess(firstParameter, testPersonNamePropertyInfo), Expression.MakeMemberAccess(secondParameter, testPersonNamePropertyInfo)), firstParameter, secondParameter), john, emily);
            Assert.IsNull(expr.Fault);
            emily.Name = null;
            Assert.IsNotNull(expr.Fault);
            emily.Name = "Emily";
            Assert.IsNull(expr.Fault);
        }

        [TestMethod]
        public void Inequality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var firstParameter1 = Expression.Parameter(typeof(TestPerson));
            var secondParameter1 = Expression.Parameter(typeof(TestPerson));
            using var expr1 = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke((Expression<Func<TestPerson, TestPerson, TestPerson>>)((p1, p2) => CombinePeople(p1, p2)), firstParameter1, secondParameter1), firstParameter1, secondParameter1), john, emily);
            var firstParameter2 = Expression.Parameter(typeof(TestPerson));
            var secondParameter2 = Expression.Parameter(typeof(TestPerson));
            using var expr2 = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke((Expression<Func<TestPerson, TestPerson, TestPerson>>)((p1, p2) => CombinePeople(p1, p2)), firstParameter2, secondParameter2), firstParameter2, secondParameter2), john, emily);
            var firstParameter3 = Expression.Parameter(typeof(TestPerson));
            var secondParameter3 = Expression.Parameter(typeof(TestPerson));
            using var expr3 = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke((Expression<Func<TestPerson, TestPerson, TestPerson>>)((p1, p2) => ReversedCombinePeople(p1, p2)), firstParameter3, secondParameter3), firstParameter3, secondParameter3), john, emily);
            var firstParameter4 = Expression.Parameter(typeof(TestPerson));
            var secondParameter4 = Expression.Parameter(typeof(TestPerson));
            using var expr4 = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke((Expression<Func<TestPerson, TestPerson, TestPerson>>)((p1, p2) => CombinePeople(p1, p2)), firstParameter4, secondParameter4), firstParameter4, secondParameter4), emily, john);
            Assert.IsFalse(expr1 != expr2);
            Assert.IsTrue(expr1 != expr3);
            Assert.IsTrue(expr1 != expr4);
        }

        [TestMethod]
        public void LambdaDelegateValue()
        {
            Func<TestPerson, TestPerson, TestPerson> @delegate = (p1, p2) => CombinePeople(p1, p2);
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var firstParameter = Expression.Parameter(typeof(TestPerson));
            var secondParameter = Expression.Parameter(typeof(TestPerson));
            using var expr = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke(Expression.Constant(@delegate), firstParameter, secondParameter), firstParameter, secondParameter), john, emily);
            Assert.AreEqual("John Emily", expr.Value!.Name);
        }

        [TestMethod]
        public void LambdaValue()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var firstParameter = Expression.Parameter(typeof(TestPerson));
            var secondParameter = Expression.Parameter(typeof(TestPerson));
            using var expr = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke((Expression<Func<TestPerson, TestPerson, TestPerson>>)((p1, p2) => CombinePeople(p1, p2)), firstParameter, secondParameter), firstParameter, secondParameter), john, emily);
            Assert.AreEqual("John Emily", expr.Value!.Name);
        }

        [TestMethod]
        public void LocalMethodDelegateValue()
        {
            TestPerson localMethod(TestPerson p1, TestPerson p2) => CombinePeople(p1, p2);
            Func<TestPerson, TestPerson, TestPerson> @delegate = localMethod;
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var firstParameter = Expression.Parameter(typeof(TestPerson));
            var secondParameter = Expression.Parameter(typeof(TestPerson));
            using var expr = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke(Expression.Constant(@delegate), firstParameter, secondParameter), firstParameter, secondParameter), john, emily);
            Assert.AreEqual("John Emily", expr.Value!.Name);
        }

        [TestMethod]
        public void MethodDelegateValue()
        {
            Func<TestPerson, TestPerson, TestPerson> @delegate = CombinePeople;
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var firstParameter = Expression.Parameter(typeof(TestPerson));
            var secondParameter = Expression.Parameter(typeof(TestPerson));
            using var expr = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke(Expression.Constant(@delegate), firstParameter, secondParameter), firstParameter, secondParameter), john, emily);
            Assert.AreEqual("John Emily", expr.Value!.Name);
        }

        [TestMethod]
        public void StringConversion()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var firstParameter = Expression.Parameter(typeof(TestPerson));
            var secondParameter = Expression.Parameter(typeof(TestPerson));
            using var expr = ActiveExpression.Create<TestPerson>(Expression.Lambda(Expression.Invoke((Expression<Func<TestPerson, TestPerson, TestPerson>>)((p1, p2) => CombinePeople(p1, p2)), firstParameter, secondParameter), firstParameter, secondParameter), john, emily);
            Assert.AreEqual("λ({C} /* Cogs.ActiveExpressions.Tests.ActiveInvocationExpression */.CombinePeople({C} /* {John} */, {C} /* {Emily} */) /* {John Emily} */)", expr.ToString());
        }
    }
}
