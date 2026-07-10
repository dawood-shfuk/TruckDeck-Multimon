using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TruckDeck.Multimon.Controls;
using TruckDeck.Multimon.Helpers;
using TruckDeck.Multimon.Models;
using TruckDeck.Multimon.Presets;
using TruckDeck.Multimon.Services;

namespace TruckDeck.Multimon
{
    public partial class MainForm : Form
    {
        DisplayEnumerationResult _displays;
        LayoutProfile _profile;
        IList<PresetDefinition> _availablePresets = new List<PresetDefinition>();
        readonly Dictionary<int, ScreenTileControl> _tiles = new Dictionary<int, ScreenTileControl>();
        bool _suppressMainDisplayEvents;

        public MainForm()
        {
            InitializeComponent();
            ApplicationIconHelper.Apply(this);
            MultimonTheme.Apply(this);
            MultimonTheme.WireModernTabs(mainTabControl);
            MultimonTheme.ApplyDefaultWindowSize(this);
            titleLabel.ForeColor = MultimonTheme.Accent;
            titleLabel.Font = MultimonTheme.TitleFont;
            FormClosing += (_, __) =>
            {
                OverlayHostService.Stop();
                DrivingModeActivationService.Cancel();
            };
            RefreshDisplays();
        }

        void RefreshDisplays()
        {
            _displays = DisplayEnumerationService.Enumerate();
            UpdateStatusLabels();
            LoadPresets();
            ApplySelectedPreset();
        }

        void UpdateStatusLabels()
        {
            var count = _displays.Screens.Count;
            var bounds = _displays.VirtualDesktopBounds;
            displayStatusLabel.Text =
                $"Detected {count} hardware display{(count == 1 ? "" : "s")}  ·  Windows desktop: {bounds.Width}×{bounds.Height}";

            if (_profile != null)
            {
                DisplayLayoutHelper.FinalizeProfile(_profile);
                var active = DisplayLayoutHelper.CountActivePhysicalScreens(_profile);
                var game = _profile.GameDesktopBounds;
                if (_profile.UseMainPipMode)
                {
                    var main = MenuLayoutHelper.GetPrimaryBounds(_profile);
                    displayStatusLabel.Text +=
                        $"  ·  MAIN PiP: {main.Width}×{main.Height} native  ·  {_profile.MainPipPanels.Count} panel(s)";
                }
                else
                {
                    displayStatusLabel.Text +=
                        $"  ·  MAIN: {game.Width}×{game.Height} (full desktop canvas)  ·  {active} camera monitor{(active == 1 ? "" : "s")}";
                }
            }

            if (count > 6)
                surroundStatusLabel.Text = "More than 6 screens detected — only the first 6 are configurable in v1.";
            else if (DisplayLayoutHelper.Is2x2Grid(_displays.Screens.ToList()))
                surroundStatusLabel.Text =
                    "2×2 grid — pick MAIN (bottom-left for game center), set other displays below. Mark unused monitors as Unused.";
            else if (DisplayLayoutHelper.IsVerticallyStacked(_displays.Screens.ToList()))
                surroundStatusLabel.Text =
                    "Stacked monitors — bottom = center road view, top = split side windows. Drag views on the layout or use the Stacked preset.";
            else if (count >= 2)
                surroundStatusLabel.Text =
                    "Drag views onto the monitor layout below — drop Center on MAIN, side windows on your other display.";
            else if (_displays.LooksLikeSingleSurface)
                surroundStatusLabel.Text =
                    $"Extended desktop ready — Apply && Launch spans {bounds.Width}×{bounds.Height} across all screens.";
            else if (_displays.HasGaps)
                surroundStatusLabel.Text = "Gap detected between displays — Apply & Launch auto-stretches the game window across active monitors.";
            else
                surroundStatusLabel.Text = "Single display detected.";
        }

