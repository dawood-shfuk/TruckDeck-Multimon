using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Services
{
    /// <summary>
    /// Packs center + side cameras onto the MAIN physical monitor only (native resolution).
    /// Side strips are capture sources for floating overlay windows placed elsewhere.
    /// </summary>
    public static class NativeMainFloatingLayout
    {
        const int MinStripWidth = 160;
        const int PreferredStripWidth = 320;

        public static IList<ViewportDefinition> CreateViewports(LayoutProfile profile)
        {
            var result = new List<ViewportDefinition>();
            if (profile == null)
                return result;

            var main = MenuLayoutHelper.GetPrimaryBounds(profile);
            if (main.Width <= 0 || main.Height <= 0)
                return result;

            var sideRoles = CollectSideRoles(profile);
            if (sideRoles.Count == 0)
            {
                // Cabin only on MAIN.
                result.Add(Create(
                    ViewportRole.Center, main, main, profile.MainScreenIndex, profile.DriveSide));
                return result;
            }

            var stripWidth = Math.Max(MinStripWidth, Math.Min(PreferredStripWidth, main.Width / 10));
            var centerWidth = main.Width - stripWidth;
            if (centerWidth < main.Width / 2)
            {
                stripWidth = main.Width / 6;
                centerWidth = main.Width - stripWidth;
            }

            var centerBounds = new Rectangle(main.Left, main.Top, centerWidth, main.Height);
            var column = new Rectangle(main.Left + centerWidth, main.Top, stripWidth, main.Height);

            result.Add(Create(
                ViewportRole.Center, centerBounds, main, profile.MainScreenIndex, profile.DriveSide));

            var pieceHeight = column.Height / sideRoles.Count;
            for (var i = 0; i < sideRoles.Count; i++)
            {
                var top = column.Top + i * pieceHeight;
                var height = i == sideRoles.Count - 1
                    ? column.Bottom - top
                    : pieceHeight;
                var bounds = new Rectangle(column.Left, top, column.Width, height);
                result.Add(Create(
                    sideRoles[i], bounds, main, profile.MainScreenIndex, profile.DriveSide));
            }

            return result;
        }

        public static MultimonCfgPatchOptions CreateCfgOptions(LayoutProfile profile)
        {
            var main = MenuLayoutHelper.GetPrimaryBounds(profile);
            if (main.Width <= 0 || main.Height <= 0)
                return null;

            return new MultimonCfgPatchOptions
            {
                ModeWidth = main.Width,
                ModeHeight = main.Height,
                Fullscreen = false,
                OutputIndex = -1,
                WindowX = main.Left,
                WindowY = main.Top,
                EnableDeveloperConsole = true
            };
        }

        /// <summary>Preferred initial floating window size for a side cam (user can drag/resize).</summary>
        public static Size PreferredOverlaySize(PhysicalScreenInfo additionalScreenOrNull)
        {
            if (additionalScreenOrNull != null && additionalScreenOrNull.Bounds.Width > 0)
            {
                var w = Math.Max(480, additionalScreenOrNull.Bounds.Width / 2);
                var h = Math.Max(270, additionalScreenOrNull.Bounds.Height / 2);
                return new Size(w, h);
            }

            return new Size(640, 360);
        }

        public static Point PreferredOverlayOrigin(
            PhysicalScreenInfo additionalScreenOrNull,
            int paneIndex,
            Size size)
        {
            if (additionalScreenOrNull != null)
            {
                var x = additionalScreenOrNull.Bounds.Left + 24 + (paneIndex % 2) * (size.Width + 16);
                var y = additionalScreenOrNull.Bounds.Top + 24 + (paneIndex / 2) * (size.Height + 16);
                return new Point(x, y);
            }

            return new Point(80 + paneIndex * 40, 80 + paneIndex * 40);
        }

        static List<ViewportRole> CollectSideRoles(LayoutProfile profile)
        {
            var roles = new List<ViewportRole>();
            foreach (var entry in profile.ScreenLayouts ?? Enumerable.Empty<ScreenLayoutEntry>())
            {
                if (entry.ScreenIndex == profile.MainScreenIndex)
                    continue;

                if (entry.Split)
                {
                    foreach (var piece in NormalizedCoordCalculator.EnumerateSplitPanes(
                                 entry, Rectangle.FromLTRB(0, 0, 100, 100)))
                    {
                        if (piece.Role != ViewportRole.Unused && piece.Role != ViewportRole.Center &&
                            !roles.Contains(piece.Role))
                            roles.Add(piece.Role);
                    }
                }
                else if (entry.Role != ViewportRole.Unused && entry.Role != ViewportRole.Center &&
                         !roles.Contains(entry.Role))
                {
                    roles.Add(entry.Role);
                }
            }

            // Cap at 3 sides so Center + sides ≤ 4 (SCS official).
            if (roles.Count > 3)
                roles = roles.Take(3).ToList();

            if (roles.Count == 0)
            {
                roles.Add(ViewportRole.Left);
                roles.Add(ViewportRole.Right);
            }

            return roles;
        }

        static ViewportDefinition Create(
            ViewportRole role,
            Rectangle pixelBounds,
            Rectangle canvas,
            int screenIndex,
            DriveSide driveSide)
        {
            // Always top-left origin for single-monitor canvas packing.
            var normalized = NormalizedCoordCalculator.ToNormalized(pixelBounds, canvas, verticallyStacked: true);
            var camera = CameraDefaults.ForRole(role, driveSide);

            // Thin capture strips: keep side FOV moderate so overlays look usable when scaled.
            if (role != ViewportRole.Center && camera.HorizontalFovOverride <= 0f)
                camera.HorizontalFovOverride = 65f;

            return new ViewportDefinition
            {
                Name = camera.Name,
                Role = role,
                PixelBounds = pixelBounds,
                Normalized = normalized,
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
    }
}
