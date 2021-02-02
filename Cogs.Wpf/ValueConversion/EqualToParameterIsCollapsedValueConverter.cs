using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Cogs.Wpf.ValueConversion
{
    /// <summary>
    /// Compares the value to the parameter using <see cref="object.Equals(object?, object?)"/>,
    /// converting to <see cref="Visibility.Collapsed"/> when <c>true</c> and <see cref="Visibility.Visible"/> when <c>false</c>
    /// </summary>
    public class EqualToParameterIsCollapsedValueConverter : IValueConverter
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
            Equals(value, parameter) ? Visibility.Collapsed : Visibility.Visible;

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
    }
}
