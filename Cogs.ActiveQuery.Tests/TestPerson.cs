namespace Cogs.ActiveQuery.Tests;

class TestPerson : PropertyChangeNotifier, IComparable<TestPerson>
{
    public static SynchronizedRangeObservableCollection<TestPerson> CreatePeopleCollection(SynchronizationContext? synchronizationContext = null) =>
        new SynchronizedRangeObservableCollection<TestPerson>(synchronizationContext, MakePeople());

    public static SynchronizedObservableDictionary<int, TestPerson> CreatePeopleDictionary(SynchronizationContext? synchronizationContext = null) =>
        new SynchronizedObservableDictionary<int, TestPerson>(synchronizationContext, MakePeople().Select((person, index) => (person, index)).ToDictionary(pi => pi.index, pi => pi.person));

    public static IEnumerable<TestPerson> MakePeople() => new TestPerson[]
    {
        new TestPerson("John"),
        new TestPerson("Emily"),
        new TestPerson("Charles"),
        new TestPerson("Erin"),
        new TestPerson("Cliff"),
        new TestPerson("Hunter"),
        new TestPerson("Ben"),
        new TestPerson("Craig"),
        new TestPerson("Bridget"),
        new TestPerson("Nanette"),
        new TestPerson("George"),
        new TestPerson("Bryan"),
        new TestPerson("James"),
        new TestPerson("Steve")
    };

    public static TestPerson operator +(TestPerson a, TestPerson b) => new TestPerson { name = $"{a.name} {b.name}" };

    public static TestPerson operator -(TestPerson testPerson) => new TestPerson { name = new string(testPerson.name?.Reverse().ToArray()) };

    public TestPerson()
    {
    }

    public TestPerson(string? name) => this.name = name;

    string? name;

    public int CompareTo(TestPerson? other) => GetHashCode().CompareTo(other?.GetHashCode() ?? 0);

    public string? Name
    {
        get => name;
        set => SetBackedProperty(ref name, in value);
    }
}
