namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions;

[TestClass]
public class ActiveSelectMany
{
    [TestMethod]
    public void BadIndexingStrategy()
    {
        var words = Array.Empty<string>();
        var argumentOutOfRangeThrown = false;
        try
        {
            words.ActiveSelectMany(word => word.ToCharArray(), IndexingStrategy.NoneOrInherit);
        }
        catch (ArgumentOutOfRangeException)
        {
            argumentOutOfRangeThrown = true;
        }
        Assert.IsTrue(argumentOutOfRangeThrown);
    }

    [TestMethod]
    public void Initializer()
    {
        var teams = new SynchronizedRangeObservableCollection<TestTeam>()
        {
            new TestTeam(new SynchronizedRangeObservableCollection<TestPerson>()
            {
                new TestPerson("Emily")
            }),
            new TestTeam(new SynchronizedRangeObservableCollection<TestPerson>()
            {
                new TestPerson("Charles")
            }),
            new TestTeam(new SynchronizedRangeObservableCollection<TestPerson>()
            {
                new TestPerson("Erin")
            })
        };
        using var expr = teams.ActiveSelectMany(team => team.People!);
        Assert.AreEqual("EmilyCharlesErin", string.Join(string.Empty, expr.Select(person => person.Name)));
    }

    [TestMethod]
    public void SourceManipulation()
    {
        var teams = new SynchronizedRangeObservableCollection<TestTeam>();
        using var expr = teams.ActiveSelectMany(team => team.People!);
        void checkMergedNames(string against) => Assert.AreEqual(against, string.Join(string.Empty, expr.Select(person => person.Name)));
        checkMergedNames(string.Empty);
        var management = new TestTeam();
        management.People!.Add(new TestPerson("Charles"));
        teams.Add(management);
        checkMergedNames("Charles");
        management.People!.Add(new TestPerson("Michael"));
        checkMergedNames("CharlesMichael");
        management.People!.RemoveAt(1);
        checkMergedNames("Charles");
        var development = new TestTeam();
        teams.Add(development);
        checkMergedNames("Charles");
        development.People!.AddRange(new TestPerson[]
        {
            new TestPerson("John"),
            new TestPerson("Emily"),
            new TestPerson("Edward"),
            new TestPerson("Andrew")
        });
        checkMergedNames("CharlesJohnEmilyEdwardAndrew");
        development.People.RemoveRange(2, 2);
        checkMergedNames("CharlesJohnEmily");
        var qa = new TestTeam();
        qa.People!.AddRange(new TestPerson[]
        {
            new TestPerson("Aaron"),
            new TestPerson("Cliff")
        });
        teams.Add(qa);
        checkMergedNames("CharlesJohnEmilyAaronCliff");
        qa.People[0].Name = "Erin";
        checkMergedNames("CharlesJohnEmilyErinCliff");
        var bryan = new TestPerson("Brian");
        var it = new TestTeam();
        it.People!.AddRange(new TestPerson[] { bryan, bryan });
        teams.Add(it);
        checkMergedNames("CharlesJohnEmilyErinCliffBrianBrian");
        bryan.Name = "Bryan";
        checkMergedNames("CharlesJohnEmilyErinCliffBryanBryan");
        it.People.Clear();
        checkMergedNames("CharlesJohnEmilyErinCliff");
        it.People = null;
        checkMergedNames("CharlesJohnEmilyErinCliff");
        it.People = new SynchronizedRangeObservableCollection<TestPerson>()
        {
            new TestPerson("Paul")
        };
        checkMergedNames("CharlesJohnEmilyErinCliffPaul");
        it.People[0] = new TestPerson("Alex");
        checkMergedNames("CharlesJohnEmilyErinCliffAlex");
        development.People.Move(1, 0);
        checkMergedNames("CharlesEmilyJohnErinCliffAlex");
        development.People.ReplaceRange(0, 2, development.People.GetRange(0, 1));
        checkMergedNames("CharlesEmilyErinCliffAlex");
        it.People.Clear();
        it.People.Reset(new TestPerson[] { new TestPerson("Daniel") });
        checkMergedNames("CharlesEmilyErinCliffDaniel");
        teams.Add(management);
        checkMergedNames("CharlesEmilyErinCliffDanielCharles");
        management.People.Insert(0, new TestPerson("George"));
        checkMergedNames("GeorgeCharlesEmilyErinCliffDanielGeorgeCharles");
        var currentManagers = management.People;
        var otherManagers = new SynchronizedRangeObservableCollection<TestPerson>()
            {
                new TestPerson("Josh"),
                new TestPerson("Jessica")
            };
        management.People = otherManagers;
        checkMergedNames("JoshJessicaEmilyErinCliffDanielJoshJessica");
        management.People = currentManagers;
        teams.RemoveAt(teams.Count - 1);
        checkMergedNames("GeorgeCharlesEmilyErinCliffDaniel");
        teams.Insert(0, management);
        checkMergedNames("GeorgeCharlesGeorgeCharlesEmilyErinCliffDaniel");
        teams.Move(2, 1);
        checkMergedNames("GeorgeCharlesEmilyGeorgeCharlesErinCliffDaniel");
        teams.Move(1, 2);
        checkMergedNames("GeorgeCharlesGeorgeCharlesEmilyErinCliffDaniel");
        teams.RemoveAt(1);
        checkMergedNames("GeorgeCharlesEmilyErinCliffDaniel");
        teams.RemoveAt(0);
        checkMergedNames("EmilyErinCliffDaniel");
    }