        void LoadPresets()
        {
            var previous = presetComboBox.SelectedItem as PresetDefinition;
            _availablePresets = PresetLibrary.GetPresetsForScreenCount(_displays.Screens.Count);
            presetComboBox.Items.Clear();
            foreach (var preset in _availablePresets)
                presetComboBox.Items.Add(preset);

            var custom = new PresetDefinition
            {
                Name = "Custom",
                MinScreens = 1,
                MaxScreens = 6
            };
            presetComboBox.Items.Add(custom);

            if (previous != null)
            {
                var match = _availablePresets.FirstOrDefault(p => p.Name == previous.Name);
                presetComboBox.SelectedItem = match ?? custom;
            }
            else if (DisplayLayoutHelper.Is2x2Grid(_displays.Screens.ToList()))
            {
                var quad = _availablePresets.FirstOrDefault(p => p.Name == "4 screens — bottom center + side window screen");
                presetComboBox.SelectedItem = quad ?? _availablePresets.FirstOrDefault() ?? custom;
            }
            else if (DisplayLayoutHelper.IsVerticallyStacked(_displays.Screens.ToList()))
            {
                var stacked = _availablePresets.FirstOrDefault(p => p.Layout == "stackedBottomCenter_topSplit");
                presetComboBox.SelectedItem = stacked ?? _availablePresets.FirstOrDefault() ?? custom;
            }
            else if (_displays.Screens.Count == 2)
            {
                var dual = _availablePresets.FirstOrDefault(p =>
                    p.Layout == "mainCenter_otherSplit" ||
                    p.Name == "Dual — Main + Split Side");
                presetComboBox.SelectedItem = dual ?? _availablePresets.FirstOrDefault() ?? custom;
            }
            else
            {
                presetComboBox.SelectedItem = _availablePresets.FirstOrDefault() ?? custom;
            }
        }

        void ApplySelectedPreset()
        {
            var driveSide = lhdRadioButton.Checked ? DriveSide.Lhd : DriveSide.Rhd;
            var preset = presetComboBox.SelectedItem as PresetDefinition;

            if (preset != null && preset.Name != "Custom")
                _profile = PresetLibrary.ApplyPreset(preset, _displays, driveSide);
            else
                _profile = PresetLibrary.CreateBlankProfile(_displays, driveSide);

            PopulateMainDisplayCombo();
            RebuildTiles();
            layoutCanvas.SetProfile(_profile);
            SyncPipUiFromProfile();
            UpdateStatusLabels();
            UpdateWarnings();
        }

        void layoutCanvas_LayoutChanged(object sender, EventArgs e)
        {
            if (_profile == null)
                return;

            _profile.PresetName = "Custom";
            SyncMainDisplayFromProfile();
            RefreshTileBindings();
            UpdateStatusLabels();
            UpdateWarnings();
        }

        void SyncMainDisplayFromProfile()
        {
            _suppressMainDisplayEvents = true;
            var selected = mainDisplayComboBox.Items
                .Cast<MainDisplayItem>()
                .FirstOrDefault(item => item.ScreenIndex == _profile.MainScreenIndex);
            if (selected != null)
                mainDisplayComboBox.SelectedItem = selected;
            _suppressMainDisplayEvents = false;
        }

        void PopulateMainDisplayCombo()
        {
            _suppressMainDisplayEvents = true;
            mainDisplayComboBox.Items.Clear();

            DisplayLayoutHelper.FinalizeProfile(_profile);
            var canvas = _profile != null ? _profile.GameDesktopBounds : _displays.VirtualDesktopBounds;

            foreach (var screen in _displays.Screens.Take(6))
            {
                mainDisplayComboBox.Items.Add(new MainDisplayItem(
                    screen.Index,
                    DisplayLayoutHelper.FormatMainDisplayOption(
                        screen,
                        _displays.Screens.ToList(),
                        canvas)));
            }

            var selected = mainDisplayComboBox.Items
                .Cast<MainDisplayItem>()
                .FirstOrDefault(item => item.ScreenIndex == _profile.MainScreenIndex);

            mainDisplayComboBox.SelectedItem = selected ?? (mainDisplayComboBox.Items.Count > 0 ? mainDisplayComboBox.Items[0] : null);
            _suppressMainDisplayEvents = false;
        }

