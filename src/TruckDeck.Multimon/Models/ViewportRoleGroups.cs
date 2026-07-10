using System.Collections.Generic;

namespace TruckDeck.Multimon.Models
{
    public static class ViewportRoleGroups
    {
        public static readonly IReadOnlyList<ViewportRole> FullScreenRoles = new[]
        {
            ViewportRole.Center,
            ViewportRole.Left,
            ViewportRole.Right,
            ViewportRole.Aux,
            ViewportRole.Unused
        };

        public static readonly IReadOnlyList<ViewportRole> SplitHalfRoles = new[]
        {
            ViewportRole.Left,
            ViewportRole.Right,
            ViewportRole.MirrorLeft,
            ViewportRole.MirrorRight,
            ViewportRole.Center,
            ViewportRole.Aux,
            ViewportRole.Unused
        };

        public static string FormatRoleLabel(ViewportRole role)
        {
            switch (role)
            {
                case ViewportRole.MirrorLeft: return "Left mirror";
                case ViewportRole.MirrorRight: return "Right mirror";
                case ViewportRole.Left: return "Left window";
                case ViewportRole.Right: return "Right window";
                case ViewportRole.Unused: return "Unused";
                default: return role.ToString();
            }
        }
    }
}