    [TestMethod]
    public void SourceManipulationSorted()
    {
        var teams = new SynchronizedRangeObservableCollection<TestTeam>();
        using var expr = teams.ActiveSelectMany(team => team.People!, IndexingStrategy.SelfBalancingBinarySearchTree);
        void checkMergedNames(string against) => Assert.AreEqual(against, string.Join(string.Empty, expr.Select(person => person.Name)));
        checkMergedNames(string.Empty);
        var management = new TestTeam();
        management.People!.Add(new TestPerson("Charles"));
        teams.Add(management);
        checkMergedNames("Charles");
        management.People.Add(new TestPerson("Michael"));
        checkMergedNames("CharlesMichael");
        management.People.RemoveAt(1);
        checkMergedNames("Charles");
        var development = new TestTeam();
        teams.Add(development);
        checkMergedNames("Charles");
        development.People!.AddRange(new TestPerson[]
        {
            new TestPerson("John"),
            new TestPerson("Emily"),
            new TestPerson("Edward"),
            new TestPerson("Andrew")
        });
        checkMergedNames("CharlesJohnEmilyEdwardAndrew");
        development.People.RemoveRange(2, 2);
        checkMergedNames("CharlesJohnEmily");
        var qa = new TestTeam();
        qa.People!.AddRange(new TestPerson[]
        {
            new TestPerson("Aaron"),
            new TestPerson("Cliff")
        });
        teams.Add(qa);
        checkMergedNames("CharlesJohnEmilyAaronCliff");
        qa.People[0].Name = "Erin";
        checkMergedNames("CharlesJohnEmilyErinCliff");
        var bryan = new TestPerson("Brian");
        var it = new TestTeam();
        it.People!.AddRange(new TestPerson[] { bryan, bryan });
        teams.Add(it);
        checkMergedNames("CharlesJohnEmilyErinCliffBrianBrian");
        bryan.Name = "Bryan";
        checkMergedNames("CharlesJohnEmilyErinCliffBryanBryan");
        it.People.Clear();
        checkMergedNames("CharlesJohnEmilyErinCliff");
        it.People = null;
        checkMergedNames("CharlesJohnEmilyErinCliff");
        it.People = new SynchronizedRangeObservableCollection<TestPerson>()
        {
            new TestPerson("Paul")
        };
        checkMergedNames("CharlesJohnEmilyErinCliffPaul");
        it.People[0] = new TestPerson("Alex");
        checkMergedNames("CharlesJohnEmilyErinCliffAlex");
        development.People.Move(1, 0);
        checkMergedNames("CharlesEmilyJohnErinCliffAlex");
        development.People.ReplaceRange(0, 2, development.People.GetRange(0, 1));
        checkMergedNames("CharlesEmilyErinCliffAlex");
        it.People.Clear();
        it.People.Reset(new TestPerson[] { new TestPerson("Daniel") });
        checkMergedNames("CharlesEmilyErinCliffDaniel");
        teams.Add(management);
        checkMergedNames("CharlesEmilyErinCliffDanielCharles");
        management.People.Insert(0, new TestPerson("George"));
        checkMergedNames("GeorgeCharlesEmilyErinCliffDanielGeorgeCharles");
        var currentManagers = management.People;
        var otherManagers = new SynchronizedRangeObservableCollection<TestPerson>()
        {
            new TestPerson("Josh"),
            new TestPerson("Jessica")
        };
        management.People = otherManagers;
        checkMergedNames("JoshJessicaEmilyErinCliffDanielJoshJessica");
        management.People = currentManagers;
        teams.RemoveAt(teams.Count - 1);
        checkMergedNames("GeorgeCharlesEmilyErinCliffDaniel");
        teams.Insert(0, management);
        checkMergedNames("GeorgeCharlesGeorgeCharlesEmilyErinCliffDaniel");
        teams.Move(2, 1);
        checkMergedNames("GeorgeCharlesEmilyGeorgeCharlesErinCliffDaniel");
        teams.Move(1, 2);
        checkMergedNames("GeorgeCharlesGeorgeCharlesEmilyErinCliffDaniel");
        teams.RemoveAt(1);
        checkMergedNames("GeorgeCharlesEmilyErinCliffDaniel");
        teams.RemoveAt(0);
        checkMergedNames("EmilyErinCliffDaniel");
    }
}
