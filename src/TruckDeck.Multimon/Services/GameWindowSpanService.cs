using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Services
{
    /// <summary>
    /// Forces ETS2/ATS to use the full multimon canvas: g_mode (resolution) + window position (negative Y for top monitor).
    /// </summary>
    public static class GameWindowSpanService
    {
        const uint SwpShowWindow = 0x0040;
        const uint SwpNoZOrder = 0x0004;
        const int SwShowNormal = 1;
        static readonly IntPtr DpiAwarenessContextPerMonitorV2 = new IntPtr(-4);

        [DllImport("user32.dll")]
        static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        struct NativeRect
        {
            public int Left, Top, Right, Bottom;
            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        static bool _dpiInitialized;
        static bool _gModeSent;

        public static void EnsureDpiAwareness()
        {
            if (_dpiInitialized)
                return;
            try { SetProcessDpiAwarenessContext(DpiAwarenessContextPerMonitorV2); }
            catch { /* ignore */ }
            _dpiInitialized = true;
        }

        static DateTime? _firstGameWindowSeen;

        public static void ResetSession()
        {
            _gModeSent = false;
            _firstGameWindowSeen = null;
        }

        public static bool SpanWhenReady(GameTarget target, Rectangle gameDesktop, int timeoutSeconds = 180)
        {
            EnsureDpiAwareness();
            if (gameDesktop.Width <= 0 || gameDesktop.Height <= 0)
                return false;

            Log($"Waiting for game → full span {FormatRect(gameDesktop)}");
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            const int gfxInitDelaySeconds = 12;

            while (DateTime.UtcNow < deadline)
            {
                var hwnd = FindBestGameWindow(target);
                if (hwnd != IntPtr.Zero)
                {
                    if (!_firstGameWindowSeen.HasValue)
                    {
                        _firstGameWindowSeen = DateTime.UtcNow;
                        Log($"Game window detected: {ReadRect(hwnd)} — waiting {gfxInitDelaySeconds}s for graphics init.");
                    }

                    var initElapsed = (DateTime.UtcNow - _firstGameWindowSeen.Value).TotalSeconds;
                    var allowConsole = initElapsed >= gfxInitDelaySeconds;

                    if (TryFullSpan(target, gameDesktop, allowConsole))
                    {
                        Log("Full span successful.");
                        MaintainSpan(target, gameDesktop, seconds: 90);
                        return true;
                    }
                }

                Thread.Sleep(1000);
            }

            Log("Full span timed out.");
            return false;
        }

        public static bool SpanRunningGame(GameTarget target, Rectangle gameDesktop)
        {
            EnsureDpiAwareness();
            _gModeSent = false;
            return TryFullSpan(target, gameDesktop, allowConsole: true);
        }

        public static void SpanWhenReadyAsync(GameTarget target, Rectangle gameDesktop)
        {
            ResetSession();
            var desktop = gameDesktop;
            var game = target;
            new Thread(() => SpanWhenReady(game, desktop))
            {
                IsBackground = true,
                Name = "TruckDeckMultimon.WindowSpan"
            }.Start();
        }

        public static void SpanPrimaryWhenReadyAsync(GameTarget target, Rectangle primaryBounds)
        {
            ResetSession();
            var bounds = primaryBounds;
            var game = target;
            new Thread(() =>
            {
                EnsureDpiAwareness();
                Log($"Waiting for game → primary menu window {FormatRect(bounds)}");
                var deadline = DateTime.UtcNow.AddSeconds(120);
                while (DateTime.UtcNow < deadline)
                {
                    if (EnforcePrimaryWindow(game, bounds))
                    {
                        Log("Primary menu window set.");
                        return;
                    }
                    Thread.Sleep(750);
                }
                Log("Primary menu window timed out.");
            })
            {
                IsBackground = true,
                Name = "TruckDeckMultimon.MenuWindow"
            }.Start();
        }

        /// <summary>
        /// Keeps the game window on the primary monitor only (no g_mode / full-desktop span).
        /// </summary>
        public static bool EnforcePrimaryWindow(GameTarget target, Rectangle primaryBounds)
        {
            if (primaryBounds.Width <= 0 || primaryBounds.Height <= 0)
                return false;

            EnsureDpiAwareness();
            var hwnd = FindBestGameWindow(target);
            if (hwnd == IntPtr.Zero)
                return false;

            if (IsSpanCorrect(hwnd, primaryBounds))
                return true;

            Log($"Menu lock: {ReadRect(hwnd)} → {FormatRect(primaryBounds)}");
            ApplyWindowRect(hwnd, primaryBounds);
            Thread.Sleep(200);
            return IsSpanCorrect(hwnd, primaryBounds);
        }

        public static bool IsWindowLargerThanPrimary(GameTarget target, Rectangle primaryBounds)
        {
            var hwnd = FindBestGameWindow(target);
            if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var rect))
                return false;

            return rect.Width > primaryBounds.Width + 24 || rect.Height > primaryBounds.Height + 24;
        }

        static bool TryFullSpan(GameTarget target, Rectangle gameDesktop, bool allowConsole)
        {
            var hwnd = FindBestGameWindow(target);
            if (hwnd == IntPtr.Zero)
                return false;

            Log($"Found hwnd={hwnd} current={ReadRect(hwnd)}");

            if (allowConsole && !_gModeSent)
            {
                var hz = GameConsoleService.ReadRefreshRateFromConfig(target);
                if (GameConsoleService.TrySendGMode(hwnd, gameDesktop, hz))
                {
                    _gModeSent = true;
                    Thread.Sleep(2000);
                }
            }
            else if (!allowConsole && IsSpanCorrect(hwnd, gameDesktop))
            {
                Log($"Window position OK before g_mode: {ReadRect(hwnd)}");
                return true;
            }

            for (var attempt = 0; attempt < 5; attempt++)
            {
                ApplyWindowRect(hwnd, gameDesktop);
                Thread.Sleep(400);

                if (IsSpanCorrect(hwnd, gameDesktop))
                {
                    Log($"Window OK after attempt {attempt + 1}: {ReadRect(hwnd)}");
                    return true;
                }

                Log($"Attempt {attempt + 1} rect={ReadRect(hwnd)} (want {FormatRect(gameDesktop)})");
            }

            return IsSpanCorrect(hwnd, gameDesktop);
        }

        static void MaintainSpan(GameTarget target, Rectangle gameDesktop, int seconds)
        {
            var end = DateTime.UtcNow.AddSeconds(seconds);
            while (DateTime.UtcNow < end)
            {
                var hwnd = FindBestGameWindow(target);
                if (hwnd == IntPtr.Zero)
                    return;

                if (!IsSpanCorrect(hwnd, gameDesktop))
                {
                    Log($"Re-spanning: {ReadRect(hwnd)}");
                    ApplyWindowRect(hwnd, gameDesktop);
                }

                Thread.Sleep(2500);
            }
        }

        static void ApplyWindowRect(IntPtr hwnd, Rectangle gameDesktop)
        {
            ShowWindow(hwnd, SwShowNormal);
            SetWindowPos(hwnd, IntPtr.Zero, gameDesktop.Left, gameDesktop.Top,
                gameDesktop.Width, gameDesktop.Height, SwpShowWindow | SwpNoZOrder);
            MoveWindow(hwnd, gameDesktop.Left, gameDesktop.Top, gameDesktop.Width, gameDesktop.Height, true);
        }

        public static IntPtr FindBestGameWindow(GameTarget target)
        {
            var processIds = new HashSet<int>();
            foreach (var processName in GetProcessNames(target))
            {
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    try { processIds.Add(process.Id); }
                    finally { process.Dispose(); }
                }
            }

            if (processIds.Count == 0)
                return IntPtr.Zero;

            IntPtr best = IntPtr.Zero;
            long bestArea = 0;

            EnumWindows((hWnd, _) =>
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                GetWindowThreadProcessId(hWnd, out var pid);
                if (!processIds.Contains((int)pid))
                    return true;

                if (!GetWindowRect(hWnd, out var rect) || rect.Width < 640 || rect.Height < 480)
                    return true;

                var area = (long)rect.Width * rect.Height;
                if (area > bestArea)
                {
                    bestArea = area;
                    best = hWnd;
                }

                return true;
            }, IntPtr.Zero);

            return best;
        }

        static bool IsSpanCorrect(IntPtr hwnd, Rectangle expected)
        {
            if (!GetWindowRect(hwnd, out var rect))
                return false;

            var heightOk = Math.Abs(rect.Height - expected.Height) <= 24;
            var widthOk = Math.Abs(rect.Width - expected.Width) <= 24;
            var topOk = Math.Abs(rect.Top - expected.Top) <= 16;

            return heightOk && widthOk && topOk;
        }

        static string ReadRect(IntPtr hwnd)
        {
            if (!GetWindowRect(hwnd, out var rect))
                return "?";
            return FormatRect(Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom));
        }

        static string FormatRect(Rectangle rect) =>
            $"{rect.Width}×{rect.Height} at ({rect.Left}, {rect.Top})";

        public static void Log(string message)
        {
            try
            {
                var path = Path.Combine(Path.GetTempPath(), "TruckDeckMultimon-span.log");
                File.AppendAllText(path, $"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}");
            }
            catch { /* ignore */ }
        }

        static string[] GetProcessNames(GameTarget target)
        {
            switch (target)
            {
                case GameTarget.Ats: return new[] { "amtrucks", "amtrucks2" };
                case GameTarget.Both: return new[] { "eurotrucks2", "amtrucks", "amtrucks2" };
                default: return new[] { "eurotrucks2" };
            }
        }
    }
}
