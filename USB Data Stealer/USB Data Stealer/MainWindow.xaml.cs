#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Cache;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using USB_Data_Stealer.Core;
using USB_Data_Stealer.Core.Update;
using CheckBox = System.Windows.Controls.CheckBox;
using Message = USB_Data_Stealer.Core.Logging.Message;
using MessageBox = System.Windows.MessageBox;

#endregion

namespace USB_Data_Stealer
{
    /// <summary>
    ///     Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private bool? _showProgress;

        public MainWindow()
        {
            InitializeComponent();
            UsbStealer = new UsbStealer(UpdateDataGridLog);
        }

        private UsbStealer UsbStealer { get; }

        public bool? ShowProgress
        {
            get { return _showProgress; }
            set
            {
                _showProgress = value;
                if (ShowProgress == true)
                {
                    DictionaryTransferringWindows = new Dictionary<Guid, DataTransferringWindow>();
                    UsbStealer.OnCopyProgressStarted += CopyProgressStarted;
                    UsbStealer.OnCopyProgressChanged += ReportCopyProgress;
                    UsbStealer.OnCopyProgressFinished += CopyProgressFinished;
                }
                else
                {
                    UsbStealer.OnCopyProgressStarted -= CopyProgressStarted;
                    UsbStealer.OnCopyProgressChanged -= ReportCopyProgress;
                    UsbStealer.OnCopyProgressFinished -= CopyProgressFinished;
                }
            }
        }

        private Dictionary<Guid, DataTransferringWindow> DictionaryTransferringWindows { get; set; }

        private void ReportCopyProgress(int progressPercentage, Guid fileTransferId)
        {
            DataTransferringWindow transferringWindow = DictionaryTransferringWindows[fileTransferId];
            transferringWindow.ChangeProgressValue(progressPercentage);

            Debug.WriteLine("PROGRESS ID " + fileTransferId + "%: " + progressPercentage + "%");
        }

        private void CopyProgressStarted(Guid fileTransferId)
        {
            Dispatcher.Invoke(() =>
            {
                DictionaryTransferringWindows.Add(fileTransferId, new DataTransferringWindow());
                DictionaryTransferringWindows[fileTransferId].Show();
            });
        }

        private void CopyProgressFinished(Guid fileTransferId)
        {
            Dispatcher.Invoke(() => DictionaryTransferringWindows[fileTransferId].Close());
            DictionaryTransferringWindows.Remove(fileTransferId);
        }

        private void ButtonStartMonitor_Click(object sender, RoutedEventArgs e)
        {
            UsbStealer.StartMonitoring();
        }

        private void ButtonStopMonitor_Click(object sender, RoutedEventArgs e)
        {
            UsbStealer.StopMonitoring();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Check Updates
            var updateCheck =
                new UpdateCheck(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion);
            await updateCheck.CheckUpdates();
            if (!updateCheck.IsUpdated)
            {
                MessageBoxResult result = MessageBox.Show("Would you like to Update to the latest version?", "Update",
                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
                if (result == MessageBoxResult.Yes) updateCheck.OpenDownloadingSite();
            }

            var keys = Enum.GetNames(typeof(Key));
            ComboBoxStartHotKey.ItemsSource = keys;
            ComboBoxStopHotKey.ItemsSource = keys;
            if (!Settings.ReadFromFile(UsbStealer, this)) Settings.Reset(UsbStealer, this);

            UsbStealer.OnNewDriverAdded += UpdateListViewDrives;
            UsbStealer.OnStoppedMonitor += UsbStealerOnOnStoppedMonitor;
            UsbStealer.OnStartedMonitor += UsbStealerOnOnStartedMonitor;
            ButtonStartMonitor.DataContext = UsbStealer;
            ButtonStopMonitor.DataContext = UsbStealer;
            TextBoxStolenDataPath.DataContext = UsbStealer;
            TextBoxLogPath.DataContext = UsbStealer;
            TextBoxMonitorRefreshRate.DataContext = UsbStealer;
            TextBoxPrecedenceExtensions.DataContext = UsbStealer.FileTransferSettings;
            CheckBoxCopyFilesBySize.DataContext = UsbStealer.FileTransferSettings;
            ComboBoxStartHotKey.DataContext = UsbStealer;
            ComboBoxStopHotKey.DataContext = UsbStealer;
            CheckBoxEnableLogFile.DataContext = UsbStealer;
            CheckBoxShowProgress.DataContext = this;
            CheckBoxLaunchOnStartup.IsChecked =
                Autorun.IsEnabled(Assembly.GetExecutingAssembly().FullName);

            //Check Command Line Arguments

            CheckCommandLineArguments();
        }

        private void CheckCommandLineArguments()
        {
            var args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                if (arg == "/autostart") UsbStealer.StartMonitoring();
            }
        }

