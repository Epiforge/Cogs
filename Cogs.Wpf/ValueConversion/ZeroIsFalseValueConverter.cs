using System;
using System.Globalization;
using System.Numerics;
using System.Windows.Data;

namespace Cogs.Wpf.ValueConversion
{
    /// <summary>
    /// Converts the value to <c>false</c> when equal to zero; otherwise, <c>true</c>
    /// </summary>
    [ValueConversion(typeof(byte), typeof(bool))]
    [ValueConversion(typeof(sbyte), typeof(bool))]
    [ValueConversion(typeof(short), typeof(bool))]
    [ValueConversion(typeof(ushort), typeof(bool))]
    [ValueConversion(typeof(int), typeof(bool))]
    [ValueConversion(typeof(uint), typeof(bool))]
    [ValueConversion(typeof(long), typeof(bool))]
    [ValueConversion(typeof(ulong), typeof(bool))]
    [ValueConversion(typeof(float), typeof(bool))]
    [ValueConversion(typeof(double), typeof(bool))]
    [ValueConversion(typeof(decimal), typeof(bool))]
    [ValueConversion(typeof(BigInteger), typeof(bool))]
    public class ZeroIsFalseValueConverter : IValueConverter
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
            };

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
        /// Gets a shared instance of <see cref="ZeroIsFalseValueConverter"/>
        /// </summary>
        public static ZeroIsFalseValueConverter Default { get; } = new ZeroIsFalseValueConverter();
    }
}
