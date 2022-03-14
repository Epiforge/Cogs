namespace Cogs.Wpf.ValueConversion;

/// <summary>
/// Converts the value to a <see cref="string"/> when it is a <see cref="Guid"/>, optionally accepting a specifier to pass to <see cref="Guid.ToString(string)"/> as an argument and retaining the results of the last successful call to <see cref="ConvertBack(object, Type, object, CultureInfo)"/> which prevents two-way bindings from overriding decimals when the user is typing
/// </summary>
[ValueConversion(typeof(Guid), typeof(string))]
public class StatefulGuidIsStringValueConverter :
    IValueConverter
{
    (string str, Guid guid)? lastSuccessfulConvertBack;

    /// <summary>
    /// Converts a value
    /// </summary>
    /// <param name="value">The value produced by the binding source</param>
    /// <param name="targetType">The type of the binding target property</param>
    /// <param name="parameter">The converter parameter to use</param>
    /// <param name="culture">The culture to use in the converter</param>
    /// <returns>A converted value</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Guid guid)
        {
            if (lastSuccessfulConvertBack is { } last)
            {
                var (lastStr, lastGuid) = last;
                if (lastGuid == guid)
                    return lastStr;
            }
            return guid.ToString(parameter is string specifier ? specifier : "D", culture);
        }
        return Binding.DoNothing;
    }

    /// <summary>
    /// Converts a value
    /// </summary>
    /// <param name="value">The value produced by the binding source</param>
    /// <param name="targetType">The type of the binding target property</param>
    /// <param name="parameter">The converter parameter to use</param>
    /// <param name="culture">The culture to use in the converter</param>
    /// <returns>A converted value</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && Guid.TryParse(str, out var guid))
        {
            lastSuccessfulConvertBack = (str, guid);
            return guid;
        }
        return Binding.DoNothing;
    }

    /// <summary>
    /// Gets a new instance of <see cref="StatefulGuidIsStringValueConverter"/>
    /// </summary>
    public static StatefulGuidIsStringValueConverter Default => new();
}
