using System;
using System.Drawing;
using System.Windows.Forms;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Forms
{
    /// <summary>
    /// Floating borderless overlay — drag anywhere, resize from edges, shows a captured game camera.
    /// </summary>
    public sealed class OverlayViewportForm : Form
    {
        const int ResizeBorder = 8;
        const int TitleBarHeight = 28;

        Bitmap _frame;
        readonly object _frameLock = new object();
        string _status = "Waiting for game…";
        bool _dragging;
        bool _resizing;
        Point _dragOffset;
        ResizeEdge _resizeEdge;
        Rectangle _resizeStartBounds;
        Point _resizeStartCursor;

        public ViewportRole Role { get; private set; }
        public Rectangle SourcePixelBounds { get; set; }
        public bool AllowUserMove { get; set; } = true;

        public OverlayViewportForm(ViewportRole role, Rectangle initialBounds, Rectangle sourcePixelBounds)
        {
            Role = role;
            SourcePixelBounds = sourcePixelBounds;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = true;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Bounds = initialBounds;
            MinimumSize = new Size(240, 160);
            BackColor = Color.FromArgb(12, 14, 10);
            DoubleBuffered = true;
            Text = "TruckDeck · " + FormatRole(role);
            Cursor = Cursors.SizeAll;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        public void PresentFrame(Bitmap frameOrNull, string statusWhenEmpty = null)
        {
            lock (_frameLock)
            {
                if (_frame != null && !ReferenceEquals(_frame, frameOrNull))
                {
                    _frame.Dispose();
                    _frame = null;
                }

                _frame = frameOrNull;
                if (_frame == null && !string.IsNullOrEmpty(statusWhenEmpty))
                    _status = statusWhenEmpty;
            }

            if (IsHandleCreated && !IsDisposed)
            {
                try { BeginInvoke(new Action(Invalidate)); }
                catch { /* ignore */ }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(BackColor);

            // Title bar
            var title = new Rectangle(0, 0, Width, TitleBarHeight);
            using (var brush = new SolidBrush(Color.FromArgb(28, 35, 22)))
                g.FillRectangle(brush, title);
            TextRenderer.DrawText(g, "⠿  " + Text + "  ·  drag to move · edges to resize",
                Font, title, Color.FromArgb(200, 220, 180),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            var content = new Rectangle(0, TitleBarHeight, Width, Math.Max(1, Height - TitleBarHeight));

            Bitmap local;
            string status;
            lock (_frameLock)
            {
                local = _frame;
                status = _status;
            }

            if (local != null)
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                g.DrawImage(local, content);
            }
            else
            {
                TextRenderer.DrawText(g, status ?? "No frame", Font, content, Color.FromArgb(160, 160, 160),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            using (var pen = new Pen(Color.FromArgb(182, 255, 31), 1f))
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!AllowUserMove || e.Button != MouseButtons.Left)
                return;

            _resizeEdge = HitResizeEdge(e.Location);
            if (_resizeEdge != ResizeEdge.None)
            {
                _resizing = true;
                _resizeStartBounds = Bounds;
                _resizeStartCursor = Cursor.Position;
                Capture = true;
                return;
            }

            _dragging = true;
            _dragOffset = new Point(e.X, e.Y);
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_dragging)
            {
                var screen = PointToScreen(e.Location);
                Location = new Point(screen.X - _dragOffset.X, screen.Y - _dragOffset.Y);
                return;
            }

            if (_resizing)
            {
                var delta = new Point(
                    Cursor.Position.X - _resizeStartCursor.X,
                    Cursor.Position.Y - _resizeStartCursor.Y);
                var b = _resizeStartBounds;
                switch (_resizeEdge)
                {
                    case ResizeEdge.Right:
                        b.Width = Math.Max(MinimumSize.Width, _resizeStartBounds.Width + delta.X);
                        break;
                    case ResizeEdge.Bottom:
                        b.Height = Math.Max(MinimumSize.Height, _resizeStartBounds.Height + delta.Y);
                        break;
                    case ResizeEdge.BottomRight:
                        b.Width = Math.Max(MinimumSize.Width, _resizeStartBounds.Width + delta.X);
                        b.Height = Math.Max(MinimumSize.Height, _resizeStartBounds.Height + delta.Y);
                        break;
                    case ResizeEdge.Left:
                        var newW = Math.Max(MinimumSize.Width, _resizeStartBounds.Width - delta.X);
                        b.X = _resizeStartBounds.Right - newW;
                        b.Width = newW;
                        break;
                    case ResizeEdge.Top:
                        var newH = Math.Max(MinimumSize.Height, _resizeStartBounds.Height - delta.Y);
                        b.Y = _resizeStartBounds.Bottom - newH;
                        b.Height = newH;
                        break;
                }

                Bounds = b;
                return;
            }

            UpdateHoverCursor(HitResizeEdge(e.Location));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _dragging = false;
            _resizing = false;
            Capture = false;
            UpdateHoverCursor(HitResizeEdge(e.Location));
        }

        void UpdateHoverCursor(ResizeEdge edge)
        {
            switch (edge)
            {
                case ResizeEdge.Left:
                case ResizeEdge.Right:
                    Cursor = Cursors.SizeWE;
                    break;
                case ResizeEdge.Top:
                case ResizeEdge.Bottom:
                    Cursor = Cursors.SizeNS;
                    break;
                case ResizeEdge.BottomRight:
                    Cursor = Cursors.SizeNWSE;
                    break;
                default:
                    Cursor = Cursors.SizeAll;
                    break;
            }
        }

        ResizeEdge HitResizeEdge(Point p)
        {
            var nearLeft = p.X <= ResizeBorder;
            var nearRight = p.X >= Width - ResizeBorder;
            var nearTop = p.Y <= ResizeBorder;
            var nearBottom = p.Y >= Height - ResizeBorder;

            if (nearRight && nearBottom)
                return ResizeEdge.BottomRight;
            if (nearLeft)
                return ResizeEdge.Left;
            if (nearRight)
                return ResizeEdge.Right;
            if (nearTop)
                return ResizeEdge.Top;
            if (nearBottom)
                return ResizeEdge.Bottom;
            return ResizeEdge.None;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_frameLock)
                {
                    _frame?.Dispose();
                    _frame = null;
                }
            }

            base.Dispose(disposing);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // toolwindow
                return cp;
            }
        }

        static string FormatRole(ViewportRole role)
        {
            switch (role)
            {
                case ViewportRole.Left: return "Left window";
                case ViewportRole.Right: return "Right window";
                case ViewportRole.MirrorLeft: return "Left mirror";
                case ViewportRole.MirrorRight: return "Right mirror";
                case ViewportRole.Aux: return "Aux";
                default: return role.ToString();
            }
        }

        enum ResizeEdge
        {
            None,
            Left,
            Right,
            Top,
            Bottom,
            BottomRight
        }
    }
}
