namespace Cogs.Wpf.ValueConversion;

/// <summary>
/// Converts the value to a <see cref="string"/> when it is a <see cref="Nullable{Guid}"/>, optionally accepting a specifier to pass to <see cref="Guid.ToString(string)"/> as an argument
/// </summary>
[ValueConversion(typeof(Guid?), typeof(string))]
public class NullableGuidIsStringValueConverter : IValueConverter
{
    /// <summary>
    /// Converts a value
    /// </summary>
    /// <param name="value">The value produced by the binding source</param>
    /// <param name="targetType">The type of the binding target property</param>
    /// <param name="parameter">The converter parameter to use</param>
    /// <param name="culture">The culture to use in the converter</param>
    /// <returns>A converted value</returns>
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is Guid guid
        ?
        guid.ToString(parameter is string specifier ? specifier : "D", culture)
        :
        null;

    /// <summary>
    /// Converts a value
    /// </summary>
    /// <param name="value">The value produced by the binding source</param>
    /// <param name="targetType">The type of the binding target property</param>
    /// <param name="parameter">The converter parameter to use</param>
    /// <param name="culture">The culture to use in the converter</param>
    /// <returns>A converted value</returns>
    public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is string str && Guid.TryParse(str, out var guid) ? guid : null;

    /// <summary>
    /// Gets a shared instance of <see cref="NullableGuidIsStringValueConverter"/>
    /// </summary>
    public static NullableGuidIsStringValueConverter Default { get; } = new NullableGuidIsStringValueConverter();
}
