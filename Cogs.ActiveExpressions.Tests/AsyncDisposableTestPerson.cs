namespace Cogs.ActiveExpressions.Tests;

class AsyncDisposableTestPerson : AsyncDisposable
{
    public AsyncDisposableTestPerson()
    {
    }

    public AsyncDisposableTestPerson(string name) => this.name = name;

    string? name;
    long nameGets;

    protected override ValueTask<bool> DisposeAsync(bool disposing) => new(true);

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

    public static AsyncDisposableTestPerson CreateEmily() => new() { name = "Emily" };

    public static AsyncDisposableTestPerson CreateJohn() => new() { name = "John" };

    public static AsyncDisposableTestPerson operator +(AsyncDisposableTestPerson a, AsyncDisposableTestPerson b) => new() { name = $"{a.name} {b.name}" };

    public static AsyncDisposableTestPerson operator -(AsyncDisposableTestPerson asyncDisposableTestPerson) => new() { name = new string(asyncDisposableTestPerson.name?.Reverse().ToArray()) };
}
