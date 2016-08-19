using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace USB_Data_Stealer.Core.Logging
{
    internal static class Logger
    {
        public delegate void NewLogMessageAppended(Message message);

        public const string LogFileName = "log.txt";

        private static bool _enableLogFile;

        private static string _logPath;

        static Logger()
        {
            LogData = new List<Message>();
        }

        public static string LogPath
        {
            get { return _logPath; }
            set
            {
                if (_logPath == value) return;
                _logPath = value;
                OnPropertyChanged(new PropertyChangedEventArgs("LogPath"));
                Append(new Message(Message.EventTypes.Info, "Log Path changed to: " + LogPath + "."));
            }
        }

        public static string LogFullPath
            => string.IsNullOrEmpty(LogPath) ? LogFileName : Path.Combine(LogPath, LogFileName);

        public static bool EnableLogFile
        {
            get { return _enableLogFile; }
            set
            {
                if (_enableLogFile == value) return;
                _enableLogFile = value;
                OnPropertyChanged(new PropertyChangedEventArgs("EnableLogFile"));
                Append(new Message(Message.EventTypes.Info, "Enable Log File changed to: " + EnableLogFile + "."));
            }
        }

        public static List<Message> LogData { get; }

        public static void Append(Message message)
        {
            Debug.WriteLine(message.ToString());

            try
            {
                LogData.Add(message);
                if (EnableLogFile)
                {
                    using (var sw = new StreamWriter(LogFullPath, true))
                    {
                        sw.WriteLine(message.ToString());
                    }
                }
                OnOnNewLogMessageAppended(message);
            }
            catch (Exception ex)
            {
                if (ex is OutOfMemoryException)
                {
                    LogData.Clear();
                }
                else
                {
                    var savingLogError = new Message(Message.EventTypes.Error,
                        "Couldn't save Log Message to the specified file:" + ex.Message);
                    LogData.Add(savingLogError);
                    OnOnNewLogMessageAppended(savingLogError);
                    EnableLogFile = false;
                }
            }
        }

        private static void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(typeof(Logger), e);
        }

        public static event PropertyChangedEventHandler PropertyChanged;

        public static event NewLogMessageAppended OnNewLogMessageAppended;

        private static void OnOnNewLogMessageAppended(Message message)
        {
            OnNewLogMessageAppended?.Invoke(message);
        }
    }
}