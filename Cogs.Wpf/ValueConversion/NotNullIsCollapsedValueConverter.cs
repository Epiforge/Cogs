namespace Cogs.Wpf.ValueConversion;

/// <summary>
/// Converts the value to <see cref="Visibility.Collapsed"/> when not <c>null</c>; otherwise, <see cref="Visibility.Visible"/>
/// </summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public sealed class NotNullIsCollapsedValueConverter :
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
        value is not null ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// Converts a value
    /// </summary>
    /// <param name="value">The value produced by the binding source</param>
    /// <param name="targetType">The type of the binding target property</param>
    /// <param name="parameter">The converter parameter to use</param>
    /// <param name="culture">The culture to use in the converter</param>
    /// <returns>A converted value</returns>
    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility visibility && visibility != Visibility.Collapsed ? null : Binding.DoNothing;

    /// <summary>
    /// Gets a shared instance of <see cref="NotNullIsCollapsedValueConverter"/>
    /// </summary>
    public static NotNullIsCollapsedValueConverter Default { get; } = new NotNullIsCollapsedValueConverter();
}
