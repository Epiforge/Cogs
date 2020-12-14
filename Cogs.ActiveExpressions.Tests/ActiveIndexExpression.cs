using Cogs.Collections;
using Cogs.Collections.Synchronized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Cogs.ActiveExpressions.Tests
{
    [TestClass]
    public class ActiveIndexExpression
    {
        #region TestRangeObservableCollection

        class TestRangeObservableCollection<T> : SynchronizedRangeObservableCollection<T>
        {
            public TestRangeObservableCollection() : base()
            {
            }

            public TestRangeObservableCollection(IEnumerable<T> collection) : base(collection)
            {
            }

            public void ChangeElementAndOnlyNotifyProperty(int index, T value)
            {
                Items[index] = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Item"));
            }
        }

        #endregion TestRangeObservableCollection

        [TestMethod]
        public void ArgumentChanges()
        {
            var reversedNumbersList = Enumerable.Range(1, 10).Reverse().ToImmutableList();
            var john = TestPerson.CreateJohn();
            var values = new BlockingCollection<int>();
            using (var expr = ActiveExpression.Create((p1, p2) => p1[p2.Name!.Length], reversedNumbersList, john))
            {
                void propertyChanged(object? sender, PropertyChangedEventArgs e) => values.Add(expr.Value);
                expr.PropertyChanged += propertyChanged;
                values.Add(expr.Value);
                john.Name = "J";
                john.Name = "Joh";
                john.Name = string.Empty;
                john.Name = "Johnny";
                john.Name = "John";
                expr.PropertyChanged -= propertyChanged;
            }
            Assert.IsTrue(new int[] { 6, 9, 7, 10, 4, 6 }.SequenceEqual(values));
        }

        [TestMethod]
        public void ArgumentFaultPropagation()
        {
            var numbers = new ObservableCollection<int>(Enumerable.Range(0, 10));
            var john = TestPerson.CreateJohn();
            using var expr = ActiveExpression.Create((p1, p2) => p1[p2.Name!.Length], numbers, john);
            Assert.IsNull(expr.Fault);
            john.Name = null;
            Assert.IsNotNull(expr.Fault);
            john.Name = "John";
            Assert.IsNull(expr.Fault);
        }

        [TestMethod]
        public void CollectionChanges()
        {
            var numbers = new SynchronizedRangeObservableCollection<int>(Enumerable.Range(1, 10));
            var values = new BlockingCollection<int>();
            using (var expr = ActiveExpression.Create(p1 => p1[5], numbers))
            {
                void propertyChanged(object? sender, PropertyChangedEventArgs e) => values.Add(expr.Value);
                expr.PropertyChanged += propertyChanged;
                values.Add(expr.Value);
                numbers.Add(11);
                numbers.Insert(0, 0);
                numbers.Remove(11);
                numbers.Remove(0);
                numbers[4] = 50;
                numbers[4] = 5;
                numbers[5] = 60;
                numbers[5] = 6;
                numbers[6] = 70;
                numbers[6] = 7;
                numbers.Move(0, 1);
                numbers.Move(0, 1);
                numbers.MoveRange(0, 5, 5);
                numbers.MoveRange(0, 5, 5);
                numbers.MoveRange(5, 0, 5);
                numbers.MoveRange(5, 0, 5);
                numbers.Reset(numbers.Select(i => i * 10).ToImmutableArray());
                expr.PropertyChanged -= propertyChanged;
            }
            Assert.IsTrue(new int[] { 6, 5, 6, 60, 6, 1, 6, 1, 6, 60 }.SequenceEqual(values));
        }

        [TestMethod]
        public void ConsistentHashCode()
        {
            int hashCode1, hashCode2;
            var john = TestPerson.CreateJohn();
            var men = new List<TestPerson> { john, null! };
            using (var expr = ActiveExpression.Create(p1 => p1[0], men))
                hashCode1 = expr.GetHashCode();
            using (var expr = ActiveExpression.Create(p1 => p1[0], men))
                hashCode2 = expr.GetHashCode();
            Assert.IsTrue(hashCode1 == hashCode2);
        }

        [TestMethod]
        public void DictionaryChanges()
        {
            var perfectNumbers = new ObservableDictionary<int, int>(Enumerable.Range(1, 10).ToDictionary(i => i, i => i * i));
            var values = new BlockingCollection<int>();
            using (var expr = ActiveExpression.Create(p1 => p1[5], perfectNumbers))
            {
                void propertyChanged(object? sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName == nameof(ActiveExpression<object?>.Value))
                        values.Add(expr.Value);
                }

                expr.PropertyChanged += propertyChanged;
                values.Add(expr.Value);
                perfectNumbers.Add(11, 11 * 11);
                perfectNumbers.AddRange(Enumerable.Range(12, 3).ToDictionary(i => i, i => i * i));
                perfectNumbers.Remove(11);
                perfectNumbers.RemoveRange(Enumerable.Range(12, 3));
                perfectNumbers.Remove(5);
                perfectNumbers.Add(5, 30);
                perfectNumbers[5] = 25;
                perfectNumbers.RemoveRange(Enumerable.Range(4, 3));
                perfectNumbers.AddRange(Enumerable.Range(4, 3).ToDictionary(i => i, i => i * i));
                perfectNumbers.Clear();
                expr.PropertyChanged -= propertyChanged;
            }
            Assert.IsTrue(new int[] { 25, 0, 30, 25, 0, 25, 0 }.SequenceEqual(values));
        }

        [TestMethod]
        public void Equality()
        {
            var john = TestPerson.CreateJohn();
            var men = new List<TestPerson> { john, null! };
            var emily = TestPerson.CreateEmily();
            var women = new List<TestPerson> { emily, null! };
            using var expr1 = ActiveExpression.Create(p1 => p1[0], men);
            using var expr2 = ActiveExpression.Create(p1 => p1[0], men);
            using var expr3 = ActiveExpression.Create(p1 => p1[1], men);
            using var expr4 = ActiveExpression.Create(p1 => p1[0], women);
            Assert.IsTrue(expr1 == expr2);
            Assert.IsFalse(expr1 == expr3);
            Assert.IsFalse(expr1 == expr4);
        }

        [TestMethod]
        public void Equals()
        {
            var john = TestPerson.CreateJohn();
            var men = new List<TestPerson> { john, null! };
            var emily = TestPerson.CreateEmily();
            var women = new List<TestPerson> { emily, null! };
            using var expr1 = ActiveExpression.Create(p1 => p1[0], men);
            using var expr2 = ActiveExpression.Create(p1 => p1[0], men);
            using var expr3 = ActiveExpression.Create(p1 => p1[1], men);
            using var expr4 = ActiveExpression.Create(p1 => p1[0], women);
            Assert.IsTrue(expr1.Equals(expr2));
            Assert.IsFalse(expr1.Equals(expr3));
            Assert.IsFalse(expr1.Equals(expr4));
        }

        [TestMethod]
        public void Inequality()
        {
            var john = TestPerson.CreateJohn();
            var men = new List<TestPerson> { john, null! };
            var emily = TestPerson.CreateEmily();
            var women = new List<TestPerson> { emily, null! };
            using var expr1 = ActiveExpression.Create(p1 => p1[0], men);
            using var expr2 = ActiveExpression.Create(p1 => p1[0], men);
            using var expr3 = ActiveExpression.Create(p1 => p1[1], men);
            using var expr4 = ActiveExpression.Create(p1 => p1[0], women);
            Assert.IsFalse(expr1 != expr2);
            Assert.IsTrue(expr1 != expr3);
            Assert.IsTrue(expr1 != expr4);
        }

        [TestMethod]
        public void ManualCreation()
        {
            var people = new List<TestPerson>() { TestPerson.CreateEmily() };
            using var expr = ActiveExpression.Create(Expression.Lambda<Func<string>>(Expression.MakeMemberAccess(Expression.MakeIndex(Expression.Constant(people), typeof(List<TestPerson>).GetProperties().First(p => p.GetIndexParameters().Length > 0), new Expression[] { Expression.Constant(0) }), typeof(TestPerson).GetProperty(nameof(TestPerson.Name))!)));
            Assert.IsNull(expr.Fault);
            Assert.AreEqual("Emily", expr.Value);
        }

        [TestMethod]
        public void ObjectChanges()
        {
            var john = TestPerson.CreateJohn();
            var men = new ObservableCollection<TestPerson> { john };
            var emily = TestPerson.CreateEmily();
            var women = new ObservableCollection<TestPerson> { emily };
            using var expr = ActiveExpression.Create((p1, p2) => (p1.Count > 0 ? p1 : p2)[0], men, women);
            Assert.AreSame(john, expr.Value);
            men.Clear();
            Assert.AreSame(emily, expr.Value);
        }

        [TestMethod]
        public void ObjectFaultPropagation()
        {
            var numbers = new ObservableCollection<int>(Enumerable.Range(0, 10));
            var otherNumbers = new ObservableCollection<int>(Enumerable.Range(0, 10));
            var john = TestPerson.CreateJohn();
            using var expr = ActiveExpression.Create((p1, p2, p3) => (p3.Name!.Length == 0 ? p1 : p2)[0], numbers, otherNumbers, john);
            Assert.IsNull(expr.Fault);
            john.Name = null;
            Assert.IsNotNull(expr.Fault);
            john.Name = "John";
            Assert.IsNull(expr.Fault);
        }

        [TestMethod]
        public void ObjectValueChanges()
        {
            var numbers = new TestRangeObservableCollection<int>(Enumerable.Range(0, 10));
            using var expr = ActiveExpression.Create(p1 => p1[0], numbers);
            Assert.AreEqual(expr.Value, 0);
            numbers.ChangeElementAndOnlyNotifyProperty(0, 100);
            Assert.AreEqual(expr.Value, 100);
        }

        [TestMethod]
        public void StringConversion()
        {
            var emily = TestPerson.CreateEmily();
            emily.Name = "X";
            var people = new ObservableCollection<TestPerson> { emily };
            using var expr = ActiveExpression.Create(p1 => p1[0].Name!.Length + 1, people);
            Assert.AreEqual($"({{C}} /* {people} */[{{C}} /* 0 */] /* {{X}} */.Name /* \"X\" */.Length /* 1 */ + {{C}} /* 1 */) /* 2 */", expr.ToString());
        }

        [TestMethod]
        public async Task ValueAsyncDisposal()
        {
            var john = AsyncDisposableTestPerson.CreateJohn();
            var emily = AsyncDisposableTestPerson.CreateEmily();
            var people = new ObservableCollection<AsyncDisposableTestPerson> { john };
            var options = new ActiveExpressionOptions();
            options.AddExpressionValueDisposal(() => new ObservableCollection<AsyncDisposableTestPerson>()[0]);
            var disposedTcs = new TaskCompletionSource<object?>();
            using (var ae = ActiveExpression.Create(p => p[0], people, options))
            {
                Assert.AreSame(john, ae.Value);
                Assert.IsFalse(john.IsDisposed);
                john.Disposed += (sender, e) => disposedTcs.SetResult(null);
                people[0] = emily;
                await Task.WhenAny(disposedTcs.Task, Task.Delay(TimeSpan.FromSeconds(1)));
                Assert.AreSame(emily, ae.Value);
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
            var people = new ObservableCollection<SyncDisposableTestPerson> { john };
            var options = new ActiveExpressionOptions();
            options.AddExpressionValueDisposal(() => new ObservableCollection<SyncDisposableTestPerson>()[0]);
            using (var ae = ActiveExpression.Create(p => p[0], people, options))
            {
                Assert.AreSame(john, ae.Value);
                Assert.IsFalse(john.IsDisposed);
                people[0] = emily;
                Assert.AreSame(emily, ae.Value);
                Assert.IsTrue(john.IsDisposed);
            }
            Assert.IsTrue(emily.IsDisposed);
        }
    }
}
