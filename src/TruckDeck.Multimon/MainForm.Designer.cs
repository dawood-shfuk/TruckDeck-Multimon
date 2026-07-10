namespace TruckDeck.Multimon
{
    partial class MainForm
    {
        System.ComponentModel.IContainer components = null;

        System.Windows.Forms.Panel headerPanel;
        System.Windows.Forms.Label titleLabel;
        System.Windows.Forms.Label subtitleLabel;
        System.Windows.Forms.Label displayStatusLabel;
        System.Windows.Forms.Label surroundStatusLabel;
        System.Windows.Forms.TabControl mainTabControl;

        System.Windows.Forms.TabPage layoutTabPage;
        System.Windows.Forms.TabPage screensTabPage;
        System.Windows.Forms.TabPage pipTabPage;
        System.Windows.Forms.TabPage toolsTabPage;
        System.Windows.Forms.TabPage helpTabPage;

        System.Windows.Forms.CheckBox useMainPipCheckBox;
        System.Windows.Forms.Panel pipToolbarPanel;
        System.Windows.Forms.Panel pipBodyPanel;
        System.Windows.Forms.Button addPipLeftButton;
        System.Windows.Forms.Button addPipRightButton;
        System.Windows.Forms.Button addPipMirrorButton;
        System.Windows.Forms.Button resetPipsButton;
        System.Windows.Forms.Button loadSavedCamerasButton;
        Controls.MainPipEditorControl pipEditor;
        Controls.PipCameraAdjustControl pipCameraAdjust;

        System.Windows.Forms.Label presetLabel;
        System.Windows.Forms.ComboBox presetComboBox;
        System.Windows.Forms.RadioButton lhdRadioButton;
        System.Windows.Forms.RadioButton rhdRadioButton;
        System.Windows.Forms.Label gameTargetLabel;
        System.Windows.Forms.ComboBox gameTargetComboBox;
        System.Windows.Forms.Button refreshButton;
        System.Windows.Forms.Label mainDisplayLabel;
        System.Windows.Forms.ComboBox mainDisplayComboBox;
        System.Windows.Forms.Panel layoutToolbarPanel;
        Controls.InteractiveLayoutCanvas layoutCanvas;
        System.Windows.Forms.FlowLayoutPanel tilesFlowPanel;

        System.Windows.Forms.Label toolsIntroLabel;
        System.Windows.Forms.Button stretchWindowButton;
        System.Windows.Forms.CheckBox delayMultimonCheckBox;
        System.Windows.Forms.Button resetGraphicsButton;
        System.Windows.Forms.Label resetGraphicsHintLabel;
        System.Windows.Forms.Button startOverlaysButton;
        System.Windows.Forms.Button stopOverlaysButton;
        System.Windows.Forms.Label overlaysHintLabel;
        System.Windows.Forms.CheckBox floatingNativeCheckBox;

        System.Windows.Forms.Label warningsLabel;
        System.Windows.Forms.LinkLabel docsLinkLabel;
        System.Windows.Forms.LinkLabel truckdeckLinkLabel;

        System.Windows.Forms.Panel footerPanel;
        System.Windows.Forms.Label footerHintLabel;
        System.Windows.Forms.Button applyButton;
        System.Windows.Forms.Button applyLaunchButton;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            headerPanel = new System.Windows.Forms.Panel();
            titleLabel = new System.Windows.Forms.Label();
            subtitleLabel = new System.Windows.Forms.Label();
            displayStatusLabel = new System.Windows.Forms.Label();
            surroundStatusLabel = new System.Windows.Forms.Label();
            mainTabControl = new System.Windows.Forms.TabControl();

            layoutTabPage = new System.Windows.Forms.TabPage();
            screensTabPage = new System.Windows.Forms.TabPage();
            pipTabPage = new System.Windows.Forms.TabPage();
            toolsTabPage = new System.Windows.Forms.TabPage();
            helpTabPage = new System.Windows.Forms.TabPage();

            useMainPipCheckBox = new System.Windows.Forms.CheckBox();
            pipToolbarPanel = new System.Windows.Forms.Panel();
            pipBodyPanel = new System.Windows.Forms.Panel();
            addPipLeftButton = new System.Windows.Forms.Button();
            addPipRightButton = new System.Windows.Forms.Button();
            addPipMirrorButton = new System.Windows.Forms.Button();
            resetPipsButton = new System.Windows.Forms.Button();
            loadSavedCamerasButton = new System.Windows.Forms.Button();
            pipEditor = new Controls.MainPipEditorControl();
            pipCameraAdjust = new Controls.PipCameraAdjustControl();

            presetLabel = new System.Windows.Forms.Label();
            presetComboBox = new System.Windows.Forms.ComboBox();
            lhdRadioButton = new System.Windows.Forms.RadioButton();
            rhdRadioButton = new System.Windows.Forms.RadioButton();
            gameTargetLabel = new System.Windows.Forms.Label();
            gameTargetComboBox = new System.Windows.Forms.ComboBox();
            refreshButton = new System.Windows.Forms.Button();
            mainDisplayLabel = new System.Windows.Forms.Label();
            mainDisplayComboBox = new System.Windows.Forms.ComboBox();
            layoutToolbarPanel = new System.Windows.Forms.Panel();
            layoutCanvas = new Controls.InteractiveLayoutCanvas();
            tilesFlowPanel = new System.Windows.Forms.FlowLayoutPanel();

            toolsIntroLabel = new System.Windows.Forms.Label();
            stretchWindowButton = new System.Windows.Forms.Button();
            delayMultimonCheckBox = new System.Windows.Forms.CheckBox();
            resetGraphicsButton = new System.Windows.Forms.Button();
            resetGraphicsHintLabel = new System.Windows.Forms.Label();
            startOverlaysButton = new System.Windows.Forms.Button();
            stopOverlaysButton = new System.Windows.Forms.Button();
            overlaysHintLabel = new System.Windows.Forms.Label();
            floatingNativeCheckBox = new System.Windows.Forms.CheckBox();

            warningsLabel = new System.Windows.Forms.Label();
            docsLinkLabel = new System.Windows.Forms.LinkLabel();
            truckdeckLinkLabel = new System.Windows.Forms.LinkLabel();

            footerPanel = new System.Windows.Forms.Panel();
            footerHintLabel = new System.Windows.Forms.Label();
            applyButton = new System.Windows.Forms.Button();
            applyLaunchButton = new System.Windows.Forms.Button();

            SuspendLayout();
            headerPanel.SuspendLayout();
            mainTabControl.SuspendLayout();
            layoutTabPage.SuspendLayout();
            layoutToolbarPanel.SuspendLayout();
            screensTabPage.SuspendLayout();
            pipTabPage.SuspendLayout();
            pipToolbarPanel.SuspendLayout();
            pipBodyPanel.SuspendLayout();
            toolsTabPage.SuspendLayout();
            helpTabPage.SuspendLayout();
            footerPanel.SuspendLayout();

            Text = "TruckDeck Multimon";
            // Final size set in MainForm ctor: content × 1.5 (see MultimonTheme.ApplyDefaultWindowSize).
            ClientSize = Helpers.MultimonTheme.DefaultClientSize;
            MinimumSize = new System.Drawing.Size(
                Helpers.MultimonTheme.ContentClientWidth,
                Helpers.MultimonTheme.ContentClientHeight);
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Font = new System.Drawing.Font("Segoe UI", 9.5f);

            // —— Header (equal 16px inset) ——
            headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            headerPanel.Height = Helpers.MultimonTheme.HeaderHeight;
            headerPanel.Padding = Helpers.MultimonTheme.Equal(Helpers.MultimonTheme.Space);
            headerPanel.Tag = "header";

            titleLabel.AutoSize = true;
            titleLabel.Location = new System.Drawing.Point(Helpers.MultimonTheme.Space, Helpers.MultimonTheme.Space);
            titleLabel.Text = "TruckDeck Multimon";
            titleLabel.Tag = "accent";
            titleLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 18f, System.Drawing.FontStyle.Bold);

            subtitleLabel.AutoSize = true;
            subtitleLabel.Location = new System.Drawing.Point(Helpers.MultimonTheme.Space, 46);
            subtitleLabel.Text = "Multi-monitor layout for ETS2 / ATS";
            subtitleLabel.Tag = "muted";

            displayStatusLabel.AutoSize = true;
            displayStatusLabel.Location = new System.Drawing.Point(Helpers.MultimonTheme.Space, 68);
            displayStatusLabel.MaximumSize = new System.Drawing.Size(1040, 0);
            displayStatusLabel.Tag = "muted";

            surroundStatusLabel.AutoSize = true;
            surroundStatusLabel.Location = new System.Drawing.Point(520, 46);
            surroundStatusLabel.MaximumSize = new System.Drawing.Size(540, 40);
            surroundStatusLabel.Tag = "muted";
            surroundStatusLabel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(subtitleLabel);
            headerPanel.Controls.Add(displayStatusLabel);
            headerPanel.Controls.Add(surroundStatusLabel);

            // —— Footer (equal 16px inset all sides) ——
            footerPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            footerPanel.Height = Helpers.MultimonTheme.FooterHeight;
            footerPanel.Padding = Helpers.MultimonTheme.Equal(Helpers.MultimonTheme.Space);
            footerPanel.Tag = "footer";

            var footerButtons = new System.Windows.Forms.FlowLayoutPanel
            {
                Dock = System.Windows.Forms.DockStyle.Right,
                FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink,
                Padding = new System.Windows.Forms.Padding(Helpers.MultimonTheme.Space, 0, 0, 0),
                Margin = new System.Windows.Forms.Padding(0),
                Tag = "footer-buttons"
            };

            applyLaunchButton.Size = new System.Drawing.Size(220, Helpers.MultimonTheme.ButtonHeight);
            applyLaunchButton.Text = "Apply & Launch";
            applyLaunchButton.Tag = "accent";
            applyLaunchButton.Margin = new System.Windows.Forms.Padding(0, 0, Helpers.MultimonTheme.SpaceSm, 0);
            applyLaunchButton.Click += applyLaunchButton_Click;

            applyButton.Size = new System.Drawing.Size(140, Helpers.MultimonTheme.ButtonHeight);
            applyButton.Text = "Apply";
            applyButton.Margin = new System.Windows.Forms.Padding(0);
            applyButton.Click += applyButton_Click;

            footerButtons.Controls.Add(applyLaunchButton);
            footerButtons.Controls.Add(applyButton);

            footerHintLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            footerHintLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            footerHintLabel.Padding = new System.Windows.Forms.Padding(0, 0, Helpers.MultimonTheme.Space, 0);
            footerHintLabel.Text = "Apply writes multimon_config.sii + config.cfg. Apply & Launch starts Steam and spans the window.";
            footerHintLabel.Tag = "muted";

            // Fill first, then Right — so hint uses remaining space and buttons stay visible.
            footerPanel.Controls.Add(footerHintLabel);
            footerPanel.Controls.Add(footerButtons);

            // —— Tabs ——
            mainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            mainTabControl.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5f);
            mainTabControl.Controls.Add(layoutTabPage);
            mainTabControl.Controls.Add(screensTabPage);
            mainTabControl.Controls.Add(pipTabPage);
            mainTabControl.Controls.Add(toolsTabPage);
            mainTabControl.Controls.Add(helpTabPage);

            layoutTabPage.Text = "Layout";
            layoutTabPage.Padding = Helpers.MultimonTheme.PagePadding;
            layoutTabPage.UseVisualStyleBackColor = false;

            screensTabPage.Text = "Screens";
            screensTabPage.Padding = Helpers.MultimonTheme.PagePadding;
            screensTabPage.UseVisualStyleBackColor = false;
            screensTabPage.AutoScroll = true;

            pipTabPage.Text = "PiP on MAIN";
            pipTabPage.Padding = Helpers.MultimonTheme.PagePadding;
            pipTabPage.UseVisualStyleBackColor = false;

            toolsTabPage.Text = "Tools";
            toolsTabPage.Padding = Helpers.MultimonTheme.PagePadding;
            toolsTabPage.UseVisualStyleBackColor = false;
            toolsTabPage.AutoScroll = true;

            helpTabPage.Text = "Help";
            helpTabPage.Padding = Helpers.MultimonTheme.PagePadding;
            helpTabPage.UseVisualStyleBackColor = false;

            // —— Layout tab ——
            layoutToolbarPanel.Dock = System.Windows.Forms.DockStyle.Top;
            layoutToolbarPanel.Height = 92;
            layoutToolbarPanel.Padding = Helpers.MultimonTheme.CardPadding;
            layoutToolbarPanel.Tag = "card";

            presetLabel.AutoSize = true;
            presetLabel.Location = new System.Drawing.Point(12, 16);
            presetLabel.Text = "Preset";
            presetLabel.Tag = "muted";

            presetComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            presetComboBox.Location = new System.Drawing.Point(64, 12);
            presetComboBox.Size = new System.Drawing.Size(300, 28);
            presetComboBox.SelectedIndexChanged += presetComboBox_SelectedIndexChanged;

            lhdRadioButton.AutoSize = true;
            lhdRadioButton.Location = new System.Drawing.Point(384, 14);
            lhdRadioButton.Text = "LHD";
            lhdRadioButton.Checked = true;
            lhdRadioButton.CheckedChanged += driveSide_CheckedChanged;

            rhdRadioButton.AutoSize = true;
            rhdRadioButton.Location = new System.Drawing.Point(448, 14);
            rhdRadioButton.Text = "RHD";
            rhdRadioButton.CheckedChanged += driveSide_CheckedChanged;

            gameTargetLabel.AutoSize = true;
            gameTargetLabel.Location = new System.Drawing.Point(520, 16);
            gameTargetLabel.Text = "Game";
            gameTargetLabel.Tag = "muted";

            gameTargetComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            gameTargetComboBox.Location = new System.Drawing.Point(568, 12);
            gameTargetComboBox.Size = new System.Drawing.Size(120, 28);
            gameTargetComboBox.Items.AddRange(new object[] { "ETS2", "ATS", "Both" });
            gameTargetComboBox.SelectedIndex = 0;

            refreshButton.Location = new System.Drawing.Point(708, 10);
            refreshButton.Size = new System.Drawing.Size(110, 32);
            refreshButton.Text = "Refresh";
            refreshButton.Click += refreshButton_Click;

            mainDisplayLabel.AutoSize = true;
            mainDisplayLabel.Location = new System.Drawing.Point(12, 54);
            mainDisplayLabel.Text = "MAIN display";
            mainDisplayLabel.Tag = "muted";

            mainDisplayComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            mainDisplayComboBox.Location = new System.Drawing.Point(110, 50);
            mainDisplayComboBox.Size = new System.Drawing.Size(708, 28);
            mainDisplayComboBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left |
                                         System.Windows.Forms.AnchorStyles.Right;
            mainDisplayComboBox.SelectedIndexChanged += mainDisplayComboBox_SelectedIndexChanged;

            layoutToolbarPanel.Controls.Add(presetLabel);
            layoutToolbarPanel.Controls.Add(presetComboBox);
            layoutToolbarPanel.Controls.Add(lhdRadioButton);
            layoutToolbarPanel.Controls.Add(rhdRadioButton);
            layoutToolbarPanel.Controls.Add(gameTargetLabel);
            layoutToolbarPanel.Controls.Add(gameTargetComboBox);
            layoutToolbarPanel.Controls.Add(refreshButton);
            layoutToolbarPanel.Controls.Add(mainDisplayLabel);
            layoutToolbarPanel.Controls.Add(mainDisplayComboBox);

            layoutCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
            layoutCanvas.LayoutChanged += layoutCanvas_LayoutChanged;

            layoutTabPage.Controls.Add(layoutCanvas);
            layoutTabPage.Controls.Add(layoutToolbarPanel);

            // —— Screens tab ——
            var screensHint = new System.Windows.Forms.Label
            {
                AutoSize = true,
                Dock = System.Windows.Forms.DockStyle.Top,
                Padding = new System.Windows.Forms.Padding(0, 0, 0, Helpers.MultimonTheme.Space),
                MaximumSize = new System.Drawing.Size(0, 0),
                Text = "Per-monitor roles. MAIN stays full cabin. Additional screens: Split 2 (L/R) or Split 4 (2×2).",
                Tag = "muted"
            };

            tilesFlowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            tilesFlowPanel.AutoScroll = true;
            tilesFlowPanel.WrapContents = true;
            tilesFlowPanel.Padding = Helpers.MultimonTheme.Equal(0);

            screensTabPage.Controls.Add(tilesFlowPanel);
            screensTabPage.Controls.Add(screensHint);

            // —— PiP on MAIN tab ——
            pipToolbarPanel.Dock = System.Windows.Forms.DockStyle.Top;
            pipToolbarPanel.Height = 92;
            pipToolbarPanel.Padding = Helpers.MultimonTheme.CardPadding;
            pipToolbarPanel.Tag = "card";

            useMainPipCheckBox.AutoSize = true;
            useMainPipCheckBox.Location = new System.Drawing.Point(Helpers.MultimonTheme.Space, Helpers.MultimonTheme.SpaceSm);
            useMainPipCheckBox.Checked = false;
            useMainPipCheckBox.Text =
                "Use PiP on MAIN only — native MAIN res, free-place panels, camera arrows on the right";
            useMainPipCheckBox.CheckedChanged += useMainPipCheckBox_CheckedChanged;

            var btnY = 46;
            var btnH = 32;
            var btnGap = Helpers.MultimonTheme.SpaceSm;
            addPipLeftButton.Location = new System.Drawing.Point(Helpers.MultimonTheme.Space, btnY);
            addPipLeftButton.Size = new System.Drawing.Size(128, btnH);
            addPipLeftButton.Text = "+ Left window";
            addPipLeftButton.Click += addPipLeftButton_Click;

            addPipRightButton.Location = new System.Drawing.Point(Helpers.MultimonTheme.Space + 128 + btnGap, btnY);
            addPipRightButton.Size = new System.Drawing.Size(128, btnH);
            addPipRightButton.Text = "+ Right window";
            addPipRightButton.Click += addPipRightButton_Click;

            addPipMirrorButton.Location = new System.Drawing.Point(Helpers.MultimonTheme.Space + (128 + btnGap) * 2, btnY);
            addPipMirrorButton.Size = new System.Drawing.Size(110, btnH);
            addPipMirrorButton.Text = "+ Mirror";
            addPipMirrorButton.Click += addPipMirrorButton_Click;

            resetPipsButton.Location = new System.Drawing.Point(Helpers.MultimonTheme.Space + (128 + btnGap) * 2 + 110 + btnGap, btnY);
            resetPipsButton.Size = new System.Drawing.Size(140, btnH);
            resetPipsButton.Text = "Reset L / R";
            resetPipsButton.Click += resetPipsButton_Click;

            loadSavedCamerasButton.Location = new System.Drawing.Point(Helpers.MultimonTheme.Space + (128 + btnGap) * 2 + 110 + btnGap + 140 + btnGap, btnY);
            loadSavedCamerasButton.Size = new System.Drawing.Size(180, btnH);
            loadSavedCamerasButton.Text = "Load saved cameras";
            loadSavedCamerasButton.Click += loadSavedCamerasButton_Click;

            pipToolbarPanel.Controls.Add(useMainPipCheckBox);
            pipToolbarPanel.Controls.Add(addPipLeftButton);
            pipToolbarPanel.Controls.Add(addPipRightButton);
            pipToolbarPanel.Controls.Add(addPipMirrorButton);
            pipToolbarPanel.Controls.Add(resetPipsButton);
            pipToolbarPanel.Controls.Add(loadSavedCamerasButton);

            pipBodyPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            pipBodyPanel.Padding = new System.Windows.Forms.Padding(0, Helpers.MultimonTheme.Space, 0, 0);

            // Equal gap between editor and camera panel.
            var pipCameraHost = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Right,
                Width = Helpers.MultimonTheme.SidePanelWidth + Helpers.MultimonTheme.Space,
                Padding = new System.Windows.Forms.Padding(Helpers.MultimonTheme.Space, 0, 0, 0),
                BackColor = Helpers.MultimonTheme.Bg
            };

            pipEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            pipEditor.LayoutChanged += pipEditor_LayoutChanged;
            pipEditor.SelectionChanged += pipEditor_SelectionChanged;

            pipCameraAdjust.Dock = System.Windows.Forms.DockStyle.Fill;
            pipCameraAdjust.CameraChanged += pipCameraAdjust_CameraChanged;

            pipCameraHost.Controls.Add(pipCameraAdjust);
            pipBodyPanel.Controls.Add(pipEditor);
            pipBodyPanel.Controls.Add(pipCameraHost);

            pipTabPage.Controls.Add(pipBodyPanel);
            pipTabPage.Controls.Add(pipToolbarPanel);

            // —— Tools tab ——
            toolsIntroLabel.AutoSize = true;
            toolsIntroLabel.Location = new System.Drawing.Point(Helpers.MultimonTheme.Space, Helpers.MultimonTheme.Space);
            toolsIntroLabel.MaximumSize = new System.Drawing.Size(980, 0);
            toolsIntroLabel.Text =
                "Full-span = stacked dual monitors. PiP on MAIN = native MAIN + free panels. Floating overlays = experimental.";
            toolsIntroLabel.Tag = "muted";

            floatingNativeCheckBox.AutoSize = true;
            floatingNativeCheckBox.Location = new System.Drawing.Point(Helpers.MultimonTheme.Space, 48);
            floatingNativeCheckBox.Checked = false;
            floatingNativeCheckBox.Text =
                "Experimental: floating overlays (often looks wrong with DX11 — keep OFF)";
            floatingNativeCheckBox.Tag = "muted";

            stretchWindowButton.Location = new System.Drawing.Point(Helpers.MultimonTheme.Space, 92);
            stretchWindowButton.Size = new System.Drawing.Size(280, Helpers.MultimonTheme.ButtonHeight);
            stretchWindowButton.Text = "Enable full-span split now";
            stretchWindowButton.Click += stretchWindowButton_Click;

            var stretchHint = new System.Windows.Forms.Label
            {
                AutoSize = true,
                Location = new System.Drawing.Point(Helpers.MultimonTheme.Space + 280 + Helpers.MultimonTheme.Space, 100),
                MaximumSize = new System.Drawing.Size(620, 0),
                Text = "Spans the game across the full Windows desktop when already running.",
                Tag = "muted"
            };

            delayMultimonCheckBox.AutoSize = true;
            delayMultimonCheckBox.Location = new System.Drawing.Point(Helpers.MultimonTheme.Space, 152);
            delayMultimonCheckBox.Checked = false;
            delayMultimonCheckBox.Text = "Experimental: menu on primary only (may crash — prefer full span / PiP)";
            delayMultimonCheckBox.Tag = "muted";

            resetGraphicsButton.Location = new System.Drawing.Point(Helpers.MultimonTheme.Space, 196);
            resetGraphicsButton.Size = new System.Drawing.Size(280, Helpers.MultimonTheme.ButtonHeight);
            resetGraphicsButton.Text = "Reset game graphics to default";
            resetGraphicsButton.Click += resetGraphicsButton_Click;

            resetGraphicsHintLabel.AutoSize = true;
            resetGraphicsHintLabel.Location = new System.Drawing.Point(Helpers.MultimonTheme.Space, 248);
            resetGraphicsHintLabel.MaximumSize = new System.Drawing.Size(980, 0);
            resetGraphicsHintLabel.Text =
                "Removes multimon_config.sii and restores single-monitor display settings. Close the game first.";
            resetGraphicsHintLabel.Tag = "muted";

            startOverlaysButton.Location = new System.Drawing.Point(Helpers.MultimonTheme.Space, 304);
            startOverlaysButton.Size = new System.Drawing.Size(280, Helpers.MultimonTheme.ButtonHeight);
            startOverlaysButton.Text = "Start floating overlays";
            startOverlaysButton.Click += startOverlaysButton_Click;

            stopOverlaysButton.Location = new System.Drawing.Point(Helpers.MultimonTheme.Space + 280 + Helpers.MultimonTheme.Space, 304);
            stopOverlaysButton.Size = new System.Drawing.Size(200, Helpers.MultimonTheme.ButtonHeight);
            stopOverlaysButton.Text = "Stop overlays";
            stopOverlaysButton.Click += stopOverlaysButton_Click;

            overlaysHintLabel.AutoSize = true;
            overlaysHintLabel.Location = new System.Drawing.Point(Helpers.MultimonTheme.Space, 360);
            overlaysHintLabel.MaximumSize = new System.Drawing.Size(980, 0);
            overlaysHintLabel.Text =
                "Recommended: Floating OFF → Apply & Launch → full-span Split 2, or use PiP on MAIN for native-res panels.";
            overlaysHintLabel.Tag = "muted";

            toolsTabPage.Controls.Add(toolsIntroLabel);
            toolsTabPage.Controls.Add(floatingNativeCheckBox);
            toolsTabPage.Controls.Add(stretchWindowButton);
            toolsTabPage.Controls.Add(stretchHint);
            toolsTabPage.Controls.Add(delayMultimonCheckBox);
            toolsTabPage.Controls.Add(resetGraphicsButton);
            toolsTabPage.Controls.Add(resetGraphicsHintLabel);
            toolsTabPage.Controls.Add(startOverlaysButton);
            toolsTabPage.Controls.Add(stopOverlaysButton);
            toolsTabPage.Controls.Add(overlaysHintLabel);

            // —— Help tab ——
            warningsLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            warningsLabel.Padding = new System.Windows.Forms.Padding(4);
            warningsLabel.ForeColor = Helpers.MultimonTheme.Warning;
            warningsLabel.Tag = "muted";

            var helpLinks = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Bottom,
                Height = 36
            };

            docsLinkLabel.AutoSize = true;
            docsLinkLabel.Location = new System.Drawing.Point(4, 8);
            docsLinkLabel.Text = "SCS multimon docs";
            docsLinkLabel.LinkClicked += docsLinkLabel_LinkClicked;

            truckdeckLinkLabel.AutoSize = true;
            truckdeckLinkLabel.Location = new System.Drawing.Point(160, 8);
            truckdeckLinkLabel.Text = "truckdeck.site";
            truckdeckLinkLabel.LinkClicked += truckdeckLinkLabel_LinkClicked;

            helpLinks.Controls.Add(docsLinkLabel);
            helpLinks.Controls.Add(truckdeckLinkLabel);

            helpTabPage.Controls.Add(warningsLabel);
            helpTabPage.Controls.Add(helpLinks);

            // Root dock order: Fill first, then Bottom, then Top
            Controls.Add(mainTabControl);
            Controls.Add(footerPanel);
            Controls.Add(headerPanel);

            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            mainTabControl.ResumeLayout(false);
            layoutToolbarPanel.ResumeLayout(false);
            layoutToolbarPanel.PerformLayout();
            layoutTabPage.ResumeLayout(false);
            layoutTabPage.PerformLayout();
            screensTabPage.ResumeLayout(false);
            screensTabPage.PerformLayout();
            pipToolbarPanel.ResumeLayout(false);
            pipToolbarPanel.PerformLayout();
            pipBodyPanel.ResumeLayout(false);
            pipBodyPanel.PerformLayout();
            pipTabPage.ResumeLayout(false);
            pipTabPage.PerformLayout();
            toolsTabPage.ResumeLayout(false);
            toolsTabPage.PerformLayout();
            helpTabPage.ResumeLayout(false);
            helpTabPage.PerformLayout();
            footerPanel.ResumeLayout(false);
            footerPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
