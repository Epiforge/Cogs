namespace Cogs.ActiveExpressions.Tests;

class SyncDisposableTestPerson : SyncDisposable
{
    public SyncDisposableTestPerson()
    {
    }

    public SyncDisposableTestPerson(string name) => this.name = name;

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

    public override string ToString() => $"{{{name}}}";

    public static SyncDisposableTestPerson CreateEmily() => new() { name = "Emily" };

    public static SyncDisposableTestPerson CreateJohn() => new() { name = "John" };

    public static SyncDisposableTestPerson operator +(SyncDisposableTestPerson a, SyncDisposableTestPerson b) => new()
    {
        name = $"{a.name} {b.name}",
    };

    public static SyncDisposableTestPerson operator -(SyncDisposableTestPerson syncDisposableTestPerson) => new()
    {
        name = new string(syncDisposableTestPerson.name?.Reverse().ToArray()),
    };
}
