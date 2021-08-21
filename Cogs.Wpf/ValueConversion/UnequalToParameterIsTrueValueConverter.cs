namespace Cogs.Wpf.ValueConversion;

/// <summary>
/// Compares the value to the parameter using <see cref="object.Equals(object?, object?)"/>,
/// converting to <c>true</c> when <c>false</c> and <c>false</c> when <c>true</c>
/// </summary>
[ValueConversion(typeof(object), typeof(bool))]
public class UnequalToParameterIsTrueValueConverter : IValueConverter
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
        !Equals(value, parameter);

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
    /// Gets a shared instance of <see cref="UnequalToParameterIsTrueValueConverter"/>
    /// </summary>
    public static UnequalToParameterIsTrueValueConverter Default { get; } = new UnequalToParameterIsTrueValueConverter();
}
