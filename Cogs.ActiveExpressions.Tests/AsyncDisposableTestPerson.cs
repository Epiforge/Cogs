namespace Cogs.ActiveExpressions.Tests;

class AsyncDisposableTestPerson : AsyncDisposable
{
    public AsyncDisposableTestPerson()
    {
    }

    public AsyncDisposableTestPerson(string name) => this.name = name;

    string? name;
    long nameGets;

    protected override ValueTask<bool> DisposeAsync(bool disposing) => new ValueTask<bool>(true);

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

    public static AsyncDisposableTestPerson CreateEmily() => new AsyncDisposableTestPerson { name = "Emily" };

    public static AsyncDisposableTestPerson CreateJohn() => new AsyncDisposableTestPerson { name = "John" };

    public static AsyncDisposableTestPerson operator +(AsyncDisposableTestPerson a, AsyncDisposableTestPerson b) => new AsyncDisposableTestPerson { name = $"{a.name} {b.name}" };

    public static AsyncDisposableTestPerson operator -(AsyncDisposableTestPerson asyncDisposableTestPerson) => new AsyncDisposableTestPerson { name = new string(asyncDisposableTestPerson.name?.Reverse().ToArray()) };
}