        void mainDisplayComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressMainDisplayEvents || _profile == null)
                return;

            if (!(mainDisplayComboBox.SelectedItem is MainDisplayItem item))
                return;

            _profile.MainScreenIndex = item.ScreenIndex;
            ApplyMainDisplayPresetRoles();
            layoutCanvas.SetProfile(_profile);
            RebuildTiles();
            UpdateStatusLabels();
            UpdateWarnings();
        }

        void ApplyMainDisplayPresetRoles()
        {
            var preset = presetComboBox.SelectedItem as PresetDefinition;
            if (preset == null || preset.Name == "Custom")
                return;

            var layout = preset.Layout;
            if (string.IsNullOrWhiteSpace(layout))
            {
                if (preset.Name != null && preset.Name.IndexOf("side windows /", StringComparison.OrdinalIgnoreCase) >= 0)
                    layout = "mainSplit_otherCenter";
                else if (preset.Name != null && preset.Name.IndexOf("split", StringComparison.OrdinalIgnoreCase) >= 0)
                    layout = "mainCenter_otherSplit";
            }

            switch (layout)
            {
                case "mainCenter_otherSplit":
                case "primaryCenter_secondarySplit":
                    DisplayLayoutHelper.ApplyCenterOnMainLayout(_profile, splitOtherScreensWithSideWindows: true);
                    break;
                case "mainSplit_otherCenter":
                case "primarySplit_secondaryCenter":
                    DisplayLayoutHelper.ApplyCenterOnOtherLayout(_profile);
                    break;
            }
        }

        void RebuildTiles()
        {
            tilesFlowPanel.SuspendLayout();
            tilesFlowPanel.Controls.Clear();
            _tiles.Clear();

            foreach (var screen in _displays.Screens.Take(6))
            {
                var positionLabel = DisplayLayoutHelper.GetPositionLabel(
                    screen,
                    _displays.Screens.ToList(),
                    _profile?.MainScreenIndex);
                var entry = _profile.ScreenLayouts.First(e => e.ScreenIndex == screen.Index);
                var isMain = _profile != null && screen.Index == _profile.MainScreenIndex;
                var tile = new ScreenTileControl(screen, positionLabel, isMainScreen: isMain);
                tile.Bind(entry);
                tile.LayoutChanged += (_, __) =>
                {
                    tile.ApplyTo(entry);
                    DisplayLayoutHelper.FinalizeProfile(_profile);
                    layoutCanvas.SetProfile(_profile);
                    UpdateStatusLabels();
                    UpdateWarnings();
                };
                _tiles[screen.Index] = tile;
                tilesFlowPanel.Controls.Add(tile);
            }

            tilesFlowPanel.ResumeLayout();
        }

        void RefreshTileBindings()
        {
            DisplayLayoutHelper.FinalizeProfile(_profile);
            foreach (var screen in _displays.Screens.Take(6))
            {
                if (!_tiles.TryGetValue(screen.Index, out var tile))
                    continue;

                var entry = _profile.ScreenLayouts.First(e => e.ScreenIndex == screen.Index);
                tile.SetTitle(
                    DisplayLayoutHelper.GetPositionLabel(
                        screen,
                        _displays.Screens.ToList(),
                        _profile.MainScreenIndex) +
                    Environment.NewLine +
                    screen.ResolutionLabel);
                tile.Bind(entry);
            }

            layoutCanvas.SetProfile(_profile);
        }

        void UpdateWarnings()
        {
            var warnings = new List<string>
            {
                "Quick start (dual): Layout → Stacked preset → Screens Split 2 → Apply & Launch (full span).",
                "PiP on MAIN: tab 3 → enable checkbox → drag panels → click panel → arrows/zoom → Apply (game closed).",
                "Side windows render interior so wing mirrors stay visible. Use LHD/RHD to match your truck.",
                "SCS official max is 4 cameras total (Center + up to 3 PiP panels)."
            };

            if (_profile?.HasDesktopGaps == true && _profile?.UseMainPipMode != true)
                warnings.Add("Windows desktop has gaps between monitors — side views may letterbox in gap regions.");

            if (_profile != null)
            {
                DisplayLayoutHelper.FinalizeProfile(_profile);
                var viewports = _profile.UseMainPipMode
                    ? MainPipLayoutService.CreateViewports(_profile)
                    : NormalizedCoordCalculator.ExpandLayout(_profile);

                if (!_profile.UseMainPipMode)
                {
                    foreach (var warning in DisplayLayoutHelper.ValidateLayout(_profile, viewports))
                        warnings.Add(warning);
                }

                if (viewports.Count > 4)
                    warnings.Add($"Layout has {viewports.Count} cameras — SCS official max is 4. Use Split 2 / mark Unused, or remove a PiP panel.");
                else if (viewports.Count == 4)
                    warnings.Add("Layout uses 4 cameras (SCS official maximum).");

                if (!_profile.UseMainPipMode &&
                    _profile.ScreenLayouts.Any(e => e.SplitMode == AdditionalScreenSplitMode.Four))
                    warnings.Add("Split 4 defaults to 3 active panes + 1 Unused so Center + sides stay within the SCS limit of 4.");
            }

            warningsLabel.Text = string.Join(Environment.NewLine, warnings.Select(w => "• " + w));
        }

        void SyncProfileFromUi()
        {
            _profile.DriveSide = lhdRadioButton.Checked ? DriveSide.Lhd : DriveSide.Rhd;
            if (mainDisplayComboBox.SelectedItem is MainDisplayItem mainItem)
                _profile.MainScreenIndex = mainItem.ScreenIndex;

            foreach (var pair in _tiles)
            {
                var entry = _profile.ScreenLayouts.First(e => e.ScreenIndex == pair.Key);
                pair.Value.ApplyTo(entry);
            }

            _profile.UseMainPipMode = useMainPipCheckBox.Checked;
            if (_profile.UseMainPipMode)
                MainPipLayoutService.EnsureDefaultPips(_profile);

            DisplayLayoutHelper.FinalizeProfile(_profile);
        }

        void SyncPipUiFromProfile()
        {
            if (_profile == null)
                return;

            useMainPipCheckBox.Checked = _profile.UseMainPipMode;
            if (_profile.UseMainPipMode)
                MainPipLayoutService.EnsureDefaultPips(_profile);
            pipEditor.SetProfile(_profile);
            BindPipCameraAdjust();
        }

        void BindPipCameraAdjust()
        {
            if (_profile == null)
                return;
            pipCameraAdjust.Bind(pipEditor.SelectedPanel, _profile.DriveSide);
        }

        void pipEditor_SelectionChanged(object sender, EventArgs e) => BindPipCameraAdjust();

        void pipCameraAdjust_CameraChanged(object sender, EventArgs e)
        {
            if (_profile == null)
                return;

            _profile.PresetName = "Custom";
            pipEditor.Invalidate();
            UpdateWarnings();
        }

        void loadSavedCamerasButton_Click(object sender, EventArgs e)
        {
            if (_profile == null)
                return;

            var target = GetSelectedGameTarget();
            var imported = 0;
            string lastPath = null;
            foreach (var folder in GameDocumentsService.ResolveTargetFolders(target))
            {
                var path = GameDocumentsService.GetMultimonConfigPath(folder);
                lastPath = path;
                imported += MultimonConfigReader.ImportIntoProfile(_profile, path);
            }

            if (imported == 0)
            {
                MessageBox.Show(
                    "No PiP camera blocks found in multimon_config.sii.\r\n\r\n" +
                    "Apply once from this tool first, or run multimon save in-game.",
                    "Load saved cameras",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            useMainPipCheckBox.Checked = true;
            _profile.UseMainPipMode = true;
            pipEditor.SetProfile(_profile);
            pipEditor.SelectPanel(0);
            BindPipCameraAdjust();
            UpdateStatusLabels();
            UpdateWarnings();

            MessageBox.Show(
                $"Loaded {imported} panel(s) from:\r\n{lastPath}\r\n\r\n" +
                "Positions and camera angles restored. Click a panel to tweak, then Apply.",
                "Load saved cameras",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        void useMainPipCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_profile == null)
                return;

            useMainPipCheckBox.Checked = true;
            _profile.UseMainPipMode = true;
            if (_profile.UseMainPipMode)
            {
                floatingNativeCheckBox.Checked = false;
                MainPipLayoutService.EnsureDefaultPips(_profile);
            }

            pipEditor.SetProfile(_profile);
            BindPipCameraAdjust();
            UpdateStatusLabels();
            UpdateWarnings();
        }

        void pipEditor_LayoutChanged(object sender, EventArgs e)
        {
            if (_profile == null)
                return;

            _profile.PresetName = "Custom";
            UpdateStatusLabels();
            UpdateWarnings();
        }

        void addPipLeftButton_Click(object sender, EventArgs e) => TryAddPip(ViewportRole.Left);

        void addPipRightButton_Click(object sender, EventArgs e) => TryAddPip(ViewportRole.Right);

        void addPipMirrorButton_Click(object sender, EventArgs e)
        {
            var role = _profile?.DriveSide == DriveSide.Rhd
                ? ViewportRole.MirrorRight
                : ViewportRole.MirrorLeft;
            TryAddPip(role);
        }

        void TryAddPip(ViewportRole role)
        {
            if (_profile == null)
                return;

            useMainPipCheckBox.Checked = true;
            var added = MainPipLayoutService.AddPanel(_profile, role);
            if (added == null)
            {
                MessageBox.Show(
                    $"Maximum {MainPipLayoutService.MaxPipPanels} PiP panels (Center + {MainPipLayoutService.MaxPipPanels} = SCS max of 4).",
                    "PiP limit",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            pipEditor.SetProfile(_profile);
            pipEditor.SelectPanel(_profile.MainPipPanels.Count - 1);
            BindPipCameraAdjust();
            UpdateStatusLabels();
            UpdateWarnings();
        }

        void resetPipsButton_Click(object sender, EventArgs e)
        {
            if (_profile == null)
                return;

            _profile.MainPipPanels.Clear();
            MainPipLayoutService.EnsureDefaultPips(_profile);
            useMainPipCheckBox.Checked = true;
            pipEditor.SetProfile(_profile);
            pipEditor.SelectPanel(0);
            BindPipCameraAdjust();
            UpdateStatusLabels();
            UpdateWarnings();
        }

        GameTarget GetSelectedGameTarget()
        {
            switch (gameTargetComboBox.SelectedIndex)
            {
                case 1: return GameTarget.Ats;
                case 2: return GameTarget.Both;
                default: return GameTarget.Ets2;
            }
        }

        void applyButton_Click(object sender, EventArgs e) => RunApply(launchAfter: false);

        void applyLaunchButton_Click(object sender, EventArgs e) => RunApply(launchAfter: true);

        void RunApply(bool launchAfter)
        {
            SyncProfileFromUi();
            var pip = useMainPipCheckBox.Checked;
            var floating = !pip && floatingNativeCheckBox.Checked && _displays.Screens.Count > 1;
            var viewports = pip
                ? MainPipLayoutService.CreateViewports(_profile)
                : floating
                    ? NativeMainFloatingLayout.CreateViewports(_profile)
                    : NormalizedCoordCalculator.ExpandLayout(_profile);

            if (viewports.Count > 4)
            {
                var confirm = MessageBox.Show(
                    $"This layout has {viewports.Count} cameras.\r\n\r\n" +
                    "SCS officially supports up to 4 (e.g. Center + 3 side views).\r\n\r\n" +
                    "Recommended: remove a PiP panel, or on Screens use Split 2 / Unused.\r\n\r\n" +
                    "Apply anyway as experimental?",
                    "More than 4 cameras",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes)
                    return;
            }

            var target = GetSelectedGameTarget();
            var delayUntilDriving = !pip && !floating && launchAfter &&
                                    delayMultimonCheckBox.Checked &&
                                    _displays.Screens.Count > 1;

            MultimonApplyPhase phase;
            if (delayUntilDriving)
                phase = MultimonApplyPhase.Menu;
            else if (pip)
                phase = MultimonApplyPhase.MainPip;
            else if (floating)
                phase = MultimonApplyPhase.FloatingNativeMain;
            else
                phase = MultimonApplyPhase.Driving;

            OverlayHostService.Stop();
            var result = ConfigurationApplyService.Apply(_profile, target, phase);
            var text = string.Join(Environment.NewLine, result.Messages.Concat(result.Warnings));
            MessageBox.Show(
                text,
                result.Success ? "Configuration applied" : "Could not apply",
                MessageBoxButtons.OK,
                result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

            if (!result.Success || !launchAfter)
                return;

            if (!GameLaunchService.TryLaunch(target, out var launchError))
            {
                MessageBox.Show(launchError, "Launch failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DrivingModeActivationService.Cancel();

            if (delayUntilDriving)
            {
                var primary = MenuLayoutHelper.GetPrimaryBounds(_profile);
                GameWindowSpanService.SpanPrimaryWhenReadyAsync(target, primary);
                DrivingModeActivationService.BeginAfterLaunch(_profile, target);

                MessageBox.Show(
                    "Game launch requested via Steam.\r\n\r\n" +
                    "Experimental menu-only mode: primary monitor until cab entry.\r\n" +
                    "If the game crashes, uncheck this option and use full span / PiP / floating mode.",
                    "Launching",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (pip)
            {
                var primary = MenuLayoutHelper.GetPrimaryBounds(_profile);
                GameWindowSpanService.ResetSession();
                GameWindowSpanService.SpanWhenReadyAsync(target, primary);

                MessageBox.Show(
                    "Game launch requested.\r\n\r\n" +
                    $"PiP on MAIN: {primary.Width}×{primary.Height} native with free-placed side cameras.\r\n" +
                    "Second monitor is unused by the game — put TruckDeck / browser there.\r\n\r\n" +
                    "Adjust panel size/position on the PiP tab, then Apply again (close game first).",
                    "Launching (PiP on MAIN)",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (floating)
            {
                var warn = MessageBox.Show(
                    "Floating mode packs thin side-camera strips onto MAIN and captures them into separate windows.\r\n" +
                    "That usually looks bad (warped cabin, wrong overlay crops) with DX11.\r\n\r\n" +
                    "Recommended: click No, use PiP on MAIN (tab 3) or full-span Split 2 instead.\r\n\r\n" +
                    "Continue with floating mode anyway?",
                    "Floating mode warning",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (warn != DialogResult.Yes)
                    return;

                var primary = MenuLayoutHelper.GetPrimaryBounds(_profile);
                GameWindowSpanService.ResetSession();
                GameWindowSpanService.SpanWhenReadyAsync(target, primary);

                MessageBox.Show(
                    "Game launch requested.\r\n\r\n" +
                    $"MAIN-only window: {primary.Width}×{primary.Height}.\r\n" +
                    "After you are in the cab, use Tools → Start floating overlays if you still want them.\r\n" +
                    "They will not auto-start.",
                    "Launching (experimental floating)",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            GameWindowSpanService.ResetSession();
            GameWindowSpanService.SpanWhenReadyAsync(target, _profile.GameDesktopBounds);

            var gameArea = _profile.GameDesktopBounds;
            MessageBox.Show(
                "Game launch requested via Steam.\r\n\r\n" +
                $"Full span canvas: {gameArea.Width}×{gameArea.Height} at ({gameArea.Left}, {gameArea.Top}).\r\n" +
                "TruckDeck stretches the window across all monitors when ETS2 opens.\r\n\r\n" +
                "If side views are missing after 60s, use Tools → Enable full-span split now.",
                "Launching",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        void stretchWindowButton_Click(object sender, EventArgs e)
        {
            SyncProfileFromUi();
            if (!GameProcessGuard.IsGameRunning(out var running))
            {
                MessageBox.Show(
                    "Start ETS2/ATS first, then click Enable split views now.",
                    "Game not running",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var target = GetSelectedGameTarget();
            var area = _profile.GameDesktopBounds;
            stretchWindowButton.Enabled = false;
            stretchWindowButton.Text = "Enabling…";
            try
            {
                DrivingModeActivationService.ActivateDrivingMode(_profile, target, "manual enable");
                var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TruckDeckMultimon-span.log");
                MessageBox.Show(
                    $"Split views enabled on secondary display.\r\n\r\nCanvas: {area.Width}×{area.Height} at ({area.Left}, {area.Top}).\r\n\r\nIf top monitor is still blank: in-game Options → Display → toggle VSync, press Escape, then click Enable split views again.\r\n\r\nLog: {logPath}",
                    "Split views enabled",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            finally
            {
                stretchWindowButton.Enabled = true;
                stretchWindowButton.Text = "Enable full-span split now";
            }
        }

        void refreshButton_Click(object sender, EventArgs e) => RefreshDisplays();

        void resetGraphicsButton_Click(object sender, EventArgs e)
        {
            var target = GetSelectedGameTarget();
            var gameName = target == GameTarget.Ats ? "ATS" : target == GameTarget.Both ? "ETS2 and ATS" : "ETS2";

            var confirm = MessageBox.Show(
                $"Reset {gameName} graphics/display settings to single-monitor defaults?\r\n\r\n" +
                "• Deletes multimon_config.sii\r\n" +
                "• Sets r_multimon_mode to 0\r\n" +
                "• Restores fullscreen (not borderless)\r\n\r\n" +
                "Timestamped backups are created. Close the game first.",
                "Reset graphics to default",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            OverlayHostService.Stop();
            DrivingModeActivationService.Cancel();
            var result = GraphicsResetService.ResetToDefaults(target);
            var text = string.Join(Environment.NewLine, result.Messages.Concat(result.Warnings));
            MessageBox.Show(
                text,
                result.Success ? "Graphics reset" : "Could not reset",
                MessageBoxButtons.OK,
                result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        void startOverlaysButton_Click(object sender, EventArgs e)
        {
            SyncProfileFromUi();
            if (!GameProcessGuard.IsGameRunning(out _))
            {
                MessageBox.Show(
                    "Start ETS2/ATS first (Apply & Launch), wait until you are in the cab, then start overlays.",
                    "Game not running",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var target = GetSelectedGameTarget();
            var floating = floatingNativeCheckBox.Checked;
            OverlayHostService.Start(_profile, target, floatingMode: floating);
            MessageBox.Show(
                floating
                    ? "Floating overlays started.\r\n\r\nDrag the title bar to move · drag edges to resize · place on any monitor.\r\n\r\nIf black: toggle VSync in-game, then Stop → Start again."
                    : "Pinned overlays started on additional-screen panes.",
                "Overlays",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        void stopOverlaysButton_Click(object sender, EventArgs e)
        {
            OverlayHostService.Stop();
            MessageBox.Show("Overlays stopped.", "Overlays", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void presetComboBox_SelectedIndexChanged(object sender, EventArgs e) => ApplySelectedPreset();

        void driveSide_CheckedChanged(object sender, EventArgs e)
        {
            if (_profile != null)
                _profile.DriveSide = lhdRadioButton.Checked ? DriveSide.Lhd : DriveSide.Rhd;
            pipCameraAdjust.SetDriveSide(_profile?.DriveSide ?? DriveSide.Lhd);
            UpdateWarnings();
        }

        void docsLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("https://modding.scssoft.com/wiki/Documentation/Engine/Multi_monitor_configuration");
            }
            catch
            {
                // ignore
            }
        }

        void truckdeckLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("https://truckdeck.site");
            }
            catch
            {
                // ignore
            }
        }

        sealed class MainDisplayItem
        {
            public MainDisplayItem(int screenIndex, string label)
            {
                ScreenIndex = screenIndex;
                Label = label;
            }

            public int ScreenIndex { get; }
            public string Label { get; }

            public override string ToString() => Label;
        }
    }
}
