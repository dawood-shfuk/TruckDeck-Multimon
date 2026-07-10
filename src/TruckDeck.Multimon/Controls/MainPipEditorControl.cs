using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TruckDeck.Multimon.Helpers;
using TruckDeck.Multimon.Models;
using TruckDeck.Multimon.Services;

namespace TruckDeck.Multimon.Controls
{
    /// <summary>
    /// Interactive editor: MAIN screen preview with free-place/resize PiP camera panels.
    /// </summary>
    public sealed class MainPipEditorControl : UserControl
    {
        const int HandleSize = 10;
        LayoutProfile _profile;
        int _activeIndex = -1;
        bool _dragging;
        bool _resizing;
        Point _grabOffset;
        Rectangle _startPixel;
        Point _startCursor;

        public event EventHandler LayoutChanged;
        public event EventHandler SelectionChanged;

        public int SelectedIndex => _activeIndex;
        public MainPipPanel SelectedPanel =>
            _profile?.MainPipPanels != null && _activeIndex >= 0 && _activeIndex < _profile.MainPipPanels.Count
                ? _profile.MainPipPanels[_activeIndex]
                : null;

        public MainPipEditorControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = MultimonTheme.Bg;
            Dock = DockStyle.Fill;
            DoubleBuffered = true;
        }

        public void SetProfile(LayoutProfile profile)
        {
            _profile = profile;
            // Only seed default L/R panels when PiP mode is active (or already has panels to edit).
            if (_profile != null &&
                (_profile.UseMainPipMode || (_profile.MainPipPanels != null && _profile.MainPipPanels.Count > 0)))
                MainPipLayoutService.EnsureDefaultPips(_profile);
            Invalidate();
        }

        Rectangle GetMainMapArea()
        {
            if (_profile == null)
                return ClientRectangle;

            var main = MenuLayoutHelper.GetPrimaryBounds(_profile);
            if (main.Width <= 0 || main.Height <= 0)
                return Rectangle.Inflate(ClientRectangle, -16, -16);

            var margin = MultimonTheme.Space;
            var area = new Rectangle(margin, margin, Width - margin * 2, Height - margin * 2 - 28);
            var scale = Math.Min((float)area.Width / main.Width, (float)area.Height / main.Height);
            var w = (int)(main.Width * scale);
            var h = (int)(main.Height * scale);
            return new Rectangle(
                area.Left + (area.Width - w) / 2,
                area.Top + (area.Height - h) / 2,
                w, h);
        }

        Rectangle MapToEditor(Rectangle mainPixel)
        {
            var main = MenuLayoutHelper.GetPrimaryBounds(_profile);
            var map = GetMainMapArea();
            if (main.Width <= 0 || main.Height <= 0)
                return Rectangle.Empty;

            var x = map.Left + (int)((mainPixel.Left - main.Left) * (float)map.Width / main.Width);
            var y = map.Top + (int)((mainPixel.Top - main.Top) * (float)map.Height / main.Height);
            var w = Math.Max(8, (int)(mainPixel.Width * (float)map.Width / main.Width));
            var h = Math.Max(8, (int)(mainPixel.Height * (float)map.Height / main.Height));
            return new Rectangle(x, y, w, h);
        }

