using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using TruckDeck.Multimon.Forms;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Services
{
    /// <summary>
    /// Floating, user-draggable overlay windows that display cropped SCS cameras
    /// rendered on the MAIN native-resolution game window.
    /// </summary>
    public static class OverlayHostService
    {
        static readonly object Sync = new object();
        static int _running;
        static Thread _thread;
        static List<OverlayViewportForm> _overlays = new List<OverlayViewportForm>();
        static LayoutProfile _profile;
        static GameTarget _target;
        static SynchronizationContext _ui;
        static bool _floatingMode;

        public static bool IsRunning => Interlocked.CompareExchange(ref _running, 1, 1) == 1;

        /// <param name="floatingMode">
        /// True = native MAIN game + freely draggable overlays.
        /// False = overlays pinned to additional-screen split panes (legacy).
        /// </param>
        public static void Start(LayoutProfile profile, GameTarget target, bool floatingMode = true)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            Stop();

            _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            _profile = profile;
            _target = target;
            _floatingMode = floatingMode;

            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
                return;

            DisplayLayoutHelper.FinalizeProfile(_profile);
            GameWindowSpanService.Log(
                floatingMode
                    ? "Overlay host starting — floating draggable windows (native MAIN capture)."
                    : "Overlay host starting — panes pinned to additional screens.");

            _thread = new Thread(CaptureLoop)
            {
                IsBackground = true,
                Name = "TruckDeckMultimon.Overlays"
            };
            _thread.Start();
        }

        public static void Stop()
        {
            Interlocked.Exchange(ref _running, 0);
            try { _thread?.Join(1500); }
            catch { /* ignore */ }
            _thread = null;

            CloseAllOverlays();
            GameWindowSpanService.Log("Overlay host stopped.");
        }

        static void CaptureLoop()
        {
            var startedOverlays = false;
            var deadline = DateTime.UtcNow.AddMinutes(60);

            while (Interlocked.CompareExchange(ref _running, 1, 1) == 1 && DateTime.UtcNow < deadline)
            {
                try
                {
                    if (!GameProcessGuard.IsGameRunning(out _))
                    {
                        if (startedOverlays)
                        {
                            GameWindowSpanService.Log("Overlay host: game closed.");
                            break;
                        }

                        Thread.Sleep(500);
                        continue;
                    }

                    var hwnd = GameWindowSpanService.FindBestGameWindow(_target);
                    if (hwnd == IntPtr.Zero)
                    {
                        Thread.Sleep(400);
                        continue;
                    }

                    if (!OverlayCaptureService.TryGetClientScreenBounds(hwnd, out var clientScreen))
                    {
                        Thread.Sleep(400);
                        continue;
                    }

                    if (!startedOverlays)
                    {
                        startedOverlays = EnsureOverlaysCreated();
                        if (!startedOverlays)
                        {
                            Thread.Sleep(500);
                            continue;
                        }
                    }

                    // Refresh source rects (in case game client moved) from current profile packing.
                    RefreshSourceBounds(clientScreen);

                    using (var full = OverlayCaptureService.CaptureClient(hwnd))
                    {
                        if (full == null)
                        {
                            PresentStatus("Capture failed (DX). Toggle VSync / keep borderless.");
                            Thread.Sleep(500);
                            continue;
                        }

                        List<OverlayViewportForm> snapshot;
                        lock (Sync)
                            snapshot = _overlays.ToList();

                        foreach (var overlay in snapshot)
                        {
                            var relative = OverlayCaptureService.ToClientRelative(
                                overlay.SourcePixelBounds, clientScreen);
                            var crop = OverlayCaptureService.Crop(full, relative);
                            if (crop != null)
                                overlay.PresentFrame(crop);
                            else
                                overlay.PresentFrame(null, "Viewport outside game window");
                        }
                    }
                }
                catch (Exception ex)
                {
                    GameWindowSpanService.Log("Overlay loop: " + ex.Message);
                }

                Thread.Sleep(33);
            }

            Interlocked.Exchange(ref _running, 0);
            CloseAllOverlays();
        }

        static void RefreshSourceBounds(Rectangle gameClientScreen)
        {
            IList<ViewportDefinition> viewports;
            try
            {
                viewports = _floatingMode
                    ? NativeMainFloatingLayout.CreateViewports(_profile)
                    : NormalizedCoordCalculator.ExpandLayout(_profile);
            }
            catch
            {
                return;
            }

            List<OverlayViewportForm> snapshot;
            lock (Sync)
                snapshot = _overlays.ToList();

            foreach (var overlay in snapshot)
            {
                var source = viewports.FirstOrDefault(v => v.Role == overlay.Role);
                if (source != null)
                {
                    // Map from profile absolute pixels → if game window sits on MAIN, PixelBounds already match desktop.
                    overlay.SourcePixelBounds = source.PixelBounds;
                }
            }
        }

        static bool EnsureOverlaysCreated()
        {
            var panes = BuildOverlayPanes();
            if (panes.Count == 0)
            {
                GameWindowSpanService.Log("Overlay host: no side cameras to float.");
                return false;
            }

            var created = false;
            var done = new ManualResetEventSlim(false);
            _ui.Post(_ =>
            {
                try
                {
                    lock (Sync)
                    {
                        CloseAllOverlaysUnsafe();
                        foreach (var pane in panes)
                        {
                            var form = new OverlayViewportForm(pane.Role, pane.InitialBounds, pane.SourceBounds)
                            {
                                AllowUserMove = true
                            };
                            form.Show();
                            _overlays.Add(form);
                            GameWindowSpanService.Log(
                                $"Floating overlay {pane.Role}: {pane.InitialBounds.Width}×{pane.InitialBounds.Height} at ({pane.InitialBounds.X},{pane.InitialBounds.Y})");
                        }
                    }

                    created = true;
                }
                catch (Exception ex)
                {
                    GameWindowSpanService.Log("Overlay create failed: " + ex.Message);
                }
                finally
                {
                    done.Set();
                }
            }, null);

            done.Wait(5000);
            return created;
        }

        static void PresentStatus(string status)
        {
            List<OverlayViewportForm> snapshot;
            lock (Sync)
                snapshot = _overlays.ToList();
            foreach (var overlay in snapshot)
                overlay.PresentFrame(null, status);
        }

        static void CloseAllOverlays()
        {
            if (_ui == null)
            {
                lock (Sync)
                    CloseAllOverlaysUnsafe();
                return;
            }

            var done = new ManualResetEventSlim(false);
            _ui.Post(_ =>
            {
                try
                {
                    lock (Sync)
                        CloseAllOverlaysUnsafe();
                }
                finally
                {
                    done.Set();
                }
            }, null);
            done.Wait(2000);
        }

        static void CloseAllOverlaysUnsafe()
        {
            foreach (var form in _overlays)
            {
                try
                {
                    form.Close();
                    form.Dispose();
                }
                catch { /* ignore */ }
            }

            _overlays.Clear();
        }

        sealed class OverlayPane
        {
            public ViewportRole Role;
            public Rectangle InitialBounds;
            public Rectangle SourceBounds;
        }

        static List<OverlayPane> BuildOverlayPanes()
        {
            var panes = new List<OverlayPane>();
            if (_profile == null)
                return panes;

            DisplayLayoutHelper.FinalizeProfile(_profile);

            IList<ViewportDefinition> sources;
            if (_floatingMode)
                sources = NativeMainFloatingLayout.CreateViewports(_profile);
            else
                sources = NormalizedCoordCalculator.ExpandLayout(_profile);

            var sideSources = sources
                .Where(v => v.Role != ViewportRole.Center && v.Role != ViewportRole.Unused)
                .ToList();

            var additional = _profile.PhysicalScreens
                .FirstOrDefault(s => s.Index != _profile.MainScreenIndex);

            for (var i = 0; i < sideSources.Count; i++)
            {
                var source = sideSources[i];
                Rectangle initial;
                if (_floatingMode)
                {
                    var size = NativeMainFloatingLayout.PreferredOverlaySize(additional);
                    var origin = NativeMainFloatingLayout.PreferredOverlayOrigin(additional, i, size);
                    initial = new Rectangle(origin, size);
                }
                else
                {
                    // Pin to additional-screen layout panes if possible.
                    initial = source.PixelBounds;
                    if (additional != null && !additional.Bounds.Contains(Rectangle.Inflate(initial, -2, -2)))
                    {
                        var size = NativeMainFloatingLayout.PreferredOverlaySize(additional);
                        var origin = NativeMainFloatingLayout.PreferredOverlayOrigin(additional, i, size);
                        initial = new Rectangle(origin, size);
                    }
                }

                panes.Add(new OverlayPane
                {
                    Role = source.Role,
                    InitialBounds = initial,
                    SourceBounds = source.PixelBounds
                });
            }

            return panes;
        }
    }
}
