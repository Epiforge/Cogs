namespace Cogs.Wpf.ValueConversion;

/// <summary>
/// Converts the value to its corresponding <see cref="Type"/>
/// </summary>
[ValueConversion(typeof(object), typeof(Type))]
public class ObjectIsTypeValueConverter :
    IValueConverter
{
    /// <summary>
    /// Converts a value
    /// </summary>
    /// <param name="value">The value produced by the binding source</param>
    /// <param name="targetType">The type of the binding target property</param>
    /// <param name="parameter">The converter parameter to use</param>
    /// <param name="culture">The culture to use in the converter</param>
    /// <returns>A converted value</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value?.GetType() ?? typeof(void);

    /// <summary>
    /// Converts a value
    /// </summary>
    /// <param name="value">The value produced by the binding source</param>
    /// <param name="targetType">The type of the binding target property</param>
    /// <param name="parameter">The converter parameter to use</param>
    /// <param name="culture">The culture to use in the converter</param>
    /// <returns>A converted value</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;

    /// <summary>
    /// Gets a shared instance of <see cref="AnyFalseIsCollapsedMultiValueConverter"/>
    /// </summary>
    public static ObjectIsTypeValueConverter Default { get; } = new ObjectIsTypeValueConverter();
}
