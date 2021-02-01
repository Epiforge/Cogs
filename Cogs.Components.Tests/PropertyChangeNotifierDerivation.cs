namespace Cogs.Components.Tests
{
    class PropertyChangeNotifierDerivation : Components.PropertyChangeNotifier
    {
        public PropertyChangeNotifierDerivation(string text) => this.text = text;

        string? nullableText;
        string text;

        public string? NullableText
        {
            get => nullableText;
            set => SetBackedProperty(ref nullableText, in value);
        }

        public string Text
        {
            get => text;
            set => SetBackedProperty(ref text, in value);
        }
    }
}
