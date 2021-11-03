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

#pragma warning disable CA1822 // Mark members as static
    public string? Placeholder => null;
#pragma warning restore CA1822 // Mark members as static

    public static TestPerson CreateEmily() => new() { name = "Emily" };

    public static TestPerson CreateJohn() => new() { name = "John" };

    public static TestPerson operator +(TestPerson a, TestPerson b) => new() { name = $"{a.name} {b.name}" };

    public static TestPerson operator -(TestPerson testPerson) => new() { name = new string(testPerson.name?.Reverse().ToArray()) };
}
