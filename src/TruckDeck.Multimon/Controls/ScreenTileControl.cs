using System;
using System.Drawing;
using System.Windows.Forms;
using TruckDeck.Multimon.Helpers;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Controls
{
    public sealed class ScreenTileControl : Panel
    {
        readonly Label _titleLabel;
        readonly ComboBox _roleCombo;
        readonly ComboBox _splitModeCombo;
        readonly CheckBox _spanNextCheckBox;
        readonly Panel _twoPanePanel;
        readonly Panel _fourPanePanel;
        readonly ComboBox _splitLeftCombo;
        readonly ComboBox _splitRightCombo;
        readonly ComboBox _tlCombo;
        readonly ComboBox _trCombo;
        readonly ComboBox _blCombo;
        readonly ComboBox _brCombo;
        readonly bool _isMainScreen;

        public event EventHandler LayoutChanged;

        public ScreenTileControl(PhysicalScreenInfo screen, string positionLabel = null, bool isMainScreen = false)
        {
            _isMainScreen = isMainScreen;
            Tag = "card";
            Width = 248;
            Height = 140;
            MultimonTheme.StyleCard(this);

            _titleLabel = new Label
            {
                AutoSize = false,
                Height = 44,
                Dock = DockStyle.Top,
                Text = $"{positionLabel ?? $"Screen {screen.Index + 1}"}\r\n{screen.ResolutionLabel}",
                Tag = "muted",
                Font = MultimonTheme.CaptionFont
            };

            _roleCombo = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            PopulateRoleCombo(_roleCombo, ViewportRoleGroups.FullScreenRoles);
            _roleCombo.SelectedIndexChanged += (_, __) =>
            {
                UpdateSplitVisibility();
                LayoutChanged?.Invoke(this, EventArgs.Empty);
            };

            _splitModeCombo = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _splitModeCombo.Items.Add(new SplitModeItem(AdditionalScreenSplitMode.Off, "Full screen (no split)"));
            _splitModeCombo.Items.Add(new SplitModeItem(AdditionalScreenSplitMode.Two, "Split 2 — L/R panes"));
            _splitModeCombo.Items.Add(new SplitModeItem(AdditionalScreenSplitMode.Four, "Split 4 — 2×2 panes"));
            _splitModeCombo.DisplayMember = nameof(SplitModeItem.Label);
            _splitModeCombo.SelectedIndex = 0;
            _splitModeCombo.SelectedIndexChanged += OnSplitModeChanged;
            _splitModeCombo.Visible = !_isMainScreen;

            _twoPanePanel = BuildTwoPanePanel(out _splitLeftCombo, out _splitRightCombo);
            _fourPanePanel = BuildFourPanePanel(out _tlCombo, out _trCombo, out _blCombo, out _brCombo);

            SelectRole(_splitLeftCombo, ViewportRole.Left);
            SelectRole(_splitRightCombo, ViewportRole.Right);
            SelectRole(_tlCombo, ViewportRole.Left);
            SelectRole(_trCombo, ViewportRole.Right);
            SelectRole(_blCombo, ViewportRole.MirrorLeft);
            SelectRole(_brCombo, ViewportRole.MirrorRight);

            _spanNextCheckBox = new CheckBox
            {
                Dock = DockStyle.Bottom,
                Text = "Span → next screen (wide view)",
                AutoSize = true
            };
            _spanNextCheckBox.CheckedChanged += (_, __) => LayoutChanged?.Invoke(this, EventArgs.Empty);

            Controls.Add(_spanNextCheckBox);
            Controls.Add(_fourPanePanel);
            Controls.Add(_twoPanePanel);
            Controls.Add(_splitModeCombo);
            Controls.Add(_roleCombo);
            Controls.Add(_titleLabel);

            UpdateSplitVisibility();
        }

        Panel BuildTwoPanePanel(out ComboBox left, out ComboBox right)
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = 72, Visible = false };
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            left = CreateSplitCombo();
            right = CreateSplitCombo();
            table.Controls.Add(CreateMutedLabel("L pane"), 0, 0);
            table.Controls.Add(left, 1, 0);
            table.Controls.Add(CreateMutedLabel("R pane"), 0, 1);
            table.Controls.Add(right, 1, 1);
            panel.Controls.Add(table);
            return panel;
        }

        Panel BuildFourPanePanel(out ComboBox tl, out ComboBox tr, out ComboBox bl, out ComboBox br)
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = 128, Visible = false };
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            tl = CreateSplitCombo();
            tr = CreateSplitCombo();
            bl = CreateSplitCombo();
            br = CreateSplitCombo();
            table.Controls.Add(CreateMutedLabel("TL"), 0, 0);
            table.Controls.Add(tl, 1, 0);
            table.Controls.Add(CreateMutedLabel("TR"), 0, 1);
            table.Controls.Add(tr, 1, 1);
            table.Controls.Add(CreateMutedLabel("BL"), 0, 2);
            table.Controls.Add(bl, 1, 2);
            table.Controls.Add(CreateMutedLabel("BR"), 0, 3);
            table.Controls.Add(br, 1, 3);
            panel.Controls.Add(table);
            return panel;
        }

        static Label CreateMutedLabel(string text) =>
            new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Tag = "muted"
            };

        ComboBox CreateSplitCombo()
        {
            var combo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 0, 0, 2)
            };
            PopulateRoleCombo(combo, ViewportRoleGroups.SplitHalfRoles);
            combo.SelectedIndexChanged += (_, __) => LayoutChanged?.Invoke(this, EventArgs.Empty);
            return combo;
        }

        static void PopulateRoleCombo(ComboBox combo, System.Collections.Generic.IReadOnlyList<ViewportRole> roles)
        {
            combo.Items.Clear();
            foreach (var role in roles)
                combo.Items.Add(new RoleItem(role, ViewportRoleGroups.FormatRoleLabel(role)));
            combo.DisplayMember = nameof(RoleItem.Label);
        }

        void OnSplitModeChanged(object sender, EventArgs e)
        {
            UpdateSplitVisibility();
            LayoutChanged?.Invoke(this, EventArgs.Empty);
        }

        void UpdateSplitVisibility()
        {
            var mode = GetSelectedSplitMode();
            if (_isMainScreen)
            {
                mode = AdditionalScreenSplitMode.Off;
                _splitModeCombo.Visible = false;
            }

            var split = mode != AdditionalScreenSplitMode.Off;
            _twoPanePanel.Visible = mode == AdditionalScreenSplitMode.Two;
            _fourPanePanel.Visible = mode == AdditionalScreenSplitMode.Four;
            _roleCombo.Visible = !split;
            _spanNextCheckBox.Visible = !split;
            _spanNextCheckBox.Enabled = !split && GetSelectedRole(_roleCombo) != ViewportRole.Unused;

            if (_isMainScreen)
                Height = _spanNextCheckBox.Visible ? 160 : 140;
            else if (mode == AdditionalScreenSplitMode.Four)
                Height = 268;
            else if (mode == AdditionalScreenSplitMode.Two)
                Height = 230;
            else
                Height = _spanNextCheckBox.Visible ? 188 : 168;
        }

        public void SetTitle(string title)
        {
            _titleLabel.Text = title;
        }

        public void Bind(ScreenLayoutEntry entry)
        {
            SelectRole(_roleCombo, entry.Role);
            SelectSplitMode(_isMainScreen ? AdditionalScreenSplitMode.Off : entry.SplitMode);
            SelectRole(_splitLeftCombo, entry.SplitLeftRole);
            SelectRole(_splitRightCombo, entry.SplitRightRole);
            SelectRole(_tlCombo, entry.SplitTopLeftRole);
            SelectRole(_trCombo, entry.SplitTopRightRole);
            SelectRole(_blCombo, entry.SplitBottomLeftRole);
            SelectRole(_brCombo, entry.SplitBottomRightRole);
            _spanNextCheckBox.Checked = entry.SpanNext;
            UpdateSplitVisibility();
        }

        public void ApplyTo(ScreenLayoutEntry entry)
        {
            entry.Role = GetSelectedRole(_roleCombo);
            entry.SplitMode = _isMainScreen ? AdditionalScreenSplitMode.Off : GetSelectedSplitMode();
            entry.SplitLeftRole = GetSelectedRole(_splitLeftCombo);
            entry.SplitRightRole = GetSelectedRole(_splitRightCombo);
            entry.SplitTopLeftRole = GetSelectedRole(_tlCombo);
            entry.SplitTopRightRole = GetSelectedRole(_trCombo);
            entry.SplitBottomLeftRole = GetSelectedRole(_blCombo);
            entry.SplitBottomRightRole = GetSelectedRole(_brCombo);
            entry.SpanNext = _spanNextCheckBox.Checked && entry.SplitMode == AdditionalScreenSplitMode.Off;

            if (entry.SplitMode != AdditionalScreenSplitMode.Off)
                entry.Role = ViewportRole.Unused;
        }

        void SelectSplitMode(AdditionalScreenSplitMode mode)
        {
            foreach (SplitModeItem item in _splitModeCombo.Items)
            {
                if (item.Mode == mode)
                {
                    _splitModeCombo.SelectedItem = item;
                    return;
                }
            }

            _splitModeCombo.SelectedIndex = 0;
        }

        AdditionalScreenSplitMode GetSelectedSplitMode() =>
            _splitModeCombo.SelectedItem is SplitModeItem item
                ? item.Mode
                : AdditionalScreenSplitMode.Off;

        static void SelectRole(ComboBox combo, ViewportRole role)
        {
            foreach (RoleItem item in combo.Items)
            {
                if (item.Role == role)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }

            if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        static ViewportRole GetSelectedRole(ComboBox combo) =>
            combo.SelectedItem is RoleItem item ? item.Role : ViewportRole.Unused;

        sealed class RoleItem
        {
            public RoleItem(ViewportRole role, string label)
            {
                Role = role;
                Label = label;
            }

            public ViewportRole Role { get; }
            public string Label { get; }
        }

        sealed class SplitModeItem
        {
            public SplitModeItem(AdditionalScreenSplitMode mode, string label)
            {
                Mode = mode;
                Label = label;
            }

            public AdditionalScreenSplitMode Mode { get; }
            public string Label { get; }
        }
    }
}
