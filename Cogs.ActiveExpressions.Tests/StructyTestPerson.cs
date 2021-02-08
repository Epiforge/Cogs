namespace Cogs.ActiveExpressions.Tests
{
    struct StructyTestPerson
    {
        public StructyTestPerson(string? name) => Name = name;

        public string? Name;

        public override string ToString() => $"{{{Name}}}";
    }
}
