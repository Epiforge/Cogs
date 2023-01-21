using System.Reflection;

namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions;

[TestClass]
public class ActiveOrderBy
{
    [TestMethod]
    public void NoSelectors()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var expr = people.ActiveOrderBy();
        void checkMergedNames(string against) => Assert.AreEqual(against, string.Join(string.Empty, expr.Select(person => person.Name)));
        checkMergedNames("JohnEmilyCharlesErinCliffHunterBenCraigBridgetNanetteGeorgeBryanJamesSteve");
    }

    [TestMethod]
    public void SelectorsDirections()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var expr = people.ActiveOrderBy(new ActiveOrderingKeySelector<TestPerson>(person => person.Name!.Length, true), new ActiveOrderingKeySelector<TestPerson>(person => person.Name!));
        void checkMergedNames(string against) => Assert.AreEqual(against, string.Join(string.Empty, expr.Select(person => person.Name)));
        checkMergedNames("BridgetCharlesNanetteGeorgeHunterBryanCliffCraigEmilyJamesSteveErinJohnBen");
    }

    [TestMethod]
    public void SelectorImplicitConversion()
    {
        Expression<Func<TestPerson, IComparable>> selector1 = person => person.Name!.Length;
        var conversion1 = new ActiveOrderingKeySelector<TestPerson>(selector1);
        Expression<Func<TestPerson, IComparable>> selector2 = person => person.Name!;
        var conversion2 = new ActiveOrderingKeySelector<TestPerson>(selector2);
        var people = TestPerson.CreatePeopleCollection();
        using var query = people.ActiveOrderBy(conversion1, conversion2);
        void checkMergedNames(string against) => Assert.AreEqual(against, string.Join(string.Empty, query.Select(person => person.Name)));
        checkMergedNames("BenErinJohnBryanCliffCraigEmilyJamesSteveGeorgeHunterBridgetCharlesNanette");
    }

    [TestMethod]
    public void SelectorsOptions()
    {
        var people = TestPerson.CreatePeopleCollection();
        var options = new ActiveExpressionOptions();
        using var expr = people.ActiveOrderBy(new ActiveOrderingKeySelector<TestPerson>(person => person.Name!.Length, options), new ActiveOrderingKeySelector<TestPerson>(person => person.Name!, options));
        void checkMergedNames(string against) => Assert.AreEqual(against, string.Join(string.Empty, expr.Select(person => person.Name)));
        checkMergedNames("BenErinJohnBryanCliffCraigEmilyJamesSteveGeorgeHunterBridgetCharlesNanette");
    }

    [TestMethod]
    public void SourceManipulation()
    {
        var people = TestPerson.CreatePeopleCollection();
        people.Add(people[0]);
        using var expr = people.ActiveOrderBy(person => person.Name!);
        void checkMergedNames(string against) => Assert.AreEqual(against, string.Join(string.Empty, expr.Select(person => person.Name)));
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJohnJohnNanetteSteve");
        people.Add(people[0]);
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJohnJohnJohnNanetteSteve");
        people[0].Name = "Buddy";
        checkMergedNames("BenBridgetBryanBuddyBuddyBuddyCharlesCliffCraigEmilyErinGeorgeHunterJamesNanetteSteve");
        people[0].Name = "John";
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJohnJohnJohnNanetteSteve");
        people.RemoveRange(people.Count - 2, 2);
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJohnNanetteSteve");
        people.Add(new TestPerson("Javon"));
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJavonJohnNanetteSteve");
        people[0].Name = null;
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJavonNanetteSteve");
        people.Insert(0, new TestPerson());
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJavonNanetteSteve");
        people.RemoveRange(0, 2);
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJavonNanetteSteve");
        people.Add(new TestPerson());
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJavonNanetteSteve");
        people.Add(new TestPerson("Daniel"));
        checkMergedNames("BenBridgetBryanCharlesCliffCraigDanielEmilyErinGeorgeHunterJamesJavonNanetteSteve");
        people.Reset(new TestPerson[] { new TestPerson("Kelly") });
        checkMergedNames("Kelly");
    }

    [TestMethod]
    public void SourceManipulationMultipleSelectors()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var expr = people.ActiveOrderBy(new ActiveOrderingKeySelector<TestPerson>(person => person.Name!.Length), new ActiveOrderingKeySelector<TestPerson>(person => person.Name!));
        void checkMergedNames(string against) => Assert.AreEqual(against, string.Join(string.Empty, expr.Select(person => person.Name)));
        checkMergedNames("BenErinJohnBryanCliffCraigEmilyJamesSteveGeorgeHunterBridgetCharlesNanette");
        people[0].Name = "J";
        checkMergedNames("JBenErinBryanCliffCraigEmilyJamesSteveGeorgeHunterBridgetCharlesNanette");
        people[0].Name = "Johnathon";
        checkMergedNames("BenErinBryanCliffCraigEmilyJamesSteveGeorgeHunterBridgetCharlesNanetteJohnathon");
        Assert.AreEqual(0, expr.GetElementFaults().Count);
        people[0].Name = null;
        checkMergedNames("BenErinBryanCliffCraigEmilyJamesSteveGeorgeHunterBridgetCharlesNanette");
        Assert.AreEqual(1, expr.GetElementFaults().Count);
        var (element, fault) = expr.GetElementFaults()[0];
        Assert.AreSame(people[0], element);
        Assert.IsInstanceOfType(fault, typeof(TargetException));
        people[0].Name = "John";
        checkMergedNames("BenErinJohnBryanCliffCraigEmilyJamesSteveGeorgeHunterBridgetCharlesNanette");
        Assert.AreEqual(0, expr.GetElementFaults().Count);
        people.Add(new TestPerson("Daniel"));
        checkMergedNames("BenErinJohnBryanCliffCraigEmilyJamesSteveDanielGeorgeHunterBridgetCharlesNanette");
        people.RemoveAt(people.Count - 1);
        checkMergedNames("BenErinJohnBryanCliffCraigEmilyJamesSteveGeorgeHunterBridgetCharlesNanette");
        people.Reset(new TestPerson[] { new TestPerson("Kelly") });
        checkMergedNames("Kelly");
    }

    [TestMethod]
    public void SourceManipulationSorted()
    {
        var people = TestPerson.CreatePeopleCollection();
        people.Add(people[0]);
        using var expr = people.ActiveOrderBy(IndexingStrategy.SelfBalancingBinarySearchTree, person => person.Name!);
        void checkMergedNames(string against) => Assert.AreEqual(against, string.Join(string.Empty, expr.Select(person => person.Name)));
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJohnJohnNanetteSteve");
        people.Add(people[0]);
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJohnJohnJohnNanetteSteve");
        people[0].Name = "Buddy";
        checkMergedNames("BenBridgetBryanBuddyBuddyBuddyCharlesCliffCraigEmilyErinGeorgeHunterJamesNanetteSteve");
        people[0].Name = "John";
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJohnJohnJohnNanetteSteve");
        people.RemoveRange(people.Count - 2, 2);
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJohnNanetteSteve");
        people.Add(new TestPerson("Javon"));
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJavonJohnNanetteSteve");
        people[0].Name = null;
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJavonNanetteSteve");
        people.Insert(0, new TestPerson());
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJavonNanetteSteve");
        people.RemoveRange(0, 2);
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJavonNanetteSteve");
        people.Add(new TestPerson());
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJavonNanetteSteve");
        people.Add(new TestPerson("Daniel"));
        checkMergedNames("BenBridgetBryanCharlesCliffCraigDanielEmilyErinGeorgeHunterJamesJavonNanetteSteve");
        people.Reset(new TestPerson[] { new TestPerson("Kelly") });
        checkMergedNames("Kelly");
    }

    [TestMethod]
    public void SourceManipulationUnindexed()
    {
        var people = TestPerson.CreatePeopleCollection();
        people.Add(people[0]);
        using var expr = people.ActiveOrderBy(IndexingStrategy.NoneOrInherit, person => person.Name!);
        void checkMergedNames(string against) => Assert.AreEqual(against, string.Join(string.Empty, expr.Select(person => person.Name)));
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJohnJohnNanetteSteve");
        people.Add(people[0]);
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJohnJohnJohnNanetteSteve");
        people[0].Name = "Buddy";
        checkMergedNames("BenBridgetBryanBuddyBuddyBuddyCharlesCliffCraigEmilyErinGeorgeHunterJamesNanetteSteve");
        people[0].Name = "John";
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJohnJohnJohnNanetteSteve");
        people.RemoveRange(people.Count - 2, 2);
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJohnNanetteSteve");
        people.Add(new TestPerson("Javon"));
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJavonJohnNanetteSteve");
        people[0].Name = null;
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJavonNanetteSteve");
        people.Insert(0, new TestPerson());
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJavonNanetteSteve");
        people.RemoveRange(0, 2);
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJavonNanetteSteve");
        people.Add(new TestPerson());
        checkMergedNames("BenBridgetBryanCharlesCliffCraigEmilyErinGeorgeHunterJamesJavonNanetteSteve");
        people.Add(new TestPerson("Daniel"));
        checkMergedNames("BenBridgetBryanCharlesCliffCraigDanielEmilyErinGeorgeHunterJamesJavonNanetteSteve");
    }

    [TestMethod]
    public void UnindexedSelectors()
    {
        var people = TestPerson.CreatePeopleCollection();
        var options = new ActiveExpressionOptions();
        using var expr = people.ActiveOrderBy(IndexingStrategy.NoneOrInherit, new ActiveOrderingKeySelector<TestPerson>(person => person.Name!.Length), new ActiveOrderingKeySelector<TestPerson>(person => person.Name!));
        void checkMergedNames(string against) => Assert.AreEqual(against, string.Join(string.Empty, expr.Select(person => person.Name)));
        checkMergedNames("BenErinJohnBryanCliffCraigEmilyJamesSteveGeorgeHunterBridgetCharlesNanette");
    }

    [TestMethod]
    public void UnindexedSelectorsDirections()
    {
        var people = TestPerson.CreatePeopleCollection();
        using var expr = people.ActiveOrderBy(IndexingStrategy.NoneOrInherit, new ActiveOrderingKeySelector<TestPerson>(person => person.Name!.Length, true), new ActiveOrderingKeySelector<TestPerson>(person => person.Name!, false));
        void checkMergedNames(string against) => Assert.AreEqual(against, string.Join(string.Empty, expr.Select(person => person.Name)));
        checkMergedNames("BridgetCharlesNanetteGeorgeHunterBryanCliffCraigEmilyJamesSteveErinJohnBen");
    }

    [TestMethod]
    public void UnindexedSelectorsOptions()
    {
        var people = TestPerson.CreatePeopleCollection();
        var options = new ActiveExpressionOptions();
        using var expr = people.ActiveOrderBy(IndexingStrategy.NoneOrInherit, new ActiveOrderingKeySelector<TestPerson>(person => person.Name!.Length, options), new ActiveOrderingKeySelector<TestPerson>(person => person.Name!, options));
        void checkMergedNames(string against) => Assert.AreEqual(against, string.Join(string.Empty, expr.Select(person => person.Name)));
        checkMergedNames("BenErinJohnBryanCliffCraigEmilyJamesSteveGeorgeHunterBridgetCharlesNanette");
    }
}
