using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Rotatonator
{
    public static class KeyboardAutomation
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        public static void SendKey(string key)
        {
            // Get virtual key code
            byte vk = GetVirtualKeyCode(key);
            if (vk == 0) return;

            // Simulate key press
            keybd_event(vk, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            Thread.Sleep(50);
            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        private static byte GetVirtualKeyCode(string key)
        {
            if (key.Length == 1)
            {
                char c = key.ToUpper()[0];
                if (c >= '0' && c <= '9')
                    return (byte)(0x30 + (c - '0'));
                if (c >= 'A' && c <= 'Z')
                    return (byte)(0x41 + (c - 'A'));
            }

            // Special keys
            return key.ToLower() switch
            {
                "f1" => 0x70,
                "f2" => 0x71,
                "f3" => 0x72,
                "f4" => 0x73,
                "f5" => 0x74,
                "f6" => 0x75,
                "f7" => 0x76,
                "f8" => 0x77,
                "f9" => 0x78,
                "f10" => 0x79,
                "f11" => 0x7A,
                "f12" => 0x7B,
                _ => 0
            };
        }

        public static bool IsEverQuestForeground()
        {
            IntPtr hwnd = GetForegroundWindow();
            var windowText = new System.Text.StringBuilder(256);
            GetWindowText(hwnd, windowText, 256);
            string title = windowText.ToString();
            
            return title.Contains("EverQuest", StringComparison.OrdinalIgnoreCase);
        }
    }
}
