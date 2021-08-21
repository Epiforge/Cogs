namespace Cogs.ActiveQuery.Tests.ActiveDictionaryExtensions;

[TestClass]
public class ActiveOfType
{
    [TestMethod]
    public void SourceManipulation()
    {
        var things = new ObservableDictionary<int, object>
        {
            { 0, 0 },
            { 1, false },
            { 2, "John" },
            { 3, DateTime.Now },
            { 4, "Emily" },
            { 5, Guid.NewGuid() },
            { 6, "Charles" },
            { 7, TimeSpan.Zero },
            { 8, new object() }
        };
        using var strings = things.ActiveOfType<int, object, string>();
        void checkStrings(params string[] against) => Assert.IsTrue(strings.Values.OrderBy(s => s).SequenceEqual(against));
        checkStrings("Charles", "Emily", "John");
        things.Add(9, "Bridget");
        things.Remove(2);
        checkStrings("Bridget", "Charles", "Emily");
        things.Reset(new Dictionary<int, object>
        {
            { 0, new object() },
            { 1, TimeSpan.Zero },
            { 2, "George" },
            { 3, Guid.NewGuid() },
            { 4, "Craig" },
            { 5, DateTime.Now },
            { 6, "Cliff" },
            { 7, false },
            { 8, 0 }
        });
        checkStrings("Cliff", "Craig", "George");
    }
}
