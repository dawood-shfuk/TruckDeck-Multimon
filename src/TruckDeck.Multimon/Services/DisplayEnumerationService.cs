using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Services
{
    public sealed class DisplayEnumerationResult
    {
        public IList<PhysicalScreenInfo> Screens { get; set; }
        public Rectangle VirtualDesktopBounds { get; set; }
        public bool HasGaps { get; set; }
        public bool LooksLikeSingleSurface { get; set; }
    }

    public static class DisplayEnumerationService
    {
        public static DisplayEnumerationResult Enumerate()
        {
            var virtualBounds = SystemInformation.VirtualScreen;

            var screens = Screen.AllScreens
                .Select((screen, index) => new PhysicalScreenInfo
                {
                    Index = index,
                    Bounds = screen.Bounds,
                    IsPrimary = screen.Primary,
                    DeviceName = screen.DeviceName
                })
                .OrderBy(s => s.Bounds.X)
                .ThenBy(s => s.Bounds.Y)
                .ToList();

            for (var i = 0; i < screens.Count; i++)
                screens[i].Index = i;

            var hasGaps = DetectGaps(screens, virtualBounds);

            return new DisplayEnumerationResult
            {
                Screens = screens,
                VirtualDesktopBounds = virtualBounds,
                HasGaps = hasGaps,
                LooksLikeSingleSurface = !hasGaps && screens.Count > 1
            };
        }

        static bool DetectGaps(IList<PhysicalScreenInfo> screens, Rectangle virtualBounds)
        {
            if (screens.Count <= 1)
                return false;

            long covered = 0;
            foreach (var screen in screens)
                covered += (long)screen.Bounds.Width * screen.Bounds.Height;

            var total = (long)virtualBounds.Width * virtualBounds.Height;
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
    }
}
