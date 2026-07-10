using System.Collections.Generic;
using System.Drawing;

namespace TruckDeck.Multimon.Models
{
    public sealed class LayoutProfile
    {
        public string PresetName { get; set; }
        public DriveSide DriveSide { get; set; } = DriveSide.Lhd;
        public Rectangle VirtualDesktopBounds { get; set; }
        /// <summary>Game window target. MAIN-only PiP mode uses MAIN bounds; full-span uses virtual desktop.</summary>
        public Rectangle GameDesktopBounds { get; set; }
        public int MainScreenIndex { get; set; }
        public IList<PhysicalScreenInfo> PhysicalScreens { get; set; } = new List<PhysicalScreenInfo>();
        public IList<ScreenLayoutEntry> ScreenLayouts { get; set; } = new List<ScreenLayoutEntry>();
        public bool HasDesktopGaps { get; set; }

        /// <summary>When true, game runs at MAIN native res with free-placed PiP cameras on that screen only.</summary>
        public bool UseMainPipMode { get; set; }

        /// <summary>Extra layout panels on MAIN (drag/resize). Max 3 so total cameras ≤ 4 with Center.</summary>
        public IList<MainPipPanel> MainPipPanels { get; set; } = new List<MainPipPanel>();
    }
}
