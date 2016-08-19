using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using USB_Data_Stealer.Core.Logging;

namespace USB_Data_Stealer.Converters
{
    public class MessageToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((Message.EventTypes) value)
            {
                case Message.EventTypes.Error:
                    return Brushes.Red;

                case Message.EventTypes.Info:
                    return Brushes.Black;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}