        private void UsbStealerOnOnStartedMonitor()
        {
            if (UsbStealer.StopMonitorHotKey > 0)
                Visibility = Visibility.Hidden;
            else
                MessageBox.Show("Stop Hot Key is not set, the program will remain visible.", "USB Data Stealer",
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UsbStealerOnOnStoppedMonitor()
        {
            if (Visibility == Visibility.Visible) return;
            Visibility = Visibility.Visible;
        }

        private void UpdateDataGridLog(Message newMessage)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => DataGridLog.Items.Add(newMessage));
            }
            else
            {
                DataGridLog.Items.Add(newMessage);
            }
        }

        private void UpdateListViewDrives(DriveInfo driveInfo, string destinationPath)
        {
            if (ListViewDrives.Items.Contains(ListBoxItemNoDrives))
                Dispatcher.Invoke(() => ListViewDrives.Items.Remove(ListBoxItemNoDrives));

            Dispatcher.Invoke(() => ListViewDrives.Items.Add(new DisplayDriveInfo(driveInfo, destinationPath)));
        }

        private void ButtonChooseStolenDataPath_Click(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TextBoxStolenDataPath.Text = folderBrowserDialog.SelectedPath;
        }

        private void ButtonChooseLogPath_Click(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TextBoxLogPath.Text = folderBrowserDialog.SelectedPath;
        }

        private void TabItemLog_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (DataGridLog.Items.Count <= 0) return;

            DataGridLog.SelectedItem = DataGridLog.Items.GetItemAt(DataGridLog.Items.Count - 1);
            DataGridLog.ScrollIntoView(DataGridLog.SelectedItem);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                Settings.SaveToFile(UsbStealer, this);
            }
            catch (Exception ex)
            {
                MessageBoxResult result =
                    MessageBox.Show("Cannot save current Settings: " + Environment.NewLine + ex.Message +
                                    Environment.NewLine + "Do you want to exit anyway?", "Error", MessageBoxButton.YesNo,
                        MessageBoxImage.Error, MessageBoxResult.No);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void ButtonResetSettings_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to reset all Settings?",
                "Reset", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (result == MessageBoxResult.Yes)
                Settings.Reset(UsbStealer, this);
        }

        private void ListViewDrives_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedDrive = ListViewDrives.SelectedItem as DisplayDriveInfo;
            selectedDrive?.OpenDestinationPath();
        }

        private void CheckBoxLaunchOnStartup_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (((CheckBox) sender).IsChecked == true)
                    Autorun.Enable(Assembly.GetExecutingAssembly().FullName,
                        Assembly.GetExecutingAssembly().Location + " /autostart");
                else
                {
                    Autorun.Disable(Assembly.GetExecutingAssembly().FullName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error, MessageBoxResult.OK);
                ((CheckBox) sender).IsChecked = !((CheckBox) sender).IsChecked;
            }
        }

        private void ButtonDonate_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(
                "https://www.paypal.me/jacopobalducci");
        }

        private void ButtonGitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/RandomOverflow/USB-Data-Stealer");

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri("http://random-octodex.herokuapp.com/random");
            bitmap.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bitmap.EndInit();
            ImageGitHub.Source = bitmap;
        }
    }
}

//".docx .pdf .doc .txt .rtf .odt .xlsx .pptx .epub .odx";

//{".docx", ".pdf", ".doc", ".txt", ".rtf", ".odt", ".xlsx", ".pptx", ".epub", ".odx"};