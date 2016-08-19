using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace USB_Data_Stealer.Converters
{
    public class StringToKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (Key) Enum.Parse(typeof(Key), (string) value);
        }
    }
}