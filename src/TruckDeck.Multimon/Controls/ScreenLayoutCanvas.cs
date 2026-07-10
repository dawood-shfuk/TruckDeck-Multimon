using System;
using System.Drawing;
using System.Windows.Forms;
using TruckDeck.Multimon.Helpers;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Controls
{
    public sealed class ScreenLayoutCanvas : Control
    {
        LayoutProfile _profile;

        public ScreenLayoutCanvas()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            BackColor = MultimonTheme.Bg;
            Height = 120;
        }

        public void SetProfile(LayoutProfile profile)
        {
            _profile = profile;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(BackColor);

            if (_profile == null || _profile.PhysicalScreens == null || _profile.PhysicalScreens.Count == 0)
            {
                TextRenderer.DrawText(g, "No displays detected", Font, ClientRectangle, MultimonTheme.Label,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            var virtualBounds = _profile.GameDesktopBounds.Width > 0 && _profile.GameDesktopBounds.Height > 0
                ? _profile.GameDesktopBounds
                : _profile.VirtualDesktopBounds;
            if (virtualBounds.Width <= 0 || virtualBounds.Height <= 0)
                return;

            var margin = 8;
            var area = new Rectangle(margin, margin, Width - margin * 2, Height - margin * 2);

            foreach (var screen in _profile.PhysicalScreens)
            {
                var tile = MapRect(screen.Bounds, virtualBounds, area);
                using (var brush = new SolidBrush(MultimonTheme.Panel))
                using (var pen = new Pen(MultimonTheme.Line, 2f))
                {
                    g.FillRectangle(brush, tile);
                    g.DrawRectangle(pen, tile);
                }

                var layout = FindLayout(screen.Index);
                var roleText = layout == null ? "?" : FormatRole(layout);
                var label = $"#{screen.Index + 1} {roleText}";
                TextRenderer.DrawText(g, label, Font, tile, MultimonTheme.Ink,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                if (layout != null && layout.SplitMode == AdditionalScreenSplitMode.Four)
                {
                    var midX = tile.Left + tile.Width / 2;
                    var midY = tile.Top + tile.Height / 2;
                    using (var pen = new Pen(MultimonTheme.Accent, 1f))
                    {
                        g.DrawLine(pen, midX, tile.Top, midX, tile.Bottom);
                        g.DrawLine(pen, tile.Left, midY, tile.Right, midY);
                    }
                }
                else if (layout != null && layout.SplitMode == AdditionalScreenSplitMode.Two)
                {
                    var midX = tile.Left + tile.Width / 2;
                    using (var pen = new Pen(MultimonTheme.Accent, 1f))
                        g.DrawLine(pen, midX, tile.Top, midX, tile.Bottom);
                }
            }
        }

        ScreenLayoutEntry FindLayout(int screenIndex)
        {
            if (_profile?.ScreenLayouts == null)
                return null;
            foreach (var entry in _profile.ScreenLayouts)
            {
                if (entry.ScreenIndex == screenIndex)
                    return entry;
            }
            return null;
        }

        static string FormatRole(ScreenLayoutEntry entry)
        {
            if (entry.SplitMode == AdditionalScreenSplitMode.Four)
                return "2×2";

            if (entry.SplitMode == AdditionalScreenSplitMode.Two)
            {
                var left = ShortRole(entry.SplitLeftRole);
                var right = ShortRole(entry.SplitRightRole);
                return left + " | " + right;
            }

            if (entry.SpanNext)
                return entry.Role + " span→";

            return entry.Role.ToString();
        }

        static string ShortRole(ViewportRole role)
        {
            switch (role)
            {
                case ViewportRole.MirrorLeft: return "L mir";
                case ViewportRole.MirrorRight: return "R mir";
                case ViewportRole.Left: return "L win";
                case ViewportRole.Right: return "R win";
                case ViewportRole.Center: return "Ctr";
                case ViewportRole.Aux: return "Aux";
                default: return "-";
            }
        }

        static Rectangle MapRect(Rectangle source, Rectangle virtualBounds, Rectangle targetArea)
        {
            var scale = Math.Min(
                (float)targetArea.Width / virtualBounds.Width,
                (float)targetArea.Height / virtualBounds.Height);
            var drawWidth = (int)(virtualBounds.Width * scale);
            var drawHeight = (int)(virtualBounds.Height * scale);
            var offsetX = targetArea.Left + (targetArea.Width - drawWidth) / 2;
            var offsetY = targetArea.Top + (targetArea.Height - drawHeight) / 2;

            var x = offsetX + (int)((source.Left - virtualBounds.Left) * scale);
            var y = offsetY + (int)((source.Top - virtualBounds.Top) * scale);
            var w = Math.Max(1, (int)(source.Width * scale));
            var h = Math.Max(1, (int)(source.Height * scale));
            return new Rectangle(x, y, w, h);
        }
    }
}
