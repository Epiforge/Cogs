namespace Cogs.Wpf.ValueConversion;

/// <summary>
/// Converts the value to <see cref="Visibility.Collapsed"/> when equal to zero; otherwise, <see cref="Visibility.Visible"/>
/// </summary>
[ValueConversion(typeof(byte), typeof(Visibility))]
[ValueConversion(typeof(sbyte), typeof(Visibility))]
[ValueConversion(typeof(short), typeof(Visibility))]
[ValueConversion(typeof(ushort), typeof(Visibility))]
[ValueConversion(typeof(int), typeof(Visibility))]
[ValueConversion(typeof(uint), typeof(Visibility))]
[ValueConversion(typeof(long), typeof(Visibility))]
[ValueConversion(typeof(ulong), typeof(Visibility))]
[ValueConversion(typeof(float), typeof(Visibility))]
[ValueConversion(typeof(double), typeof(Visibility))]
[ValueConversion(typeof(decimal), typeof(Visibility))]
[ValueConversion(typeof(BigInteger), typeof(Visibility))]
public class NotZeroIsCollapsedValueConverter : IValueConverter
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
        value switch
        {
            byte b => b != 0,
            sbyte sb => sb != 0,
            short s => s != 0,
            ushort us => us != 0,
            int i => i != 0,
            uint ui => ui != 0,
            long l => l != 0,
            ulong ul => ul != 0,
            float f => f != 0,
            double d => d != 0,
            decimal de => de != 0,
            BigInteger bi => bi != 0,
            _ => true
        } ? Visibility.Collapsed : Visibility.Visible;

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
    /// Gets a shared instance of <see cref="NotZeroIsCollapsedValueConverter"/>
    /// </summary>
    public static NotZeroIsCollapsedValueConverter Default { get; } = new NotZeroIsCollapsedValueConverter();
}
