namespace Cogs.ActiveExpressions.Tests;

public class DisposableTestPerson : Disposable
{
    public DisposableTestPerson()
    {
    }

    public DisposableTestPerson(string name) => this.name = name;

    string? name;
    long nameGets;

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

    protected override bool Dispose(bool disposing) => true;

    protected override ValueTask<bool> DisposeAsync(bool disposing) => new(true);

    public override string ToString() => $"{{{name}}}";

    public static DisposableTestPerson CreateEmily() => new() { name = "Emily" };

    public static DisposableTestPerson CreateJohn() => new() { name = "John" };

    public static DisposableTestPerson operator +(DisposableTestPerson a, DisposableTestPerson b) => new()
    {
        name = $"{a.name} {b.name}",
    };

    public static DisposableTestPerson operator -(DisposableTestPerson disposableTestPerson) => new()
    {
        name = new string(disposableTestPerson.name?.Reverse().ToArray()),
    };
}
