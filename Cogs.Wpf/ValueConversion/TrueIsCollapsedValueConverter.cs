using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Cogs.Wpf.ValueConversion
{
    /// <summary>
    /// Converts the value to <see cref="Visibility.Collapsed"/> when <c>true</c> and <see cref="Visibility.Visible"/> when <c>false</c>
    /// </summary>
    public class TrueIsCollapsedValueConverter : IValueConverter
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
            value is bool boolean ? (boolean ? Visibility.Collapsed : Visibility.Visible) : Binding.DoNothing;

        /// <summary>
        /// Converts a value
        /// </summary>
        /// <param name="value">The value produced by the binding source</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>A converted value</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is Visibility visibility ? visibility != Visibility.Visible : Binding.DoNothing;
    }
}
