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
    public sealed class InteractiveLayoutCanvas : UserControl
    {
        const int PaletteChipHeight = 28;
        const int HintBarHeight = 20;
        const string DragFormat = "TruckDeck.Multimon.ViewportDrag";

        LayoutProfile _profile;
        readonly List<PaletteChip> _palette = new List<PaletteChip>();
        readonly List<ScreenHit> _screenHits = new List<ScreenHit>();

        int _paletteHeight = 44;
        int _dragSourceScreen = -1;
        DropZone _dragSourceZone = DropZone.Full;
        bool _isDraggingFromCanvas;
        Point _mouseDownPoint;
        DropZone? _hoverZone;
        int _hoverScreenIndex = -1;

        public event EventHandler LayoutChanged;

        public InteractiveLayoutCanvas()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = MultimonTheme.Bg;
            Dock = DockStyle.Fill;
            MinimumSize = new Size(320, 180);
            AllowDrop = true;
            BuildPalette();
        }

        void BuildPalette()
        {
            _palette.Clear();
            AddPaletteChip("Center", ViewportRole.Center, MultimonTheme.RoleCenter);
            AddPaletteChip("Left window", ViewportRole.Left, MultimonTheme.RoleLeft);
            AddPaletteChip("Right window", ViewportRole.Right, MultimonTheme.RoleRight);
            AddPaletteChip("Split 2", ViewportRole.Unused, MultimonTheme.RoleSplit, splitPair: true);
            AddPaletteChip("Split 4", ViewportRole.Unused, MultimonTheme.RoleSplit, splitQuad: true);
            AddPaletteChip("L mirror", ViewportRole.MirrorLeft, MultimonTheme.RoleMirror);
            AddPaletteChip("R mirror", ViewportRole.MirrorRight, MultimonTheme.RoleMirror);
            AddPaletteChip("Aux", ViewportRole.Aux, MultimonTheme.RoleAux);
            AddPaletteChip("Unused", ViewportRole.Unused, MultimonTheme.RoleUnused);
        }

        void AddPaletteChip(string label, ViewportRole role, Color color, bool splitPair = false, bool splitQuad = false)
        {
            _palette.Add(new PaletteChip
            {
                Label = label,
                Role = role,
                Color = color,
                IsSplitPair = splitPair,
                IsSplitQuad = splitQuad
            });
        }

        public void SetProfile(LayoutProfile profile)
        {
            _profile = profile;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutPaletteChips();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            LayoutPaletteChips();
            DrawPalette(g);
            DrawMonitors(g);
            DrawHint(g);
        }

        /// <summary>Wraps palette chips to available width and updates hit bounds.</summary>
        void LayoutPaletteChips()
        {
            var x = 8;
            var y = 6;
            var rowHeight = PaletteChipHeight + 6;
            var maxRight = Math.Max(120, Width - 8);

            foreach (var chip in _palette)
            {
                var size = TextRenderer.MeasureText(chip.Label, Font);
                var chipWidth = size.Width + 20;
                if (x > 8 && x + chipWidth > maxRight)
                {
                    x = 8;
                    y += rowHeight;
                }

                chip.Bounds = new Rectangle(x, y, chipWidth, PaletteChipHeight);
                x = chip.Bounds.Right + 6;
            }

            _paletteHeight = y + PaletteChipHeight + 10;
        }

        Rectangle GetMonitorDrawArea()
        {
            // Equal padding on all sides of the preview (same inset L/R/T/B around the map).
            var margin = MultimonTheme.Space;
            var top = _paletteHeight + margin;
            var bottom = HintBarHeight + margin;
            var height = Math.Max(40, Height - top - bottom);
            var width = Math.Max(40, Width - margin * 2);
            return new Rectangle(margin, top, width, height);
        }

        void DrawPalette(Graphics g)
        {
            using (var barBrush = new SolidBrush(MultimonTheme.BgElevated))
                g.FillRectangle(barBrush, new Rectangle(0, 0, Width, _paletteHeight));

            using (var edge = new Pen(MultimonTheme.Line))
                g.DrawLine(edge, 0, _paletteHeight - 1, Width, _paletteHeight - 1);

            foreach (var chip in _palette)
            {
                var rect = chip.Bounds;
                MultimonTheme.DrawRoundedRect(
                    g,
                    rect,
                    6,
                    Color.FromArgb(190, chip.Color),
                    MultimonTheme.LineStrong,
                    1f);

                TextRenderer.DrawText(g, chip.Label, Font, rect, MultimonTheme.Ink,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        void DrawMonitors(Graphics g)
        {
            _screenHits.Clear();
            var area = GetMonitorDrawArea();

            if (_profile?.PhysicalScreens == null || _profile.PhysicalScreens.Count == 0)
            {
                TextRenderer.DrawText(g, "No displays detected — click Refresh", Font, area, MultimonTheme.Label,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            var virtualBounds = _profile.GameDesktopBounds.Width > 0 && _profile.GameDesktopBounds.Height > 0
                ? _profile.GameDesktopBounds
                : _profile.VirtualDesktopBounds;
            if (virtualBounds.Width <= 0 || virtualBounds.Height <= 0)
                return;

            // Fill outline so the available responsive area is obvious.
            using (var guidePen = new Pen(Color.FromArgb(40, MultimonTheme.Line), 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                g.DrawRectangle(guidePen, area);

            foreach (var screen in _profile.PhysicalScreens)
            {
                var tile = MapRect(screen.Bounds, virtualBounds, area);
                var layout = FindLayout(screen.Index);
                var isMain = screen.Index == _profile.MainScreenIndex;
                var isHover = screen.Index == _hoverScreenIndex;
                var headerH = Math.Max(16, Math.Min(28, tile.Height / 8));

                MultimonTheme.DrawRoundedRect(
                    g,
                    tile,
                    8,
                    isMain ? MultimonTheme.PanelMain : MultimonTheme.Panel,
                    isMain ? MultimonTheme.Accent : MultimonTheme.LineStrong,
                    isMain ? 2.5f : 1.5f);

                if (isHover && _hoverZone.HasValue)
                    DrawDropHighlight(g, tile, _hoverZone.Value, headerH);

                var header = $"#{screen.Index + 1}  {screen.ResolutionLabel}";
                if (isMain)
                    header += "  ★ MAIN";
                var headerRect = new Rectangle(tile.Left, tile.Top, tile.Width, headerH);
                using (var headerFont = new Font(Font.FontFamily, Math.Max(7.5f, Math.Min(10f, headerH * 0.45f)), FontStyle.Bold))
                {
                    TextRenderer.DrawText(g, header, headerFont, headerRect, MultimonTheme.Label,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }

                var content = new Rectangle(tile.Left + 2, tile.Top + headerH, tile.Width - 4, Math.Max(4, tile.Height - headerH - 2));
                var leftHalf = new Rectangle(content.Left, content.Top, content.Width / 2, content.Height);
                var rightHalf = new Rectangle(content.Left + content.Width / 2, content.Top,
                    content.Width - content.Width / 2, content.Height);

                _screenHits.Add(new ScreenHit { ScreenIndex = screen.Index, Zone = DropZone.Full, Bounds = tile });
                _screenHits.Add(new ScreenHit { ScreenIndex = screen.Index, Zone = DropZone.LeftHalf, Bounds = leftHalf });
                _screenHits.Add(new ScreenHit { ScreenIndex = screen.Index, Zone = DropZone.RightHalf, Bounds = rightHalf });

                if (layout != null && layout.SplitMode == AdditionalScreenSplitMode.Four)
                {
                    var midX = content.Left + content.Width / 2;
                    var midY = content.Top + content.Height / 2;
                    using (var pen = new Pen(MultimonTheme.Line, 1f))
                    {
                        g.DrawLine(pen, midX, content.Top, midX, content.Bottom);
                        g.DrawLine(pen, content.Left, midY, content.Right, midY);
                    }

                    var tl = new Rectangle(content.Left, content.Top, midX - content.Left, midY - content.Top);
                    var tr = new Rectangle(midX, content.Top, content.Right - midX, midY - content.Top);
                    var bl = new Rectangle(content.Left, midY, midX - content.Left, content.Bottom - midY);
                    var br = new Rectangle(midX, midY, content.Right - midX, content.Bottom - midY);
                    DrawRoleBadge(g, tl, layout.SplitTopLeftRole, "TL");
                    DrawRoleBadge(g, tr, layout.SplitTopRightRole, "TR");
                    DrawRoleBadge(g, bl, layout.SplitBottomLeftRole, "BL");
                    DrawRoleBadge(g, br, layout.SplitBottomRightRole, "BR");
                }
                else if (layout != null && layout.SplitMode == AdditionalScreenSplitMode.Two)
                {
                    var midX = content.Left + content.Width / 2;
                    using (var pen = new Pen(MultimonTheme.Line, 1f))
                        g.DrawLine(pen, midX, content.Top, midX, content.Bottom);

                    DrawRoleBadge(g, leftHalf, layout.SplitLeftRole, "L");
                    DrawRoleBadge(g, rightHalf, layout.SplitRightRole, "R");
                }
                else
                {
                    DrawRoleBadge(g, content, layout?.Role ?? ViewportRole.Unused, null);
                }
            }
        }

        void DrawDropHighlight(Graphics g, Rectangle tile, DropZone zone, int headerH)
        {
            Rectangle highlight;
            var content = new Rectangle(tile.Left + 2, tile.Top + headerH, tile.Width - 4, Math.Max(4, tile.Height - headerH - 2));
            switch (zone)
            {
                case DropZone.LeftHalf:
                    highlight = new Rectangle(content.Left, content.Top, content.Width / 2, content.Height);
                    break;
                case DropZone.RightHalf:
                    highlight = new Rectangle(content.Left + content.Width / 2, content.Top,
                        content.Width - content.Width / 2, content.Height);
                    break;
                default:
                    highlight = content;
                    break;
            }

            using (var brush = new SolidBrush(Color.FromArgb(60, MultimonTheme.Accent)))
                g.FillRectangle(brush, highlight);
            using (var pen = new Pen(MultimonTheme.Accent, 2f))
                g.DrawRectangle(pen, highlight);
        }

        void DrawRoleBadge(Graphics g, Rectangle bounds, ViewportRole role, string prefix)
        {
            if (bounds.Width < 4 || bounds.Height < 4)
                return;

            var color = GetRoleColor(role);
            var label = prefix != null
                ? prefix + ": " + ViewportRoleGroups.FormatRoleLabel(role)
                : ViewportRoleGroups.FormatRoleLabel(role);

            var pad = Math.Max(1, Math.Min(4, Math.Min(bounds.Width, bounds.Height) / 16));
            var inner = Rectangle.Inflate(bounds, -pad, -pad);
            using (var brush = new SolidBrush(Color.FromArgb(role == ViewportRole.Unused ? 40 : 100, color)))
                g.FillRectangle(brush, inner);

            if (role != ViewportRole.Unused)
            {
                using (var pen = new Pen(color, 1.5f))
                    g.DrawRectangle(pen, inner);
            }

            var fontSize = Math.Max(7f, Math.Min(11f, Math.Min(inner.Width, inner.Height) * 0.12f));
            using (var font = new Font(Font.FontFamily, fontSize))
            {
                TextRenderer.DrawText(g, label, font, inner, MultimonTheme.Ink,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
            }
        }

        void DrawHint(Graphics g)
        {
            var text = "Resize the window to enlarge this view  ·  Split 2 / Split 4 on additional screen  ·  MAIN = full cabin";
            var rect = new Rectangle(8, Height - HintBarHeight + 2, Width - 16, HintBarHeight - 4);
            using (var font = new Font(Font.FontFamily, 8f))
            {
                TextRenderer.DrawText(g, text, font, rect, MultimonTheme.Label,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _mouseDownPoint = e.Location;

            if (e.Y < _paletteHeight)
            {
                var chip = _palette.FirstOrDefault(c => c.Bounds.Contains(e.Location));
                if (chip != null)
                    StartPaletteDrag(chip);
                return;
            }

            var hit = HitTest(e.Location);
            if (hit == null)
                return;

            var layout = FindLayout(hit.ScreenIndex);
            if (layout == null)
                return;

            ViewportRole role;
            DropZone zone;
            if (layout.Split && hit.Zone != DropZone.Full)
            {
                zone = hit.Zone;
                role = zone == DropZone.LeftHalf ? layout.SplitLeftRole : layout.SplitRightRole;
            }
            else
            {
                zone = DropZone.Full;
                role = layout.Role;
            }

            if (role == ViewportRole.Unused && !layout.Split)
                return;

            _dragSourceScreen = hit.ScreenIndex;
            _dragSourceZone = zone;
            _isDraggingFromCanvas = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_isDraggingFromCanvas || e.Button != MouseButtons.Left)
                return;

            if (Math.Abs(e.X - _mouseDownPoint.X) < 4 && Math.Abs(e.Y - _mouseDownPoint.Y) < 4)
                return;

            var layout = FindLayout(_dragSourceScreen);
            if (layout == null)
                return;

            ViewportRole role;
            if (layout.Split && _dragSourceZone != DropZone.Full)
                role = _dragSourceZone == DropZone.LeftHalf ? layout.SplitLeftRole : layout.SplitRightRole;
            else
                role = layout.Role;

            _isDraggingFromCanvas = false;
            var payload = ViewportDragPayload.ForMove(_dragSourceScreen, _dragSourceZone, role);
            DoDragDrop(new DataObject(DragFormat, payload), DragDropEffects.Move);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDraggingFromCanvas = false;
            _dragSourceScreen = -1;
            _hoverScreenIndex = -1;
            _hoverZone = null;
            Invalidate();
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);
            var pt = PointToClient(MousePosition);
            var hit = HitTest(pt);
            if (hit == null)
                return;

            ClearScreen(hit.ScreenIndex);
            RaiseLayoutChanged();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Right || e.Y < _paletteHeight)
                return;

            var hit = HitTest(e.Location);
            if (hit == null)
                return;

            var menu = new ContextMenuStrip();
            menu.Items.Add("Set as MAIN display", null, (_, __) =>
            {
                _profile.MainScreenIndex = hit.ScreenIndex;
                RaiseLayoutChanged();
            });
            menu.Items.Add("Clear monitor", null, (_, __) =>
            {
                ClearScreen(hit.ScreenIndex);
                RaiseLayoutChanged();
            });
            menu.Items.Add("Split 2 — L/R windows", null, (_, __) =>
            {
                ApplyPayload(hit.ScreenIndex, DropZone.Full, ViewportDragPayload.ForSplitPair());
                RaiseLayoutChanged();
            });
            menu.Items.Add("Split 4 — 2×2 cameras", null, (_, __) =>
            {
                ApplyPayload(hit.ScreenIndex, DropZone.Full, ViewportDragPayload.ForSplitQuad());
                RaiseLayoutChanged();
            });
            menu.Show(this, e.Location);
        }

        void StartPaletteDrag(PaletteChip chip)
        {
            ViewportDragPayload payload;
            if (chip.IsSplitQuad)
                payload = ViewportDragPayload.ForSplitQuad();
            else if (chip.IsSplitPair)
                payload = ViewportDragPayload.ForSplitPair();
            else
                payload = ViewportDragPayload.ForRole(chip.Role);
            DoDragDrop(new DataObject(DragFormat, payload), DragDropEffects.Copy);
        }

        protected override void OnDragEnter(DragEventArgs drgevent)
        {
            if (drgevent.Data.GetDataPresent(DragFormat))
                drgevent.Effect = DragDropEffects.Copy | DragDropEffects.Move;
        }

        protected override void OnDragOver(DragEventArgs drgevent)
        {
            if (!drgevent.Data.GetDataPresent(DragFormat))
                return;

            var pt = PointToClient(new Point(drgevent.X, drgevent.Y));
            var hit = HitTest(pt);
            if (hit != null)
            {
                _hoverScreenIndex = hit.ScreenIndex;
                _hoverZone = ResolveDropZone(hit, drgevent.Data.GetData(DragFormat) as ViewportDragPayload);
                drgevent.Effect = DragDropEffects.Copy | DragDropEffects.Move;
                Invalidate();
            }
            else
            {
                drgevent.Effect = DragDropEffects.None;
            }
        }

        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            var payload = drgevent.Data.GetData(DragFormat) as ViewportDragPayload;
            if (payload == null)
                return;

            var pt = PointToClient(new Point(drgevent.X, drgevent.Y));
            var hit = HitTest(pt);
            _hoverScreenIndex = -1;
            _hoverZone = null;
            if (hit == null)
            {
                Invalidate();
                return;
            }

            var zone = ResolveDropZone(hit, payload);
            ApplyPayload(hit.ScreenIndex, zone, payload);
            RaiseLayoutChanged();
            Invalidate();
        }

        protected override void OnDragLeave(EventArgs e)
        {
            base.OnDragLeave(e);
            _hoverScreenIndex = -1;
            _hoverZone = null;
            Invalidate();
        }

        DropZone ResolveDropZone(ScreenHit hit, ViewportDragPayload payload)
        {
            if (payload == null)
                return hit.Zone;

            if (payload.Kind == ViewportDragKind.SplitPair || payload.Kind == ViewportDragKind.SplitQuad)
                return DropZone.Full;

            if (payload.Role == ViewportRole.Center || payload.Role == ViewportRole.Aux ||
                payload.Role == ViewportRole.Unused)
                return DropZone.Full;

            if (hit.Zone == DropZone.LeftHalf || hit.Zone == DropZone.RightHalf)
                return hit.Zone;

            return DropZone.Full;
        }

        void ApplyPayload(int screenIndex, DropZone zone, ViewportDragPayload payload)
        {
            if (_profile?.ScreenLayouts == null)
                return;

            if (payload.SourceScreenIndex >= 0 && payload.Kind == ViewportDragKind.MoveAssignment)
                ClearZone(payload.SourceScreenIndex, payload.SourceZone);

            var entry = DisplayLayoutHelper.GetOrCreateEntry(_profile, screenIndex);

            if (payload.Kind == ViewportDragKind.SplitPair)
            {
                entry.EnableTwoPaneSideWindows();
                return;
            }

            if (payload.Kind == ViewportDragKind.SplitQuad)
            {
                entry.EnableFourPaneCameras();
                return;
            }

            if (zone == DropZone.Full || payload.Role == ViewportRole.Center ||
                payload.Role == ViewportRole.Aux || payload.Role == ViewportRole.Unused)
            {
                entry.Role = payload.Role;
                entry.ClearSplit();
                entry.SpanNext = false;
                if (payload.Role == ViewportRole.Center)
                    _profile.MainScreenIndex = screenIndex;
                return;
            }

            if (screenIndex == _profile.MainScreenIndex)
            {
                entry.Role = payload.Role;
                entry.ClearSplit();
                return;
            }

            if (entry.SplitMode != AdditionalScreenSplitMode.Two)
            {
                entry.EnableTwoPaneSideWindows();
                entry.SplitLeftRole = ViewportRole.Unused;
                entry.SplitRightRole = ViewportRole.Unused;
            }

            entry.Role = ViewportRole.Unused;
            entry.SpanNext = false;
            if (zone == DropZone.LeftHalf)
                entry.SplitLeftRole = payload.Role;
            else
                entry.SplitRightRole = payload.Role;

            if (!entry.HasAnySplitPaneActive())
                entry.ClearSplit();
        }

        void ClearScreen(int screenIndex)
        {
            var entry = FindLayout(screenIndex);
            if (entry == null)
                return;

            entry.Role = ViewportRole.Unused;
            entry.ClearSplit();
            entry.SplitLeftRole = ViewportRole.Left;
            entry.SplitRightRole = ViewportRole.Right;
            entry.SplitTopLeftRole = ViewportRole.Left;
            entry.SplitTopRightRole = ViewportRole.Right;
            entry.SplitBottomLeftRole = ViewportRole.MirrorLeft;
            entry.SplitBottomRightRole = ViewportRole.MirrorRight;
            entry.SpanNext = false;
        }

        void ClearZone(int screenIndex, DropZone zone)
        {
            var entry = FindLayout(screenIndex);
            if (entry == null)
                return;

            if (zone == DropZone.Full || entry.SplitMode == AdditionalScreenSplitMode.Off)
            {
                ClearScreen(screenIndex);
                return;
            }

            if (entry.SplitMode == AdditionalScreenSplitMode.Two)
            {
                if (zone == DropZone.LeftHalf)
                    entry.SplitLeftRole = ViewportRole.Unused;
                else
                    entry.SplitRightRole = ViewportRole.Unused;
            }

            if (!entry.HasAnySplitPaneActive())
            {
                entry.ClearSplit();
                entry.Role = ViewportRole.Unused;
            }
        }

        ScreenHit HitTest(Point pt)
        {
            if (pt.Y < _paletteHeight)
                return null;

            ScreenHit best = null;
            foreach (var hit in _screenHits)
            {
                if (!hit.Bounds.Contains(pt))
                    continue;

                if (best == null)
                {
                    best = hit;
                    continue;
                }

                if (hit.Zone != DropZone.Full && best.Zone == DropZone.Full)
                    best = hit;
                else if (hit.Bounds.Width * hit.Bounds.Height < best.Bounds.Width * best.Bounds.Height)
                    best = hit;
            }

            return best;
        }

        ScreenLayoutEntry FindLayout(int screenIndex)
        {
            return _profile?.ScreenLayouts?.FirstOrDefault(e => e.ScreenIndex == screenIndex);
        }

        void RaiseLayoutChanged()
        {
            DisplayLayoutHelper.FinalizeProfile(_profile);
            LayoutChanged?.Invoke(this, EventArgs.Empty);
        }

        static Color GetRoleColor(ViewportRole role)
        {
            switch (role)
            {
                case ViewportRole.Center: return MultimonTheme.RoleCenter;
                case ViewportRole.Left: return MultimonTheme.RoleLeft;
                case ViewportRole.Right: return MultimonTheme.RoleRight;
                case ViewportRole.MirrorLeft:
                case ViewportRole.MirrorRight: return MultimonTheme.RoleMirror;
                case ViewportRole.Aux: return MultimonTheme.RoleAux;
                default: return MultimonTheme.RoleUnused;
            }
        }

        /// <summary>
        /// Maps a physical screen rect into targetArea, filling as much space as possible
        /// while preserving aspect ratio (responsive fit).
        /// </summary>
        static Rectangle MapRect(Rectangle source, Rectangle virtualBounds, Rectangle targetArea)
        {
            if (virtualBounds.Width <= 0 || virtualBounds.Height <= 0 || targetArea.Width <= 0 || targetArea.Height <= 0)
                return Rectangle.Empty;

            var scale = Math.Min(
                (float)targetArea.Width / virtualBounds.Width,
                (float)targetArea.Height / virtualBounds.Height);

            // Prefer filling almost the full responsive area (leave 2px slack for borders).
            var drawWidth = Math.Max(1, (int)(virtualBounds.Width * scale) - 2);
            var drawHeight = Math.Max(1, (int)(virtualBounds.Height * scale) - 2);
            var offsetX = targetArea.Left + (targetArea.Width - drawWidth) / 2;
            var offsetY = targetArea.Top + (targetArea.Height - drawHeight) / 2;
            var usedScale = Math.Min(
                (float)drawWidth / virtualBounds.Width,
                (float)drawHeight / virtualBounds.Height);

            var x = offsetX + (int)((source.Left - virtualBounds.Left) * usedScale);
            var y = offsetY + (int)((source.Top - virtualBounds.Top) * usedScale);
            var w = Math.Max(8, (int)(source.Width * usedScale));
            var h = Math.Max(8, (int)(source.Height * usedScale));

            // Clamp into the drawn desktop rect so edges stay clean when rounding.
            var desktop = new Rectangle(offsetX, offsetY, drawWidth, drawHeight);
            var mapped = new Rectangle(x, y, w, h);
            mapped.Intersect(desktop);
            if (mapped.Width < 4)
                mapped.Width = Math.Min(8, desktop.Width);
            if (mapped.Height < 4)
                mapped.Height = Math.Min(8, desktop.Height);
            return mapped;
        }

        sealed class PaletteChip
        {
            public string Label;
            public ViewportRole Role;
            public Color Color;
            public bool IsSplitPair;
            public bool IsSplitQuad;
            public Rectangle Bounds;
        }

        sealed class ScreenHit
        {
            public int ScreenIndex;
            public DropZone Zone;
            public Rectangle Bounds;
        }
    }
}
