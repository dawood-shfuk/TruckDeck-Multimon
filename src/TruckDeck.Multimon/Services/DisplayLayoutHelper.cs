using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Services
{
    public static class DisplayLayoutHelper
    {
        public static bool Is2x2Grid(IList<PhysicalScreenInfo> screens)
        {
            if (screens == null || screens.Count != 4)
                return false;

            var distinctX = screens.Select(s => s.Bounds.Left).Distinct().Count();
            var distinctY = screens.Select(s => s.Bounds.Top).Distinct().Count();
            return distinctX == 2 && distinctY == 2;
        }

        /// <summary>Monitors arranged one above another (different Y, not a single horizontal row).</summary>
        public static bool IsVerticallyStacked(IList<PhysicalScreenInfo> screens)
        {
            if (screens == null || screens.Count < 2)
                return false;

            var distinctTops = screens.Select(s => s.Bounds.Top).Distinct().Count();
            if (distinctTops < 2)
                return false;

            var rows = screens.GroupBy(s => s.Bounds.Top).OrderBy(g => g.Key).ToList();
            return rows.Count >= 2 && rows.All(r => r.Count() >= 1);
        }

        public static PhysicalScreenInfo GetBottomMostScreen(IList<PhysicalScreenInfo> screens) =>
            screens?.OrderByDescending(s => s.Bounds.Bottom).FirstOrDefault();

        public static PhysicalScreenInfo GetTopMostScreen(IList<PhysicalScreenInfo> screens) =>
            screens?.OrderBy(s => s.Bounds.Top).FirstOrDefault();

        public static void ApplyStackedBottomCenterTopSplit(LayoutProfile profile)
        {
            if (profile?.PhysicalScreens == null)
                return;

            var bottom = GetBottomMostScreen(profile.PhysicalScreens.ToList());
            var top = GetTopMostScreen(profile.PhysicalScreens.ToList());
            if (bottom == null)
                return;

            profile.MainScreenIndex = bottom.Index;

            foreach (var screen in profile.PhysicalScreens)
            {
                var entry = GetOrCreateEntry(profile, screen.Index);
                if (screen.Index == bottom.Index)
                {
                    entry.Role = ViewportRole.Center;
                    entry.ClearSplit();
                    entry.SpanNext = false;
                }
                else if (top != null && screen.Index == top.Index)
                {
                    entry.EnableTwoPaneSideWindows();
                }
                else
                {
                    entry.Role = ViewportRole.Unused;
                    entry.ClearSplit();
                    entry.SpanNext = false;
                }
            }
        }

        public static int DefaultMainScreenIndex(IList<PhysicalScreenInfo> screens)
        {
            if (screens == null || screens.Count == 0)
                return 0;

            var primary = screens.FirstOrDefault(s => s.IsPrimary);
            if (primary != null)
                return primary.Index;

            return screens
                .OrderByDescending(s => (long)s.Bounds.Width * s.Bounds.Height)
                .First()
                .Index;
        }

        public static void FinalizeProfile(LayoutProfile profile)
        {
            if (profile == null)
                return;

            if (profile.MainScreenIndex < 0 ||
                profile.PhysicalScreens.All(s => s.Index != profile.MainScreenIndex))
            {
                profile.MainScreenIndex = DefaultMainScreenIndex(profile.PhysicalScreens);
            }

            // Always match Windows virtual desktop so the borderless game window spans every monitor.
            profile.GameDesktopBounds = ComputeGameDesktop(profile);
            profile.HasDesktopGaps = DetectGaps(profile.PhysicalScreens, profile.GameDesktopBounds);
        }

        public static Rectangle ComputeGameDesktop(LayoutProfile profile)
        {
            if (profile == null)
                return Rectangle.Empty;

            if (profile.VirtualDesktopBounds.Width > 0 && profile.VirtualDesktopBounds.Height > 0)
                return profile.VirtualDesktopBounds;

            if (profile.PhysicalScreens == null || profile.PhysicalScreens.Count == 0)
                return Rectangle.Empty;

            var left = profile.PhysicalScreens.Min(s => s.Bounds.Left);
            var top = profile.PhysicalScreens.Min(s => s.Bounds.Top);
            var right = profile.PhysicalScreens.Max(s => s.Bounds.Right);
            var bottom = profile.PhysicalScreens.Max(s => s.Bounds.Bottom);
            return Rectangle.FromLTRB(left, top, right, bottom);
        }

        public static IList<PhysicalScreenInfo> GetActiveScreens(LayoutProfile profile)
        {
            if (profile?.PhysicalScreens == null || profile.ScreenLayouts == null)
                return new List<PhysicalScreenInfo>();

            var activeIndices = profile.ScreenLayouts
                .Where(entry =>
                    entry.Role != ViewportRole.Unused ||
                    entry.HasAnySplitPaneActive())
                .Select(entry => entry.ScreenIndex)
                .ToHashSet();

            return profile.PhysicalScreens
                .Where(screen => activeIndices.Contains(screen.Index))
                .ToList();
        }

        public static void ApplyCenterOnMainLayout(LayoutProfile profile, bool splitOtherScreensWithSideWindows)
        {
            if (profile?.PhysicalScreens == null || profile.ScreenLayouts == null)
                return;

            foreach (var screen in profile.PhysicalScreens)
            {
                var entry = GetOrCreateLayoutEntry(profile, screen.Index);
                if (screen.Index == profile.MainScreenIndex)
                {
                    entry.Role = ViewportRole.Center;
                    entry.ClearSplit();
                    entry.SpanNext = false;
                    continue;
                }

                if (splitOtherScreensWithSideWindows)
                    entry.EnableTwoPaneSideWindows();
            }
        }

        public static void ApplyCenterOnOtherLayout(LayoutProfile profile)
        {
            if (profile?.PhysicalScreens == null || profile.ScreenLayouts == null)
                return;

            foreach (var screen in profile.PhysicalScreens)
            {
                var entry = GetOrCreateLayoutEntry(profile, screen.Index);
                if (screen.Index == profile.MainScreenIndex)
                {
                    entry.EnableTwoPaneSideWindows();
                    continue;
                }

                entry.Role = ViewportRole.Center;
                entry.ClearSplit();
                entry.SpanNext = false;
            }
        }

        public static string GetPhysicalPosition(PhysicalScreenInfo screen, IList<PhysicalScreenInfo> screens)
        {
            if (screen == null || screens == null || screens.Count <= 1)
                return "only display";

            var distinctTops = screens.Select(s => s.Bounds.Top).Distinct().OrderBy(t => t).ToList();
            var distinctLefts = screens.Select(s => s.Bounds.Left).Distinct().OrderBy(l => l).ToList();

            string vertical = null;
            if (distinctTops.Count > 1)
            {
                if (screen.Bounds.Top == distinctTops.First())
                    vertical = "top";
                else if (screen.Bounds.Top == distinctTops.Last())
                    vertical = "bottom";
                else
                    vertical = "middle";
            }

            string horizontal = null;
            if (distinctLefts.Count > 1)
            {
                if (screen.Bounds.Left == distinctLefts.First())
                    horizontal = "left";
                else if (screen.Bounds.Left == distinctLefts.Last())
                    horizontal = "right";
                else
                    horizontal = "center";
            }

            if (vertical != null && horizontal != null)
                return vertical + "-" + horizontal;
            return vertical ?? horizontal ?? "center";
        }

        public static string GetPositionLabel(PhysicalScreenInfo screen, IList<PhysicalScreenInfo> screens, int? mainScreenIndex = null)
        {
            if (screen == null || screens == null || screens.Count == 0)
                return $"Screen {screen?.Index + 1}";

            var position = GetPhysicalPosition(screen, screens);
            var mainTag = mainScreenIndex == screen.Index ? " · MAIN (fullscreen center)" : "";
            var primaryTag = screen.IsPrimary ? " · Windows primary" : "";
            return $"Screen {screen.Index + 1} ({position}{mainTag}{primaryTag})";
        }

        public static string FormatMainDisplayOption(
            PhysicalScreenInfo screen,
            IList<PhysicalScreenInfo> screens,
            Rectangle gameDesktop)
        {
            var position = GetPhysicalPosition(screen, screens);
            var primaryTag = screen.IsPrimary ? " · Windows primary" : "";
            var canvas = gameDesktop.Width > 0 && gameDesktop.Height > 0
                ? $"{gameDesktop.Width}×{gameDesktop.Height}"
                : screen.ResolutionLabel;
            return $"{canvas} (full desktop) · cabin on {screen.ResolutionLabel} {position}{primaryTag}";
        }

        public static string FormatGameCanvasLabel(LayoutProfile profile)
        {
            if (profile == null)
                return "";

            var game = profile.GameDesktopBounds;
            return $"MAIN / game canvas: {game.Width}×{game.Height} at ({game.Left}, {game.Top})";
        }

        public static IList<int> GetBottomRowScreenIndices(IList<PhysicalScreenInfo> screens)
        {
            if (screens == null || screens.Count == 0)
                return new List<int>();

            var bottomY = screens.Max(s => s.Bounds.Top);
            return screens
                .Where(s => s.Bounds.Top == bottomY)
                .OrderBy(s => s.Bounds.X)
                .Select(s => s.Index)
                .ToList();
        }

        public static int CountActivePhysicalScreens(LayoutProfile profile)
        {
            return GetActiveScreens(profile).Count;
        }

        public static IList<string> ValidateLayout(LayoutProfile profile, IList<ViewportDefinition> viewports)
        {
            var warnings = new List<string>();
            if (profile == null || viewports == null || viewports.Count == 0)
                return warnings;

            var center = viewports.FirstOrDefault(v => v.Role == ViewportRole.Center);
            var sideViews = viewports.Where(v =>
                v.Role == ViewportRole.MirrorLeft ||
                v.Role == ViewportRole.MirrorRight ||
                v.Role == ViewportRole.Left ||
                v.Role == ViewportRole.Right).ToList();

            if (center == null)
                warnings.Add("No center camera — assign Center to your MAIN display.");

            if (sideViews.Count == 0)
                warnings.Add("No side window views — on your extra display(s), enable Split with Left window + Right window.");

            if (center != null && sideViews.Count > 0)
            {
                foreach (var side in sideViews)
                {
                    if (Overlaps(center.PixelBounds, side.PixelBounds))
                    {
                        warnings.Add(
                            "Center and side views overlap — center belongs on MAIN only; side windows on other display(s).");
                        break;
                    }
                }
            }

            if (center != null && center.SourceScreenIndex != profile.MainScreenIndex)
            {
                warnings.Add(
                    $"Center camera is on screen {center.SourceScreenIndex + 1}, but MAIN is set to screen {profile.MainScreenIndex + 1}.");
            }

            var byScreen = viewports.GroupBy(v => v.SourceScreenIndex);
            foreach (var group in byScreen)
            {
                var hasCenter = group.Any(v => v.Role == ViewportRole.Center);
                var hasSide = group.Any(v =>
                    v.Role == ViewportRole.MirrorLeft || v.Role == ViewportRole.MirrorRight ||
                    v.Role == ViewportRole.Left || v.Role == ViewportRole.Right);
                if (hasCenter && hasSide)
                {
                    warnings.Add(
                        $"Screen {group.Key + 1} has both center and side views — use MAIN for center, other displays for side windows.");
                }
            }

            return warnings;
        }

        public static ScreenLayoutEntry GetOrCreateEntry(LayoutProfile profile, int screenIndex)
        {
            var entry = profile.ScreenLayouts.FirstOrDefault(e => e.ScreenIndex == screenIndex);
            if (entry != null)
                return entry;

            entry = new ScreenLayoutEntry { ScreenIndex = screenIndex };
            profile.ScreenLayouts.Add(entry);
            return entry;
        }

        static ScreenLayoutEntry GetOrCreateLayoutEntry(LayoutProfile profile, int screenIndex) =>
            GetOrCreateEntry(profile, screenIndex);

        static bool DetectGaps(IList<PhysicalScreenInfo> screens, Rectangle bounds)
        {
            if (screens == null || screens.Count <= 1)
                return false;

            long covered = 0;
            foreach (var screen in screens)
                covered += (long)screen.Bounds.Width * screen.Bounds.Height;

            var total = (long)bounds.Width * bounds.Height;
            if (covered < total)
                return true;

            var rows = screens.GroupBy(s => s.Bounds.Top);
            foreach (var row in rows)
            {
                var ordered = row.OrderBy(s => s.Bounds.Left).ToList();
                for (var i = 0; i < ordered.Count - 1; i++)
                {
                    if (ordered[i].Bounds.Right != ordered[i + 1].Bounds.Left)
                        return true;
                }
            }

            var columns = screens.GroupBy(s => s.Bounds.Left);
            foreach (var column in columns)
            {
                var ordered = column.OrderBy(s => s.Bounds.Top).ToList();
                for (var i = 0; i < ordered.Count - 1; i++)
                {
                    if (ordered[i].Bounds.Bottom != ordered[i + 1].Bounds.Top)
                        return true;
                }
            }

            return false;
        }

        static bool Overlaps(Rectangle a, Rectangle b) =>
            a.IntersectsWith(b) && !a.Equals(b);
    }
}
