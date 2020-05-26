using Cogs.Collections.Synchronized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions
{
    [TestClass]
    public class ActiveOfType
    {
        [TestMethod]
        public void NonNotifying()
        {
            var things = new List<object>(new object[]
            {
                0,
                false,
                "John",
                DateTime.Now,
                "Emily",
                Guid.NewGuid(),
                "Charles",
                TimeSpan.Zero,
                new object()
            });
            using var strings = things.ActiveOfType<string>();
            void checkStrings(params string[] against) => Assert.IsTrue(strings.OrderBy(s => s).SequenceEqual(against));
            checkStrings("Charles", "Emily", "John");
        }

        [TestMethod]
        public void SourceManipulation()
        {
            var things = new SynchronizedRangeObservableCollection<object>(new object[]
            {
                0,
                false,
                "John",
                DateTime.Now,
                "Emily",
                Guid.NewGuid(),
                "Charles",
                TimeSpan.Zero,
                new object()
            });
            using var strings = things.ActiveOfType<string>();
            void checkStrings(params string[] against) => Assert.IsTrue(strings.OrderBy(s => s).SequenceEqual(against));
            checkStrings("Charles", "Emily", "John");
            things.Add("Bridget");
            things.Remove("John");
            things.Move(things.Count - 1, 0);
            checkStrings("Bridget", "Charles", "Emily");
            things.Reset(new object[]
            {
                new object(),
                TimeSpan.Zero,
                "George",
                Guid.NewGuid(),
                "Craig",
                DateTime.Now,
                "Cliff",
                false,
                0
            });
            checkStrings("Cliff", "Craig", "George");
        }
    }
}
