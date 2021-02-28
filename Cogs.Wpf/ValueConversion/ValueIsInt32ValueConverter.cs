using System;
using System.Globalization;
using System.Windows.Data;

namespace Cogs.Wpf.ValueConversion
{
    /// <summary>
    /// Casts the value to a <see cref="int"/>
    /// </summary>
    [ValueConversion(typeof(int), typeof(int))]
    public class ValueIsInt32ValueConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value
        /// </summary>
        /// <param name="value">The value produced by the binding source</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>A converted value</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (int)value;

        /// <summary>
        /// Converts a value
        /// </summary>
        /// <param name="value">The value produced by the binding source</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>A converted value</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;

        /// <summary>
        /// Gets a shared instance of <see cref="ValueIsInt32ValueConverter"/>
        /// </summary>
        public static ValueIsInt32ValueConverter Default { get; } = new ValueIsInt32ValueConverter();
    }
}
