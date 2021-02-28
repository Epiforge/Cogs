using System;
using System.Globalization;
using System.Windows.Data;

namespace Cogs.Wpf.ValueConversion
{
    /// <summary>
    /// Converts the boolean value to its negative
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class NegateBooleanValueConverter : IValueConverter
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
            value is bool boolean ? !boolean : Binding.DoNothing;

        /// <summary>
        /// Converts a value
        /// </summary>
        /// <param name="value">The value produced by the binding source</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>A converted value</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool boolean ? !boolean : Binding.DoNothing;

        /// <summary>
        /// Gets a shared instance of <see cref="NegateBooleanValueConverter"/>
        /// </summary>
        public static NegateBooleanValueConverter Default { get; } = new NegateBooleanValueConverter();
    }
}
