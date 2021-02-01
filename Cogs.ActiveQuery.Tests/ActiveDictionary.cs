using Cogs.Collections;
using Cogs.Collections.Synchronized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Cogs.ActiveQuery.Tests
{
    [TestClass]
    public class ActiveDictionary
    {
        [TestMethod]
        public void ContainsKey()
        {
            using var ad = new ActiveDictionary<Guid, string>(new SynchronizedObservableDictionary<Guid, string>());
            ad.ContainsKey(default);
        }

        [TestMethod]
        public void ElementFaults()
        {
            var people = TestPerson.CreatePeopleCollection();
            using var query = people.ToActiveDictionary(person => new KeyValuePair<string, int>(person.Name!, 3 / person.Name!.Length));
            var changing = false;

            void elementFaultChanging(object? sender, ElementFaultChangeEventArgs e)
            {
                Assert.IsFalse(changing);
                Assert.AreSame(people[0], e.Element);
                Assert.AreEqual(1, e.Count);
                Assert.IsNull(e.Fault);
                changing = true;
            }

            void elementFaultChanged(object? sender, ElementFaultChangeEventArgs e)
            {
                Assert.IsTrue(changing);
                Assert.AreSame(people[0], e.Element);
                Assert.AreEqual(1, e.Count);
                Assert.IsInstanceOfType(e.Fault, typeof(DivideByZeroException));
                changing = false;
            }

            query.ElementFaultChanging += elementFaultChanging;
            query.ElementFaultChanged += elementFaultChanged;
            people[0].Name = string.Empty;
            Assert.IsFalse(changing);
            Assert.AreEqual(1, query.GetElementFaults().Count);
            query.ElementFaultChanging -= elementFaultChanging;
            query.ElementFaultChanged -= elementFaultChanged;
        }

        [TestMethod]
        public void Keys()
        {
            using var ad = new ActiveDictionary<Guid, string>(new SynchronizedObservableDictionary<Guid, string>());
            ad.Keys.ToString();
        }

        [TestMethod]
        public void NonGenericChangeEvents()
        {
            static void dictionaryChanged(object? sender, NotifyDictionaryChangedEventArgs<object?, object?> e)
            {
            }

            using var ad = new ActiveDictionary<Guid, string>(new SynchronizedObservableDictionary<Guid, string>());
            var nonGeneric = (INotifyDictionaryChanged)ad;
            nonGeneric.DictionaryChanged += dictionaryChanged;
            nonGeneric.DictionaryChanged -= dictionaryChanged;
        }

        [TestMethod]
        public void NonGenericEnumeration()
        {
            using var ad = new ActiveDictionary<Guid, string>(new SynchronizedObservableDictionary<Guid, string>());
            var enumerable = (IEnumerable)ad;
            foreach (var obj in enumerable)
                obj?.ToString();
        }

        [TestMethod]
        public void SynchronizationContext()
        {
            using var ad = new ActiveDictionary<Guid, string>(new SynchronizedObservableDictionary<Guid, string>());
            ad.SynchronizationContext!.ToString();
        }

        [TestMethod]
        public void RepeatedInstances()
        {
            using var ad1 = new ActiveDictionary<Guid, string>(new SynchronizedObservableDictionary<Guid, string>());
            using var ad2 = new ActiveDictionary<Guid, string>(ad1);
        }

        [TestMethod]
        public void TryGetValue()
        {
            using var ad = new ActiveDictionary<Guid, string>(new SynchronizedObservableDictionary<Guid, string>());
            ad.TryGetValue(default, out _);
        }
    }
}
