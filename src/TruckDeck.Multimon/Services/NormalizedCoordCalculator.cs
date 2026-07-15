using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Services
{
    public static class NormalizedCoordCalculator
    {
        public static NormalizedRect ToNormalized(Rectangle viewport, Rectangle virtualDesktop, bool verticallyStacked)
        {
            if (virtualDesktop.Width <= 0 || virtualDesktop.Height <= 0)
                throw new InvalidOperationException("Virtual desktop bounds are invalid.");

            var relative = new Rectangle(
                viewport.Left - virtualDesktop.Left,
                viewport.Top - virtualDesktop.Top,
                viewport.Width,
                viewport.Height);

            // SCS docs: normalized_y is the bottom-left corner, origin at canvas bottom-left.
            // For side-by-side monitors this matches top-left math. For vertically stacked setups
            // in windowed/borderless mode, ETS2/ATS map y=0 to the top of the canvas — use top-left
            // so bottom physical monitor = center view and top physical monitor = split side windows.
            var y = verticallyStacked
                ? (float)relative.Top / virtualDesktop.Height
                : (float)(virtualDesktop.Bottom - viewport.Bottom) / virtualDesktop.Height;

            return new NormalizedRect
            {
                X = (float)relative.Left / virtualDesktop.Width,
                Y = y,
                Width = (float)relative.Width / virtualDesktop.Width,
                Height = (float)relative.Height / virtualDesktop.Height
            };
        }

        public static NormalizedRect ToNormalized(Rectangle viewport, Rectangle virtualDesktop) =>
            ToNormalized(viewport, virtualDesktop, verticallyStacked: false);

        public static IList<ViewportDefinition> ExpandLayout(LayoutProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            var viewports = new List<ViewportDefinition>();
            var virtualDesktop = profile.GameDesktopBounds.Width > 0 && profile.GameDesktopBounds.Height > 0
                ? profile.GameDesktopBounds
                : profile.VirtualDesktopBounds;
            var verticallyStacked = DisplayLayoutHelper.IsVerticallyStacked(profile.PhysicalScreens);
            var screenByIndex = profile.PhysicalScreens.ToDictionary(s => s.Index);
            var entries = profile.ScreenLayouts.OrderBy(e => e.ScreenIndex).ToList();

            foreach (var entry in entries)
            {
                if (IsMergedIntoPrevious(entry, entries, profile.PhysicalScreens))
                    continue;

                if (!screenByIndex.TryGetValue(entry.ScreenIndex, out var screen))
                    continue;

                if (entry.Role == ViewportRole.Unused && !entry.Split)
                    continue;

                if (entry.Split)
                {
                    AddSplitViewports(viewports, entry, screen, virtualDesktop, profile.DriveSide, verticallyStacked);
                    continue;
                }

                var bounds = screen.Bounds;
                if (entry.SpanNext)
                {
                    var rightScreen = GetScreenToRight(screen, profile.PhysicalScreens);
                    if (rightScreen != null)
                        bounds = Rectangle.Union(bounds, rightScreen.Bounds);
                }

                viewports.Add(CreateViewport(
                    entry.Role,
                    bounds,
                    virtualDesktop,
                    entry.ScreenIndex,
                    profile.DriveSide,
                    verticallyStacked));
            }

            return viewports;
        }

        static void AddSplitViewports(
            IList<ViewportDefinition> viewports,
            ScreenLayoutEntry entry,
            PhysicalScreenInfo screen,
            Rectangle virtualDesktop,
            DriveSide driveSide,
            bool verticallyStacked)
        {
            foreach (var pane in EnumerateSplitPanes(entry, screen.Bounds))
            {
                if (pane.Role == ViewportRole.Unused)
                    continue;

                viewports.Add(CreateViewport(
                    pane.Role,
                    pane.Bounds,
                    virtualDesktop,
                    entry.ScreenIndex,
                    driveSide,
                    verticallyStacked));
            }
        }

        public static IEnumerable<(ViewportRole Role, Rectangle Bounds)> EnumerateSplitPanes(
            ScreenLayoutEntry entry,
            Rectangle screenBounds)
        {
            if (entry == null || entry.SplitMode == AdditionalScreenSplitMode.Off)
                yield break;

            if (entry.SplitMode == AdditionalScreenSplitMode.Two)
            {
                var halfW = screenBounds.Width / 2;
                yield return (entry.SplitLeftRole, new Rectangle(
                    screenBounds.Left, screenBounds.Top, halfW, screenBounds.Height));
                yield return (entry.SplitRightRole, new Rectangle(
                    screenBounds.Left + halfW, screenBounds.Top,
                    screenBounds.Width - halfW, screenBounds.Height));
                yield break;
            }

            // Four = 2×2 grid on additional screen only.
            var midX = screenBounds.Left + screenBounds.Width / 2;
            var midY = screenBounds.Top + screenBounds.Height / 2;
            var halfW4 = midX - screenBounds.Left;
            var halfH4 = midY - screenBounds.Top;
            var rightW = screenBounds.Right - midX;
            var bottomH = screenBounds.Bottom - midY;

            yield return (entry.SplitTopLeftRole, new Rectangle(screenBounds.Left, screenBounds.Top, halfW4, halfH4));
            yield return (entry.SplitTopRightRole, new Rectangle(midX, screenBounds.Top, rightW, halfH4));
            yield return (entry.SplitBottomLeftRole, new Rectangle(screenBounds.Left, midY, halfW4, bottomH));
            yield return (entry.SplitBottomRightRole, new Rectangle(midX, midY, rightW, bottomH));
        }

        static bool IsMergedIntoPrevious(
            ScreenLayoutEntry entry,
            IList<ScreenLayoutEntry> entries,
            IList<PhysicalScreenInfo> screens)
        {
            if (!TryGetScreen(entry.ScreenIndex, screens, out var screen))
                return false;

            foreach (var other in entries)
            {
                if (!other.SpanNext || other.ScreenIndex == entry.ScreenIndex)
                    continue;
                if (!TryGetScreen(other.ScreenIndex, screens, out var otherScreen))
                    continue;
                var right = GetScreenToRight(otherScreen, screens);
                if (right != null && right.Index == entry.ScreenIndex)
                    return true;
            }

            return false;
        }

        static bool TryGetScreen(int index, IList<PhysicalScreenInfo> screens, out PhysicalScreenInfo screen)
        {
            screen = screens.FirstOrDefault(s => s.Index == index);
            return screen != null;
        }

        public static PhysicalScreenInfo GetScreenToRight(PhysicalScreenInfo screen, IList<PhysicalScreenInfo> screens)
        {
            if (screen == null || screens == null)
                return null;

            return screens.FirstOrDefault(s =>
                s.Bounds.Top == screen.Bounds.Top &&
                s.Bounds.Left == screen.Bounds.Right);
        }

        static ViewportDefinition CreateViewport(
            ViewportRole role,
            Rectangle bounds,
            Rectangle virtualDesktop,
            int screenIndex,
            DriveSide driveSide,
            bool verticallyStacked)
        {
            var camera = CameraDefaults.ForRole(role, driveSide);
            return new ViewportDefinition
            {
                Name = camera.Name,
                Role = role,
                PixelBounds = bounds,
                Normalized = ToNormalized(bounds, virtualDesktop, verticallyStacked),
                SourceScreenIndex = screenIndex,
                HeadingOffset = camera.HeadingOffset,
                PitchOffset = camera.PitchOffset,
                RollOffset = camera.RollOffset,
                HorizontalFovOverride = camera.HorizontalFovOverride,
                VerticalFovOverride = camera.VerticalFovOverride,
                RenderInterior = camera.RenderInterior,
                RenderExterior = camera.RenderExterior
            };
        }

        public static ViewportDefinition FindUiAnchor(IList<ViewportDefinition> viewports)
        {
            if (viewports == null || viewports.Count == 0)
                return null;

            return viewports.FirstOrDefault(v => v.Role == ViewportRole.Center)
                   ?? viewports.OrderBy(v => v.Normalized.X).First();
        }
    }

    public sealed class CameraDefaults
    {
        public string Name { get; set; }
        public float HeadingOffset { get; set; }
        public float PitchOffset { get; set; }
        public float RollOffset { get; set; }
        public float HorizontalFovOverride { get; set; }
        public float VerticalFovOverride { get; set; }
        public bool RenderInterior { get; set; } = true;
        public bool RenderExterior { get; set; } = true;

        public static CameraDefaults ForRole(ViewportRole role, DriveSide driveSide)
        {
            var sign = driveSide == DriveSide.Lhd ? 1f : -1f;
            switch (role)
            {
                case ViewportRole.Left:
                    // Side window looking out — interior ON so wing mirrors stay in view.
                    // Positive SCS heading looks to the driver's right; Left view uses negative yaw.
                    return new CameraDefaults
                    {
                        Name = "left",
                        HeadingOffset = -52f * sign,
                        PitchOffset = -6f,
                        HorizontalFovOverride = 70f,
                        RenderInterior = true,
                        RenderExterior = true
                    };
                case ViewportRole.Right:
                    return new CameraDefaults
                    {
                        Name = "right",
                        HeadingOffset = 52f * sign,
                        PitchOffset = -6f,
                        HorizontalFovOverride = 70f,
                        RenderInterior = true,
                        RenderExterior = true
                    };
                case ViewportRole.Aux:
                    return new CameraDefaults { Name = "aux", PitchOffset = -40f };
                case ViewportRole.MirrorLeft:
                    // Dedicated close mirror-facing view.
                    return new CameraDefaults
                    {
                        Name = "left_mirror",
                        HeadingOffset = -78f * sign,
                        PitchOffset = -8f,
                        HorizontalFovOverride = 55f,
                        RenderInterior = true,
                        RenderExterior = true
                    };
                case ViewportRole.MirrorRight:
                    return new CameraDefaults
                    {
                        Name = "right_mirror",
                        HeadingOffset = 78f * sign,
                        PitchOffset = -8f,
                        HorizontalFovOverride = 55f,
                        RenderInterior = true,
                        RenderExterior = true
                    };
                case ViewportRole.Center:
                default:
                    // 0 FOV override = in-game seat / camera FOV (same as single-screen).
                    return new CameraDefaults
                    {
                        Name = "center",
                        HeadingOffset = 0f,
                        RenderInterior = true,
                        RenderExterior = true
                    };
            }
        }
    }
}
