using System;
using System.Drawing;
using System.Windows.Forms;
using TruckDeck.Multimon.Helpers;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Controls
{
    /// <summary>
    /// Arrow + zoom controls to tune PiP camera heading, pitch, and FOV before Apply.
    /// </summary>
    public sealed class PipCameraAdjustControl : UserControl
    {
        const float HeadingStep = 3f;
        const float PitchStep = 2f;
        const float FovStep = 5f;
        const int ArrowSize = 48;

        MainPipPanel _panel;
        DriveSide _driveSide = DriveSide.Lhd;

        readonly Label _titleLabel;
        readonly Label _hintLabel;
        readonly Label _headingValue;
        readonly Label _pitchValue;
        readonly Label _fovValue;
        readonly Button _upButton;
        readonly Button _downButton;
        readonly Button _leftButton;
        readonly Button _rightButton;
        readonly Button _zoomInButton;
        readonly Button _zoomOutButton;
        readonly Button _resetButton;

        public event EventHandler CameraChanged;

        public PipCameraAdjustControl()
        {
            MinimumSize = new Size(240, 360);
            Dock = DockStyle.Fill;
            BackColor = MultimonTheme.Panel;
            Padding = MultimonTheme.Equal(MultimonTheme.Space);

            _titleLabel = new Label
            {
                AutoSize = false,
                Height = 26,
                Dock = DockStyle.Top,
                ForeColor = MultimonTheme.Accent,
                Font = MultimonTheme.UiFontBold ?? new Font("Segoe UI", 11f, FontStyle.Bold),
                Text = "Camera view",
                TextAlign = ContentAlignment.MiddleLeft
            };

            _hintLabel = new Label
            {
                AutoSize = false,
                Height = 40,
                Dock = DockStyle.Top,
                ForeColor = MultimonTheme.Label,
                Font = MultimonTheme.CaptionFont,
                Text = "Click a PiP panel on the left, then adjust look / zoom.",
                Tag = "muted"
            };

            var dpadHost = new Panel
            {
                Dock = DockStyle.Top,
                Height = ArrowSize * 3 + MultimonTheme.Space * 2,
                BackColor = MultimonTheme.BgElevated,
                Padding = MultimonTheme.Equal(MultimonTheme.SpaceSm),
                Margin = new Padding(0, MultimonTheme.SpaceSm, 0, MultimonTheme.Space)
            };

            var dpad = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                BackColor = MultimonTheme.BgElevated
            };
            for (var i = 0; i < 3; i++)
            {
                dpad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
                dpad.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
            }

            _upButton = CreateArrowButton("▲");
            _leftButton = CreateArrowButton("◀");
            _rightButton = CreateArrowButton("▶");
            _downButton = CreateArrowButton("▼");

            _upButton.Click += (_, __) => NudgePitch(PitchStep);
            _downButton.Click += (_, __) => NudgePitch(-PitchStep);
            _leftButton.Click += (_, __) => NudgeHeading(-HeadingStep);
            _rightButton.Click += (_, __) => NudgeHeading(HeadingStep);

            // Empty corners + center keep the cross aligned.
            dpad.Controls.Add(Spacer(), 0, 0);
            dpad.Controls.Add(_upButton, 1, 0);
            dpad.Controls.Add(Spacer(), 2, 0);
            dpad.Controls.Add(_leftButton, 0, 1);
            dpad.Controls.Add(Spacer(), 1, 1);
            dpad.Controls.Add(_rightButton, 2, 1);
            dpad.Controls.Add(Spacer(), 0, 2);
            dpad.Controls.Add(_downButton, 1, 2);
            dpad.Controls.Add(Spacer(), 2, 2);
            dpadHost.Controls.Add(dpad);

            var lookLabel = SectionLabel("Look");
            lookLabel.Dock = DockStyle.Top;

            var zoomLabel = SectionLabel("Zoom");
            zoomLabel.Dock = DockStyle.Top;

            var zoomRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = MultimonTheme.ButtonHeight + 4,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, MultimonTheme.Space)
            };
            zoomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            zoomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            _zoomInButton = CreateWideButton("＋  Closer");
            _zoomOutButton = CreateWideButton("－  Wider");
            _zoomInButton.Margin = new Padding(0, 0, MultimonTheme.SpaceXs, 0);
            _zoomOutButton.Margin = new Padding(MultimonTheme.SpaceXs, 0, 0, 0);
            _zoomInButton.Click += (_, __) => NudgeFov(-FovStep);
            _zoomOutButton.Click += (_, __) => NudgeFov(FovStep);
            zoomRow.Controls.Add(_zoomInButton, 0, 0);
            zoomRow.Controls.Add(_zoomOutButton, 1, 0);

            var valuesHost = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                BackColor = MultimonTheme.BgElevated,
                Padding = MultimonTheme.Equal(MultimonTheme.SpaceSm),
                Margin = new Padding(0, 0, 0, MultimonTheme.Space)
            };

            var valuesGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                BackColor = MultimonTheme.BgElevated
            };
            valuesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
            valuesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (var i = 0; i < 3; i++)
                valuesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));

            _headingValue = ValueLabel("—");
            _pitchValue = ValueLabel("—");
            _fovValue = ValueLabel("—");

            valuesGrid.Controls.Add(MutedCell("Heading"), 0, 0);
            valuesGrid.Controls.Add(_headingValue, 1, 0);
            valuesGrid.Controls.Add(MutedCell("Pitch"), 0, 1);
            valuesGrid.Controls.Add(_pitchValue, 1, 1);
            valuesGrid.Controls.Add(MutedCell("FOV"), 0, 2);
            valuesGrid.Controls.Add(_fovValue, 1, 2);
            valuesHost.Controls.Add(valuesGrid);

            _resetButton = new Button
            {
                Dock = DockStyle.Top,
                Height = MultimonTheme.ButtonHeight,
                Text = "Reset camera"
            };
            MultimonTheme.StyleSecondaryButton(_resetButton);
            _resetButton.Click += (_, __) => ResetCamera();

            // Dock order: last added = topmost visually when using Dock.Top stack from bottom.
            Controls.Add(_resetButton);
            Controls.Add(valuesHost);
            Controls.Add(zoomRow);
            Controls.Add(zoomLabel);
            Controls.Add(dpadHost);
            Controls.Add(lookLabel);
            Controls.Add(_hintLabel);
            Controls.Add(_titleLabel);

            SetEnabled(false);
            TabStop = true;
        }

        static Label SectionLabel(string text) =>
            new Label
            {
                AutoSize = false,
                Height = 22,
                Text = text,
                ForeColor = MultimonTheme.Label,
                Font = MultimonTheme.CaptionFont,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, MultimonTheme.SpaceSm, 0, MultimonTheme.SpaceXs)
            };

        static Label MutedCell(string text) =>
            new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                ForeColor = MultimonTheme.Label,
                Font = MultimonTheme.CaptionFont,
                TextAlign = ContentAlignment.MiddleLeft
            };

        static Label ValueLabel(string text) =>
            new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                ForeColor = MultimonTheme.Ink,
                Font = new Font("Consolas", 10f),
                TextAlign = ContentAlignment.MiddleLeft
            };

        static Panel Spacer() =>
            new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_panel == null)
                return base.ProcessCmdKey(ref msg, keyData);

            switch (keyData)
            {
                case Keys.Left:
                    NudgeHeading(-HeadingStep);
                    return true;
                case Keys.Right:
                    NudgeHeading(HeadingStep);
                    return true;
                case Keys.Up:
                    NudgePitch(PitchStep);
                    return true;
                case Keys.Down:
                    NudgePitch(-PitchStep);
                    return true;
                case Keys.Oemplus:
                case Keys.Add:
                    NudgeFov(-FovStep);
                    return true;
                case Keys.OemMinus:
                case Keys.Subtract:
                    NudgeFov(FovStep);
                    return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        public void Bind(MainPipPanel panel, DriveSide driveSide)
        {
            _panel = panel;
            _driveSide = driveSide;
            RefreshUi();
        }

        public void SetDriveSide(DriveSide driveSide)
        {
            _driveSide = driveSide;
            RefreshUi();
        }

        void NudgeHeading(float delta)
        {
            if (_panel == null)
                return;
            _panel.NudgeHeading(delta, _driveSide);
            RaiseChanged();
        }

        void NudgePitch(float delta)
        {
            if (_panel == null)
                return;
            _panel.NudgePitch(delta, _driveSide);
            RaiseChanged();
        }

        void NudgeFov(float delta)
        {
            if (_panel == null)
                return;
            _panel.NudgeFov(delta, _driveSide);
            RaiseChanged();
        }

        void ResetCamera()
        {
            if (_panel == null)
                return;
            _panel.ApplyRoleDefaults(_driveSide);
            RaiseChanged();
        }

        void RaiseChanged()
        {
            RefreshUi();
            CameraChanged?.Invoke(this, EventArgs.Empty);
        }

        void RefreshUi()
        {
            if (_panel == null)
            {
                _titleLabel.Text = "Camera view";
                _headingValue.Text = "—";
                _pitchValue.Text = "—";
                _fovValue.Text = "—";
                SetEnabled(false);
                return;
            }

            _panel.ResolveCamera(_driveSide, out var heading, out var pitch, out var fov);
            _titleLabel.Text = ViewportRoleGroups.FormatRoleLabel(_panel.Role);
            _headingValue.Text = $"{heading:0.0}°";
            _pitchValue.Text = $"{pitch:0.0}°";
            _fovValue.Text = fov > 0 ? $"{fov:0.0}°" : "seat";
            SetEnabled(true);
        }

        void SetEnabled(bool enabled)
        {
            _upButton.Enabled = enabled;
            _downButton.Enabled = enabled;
            _leftButton.Enabled = enabled;
            _rightButton.Enabled = enabled;
            _zoomInButton.Enabled = enabled;
            _zoomOutButton.Enabled = enabled;
            _resetButton.Enabled = enabled;
        }

        static Button CreateArrowButton(string text)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                Margin = new Padding(3)
            };
            MultimonTheme.StyleButton(button);
            button.Padding = Padding.Empty;
            return button;
        }

        static Button CreateWideButton(string text)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Height = MultimonTheme.ButtonHeight
            };
            MultimonTheme.StyleSecondaryButton(button);
            return button;
        }
    }
}
