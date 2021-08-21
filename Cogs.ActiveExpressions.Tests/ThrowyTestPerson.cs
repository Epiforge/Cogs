namespace Cogs.ActiveExpressions.Tests;

class ThrowyTestPerson : PropertyChangeNotifier
{
    public ThrowyTestPerson(string name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));
        this.name = name;
    }

    string name;

    public override string ToString() => $"{{{name}}}";

    public string Name
    {
        get => name;
        set
        {
            if (value is null)
                throw new NullReferenceException();
            SetBackedProperty(ref name, in value);
        }
    }
}
