namespace Cogs.Wpf.ValueConversion;

/// <summary>
/// Converts the value to <c>false</c> when not <c>null</c>; otherwise, <c>true</c>
/// </summary>
[ValueConversion(typeof(object), typeof(bool))]
public sealed class NotNullIsFalseValueConverter :
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
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is null;

    /// <summary>
    /// Converts a value
    /// </summary>
    /// <param name="value">The value produced by the binding source</param>
    /// <param name="targetType">The type of the binding target property</param>
    /// <param name="parameter">The converter parameter to use</param>
    /// <param name="culture">The culture to use in the converter</param>
    /// <returns>A converted value</returns>
    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool boolean && !boolean ? Binding.DoNothing : null;

    /// <summary>
    /// Gets a shared instance of <see cref="NotNullIsFalseValueConverter"/>
    /// </summary>
    public static NotNullIsFalseValueConverter Default { get; } = new NotNullIsFalseValueConverter();
}
