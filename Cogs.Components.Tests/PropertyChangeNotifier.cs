namespace Cogs.Components.Tests;

[TestClass]
public class PropertyChangeNotifier
{
    [TestMethod]
    public void PropertyChanges()
    {
        var propertiesChanged = new List<string>();
        var propertiesChanging = new List<string>();
        var derivation = new PropertyChangeNotifierDerivation("text");

        void propertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            var propertyName = e.PropertyName;
            Assert.IsNotNull(propertyName);
            Assert.AreEqual(propertiesChanging.Count - 1, propertiesChanged.Count);
            Assert.AreEqual(propertyName, propertiesChanging.Last());
            propertiesChanged.Add(propertyName!);
        }

        void propertyChanging(object? sender, PropertyChangingEventArgs e)
        {
            var propertyName = e.PropertyName;
            Assert.IsNotNull(propertyName);
            Assert.AreEqual(propertiesChanged.Count, propertiesChanged.Count);
            propertiesChanging.Add(propertyName!);
        }

        derivation.PropertyChanged += propertyChanged;
        derivation.PropertyChanging += propertyChanging;

        derivation.Text = "other text";
        derivation.NullableText = "Suprise!";
        derivation.NullableText = null;

        derivation.PropertyChanged -= propertyChanged;
        derivation.PropertyChanging -= propertyChanging;
    }
}
