using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Services
{
    /// <summary>
    /// MAIN native resolution + free-placed PiP side cameras (SCS multimon on one monitor).
    /// No second screen required — place Left/Right/Mirror panels anywhere over the cabin.
    /// </summary>
    public static class MainPipLayoutService
    {
        public const int MaxPipPanels = 3; // Center + 3 = 4 SCS official max

        public static void EnsureDefaultPips(LayoutProfile profile)
        {
            if (profile == null)
                return;
            if (profile.MainPipPanels == null)
                profile.MainPipPanels = new List<MainPipPanel>();

            if (profile.MainPipPanels.Count > 0)
                return;

            // Default corners: L top-left, R top-right — classic PiP side window look.
            var left = new MainPipPanel
            {
                Role = ViewportRole.Left,
                X = 0.02f,
                Y = 0.08f,
                Width = 0.24f,
                Height = 0.32f
            };
            left.ApplyRoleDefaults(profile.DriveSide);
            profile.MainPipPanels.Add(left);

            var right = new MainPipPanel
            {
                Role = ViewportRole.Right,
                X = 0.74f,
                Y = 0.08f,
                Width = 0.24f,
                Height = 0.32f
            };
            right.ApplyRoleDefaults(profile.DriveSide);
            profile.MainPipPanels.Add(right);
        }

        public static IList<ViewportDefinition> CreateViewports(LayoutProfile profile)
        {
            var result = new List<ViewportDefinition>();
            if (profile == null)
                return result;

            var main = MenuLayoutHelper.GetPrimaryBounds(profile);
            if (main.Width <= 0 || main.Height <= 0)
                return result;

            EnsureDefaultPips(profile);

            // Center = full MAIN (SCS draws other monitors on top as separate frustums in their rects).
            result.Add(CreateViewport(ViewportRole.Center, main, main, profile.MainScreenIndex, profile.DriveSide));

            foreach (var pip in profile.MainPipPanels.Take(MaxPipPanels))
            {
                if (pip.Role == ViewportRole.Unused || pip.Role == ViewportRole.Center)
                    continue;

                pip.Clamp();
                var bounds = pip.ToPixelBounds(main);
                result.Add(CreateViewport(pip, bounds, main, profile.MainScreenIndex, profile.DriveSide));
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

        public static MainPipPanel AddPanel(LayoutProfile profile, ViewportRole role)
        {
            EnsureDefaultPips(profile);
            if (profile.MainPipPanels.Count >= MaxPipPanels)
                return null;

            var panel = new MainPipPanel
            {
                Role = role,
                X = 0.35f,
                Y = 0.55f,
                Width = 0.28f,
                Height = 0.28f
            };
            // Stagger if overlapping another
            panel.X = 0.1f + 0.15f * profile.MainPipPanels.Count;
            panel.Y = 0.55f;
            panel.Clamp();
            panel.ApplyRoleDefaults(profile.DriveSide);
            profile.MainPipPanels.Add(panel);
            return panel;
        }

        static ViewportDefinition CreateViewport(
            MainPipPanel pip,
            Rectangle pixelBounds,
            Rectangle canvas,
            int screenIndex,
            DriveSide driveSide)
        {
            var normalized = NormalizedCoordCalculator.ToNormalized(pixelBounds, canvas, verticallyStacked: true);
            var camera = CameraDefaults.ForRole(pip.Role, driveSide);
            pip.ResolveCamera(driveSide, out var heading, out var pitch, out var fov);

            return new ViewportDefinition
            {
                Name = camera.Name,
                Role = pip.Role,
                PixelBounds = pixelBounds,
                Normalized = normalized,
                SourceScreenIndex = screenIndex,
                HeadingOffset = heading,
                PitchOffset = pitch,
                RollOffset = camera.RollOffset,
                HorizontalFovOverride = fov,
                VerticalFovOverride = camera.VerticalFovOverride,
                RenderInterior = camera.RenderInterior,
                RenderExterior = camera.RenderExterior
            };
        }

        static ViewportDefinition CreateViewport(
            ViewportRole role,
            Rectangle pixelBounds,
            Rectangle canvas,
            int screenIndex,
            DriveSide driveSide)
        {
            var normalized = NormalizedCoordCalculator.ToNormalized(pixelBounds, canvas, verticallyStacked: true);
            var camera = CameraDefaults.ForRole(role, driveSide);

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
