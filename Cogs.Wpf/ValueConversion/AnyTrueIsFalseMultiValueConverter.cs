namespace Cogs.Wpf.ValueConversion;

/// <summary>
/// Converts the values to <c>false</c> when any of them are <c>true</c>; otherwise, to <c>true</c>
/// </summary>
public sealed class AnyTrueIsFalseMultiValueConverter :
    IMultiValueConverter
{
    /// <summary>
    /// Converts source values to a value for the binding target
    /// </summary>
    /// <param name="values">The array of values that the source bindings in the <see cref="MultiBinding"/> produces</param>
    /// <param name="targetType">The type of the binding target property</param>
    /// <param name="parameter">The converter parameter to use</param>
    /// <param name="culture">The culture to use in the converter</param>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        !values.Any(obj => obj is bool boolean && boolean);

    /// <summary>
    /// Converts a binding target value to the source binding values
    /// </summary>
    /// <param name="value">The value that the binding target produces</param>
    /// <param name="targetTypes">The array of types to convert to</param>
    /// <param name="parameter">The converter parameter to use</param>
    /// <param name="culture">The culture to use in the converter</param>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        new object[] { DependencyProperty.UnsetValue };

    /// <summary>
    /// Gets a shared instance of <see cref="AnyTrueIsFalseMultiValueConverter"/>
    /// </summary>
    public static AnyTrueIsFalseMultiValueConverter Default { get; } = new AnyTrueIsFalseMultiValueConverter();
}
