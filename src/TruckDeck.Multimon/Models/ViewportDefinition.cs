using System.Drawing;

namespace TruckDeck.Multimon.Models
{
    public sealed class NormalizedRect
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }

    public sealed class ViewportDefinition
    {
        public string Name { get; set; }
        public ViewportRole Role { get; set; }
        public Rectangle PixelBounds { get; set; }
        public NormalizedRect Normalized { get; set; }
        public int SourceScreenIndex { get; set; }

        public float HeadingOffset { get; set; }
        public float PitchOffset { get; set; }
        public float RollOffset { get; set; }
        public float HorizontalFovOverride { get; set; }
        public float VerticalFovOverride { get; set; }
        public bool RenderInterior { get; set; } = true;
        public bool RenderExterior { get; set; } = true;
    }
}
