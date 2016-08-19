using System;
using System.Globalization;
using System.Windows.Data;

namespace USB_Data_Stealer.Converters
{
    public class StringToArrayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var array = value as string[];
            return array == null ? value : string.Join(" ", (string[]) value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((string) value)?.Split(' ') ?? new string[0];
        }
    }
}