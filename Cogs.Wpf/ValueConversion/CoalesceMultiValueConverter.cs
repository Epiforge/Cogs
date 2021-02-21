using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Cogs.Wpf.ValueConversion
{
    /// <summary>
    /// Converts the values to to the first in order that is not <c>null</c>; otherwise, to <c>null</c>
    /// </summary>
    public class CoalesceMultiValueConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts source values to a value for the binding target
        /// </summary>
        /// <param name="values">The array of values that the source bindings in the <see cref="MultiBinding"/> produces</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
            values.FirstOrDefault(value => value is not null);

        /// <summary>
        /// Converts a binding target value to the source binding values
        /// </summary>
        /// <param name="value">The value that the binding target produces</param>
        /// <param name="targetTypes">The array of types to convert to</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            new object[] { DependencyProperty.UnsetValue };
    }
}
