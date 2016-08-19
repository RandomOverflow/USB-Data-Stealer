using System;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using USB_Data_Stealer.Core;

namespace USB_Data_Stealer
{
    internal static class Settings
    {
        public const string FileName = "config.ini";

        public static readonly string ApplicationPath =
            Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString();

        public static string FullPath => Path.Combine(ApplicationPath, FileName);

        public static bool ReadFromFile(UsbStealer usbStealer, MainWindow mainWindow)
        {
            StreamReader sr = null;
            try
            {
                sr = new StreamReader(FullPath);
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (line == null) continue;
                    if (line.StartsWith("StolenDataPath="))
                    {
                        usbStealer.StolenDataPath = line.Remove(0, "StolenDataPath=".Length);
                    }
                    else if (line.StartsWith("LogPath="))
                    {
                        usbStealer.LogPath = line.Remove(0, "LogPath=".Length);
                    }
                    else if (line.StartsWith("EnableLogFile="))
                    {
                        usbStealer.EnableFileLogging = bool.Parse(line.Remove(0, "EnableLogFile=".Length));
                    }
                    else if (line.StartsWith("MonitorRefreshRate="))
                    {
                        usbStealer.MonitorRefreshRate = Convert.ToInt32(line.Remove(0, "MonitorRefreshRate=".Length));
                    }
                    else if (line.StartsWith("PrecedenceExtensions="))
                    {
                        var precedenceExtensions = line.Remove(0, "PrecedenceExtensions=".Length).Split(' ');
                        usbStealer.FileTransferSettings.PrecedenceExtensions = precedenceExtensions.Length > 0
                            ? precedenceExtensions
                            : null;
                    }
                    else if (line.StartsWith("CopyBySize="))
                    {
                        bool copyBySize;
                        if (bool.TryParse(line.Remove(0, "CopyBySize=".Length), out copyBySize))
                            usbStealer.FileTransferSettings.CopyBySize = copyBySize;
                    }
                    else if (line.StartsWith("ShowProgress="))
                    {
                        bool showProgress;
                        if (bool.TryParse(line.Remove(0, "ShowProgress=".Length), out showProgress))
                            mainWindow.ShowProgress = showProgress;
                    }
                    else if (line.StartsWith("StartMonitorHotKey="))
                    {
                        Key key;
                        if (Enum.TryParse(line.Remove(0, "StartMonitorHotKey=".Length), out key))
                            usbStealer.StartMonitorHotKey = key;
                    }
                    else if (line.StartsWith("StopMonitorHotKey="))
                    {
                        Key key;
                        if (Enum.TryParse(line.Remove(0, "StopMonitorHotKey=".Length), out key))
                            usbStealer.StopMonitorHotKey = key;
                    }
                }
                sr.Close();
                return true;
            }
            catch (Exception)
            {
                sr?.Close();
                return false;
            }
        }

        public static void SaveToFile(UsbStealer usbStealer, MainWindow mainWindow)
        {
            var sw = new StreamWriter(FullPath);
            try
            {
                sw.Write("StolenDataPath=" + usbStealer.StolenDataPath + Environment.NewLine + "LogPath=" +
                         usbStealer.LogPath +
                         Environment.NewLine +
                         "MonitorRefreshRate=" + usbStealer.MonitorRefreshRate +
                         Environment.NewLine + "PrecedenceExtensions=" +
                         string.Join(" ", usbStealer.FileTransferSettings.PrecedenceExtensions) + Environment.NewLine +
                         "CopyBySize=" + usbStealer.FileTransferSettings.CopyBySize + Environment.NewLine +
                         "ShowProgress=" +
                         mainWindow.ShowProgress + Environment.NewLine +
                         "StartMonitorHotKey=" +
                         usbStealer.StartMonitorHotKey + Environment.NewLine + "StopMonitorHotKey=" +
                         usbStealer.StopMonitorHotKey + Environment.NewLine + "EnableLogFile=" +
                         usbStealer.EnableFileLogging);
            }
            finally
            {
                sw.Close();
            }
        }

        public static void Reset(UsbStealer usbStealer, MainWindow mainWindow)
        {
            usbStealer.StolenDataPath = DefaultSettings.StolenDataPath;
            usbStealer.LogPath = DefaultSettings.LogPath;
            usbStealer.MonitorRefreshRate = DefaultSettings.MonitorRefreshRate;
            usbStealer.FileTransferSettings.PrecedenceExtensions = DefaultSettings.PrecedenceExtensions;
            usbStealer.FileTransferSettings.CopyBySize = DefaultSettings.CopyBySize;
            usbStealer.StartMonitorHotKey = DefaultSettings.StartHotKey;
            usbStealer.StopMonitorHotKey = DefaultSettings.StopHotKey;
            usbStealer.EnableFileLogging = DefaultSettings.EnableFileLogging;
            mainWindow.ShowProgress = DefaultSettings.ShowProgress;
        }

        private static class DefaultSettings
        {
            public const int MonitorRefreshRate = 1000;
            public const Key StartHotKey = Key.None;
            public const Key StopHotKey = Key.None;
            public const bool EnableFileLogging = true;
            public const bool CopyBySize = true;
            public const string[] PrecedenceExtensions = null;
            public const bool ShowProgress = true;
            public static readonly string StolenDataPath = ApplicationPath;

            public static readonly string LogPath = ApplicationPath;
        }
    }
}