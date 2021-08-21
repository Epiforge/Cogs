namespace Cogs.ActiveExpressions.Tests;

class TestPerson : PropertyChangeNotifier
{
    public TestPerson()
    {
    }

    public TestPerson(string name) => this.name = name;

    string? name;
    long nameGets;

    public override string ToString() => $"{{{name}}}";

    public string? Name
    {
        get
        {
            OnPropertyChanging(nameof(NameGets));
            Interlocked.Increment(ref nameGets);
            OnPropertyChanged(nameof(NameGets));
            return name;
        }
        set => SetBackedProperty(ref name, in value);
    }

    public long NameGets => Interlocked.Read(ref nameGets);

    public string? Placeholder => null;

    public static TestPerson CreateEmily() => new TestPerson { name = "Emily" };

    public static TestPerson CreateJohn() => new TestPerson { name = "John" };

    public static TestPerson operator +(TestPerson a, TestPerson b) => new TestPerson { name = $"{a.name} {b.name}" };

    public static TestPerson operator -(TestPerson testPerson) => new TestPerson { name = new string(testPerson.name?.Reverse().ToArray()) };
}
