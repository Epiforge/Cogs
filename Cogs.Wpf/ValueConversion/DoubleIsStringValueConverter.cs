using System;
using System.Globalization;
using System.Windows.Data;

namespace Cogs.Wpf.ValueConversion
{
    /// <summary>
    /// Converts the value to a <see cref="string"/> when it is a <see cref="double"/>, optionally accepting a <see cref="NumberStyles"/> value as a parameter
    /// </summary>
    [ValueConversion(typeof(double), typeof(string))]
    public class DoubleIsStringValueConverter : IValueConverter
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
            value is double dbl
            ?
            parameter switch
            {
                NumberStyles numberStyles => numberStyles switch
                {
                    NumberStyles ns when (ns & NumberStyles.Currency) > 0 => dbl.ToString("c", culture),
                    NumberStyles ns when (ns & NumberStyles.HexNumber) > 0 => dbl.ToString("x", culture),
                    NumberStyles ns when (ns & NumberStyles.Integer) > 0 => Math.Truncate(dbl).ToString(culture),
                    NumberStyles ns when (ns & NumberStyles.Number) > 0 => dbl.ToString("n", culture),
                    _ => dbl.ToString(culture)
                },
                null => dbl.ToString(culture),
                _ => throw new NotSupportedException()
            }
            :
            Binding.DoNothing;

        /// <summary>
        /// Converts a value
        /// </summary>
        /// <param name="value">The value produced by the binding source</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>A converted value</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is string str && double.TryParse(str, parameter is NumberStyles numberStyles ? numberStyles : NumberStyles.Any, culture, out var dbl) ? dbl : Binding.DoNothing;

        /// <summary>
        /// Gets a shared instance of <see cref="DoubleIsStringValueConverter"/>
        /// </summary>
        public static DoubleIsStringValueConverter Default { get; } = new DoubleIsStringValueConverter();
    }
}
