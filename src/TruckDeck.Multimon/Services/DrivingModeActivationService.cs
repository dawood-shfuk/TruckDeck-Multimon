using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Services
{
    /// <summary>
    /// Optional experimental: keeps menu on primary. Default path uses full span from launch instead.
    /// </summary>
    public static class DrivingModeActivationService
    {
        const int CabConfirmReads = 3;

        static int _running;

        public static void BeginAfterLaunch(LayoutProfile drivingProfile, GameTarget target)
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
                return;

            var profile = drivingProfile;
            var game = target;
            new Thread(() => WatchAndActivate(profile, game))
            {
                IsBackground = true,
                Name = "TruckDeckMultimon.DrivingMode"
            }.Start();
        }

        public static void Cancel()
        {
            Interlocked.Exchange(ref _running, 0);
        }

        static void WatchAndActivate(LayoutProfile drivingProfile, GameTarget target)
        {
            try
            {
                var primaryBounds = MenuLayoutHelper.GetPrimaryBounds(drivingProfile);
                GameWindowSpanService.Log(
                    $"Experimental menu watcher — primary {primaryBounds.Width}×{primaryBounds.Height} until cab.");

                var deadline = DateTime.UtcNow.AddMinutes(30);
                List<GameLogWatcher> logWatchers = null;
                var gameWasRunning = false;
                var cabStreak = 0;

                while (Interlocked.CompareExchange(ref _running, 1, 1) == 1 && DateTime.UtcNow < deadline)
                {
                    var gameRunning = GameProcessGuard.IsGameRunning(out _);

                    if (!gameRunning)
                    {
                        if (gameWasRunning)
                        {
                            GameWindowSpanService.Log("Game closed before driving mode activated.");
                            break;
                        }

                        Thread.Sleep(500);
                        continue;
                    }

                    if (!gameWasRunning)
                    {
                        gameWasRunning = true;
                        logWatchers = CreateLogWatchers(target);
                        GameWindowSpanService.Log("Game process detected — experimental menu lock (window only, no console).");
                    }

                    MaintainMenuWindow(target, drivingProfile, primaryBounds, logWatchers);

                    if (ScsTelemetryReader.IsInCab())
                    {
                        cabStreak++;
                        if (cabStreak >= CabConfirmReads)
                        {
                            ActivateDrivingMode(drivingProfile, target, "telemetry — in cab");
                            return;
                        }
                    }
                    else
                    {
                        cabStreak = 0;
                    }

                    Thread.Sleep(400);
                }

                if (!gameWasRunning)
                    GameWindowSpanService.Log("Driving mode watcher timed out waiting for game to start.");
                else
                    GameWindowSpanService.Log("Driving mode watcher timed out waiting for cab entry.");
            }
            finally
            {
                Interlocked.Exchange(ref _running, 0);
            }
        }

        static List<GameLogWatcher> CreateLogWatchers(GameTarget target)
        {
            var logWatchers = new List<GameLogWatcher>();
            foreach (var folder in GameDocumentsService.ResolveTargetFolders(target))
            {
                var watcher = new GameLogWatcher(GameDocumentsService.GetGameLogPath(folder));
                watcher.Reset();
                logWatchers.Add(watcher);
            }

            return logWatchers;
        }

        static void MaintainMenuWindow(
            GameTarget target,
            LayoutProfile drivingProfile,
            Rectangle primaryBounds,
            List<GameLogWatcher> logWatchers)
        {
            // Never send g_mode or uset while the game is starting — that caused DX11 device-removed crashes.
            // Window-only lock is best-effort; full span from launch is the supported path.
            var resizeDetected = false;
            if (logWatchers != null)
            {
                foreach (var watcher in logWatchers)
                {
                    if (watcher.TryDetectWindowResizeBeyondPrimary(primaryBounds, out var w, out var h))
                    {
                        GameWindowSpanService.Log($"game.log resize to {w}×{h} — window lock only.");
                        resizeDetected = true;
                    }
                }
            }

            var windowExpanded = GameWindowSpanService.IsWindowLargerThanPrimary(target, primaryBounds);
            if (resizeDetected || windowExpanded)
            {
                ConfigurationApplyService.Apply(
                    drivingProfile, target, MultimonApplyPhase.Menu, allowWhileGameRunning: true);
            }

            GameWindowSpanService.EnforcePrimaryWindow(target, primaryBounds);
        }

        public static void ActivateDrivingMode(LayoutProfile drivingProfile, GameTarget target, string reason)
        {
            Interlocked.Exchange(ref _running, 0);

            GameWindowSpanService.Log($"Activating driving multimon ({reason}).");

            DisplayLayoutHelper.FinalizeProfile(drivingProfile);
            var result = ConfigurationApplyService.Apply(
                drivingProfile, target, MultimonApplyPhase.Driving, allowWhileGameRunning: true);

            foreach (var msg in result.Messages)
                GameWindowSpanService.Log(msg);
            foreach (var warn in result.Warnings)
                GameWindowSpanService.Log("WARN: " + warn);

            GameWindowSpanService.ResetSession();
            Thread.Sleep(500);
            GameWindowSpanService.SpanRunningGame(target, drivingProfile.GameDesktopBounds);
        }
    }
}
