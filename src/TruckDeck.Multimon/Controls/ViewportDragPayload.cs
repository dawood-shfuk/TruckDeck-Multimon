using System;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Controls
{
    public enum ViewportDragKind
    {
        Role,
        SplitPair,
        SplitQuad,
        MoveAssignment
    }

    public enum DropZone
    {
        Full,
        LeftHalf,
        RightHalf
    }

    [Serializable]
    public sealed class ViewportDragPayload
    {
        public ViewportDragKind Kind { get; set; }
        public ViewportRole Role { get; set; }
        public int SourceScreenIndex { get; set; } = -1;
        public DropZone SourceZone { get; set; } = DropZone.Full;

        public static ViewportDragPayload ForRole(ViewportRole role) =>
            new ViewportDragPayload { Kind = ViewportDragKind.Role, Role = role };

        public static ViewportDragPayload ForSplitPair() =>
            new ViewportDragPayload { Kind = ViewportDragKind.SplitPair };

        public static ViewportDragPayload ForSplitQuad() =>
            new ViewportDragPayload { Kind = ViewportDragKind.SplitQuad };

        public static ViewportDragPayload ForMove(int screenIndex, DropZone zone, ViewportRole role) =>
            new ViewportDragPayload
            {
                Kind = ViewportDragKind.MoveAssignment,
                Role = role,
                SourceScreenIndex = screenIndex,
                SourceZone = zone
            };
    }
}
