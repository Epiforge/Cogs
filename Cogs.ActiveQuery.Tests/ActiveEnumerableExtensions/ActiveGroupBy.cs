using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions
{
    [TestClass]
    public class ActiveGroupBy
    {
        [TestMethod]
        public void SourceManipulation()
        {
            var people = TestPerson.CreatePeopleCollection();
            using var groupsExpr = people.ActiveGroupBy(person => person.Name!.Length);
            using var orderedGroupMembersExpr = groupsExpr.ActiveSelect(group => Tuple.Create(group.Key, group.ActiveOrderBy(person => person.Name!)));
            using var orderedGroupsExpr = orderedGroupMembersExpr.ActiveOrderBy(group => group!.Item1);
            void checkMergedNames(string against) => Assert.AreEqual(against, string.Join(";", orderedGroupsExpr.Select(group => $"{group!.Item1}:{string.Join(",", group.Item2.Select(person => person.Name))}")));
            checkMergedNames("3:Ben;4:Erin,John;5:Bryan,Cliff,Craig,Emily,James,Steve;6:George,Hunter;7:Bridget,Charles,Nanette");
            people[0].Name = "Adam";
            checkMergedNames("3:Ben;4:Adam,Erin;5:Bryan,Cliff,Craig,Emily,James,Steve;6:George,Hunter;7:Bridget,Charles,Nanette");
            people[0].Name = "J";
            checkMergedNames("1:J;3:Ben;4:Erin;5:Bryan,Cliff,Craig,Emily,James,Steve;6:George,Hunter;7:Bridget,Charles,Nanette");
            people[0].Name = "John";
            checkMergedNames("3:Ben;4:Erin,John;5:Bryan,Cliff,Craig,Emily,James,Steve;6:George,Hunter;7:Bridget,Charles,Nanette");
            people.Add(new TestPerson("Daniel"));
            checkMergedNames("3:Ben;4:Erin,John;5:Bryan,Cliff,Craig,Emily,James,Steve;6:Daniel,George,Hunter;7:Bridget,Charles,Nanette");
            people.RemoveAt(people.Count - 1);
            checkMergedNames("3:Ben;4:Erin,John;5:Bryan,Cliff,Craig,Emily,James,Steve;6:George,Hunter;7:Bridget,Charles,Nanette");
        }

        [TestMethod]
        public void SourceManipulationSorted()
        {
            var people = TestPerson.CreatePeopleCollection();
            using var groupsExpr = people.ActiveGroupBy(person => person.Name!.Length, IndexingStrategy.SelfBalancingBinarySearchTree);
            using var orderedGroupMembersExpr = groupsExpr.ActiveSelect(group => Tuple.Create(group.Key, group.ActiveOrderBy(person => person.Name!)));
            using var orderedGroupsExpr = orderedGroupMembersExpr.ActiveOrderBy(group => group!.Item1);
            void checkMergedNames(string against) => Assert.AreEqual(against, string.Join(";", orderedGroupsExpr.Select(group => $"{group!.Item1}:{string.Join(",", group.Item2.Select(person => person.Name))}")));
            checkMergedNames("3:Ben;4:Erin,John;5:Bryan,Cliff,Craig,Emily,James,Steve;6:George,Hunter;7:Bridget,Charles,Nanette");
            people[0].Name = "Adam";
            checkMergedNames("3:Ben;4:Adam,Erin;5:Bryan,Cliff,Craig,Emily,James,Steve;6:George,Hunter;7:Bridget,Charles,Nanette");
            people[0].Name = "J";
            checkMergedNames("1:J;3:Ben;4:Erin;5:Bryan,Cliff,Craig,Emily,James,Steve;6:George,Hunter;7:Bridget,Charles,Nanette");
            people[0].Name = "John";
            checkMergedNames("3:Ben;4:Erin,John;5:Bryan,Cliff,Craig,Emily,James,Steve;6:George,Hunter;7:Bridget,Charles,Nanette");
            people.Add(new TestPerson("Daniel"));
            checkMergedNames("3:Ben;4:Erin,John;5:Bryan,Cliff,Craig,Emily,James,Steve;6:Daniel,George,Hunter;7:Bridget,Charles,Nanette");
            people.RemoveAt(people.Count - 1);
            checkMergedNames("3:Ben;4:Erin,John;5:Bryan,Cliff,Craig,Emily,James,Steve;6:George,Hunter;7:Bridget,Charles,Nanette");
        }

        [TestMethod]
        public void SourceManipulationUnindexed()
        {
            var argumentOutOfRangeThrown = false;
            var people = TestPerson.CreatePeopleCollection();
            try
            {
                using var groupsExpr = people.ActiveGroupBy(person => person.Name!.Length, IndexingStrategy.NoneOrInherit);
            }
            catch (ArgumentOutOfRangeException)
            {
                argumentOutOfRangeThrown = true;
            }
            Assert.IsTrue(argumentOutOfRangeThrown);
        }
    }
}
