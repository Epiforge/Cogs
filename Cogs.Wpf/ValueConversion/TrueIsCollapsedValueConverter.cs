using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Cogs.Wpf.ValueConversion
{
    public class TrueIsCollapsedValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool boolean ? (boolean ? Visibility.Collapsed : Visibility.Visible) : Binding.DoNothing;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is Visibility visibility ? visibility != Visibility.Visible : Binding.DoNothing;
    }
}
