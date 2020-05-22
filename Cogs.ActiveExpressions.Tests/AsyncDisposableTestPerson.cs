using Cogs.Disposal;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cogs.ActiveExpressions.Tests
{
    class AsyncDisposableTestPerson : AsyncDisposable
    {
        public AsyncDisposableTestPerson()
        {
        }

        public AsyncDisposableTestPerson(string name) => this.name = name;

        string? name;
        long nameGets;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected override async ValueTask<bool> DisposeAsync(bool disposing) => true;
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

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

        public static AsyncDisposableTestPerson operator -(AsyncDisposableTestPerson asyncDisposableTestPerson) => new AsyncDisposableTestPerson { name = new string(asyncDisposableTestPerson.name.Reverse().ToArray()) };
    }
}
