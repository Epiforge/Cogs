using Cogs.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions
{
    [TestClass]
    public class SwitchContext
    {
        [TestMethod]
        public async Task EnumerableNotifierAsync()
        {
            var context = new AsyncSynchronizationContext();
            var people = new ObservableCollection<TestPerson>(TestPerson.MakePeople());
            using var query = ((IEnumerable)people).SwitchContext(context);
            Assert.AreEqual(14, query.Count);
            people.Add(new TestPerson("Daniel"));
            await Task.Delay(250);
            Assert.AreEqual(15, query.Count);
            people[14] = new TestPerson("Javon");
            await Task.Delay(250);
            Assert.AreEqual("Javon", ((TestPerson)query[14]).Name);
            people.Move(14, 0);
            await Task.Delay(250);
            Assert.AreEqual("Javon", ((TestPerson)query[0]).Name);
            people.RemoveAt(0);
            await Task.Delay(250);
            Assert.AreEqual(14, query.Count);
            people.Clear();
            await Task.Delay(250);
            Assert.AreEqual(0, query.Count);
        }

        [TestMethod]
        public async Task GenericNotifierAsync()
        {
            var context1 = new AsyncSynchronizationContext();
            var context2 = new AsyncSynchronizationContext();
            var people = TestPerson.CreatePeopleCollection(context1);
            using var query = people.SwitchContext(context2);
            Assert.AreEqual(14, query.Count);
            people.Add(new TestPerson("Daniel"));
            await Task.Delay(250);
            Assert.AreEqual(15, query.Count);
            people[14] = new TestPerson("Javon");
            await Task.Delay(250);
            Assert.AreEqual("Javon", query[14].Name);
            people.Move(14, 0);
            await Task.Delay(250);
            Assert.AreEqual("Javon", query[0].Name);
            people.RemoveAt(0);
            await Task.Delay(250);
            Assert.AreEqual(14, query.Count);
            people.Clear();
            await Task.Delay(250);
            Assert.AreEqual(0, query.Count);
        }

        [TestMethod]
        public async Task NotifierAsync()
        {
            var context = new AsyncSynchronizationContext();
            var people = new ObservableCollection<TestPerson>(TestPerson.MakePeople());
            using var query = people.SwitchContext(context);
            Assert.AreEqual(14, query.Count);
            people.Add(new TestPerson("Daniel"));
            await Task.Delay(250);
            Assert.AreEqual(15, query.Count);
            people[14] = new TestPerson("Javon");
            await Task.Delay(250);
            Assert.AreEqual("Javon", query[14].Name);
            people.Move(14, 0);
            await Task.Delay(250);
            Assert.AreEqual("Javon", query[0].Name);
            people.RemoveAt(0);
            await Task.Delay(250);
            Assert.AreEqual(14, query.Count);
            people.Clear();
            await Task.Delay(250);
            Assert.AreEqual(0, query.Count);
        }
    }
}
