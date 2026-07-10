using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Services
{
    public enum MultimonApplyPhase
    {
        Menu,
        Driving,
        /// <summary>Game window = MAIN native resolution; side cams packed as strips for floating overlays.</summary>
        FloatingNativeMain,
        /// <summary>Game window = MAIN native; free-placed PiP cameras on the same screen (no 2nd monitor needed).</summary>
        MainPip
    }

    /// <summary>
    /// Builds a single-monitor menu/title layout on the MAIN display only.
    /// </summary>
    public static class MenuLayoutHelper
    {
        public static Rectangle GetPrimaryBounds(LayoutProfile profile)
        {
            if (profile?.PhysicalScreens == null || profile.PhysicalScreens.Count == 0)
                return Rectangle.Empty;

            var main = profile.PhysicalScreens.FirstOrDefault(s => s.Index == profile.MainScreenIndex)
                       ?? profile.PhysicalScreens.First();
            return main.Bounds;
        }

        public static IList<ViewportDefinition> CreateMenuViewports(LayoutProfile profile)
        {
            var primary = GetPrimaryBounds(profile);
            if (primary.Width <= 0 || primary.Height <= 0)
                return new List<ViewportDefinition>();

            var camera = CameraDefaultsForMenu();

            return new List<ViewportDefinition>
            {
                new ViewportDefinition
                {
                    Name = "center",
                    Role = ViewportRole.Center,
                    PixelBounds = primary,
                    Normalized = new NormalizedRect { X = 0, Y = 0, Width = 1, Height = 1 },
                    SourceScreenIndex = profile.MainScreenIndex,
                    HeadingOffset = camera.HeadingOffset,
                    PitchOffset = camera.PitchOffset,
                    RollOffset = camera.RollOffset,
                    HorizontalFovOverride = camera.HorizontalFovOverride,
                    VerticalFovOverride = camera.VerticalFovOverride,
                    RenderInterior = camera.RenderInterior,
                    RenderExterior = camera.RenderExterior
                }
            };
        }

        static (float HeadingOffset, float PitchOffset, float RollOffset, float HorizontalFovOverride,
            float VerticalFovOverride, bool RenderInterior, bool RenderExterior) CameraDefaultsForMenu()
        {
            return (0f, 0f, 0f, 0f, 0f, true, true);
        }
    }
}
