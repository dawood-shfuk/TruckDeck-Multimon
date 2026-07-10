using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Services
{
    /// <summary>
    /// Sends g_mode to ETS2/ATS developer console so the render canvas matches the multimon virtual desktop.
    /// </summary>
    public static class GameConsoleService
    {
        const byte VkOem3 = 0xC0; // ` ~
        const byte VkReturn = 0x0D;
        const uint KeyeventfKeyup = 0x0002;
        const uint KeyeventfUnicode = 0x0004;

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public static bool TrySendUset(IntPtr hwnd, string key, string value)
        {
            if (hwnd == IntPtr.Zero || string.IsNullOrWhiteSpace(key))
                return false;

            try
            {
                ShowWindow(hwnd, 5);
                Thread.Sleep(200);
                SetForegroundWindow(hwnd);
                Thread.Sleep(300);

                TapKey(VkOem3);
                Thread.Sleep(300);

                var command = $"uset {key} {value}";
                GameWindowSpanService.Log($"Console: {command}");
                TypeText(command);
                Thread.Sleep(100);
                TapKey(VkReturn);
                Thread.Sleep(600);
                TapKey(VkOem3);
                Thread.Sleep(300);
                return true;
            }
            catch (Exception ex)
            {
                GameWindowSpanService.Log($"uset failed: {ex.Message}");
                return false;
            }
        }

        public static bool TrySendGMode(IntPtr hwnd, Rectangle gameDesktop, int refreshHz = 60)
        {
            if (hwnd == IntPtr.Zero || gameDesktop.Width <= 0 || gameDesktop.Height <= 0)
                return false;

            try
            {
                ShowWindow(hwnd, 5);
                Thread.Sleep(300);
                SetForegroundWindow(hwnd);
                Thread.Sleep(500);

                TapKey(VkOem3);
                Thread.Sleep(400);

                var command = $"g_mode {gameDesktop.Width} {gameDesktop.Height} {refreshHz} 0";
                GameWindowSpanService.Log($"Console: {command}");
                TypeText(command);
                Thread.Sleep(100);
                TapKey(VkReturn);
                Thread.Sleep(800);
                TapKey(VkOem3);
                Thread.Sleep(400);
                return true;
            }
            catch (Exception ex)
            {
                GameWindowSpanService.Log($"g_mode failed: {ex.Message}");
                return false;
            }
        }

        public static int ReadRefreshRateFromConfig(GameTarget target)
        {
            try
            {
                foreach (var folder in GameDocumentsService.ResolveTargetFolders(target))
                {
                    var path = GameDocumentsService.GetConfigCfgPath(folder);
                    if (!System.IO.File.Exists(path))
                        continue;

                    foreach (var line in System.IO.File.ReadAllLines(path))
                    {
                        if (line.IndexOf("r_mode_refresh", StringComparison.OrdinalIgnoreCase) < 0)
                            continue;

                        var start = line.IndexOf('"');
                        var end = line.LastIndexOf('"');
                        if (start >= 0 && end > start &&
                            int.TryParse(line.Substring(start + 1, end - start - 1), out var hz) &&
                            hz > 0)
                            return hz;
                    }
                }
            }
            catch
            {
                // ignore
            }

            return 60;
        }

        static void TapKey(byte virtualKey)
        {
            SendKey(virtualKey, false);
            Thread.Sleep(30);
            SendKey(virtualKey, true);
        }

        static void SendKey(byte virtualKey, bool keyUp)
        {
            var inputs = new[]
            {
                new INPUT
                {
                    type = 1,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = virtualKey,
                            dwFlags = keyUp ? KeyeventfKeyup : 0
                        }
                    }
                }
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        static void TypeText(string text)
        {
            foreach (var ch in text)
            {
                var down = new INPUT
                {
                    type = 1,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wScan = ch,
                            dwFlags = KeyeventfUnicode
                        }
                    }
                };
                var up = new INPUT
                {
                    type = 1,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wScan = ch,
                            dwFlags = KeyeventfUnicode | KeyeventfKeyup
                        }
                    }
                };
                SendInput(1, new[] { down }, Marshal.SizeOf(typeof(INPUT)));
                Thread.Sleep(8);
                SendInput(1, new[] { up }, Marshal.SizeOf(typeof(INPUT)));
                Thread.Sleep(8);
            }
        }
    }
}
