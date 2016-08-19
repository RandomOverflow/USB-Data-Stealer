using System;
using System.Windows;
using System.Windows.Media;

namespace USB_Data_Stealer
{
    /// <summary>
    ///     Logica di interazione per DataTransferringWindow.xaml
    /// </summary>
    public partial class DataTransferringWindow
    {
        private static readonly Random Rnd = new Random();
        private static int _n = 1;

        public DataTransferringWindow()
        {
            InitializeComponent();
        }

        ~DataTransferringWindow()
        {
            _n--;
        }

        private void DataTransferringWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            Rect desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - Width;
            Top = desktopWorkingArea.Bottom - Height*_n++;

            ProgressBarTransferring.Foreground =
                new SolidColorBrush(Color.FromRgb((byte) Rnd.Next(0, 255), (byte) Rnd.Next(0, 255),
                    (byte) Rnd.Next(0, 255)));
        }

        public void ChangeProgressValue(int value)
        {
            Dispatcher.Invoke(() => ProgressBarTransferring.Value = value);
        }
    }
}