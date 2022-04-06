namespace Cogs.Disposal.Tests;

class PrimesToInt :
    DisposableValuesCache<int, PrimesToInt>.Value
{
    public IReadOnlyList<int> Primes { get; private set; } = Enumerable.Empty<int>().ToImmutableArray();

    protected override void OnInitialized()
    {
        var primes = new List<int>();
        if (Key >= 2)
            primes.Add(2);
        for (var i = 3; i <= Key; ++i)
        {
            if (!Enumerable.Range(2, i - 2).Any(d => i % d == 0))
                primes.Add(i);
        }
        Primes = primes.ToImmutableArray();
    }

    protected override void OnTerminated()
    {
    }
}
