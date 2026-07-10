using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TruckDeck.Multimon.Helpers
{
    public static class MultimonTheme
    {
        // Shared spacing scale — use these everywhere for equal padding.
        public const int SpaceXs = 6;
        public const int SpaceSm = 10;
        public const int Space = 16;
        public const int SpaceMd = 20;
        public const int SpaceLg = 24;
        public const int SpaceXl = 32;
        public const int PagePad = 16;
        public const int CardPad = 16;
        public const int FooterHeight = 96;
        public const int HeaderHeight = 100;
        public const int ButtonHeight = 40;
        public const int SidePanelWidth = 280;

        /// <summary>Tightest client size that fits the densest tab content (PiP toolbar + camera + preview).</summary>
        public const int ContentClientWidth = 1000;
        public const int ContentClientHeight = 700;

        /// <summary>Default window = content × 1.5 breathing room.</summary>
        public const float DefaultWindowScale = 1.50f;

        public static Size DefaultClientSize =>
            new Size(
                (int)(ContentClientWidth * DefaultWindowScale),
                (int)(ContentClientHeight * DefaultWindowScale));

        public static Size MinimumClientSize =>
            new Size(ContentClientWidth, ContentClientHeight);

        /// <summary>
        /// Applies default size (content × 1.2) and minimum (content), clamped to the working area.
        /// </summary>
        public static void ApplyDefaultWindowSize(Form form)
        {
            if (form == null)
                return;

            var work = Screen.FromControl(form).WorkingArea;
            var chromeW = form.Width - form.ClientSize.Width;
            var chromeH = form.Height - form.ClientSize.Height;

            var minClient = MinimumClientSize;
            var defClient = DefaultClientSize;

            // Keep at least content size when the screen allows; otherwise shrink to fit.
            var maxClientW = Math.Max(640, work.Width - chromeW - SpaceLg);
            var maxClientH = Math.Max(480, work.Height - chromeH - SpaceLg);

            minClient = new Size(
                Math.Min(minClient.Width, maxClientW),
                Math.Min(minClient.Height, maxClientH));
            defClient = new Size(
                Math.Min(Math.Max(defClient.Width, minClient.Width), maxClientW),
                Math.Min(Math.Max(defClient.Height, minClient.Height), maxClientH));

            form.MinimumSize = new Size(minClient.Width + chromeW, minClient.Height + chromeH);
            form.ClientSize = defClient;
            form.StartPosition = FormStartPosition.CenterScreen;
        }

        public static Padding PagePadding => new Padding(PagePad);
        public static Padding CardPadding => new Padding(CardPad);
        public static Padding Equal(int all) => new Padding(all);
        public static Padding Horizontal(int x, int y = 0) => new Padding(x, y, x, y);

        // Deep charcoal + lime accent — modern truck-sim tool look
        public static readonly Color Bg = Color.FromArgb(12, 14, 18);
        public static readonly Color BgElevated = Color.FromArgb(18, 21, 28);
        public static readonly Color Panel = Color.FromArgb(24, 28, 36);
        public static readonly Color PanelHover = Color.FromArgb(32, 38, 48);
        public static readonly Color Ink = Color.FromArgb(236, 240, 245);
        public static readonly Color Label = Color.FromArgb(140, 150, 165);
        public static readonly Color Accent = Color.FromArgb(182, 255, 31);
        public static readonly Color AccentSoft = Color.FromArgb(40, 182, 255, 31);
        public static readonly Color Line = Color.FromArgb(42, 50, 62);
        public static readonly Color LineStrong = Color.FromArgb(58, 68, 84);
        public static readonly Color Warning = Color.FromArgb(255, 196, 77);
        public static readonly Color ButtonBg = Color.FromArgb(36, 42, 54);
        public static readonly Color PanelMain = Color.FromArgb(22, 32, 24);
        public static readonly Color RoleCenter = Color.FromArgb(182, 255, 31);
        public static readonly Color RoleLeft = Color.FromArgb(80, 160, 255);
        public static readonly Color RoleRight = Color.FromArgb(255, 160, 80);
        public static readonly Color RoleMirror = Color.FromArgb(180, 120, 255);
        public static readonly Color RoleAux = Color.FromArgb(120, 220, 200);
        public static readonly Color RoleUnused = Color.FromArgb(80, 85, 95);
        public static readonly Color RoleSplit = Color.FromArgb(255, 220, 100);

        public static Font UiFont { get; private set; } = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        public static Font UiFontBold { get; private set; } = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
        public static Font TitleFont { get; private set; } = new Font("Segoe UI Semibold", 18f, FontStyle.Bold);
        public static Font CaptionFont { get; private set; } = new Font("Segoe UI", 8.5f, FontStyle.Regular);

        public static void Apply(Form form)
        {
            EnsureFonts();
            form.BackColor = Bg;
            form.ForeColor = Ink;
            form.Font = UiFont;
            form.Padding = new Padding(0);
            ApplyControls(form.Controls);
        }

        public static void StylePanel(Panel panel)
        {
            panel.BackColor = Panel;
            panel.ForeColor = Ink;
        }

        public static void StyleCard(Panel panel)
        {
            panel.BackColor = Panel;
            panel.ForeColor = Ink;
            panel.Padding = CardPadding;
            panel.Margin = Equal(SpaceSm);
        }

        public static void StyleButton(Button button, bool accent = false)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = accent ? Accent : LineStrong;
            button.FlatAppearance.MouseOverBackColor = accent
                ? Color.FromArgb(160, 220, 40)
                : PanelHover;
            button.FlatAppearance.MouseDownBackColor = accent
                ? Color.FromArgb(140, 200, 30)
                : Line;
            button.BackColor = accent ? Accent : ButtonBg;
            button.ForeColor = accent ? Color.FromArgb(10, 14, 10) : Ink;
            button.Font = UiFontBold ?? UiFont;
            button.Cursor = Cursors.Hand;
            // Horizontal padding only — vertical padding throws WinForms text off-center.
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.Padding = new Padding(Space, 0, Space, 0);
            button.UseVisualStyleBackColor = false;
            button.AutoEllipsis = false;
        }

        public static void StyleSecondaryButton(Button button)
        {
            StyleButton(button, accent: false);
            button.FlatAppearance.BorderColor = Line;
        }

        public static void StyleCombo(ComboBox combo)
        {
            combo.BackColor = ButtonBg;
            combo.ForeColor = Ink;
            combo.FlatStyle = FlatStyle.Flat;
            combo.Font = UiFont;
        }

        public static void WireModernTabs(TabControl tabs)
        {
            if (tabs == null)
                return;

            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.SizeMode = TabSizeMode.Fixed;
            tabs.ItemSize = new Size(152, 44);
            tabs.Font = UiFontBold ?? UiFont;
            tabs.Padding = new Point(Space, SpaceSm);
            tabs.BackColor = Bg;
            tabs.ForeColor = Ink;
            tabs.DrawItem -= Tabs_DrawItem;
            tabs.DrawItem += Tabs_DrawItem;
        }

        static void Tabs_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tabs = (TabControl)sender;
            if (e.Index < 0 || e.Index >= tabs.TabCount)
                return;

            var bounds = e.Bounds;
            var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var text = tabs.TabPages[e.Index].Text;

            using (var bg = new SolidBrush(selected ? Panel : BgElevated))
                e.Graphics.FillRectangle(bg, bounds);

            if (selected)
            {
                using (var accent = new SolidBrush(Accent))
                    e.Graphics.FillRectangle(accent, new Rectangle(bounds.Left, bounds.Bottom - 3, bounds.Width, 3));
            }

            var textColor = selected ? Ink : Label;
            TextRenderer.DrawText(
                e.Graphics,
                text,
                UiFontBold ?? UiFont,
                bounds,
                textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        public static void DrawRoundedRect(Graphics g, Rectangle bounds, int radius, Color fill, Color? border = null, float borderWidth = 1f)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            using (var path = RoundedRect(bounds, radius))
            using (var brush = new SolidBrush(fill))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillPath(brush, path);
                if (border.HasValue)
                {
                    using (var pen = new Pen(border.Value, borderWidth))
                        g.DrawPath(pen, path);
                }
            }
        }

        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            var d = Math.Max(1, radius * 2);
            if (d > bounds.Width) d = bounds.Width;
            if (d > bounds.Height) d = bounds.Height;

            path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        static void EnsureFonts()
        {
            try
            {
                if (UiFont == null || UiFont.Name != "Segoe UI")
                    UiFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                try
                {
                    UiFontBold = new Font("Segoe UI Semibold", 9.5f, FontStyle.Regular);
                }
                catch
                {
                    UiFontBold = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                try
                {
                    TitleFont = new Font("Segoe UI Semibold", 18f, FontStyle.Regular);
                }
                catch
                {
                    TitleFont = new Font("Segoe UI", 18f, FontStyle.Bold);
                }
                CaptionFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            }
            catch
            {
                // keep defaults
            }
        }

        static void ApplyControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                switch (control)
                {
                    case LinkLabel linkLabel:
                        linkLabel.LinkColor = Accent;
                        linkLabel.ActiveLinkColor = Ink;
                        linkLabel.VisitedLinkColor = Accent;
                        linkLabel.Font = UiFont;
                        break;
                    case Label label when label.Tag as string != "accent":
                        label.ForeColor = label.Tag as string == "muted" ? Label : Ink;
                        if (label.Tag as string == "muted")
                            label.Font = CaptionFont ?? UiFont;
                        break;
                    case Label label when label.Tag as string == "accent":
                        label.ForeColor = Accent;
                        break;
                    case GroupBox groupBox:
                        groupBox.ForeColor = Ink;
                        groupBox.BackColor = Bg;
                        break;
                    case TabPage tabPage:
                        tabPage.BackColor = Bg;
                        tabPage.ForeColor = Ink;
                        tabPage.Padding = PagePadding;
                        break;
                    case TabControl tabControl:
                        tabControl.BackColor = Bg;
                        tabControl.ForeColor = Ink;
                        WireModernTabs(tabControl);
                        break;
                    case FlowLayoutPanel flow:
                        if (flow.Tag as string != "footer-buttons")
                            flow.BackColor = Bg;
                        break;
                    case Panel panel:
                        if (panel.Tag as string == "card")
                            StyleCard(panel);
                        else if (panel.Tag as string == "header")
                        {
                            panel.BackColor = BgElevated;
                            panel.ForeColor = Ink;
                        }
                        else if (panel.Tag as string == "footer")
                        {
                            panel.BackColor = Panel;
                            panel.ForeColor = Ink;
                        }
                        break;
                    case ComboBox comboBox:
                        StyleCombo(comboBox);
                        break;
                    case CheckBox checkBox:
                        checkBox.ForeColor = checkBox.Tag as string == "muted" ? Label : Ink;
                        checkBox.Font = UiFont;
                        break;
                    case RadioButton radioButton:
                        radioButton.ForeColor = Ink;
                        radioButton.Font = UiFont;
                        break;
                    case Button button:
                        StyleButton(button, button.Tag as string == "accent");
                        break;
                }

                if (control.HasChildren)
                    ApplyControls(control.Controls);
            }
        }
    }
}
