using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Cogs.Wpf.ValueConversion
{
    public class NullIsCollapsedValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is null ? Visibility.Collapsed : Visibility.Visible;

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is Visibility visibility && visibility == Visibility.Collapsed ? null : Binding.DoNothing;
    }
}
