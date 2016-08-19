using System.Diagnostics;
using System.IO;

namespace USB_Data_Stealer
{
    internal class DisplayDriveInfo
    {
        public DisplayDriveInfo(DriveInfo driveInfo, string destinationPath)
        {
            FreeSpacePercentage = (driveInfo.TotalSize - driveInfo.AvailableFreeSpace)/(double) driveInfo.TotalSize*
                                  100.0;
            VolumeLabel = driveInfo.VolumeLabel;
            Name = driveInfo.Name;
            DestinationPath = destinationPath;
        }

        public string VolumeLabelAndName => VolumeLabel + " (" + Name + ")";
        public double FreeSpacePercentage { get; }
        public string VolumeLabel { get; }
        public string Name { get; }
        public string DestinationPath { get; }

        public void OpenDestinationPath()
        {
            Process.Start("explorer.exe", DestinationPath);
        }
    }
}