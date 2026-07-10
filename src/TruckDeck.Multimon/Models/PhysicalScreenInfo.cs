using System.Drawing;

namespace TruckDeck.Multimon.Models
{
    public sealed class PhysicalScreenInfo
    {
        public int Index { get; set; }
        public Rectangle Bounds { get; set; }
        public bool IsPrimary { get; set; }
        public string DeviceName { get; set; }

        public string ResolutionLabel => $"{Bounds.Width}×{Bounds.Height}";
    }
}