        Rectangle MapToMainPixels(Rectangle editorRect)
        {
            var main = MenuLayoutHelper.GetPrimaryBounds(_profile);
            var map = GetMainMapArea();
            if (map.Width <= 0 || map.Height <= 0)
                return Rectangle.Empty;

            var x = main.Left + (int)((editorRect.Left - map.Left) * (float)main.Width / map.Width);
            var y = main.Top + (int)((editorRect.Top - map.Top) * (float)main.Height / map.Height);
            var w = Math.Max(64, (int)(editorRect.Width * (float)main.Width / map.Width));
            var h = Math.Max(48, (int)(editorRect.Height * (float)main.Height / map.Height));
            return new Rectangle(x, y, w, h);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(BackColor);

            if (_profile == null)
            {
                TextRenderer.DrawText(g, "No profile loaded", Font, ClientRectangle, MultimonTheme.Label,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            var main = MenuLayoutHelper.GetPrimaryBounds(_profile);
            var map = GetMainMapArea();

            // MAIN cabin base
            MultimonTheme.DrawRoundedRect(g, map, 10, MultimonTheme.PanelMain, MultimonTheme.Accent, 2f);

            TextRenderer.DrawText(g,
                $"MAIN  {main.Width}×{main.Height}  ·  drag · resize corner · right-click remove",
                MultimonTheme.CaptionFont ?? Font,
                new Rectangle(map.Left, map.Bottom + 6, map.Width, 20),
                MultimonTheme.Label,
                TextFormatFlags.HorizontalCenter);

            using (var centerFont = new Font(Font.FontFamily, Math.Max(11f, Font.Size + 2f), FontStyle.Bold))
                TextRenderer.DrawText(g, "CENTER", centerFont, map, Color.FromArgb(70, MultimonTheme.Accent),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            var panels = _profile.MainPipPanels;
            if (panels == null)
                return;

            for (var i = 0; i < panels.Count; i++)
            {
                var pip = panels[i];
                var pixel = pip.ToPixelBounds(main);
                var rect = MapToEditor(pixel);
                var color = ColorFor(pip.Role);
                var selected = i == _activeIndex;

                MultimonTheme.DrawRoundedRect(
                    g,
                    rect,
                    6,
                    Color.FromArgb(selected ? 170 : 120, color),
                    color,
                    selected ? 2.5f : 1.5f);

                var label = ViewportRoleGroups.FormatRoleLabel(pip.Role);
                if (selected)
                {
                    pip.ResolveCamera(_profile.DriveSide, out _, out _, out var fov);
                    if (fov > 0f)
                        label += $"  FOV {fov:0}°";
                }
                TextRenderer.DrawText(g, label, Font, rect, MultimonTheme.Ink,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                // Resize handle
                var handle = new Rectangle(rect.Right - HandleSize - 2, rect.Bottom - HandleSize - 2, HandleSize + 2, HandleSize + 2);
                MultimonTheme.DrawRoundedRect(g, handle, 2, color, null);
            }
        }

        static Color ColorFor(ViewportRole role)
        {
            switch (role)
            {
                case ViewportRole.Left: return MultimonTheme.RoleLeft;
                case ViewportRole.Right: return MultimonTheme.RoleRight;
                case ViewportRole.MirrorLeft:
                case ViewportRole.MirrorRight: return MultimonTheme.RoleMirror;
                case ViewportRole.Aux: return MultimonTheme.RoleAux;
                default: return MultimonTheme.RoleSplit;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_profile == null)
                return;

            var panels = _profile.MainPipPanels;
            if (panels == null)
                return;

            var main = MenuLayoutHelper.GetPrimaryBounds(_profile);
            var previous = _activeIndex;
            _activeIndex = HitTest(e.Location, out var onHandle);
            if (previous != _activeIndex)
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();

            if (_activeIndex < 0)
                return;

            if (e.Button == MouseButtons.Right)
            {
                panels.RemoveAt(_activeIndex);
                _activeIndex = -1;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                RaiseChanged();
                return;
            }

            if (e.Button != MouseButtons.Left)
                return;

            var pip = panels[_activeIndex];
            _startPixel = pip.ToPixelBounds(main);
            _startCursor = Cursor.Position;

            if (onHandle)
            {
                _resizing = true;
            }
            else
            {
                _dragging = true;
                var editor = MapToEditor(_startPixel);
                _grabOffset = new Point(e.X - editor.X, e.Y - editor.Y);
            }

            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_profile == null || _activeIndex < 0)
            {
                Cursor = Cursors.Default;
                return;
            }

            var panels = _profile.MainPipPanels;
            if (panels == null || _activeIndex >= panels.Count)
                return;

            var main = MenuLayoutHelper.GetPrimaryBounds(_profile);
            var pip = panels[_activeIndex];

            if (_resizing)
            {
                var delta = new Point(Cursor.Position.X - _startCursor.X, Cursor.Position.Y - _startCursor.Y);
                // Approximate delta in MAIN pixels via map scale
                var map = GetMainMapArea();
                var dx = (int)(delta.X * (float)main.Width / Math.Max(1, map.Width));
                var dy = (int)(delta.Y * (float)main.Height / Math.Max(1, map.Height));
                var r = _startPixel;
                r.Width = Math.Max(64, _startPixel.Width + dx);
                r.Height = Math.Max(48, _startPixel.Height + dy);
                if (r.Right > main.Right)
                    r.Width = main.Right - r.Left;
                if (r.Bottom > main.Bottom)
                    r.Height = main.Bottom - r.Top;
                pip.SetFromPixelBounds(r, main);
                Invalidate();
                return;
            }

            if (_dragging)
            {
                var map = GetMainMapArea();
                var ed = new Rectangle(e.X - _grabOffset.X, e.Y - _grabOffset.Y,
                    MapToEditor(_startPixel).Width, MapToEditor(_startPixel).Height);
                // Clamp editor rect inside map
                if (ed.Left < map.Left) ed.X = map.Left;
                if (ed.Top < map.Top) ed.Y = map.Top;
                if (ed.Right > map.Right) ed.X = map.Right - ed.Width;
                if (ed.Bottom > map.Bottom) ed.Y = map.Bottom - ed.Height;
                var pixels = MapToMainPixels(ed);
                pip.SetFromPixelBounds(pixels, main);
                Invalidate();
                return;
            }

            HitTest(e.Location, out var handle);
            Cursor = handle ? Cursors.SizeNWSE : Cursors.SizeAll;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_dragging || _resizing)
                RaiseChanged();
            _dragging = false;
            _resizing = false;
            Capture = false;
        }

        int HitTest(Point p, out bool onHandle)
        {
            onHandle = false;
            if (_profile?.MainPipPanels == null)
                return -1;

            var panels = _profile.MainPipPanels;
            if (panels == null)
                return -1;

            var main = MenuLayoutHelper.GetPrimaryBounds(_profile);
            for (var i = panels.Count - 1; i >= 0; i--)
            {
                var rect = MapToEditor(panels[i].ToPixelBounds(main));
                var handle = new Rectangle(rect.Right - HandleSize - 2, rect.Bottom - HandleSize - 2,
                    HandleSize + 4, HandleSize + 4);
                if (handle.Contains(p))
                {
                    onHandle = true;
                    return i;
                }
                if (rect.Contains(p))
                    return i;
            }

            return -1;
        }

        public void SelectPanel(int index)
        {
            if (_profile?.MainPipPanels == null)
                return;
            if (index < -1 || index >= _profile.MainPipPanels.Count)
                return;

            var previous = _activeIndex;
            _activeIndex = index;
            if (previous != _activeIndex)
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        void RaiseChanged()
        {
            LayoutChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }
    }
}
