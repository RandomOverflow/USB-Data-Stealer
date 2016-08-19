using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace USB_Data_Stealer.Core.Update
{
    public class UpdateCheck
    {
        private const string UpdateModule = "http://www.psys.altervista.org/usbdatastealer/lastversion";
        private const string DownloadingSite = "http://www.psys.altervista.org/usbdatastealer/download";

        public UpdateCheck(string currentVersion)
        {
            CurrentVersion = Version.Parse(currentVersion);
        }

        public Version CurrentVersion { get; }
        public Version LatestVersion { get; private set; }
        public bool IsUpdated => LatestVersion == null || CurrentVersion >= LatestVersion;

        public void OpenDownloadingSite()
        {
            Process.Start(DownloadingSite);
        }

        public async Task<bool> CheckUpdates()
        {
            using (var client = new WebClient())
            {
                try
                {
                    string versionString = await client.DownloadStringTaskAsync(new Uri(UpdateModule));
                    LatestVersion = Version.Parse(versionString);
                }
                catch (Exception)
                {
                    //
                }
            }
            return IsUpdated;
        }
    }
}