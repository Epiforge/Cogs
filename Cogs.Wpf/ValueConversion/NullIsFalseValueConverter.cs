namespace Cogs.Wpf.ValueConversion;

/// <summary>
/// Converts the value to <c>false</c> when <c>null</c>; otherwise, <c>true</c>
/// </summary>
[ValueConversion(typeof(object), typeof(bool))]
public class NullIsFalseValueConverter : IValueConverter
{
    /// <summary>
    /// Converts a value
    /// </summary>
    /// <param name="value">The value produced by the binding source</param>
    /// <param name="targetType">The type of the binding target property</param>
    /// <param name="parameter">The converter parameter to use</param>
    /// <param name="culture">The culture to use in the converter</param>
    /// <returns>A converted value</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is not null;

    /// <summary>
    /// Converts a value
    /// </summary>
    /// <param name="value">The value produced by the binding source</param>
    /// <param name="targetType">The type of the binding target property</param>
    /// <param name="parameter">The converter parameter to use</param>
    /// <param name="culture">The culture to use in the converter</param>
    /// <returns>A converted value</returns>
    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool boolean && !boolean ? null : Binding.DoNothing;

    /// <summary>
    /// Gets a shared instance of <see cref="NullIsFalseValueConverter"/>
    /// </summary>
    public static NullIsFalseValueConverter Default { get; } = new NullIsFalseValueConverter();
}
