namespace TruckDeck.Multimon.Models
{
    /// <summary>
    /// How an additional screen is subdivided into camera panes (MAIN stays a single full view).
    /// </summary>
    public enum AdditionalScreenSplitMode
    {
        Off = 0,
        Two = 2,
        Four = 4
    }

    public sealed class ScreenLayoutEntry
    {
        public int ScreenIndex { get; set; }
        public ViewportRole Role { get; set; } = ViewportRole.Unused;

        /// <summary>True when this (additional) screen uses L/R or 2×2 panes.</summary>
        public bool Split => SplitMode != AdditionalScreenSplitMode.Off;

        public AdditionalScreenSplitMode SplitMode { get; set; } = AdditionalScreenSplitMode.Off;

        public ViewportRole SplitLeftRole { get; set; } = ViewportRole.Left;
        public ViewportRole SplitRightRole { get; set; } = ViewportRole.Right;

        /// <summary>2×2 panes on additional screen: TL, TR, BL, BR.</summary>
        public ViewportRole SplitTopLeftRole { get; set; } = ViewportRole.Left;
        public ViewportRole SplitTopRightRole { get; set; } = ViewportRole.Right;
        public ViewportRole SplitBottomLeftRole { get; set; } = ViewportRole.MirrorLeft;
        public ViewportRole SplitBottomRightRole { get; set; } = ViewportRole.MirrorRight;

        /// <summary>Merge this screen with the monitor immediately to its right into one viewport.</summary>
        public bool SpanNext { get; set; }

        public bool HasAnySplitPaneActive()
        {
            if (SplitMode == AdditionalScreenSplitMode.Two)
                return SplitLeftRole != ViewportRole.Unused || SplitRightRole != ViewportRole.Unused;

            if (SplitMode == AdditionalScreenSplitMode.Four)
            {
                return SplitTopLeftRole != ViewportRole.Unused
                       || SplitTopRightRole != ViewportRole.Unused
                       || SplitBottomLeftRole != ViewportRole.Unused
                       || SplitBottomRightRole != ViewportRole.Unused;
            }

            return false;
        }

        public void EnableTwoPaneSideWindows()
        {
            SplitMode = AdditionalScreenSplitMode.Two;
            Role = ViewportRole.Unused;
            SpanNext = false;
            SplitLeftRole = ViewportRole.Left;
            SplitRightRole = ViewportRole.Right;
        }

        public void EnableFourPaneCameras()
        {
            SplitMode = AdditionalScreenSplitMode.Four;
            Role = ViewportRole.Unused;
            SpanNext = false;
            // Keep total ≤ 4 with MAIN Center: only 3 active side panes (SCS official max).
            SplitTopLeftRole = ViewportRole.Left;
            SplitTopRightRole = ViewportRole.Right;
            SplitBottomLeftRole = ViewportRole.MirrorLeft;
            SplitBottomRightRole = ViewportRole.Unused;
            SplitLeftRole = ViewportRole.Left;
            SplitRightRole = ViewportRole.Right;
        }

        public void ClearSplit()
        {
            SplitMode = AdditionalScreenSplitMode.Off;
        }
    }
}
