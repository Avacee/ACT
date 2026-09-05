using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

using Advanced_Combat_Tracker;

using ReactiveTracker.Models;
using ReactiveTracker.Presenters;

namespace ReactiveTracker
{
    public class ReactiveTracker : UserControl, IActPluginV1
    {
        // ===== Configuration Defaults =====
        private const int DefaultSingleCount = 5;
        private const int DefaultGroupCount = 3;
        private const int DefaultWarningCountThreshold = 1;
        private const int DefaultWarningSecondsThreshold = 5;

        // ===== Settings XML Constants =====
        private const string SettingsRootElementName = "ReactiveTracker";
        private const string SettingsOverlayElementName = "OverlayPosition";
        private const string SettingsOptionsElementName = "Options";
        private const string SettingsAttributeX = "X";
        private const string SettingsAttributeY = "Y";
        private const string SettingsAttributeWidth = "Width";
        private const string SettingsAttributeHeight = "Height";
        private const string SettingsAttributeVisible = "Visible";
        private const string SettingsAttributeCoerciveHealing = "CoerciveHealing";
        private const string SettingsAttributeEofRaid2Set = "EofRaid2Set";
        private const string SettingsAttributeClass = "Class";
        private const string SettingsAttributeWarningCountThreshold = "WarningCountThreshold";
        private const string SettingsAttributeWarningSecondsThreshold = "WarningSecondsThreshold";
        private const string SettingsAttributeBackgroundColor = "BackgroundColor";
        private const string SettingsAttributeTextColor = "TextColor";
        private const string SettingsAttributeActiveColor = "ActiveColor";
        private const string SettingsAttributeThresholdColor = "ThresholdColor";
        private const string SettingsAttributeBackgroundTransparency = "BackgroundTransparency";
        private static readonly string SettingsFile = System.IO.Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "ReactiveTrackerSettings.xml");
        private static readonly System.Drawing.Color DefaultBackgroundColor = System.Drawing.Color.Black;
        private static readonly System.Drawing.Color DefaultTextColor = System.Drawing.Color.LightYellow;
        private static readonly System.Drawing.Color DefaultActiveColor = System.Drawing.Color.Green;
        private static readonly System.Drawing.Color DefaultThresholdColor = System.Drawing.Color.Red;
        private const int DefaultBackgroundTransparency = 255;

        // ===== Regex Patterns for Log Parsing =====
        private const string logTimeStampRegexStr = @"\(\d{10}\)\[.{24}\] "; // Borrowed from EqAditu.
        private readonly Regex rxSingleProcInq = new Regex(logTimeStampRegexStr + @"(YOUR Vengeful Faith heals (?<victim>.+?) for (a critical of )?(?<damage>[\d,\.KMBTQ]+) hit points?\.)", RegexOptions.Compiled);
        private readonly Regex rxSingleProcTemp = new Regex(logTimeStampRegexStr + @"(YOUR Supplicant's Prayer heals (?<victim>.+?) for (a critical of )?(?<damage>[\d,\.KMBTQ]+) hit points?\.)", RegexOptions.Compiled);
        private readonly Regex rxSingleReacExpiry = new Regex(logTimeStampRegexStr + @"(You feel the strong prayerful protection dissipate\.)", RegexOptions.Compiled);
        private readonly Regex rxGroupProcInq = new Regex(logTimeStampRegexStr + @"(YOUR Atoning Faith heals (?<victim>.+?) for (a critical of )?(?<damage>[\d,\.KMBTQ]+) hit points?\.)", RegexOptions.Compiled);
        private readonly Regex rxGroupProcTemp = new Regex(logTimeStampRegexStr + @"(YOUR Divine Prayer heals (?<victim>.+?) for (a critical of )?(?<damage>[\d,\.KMBTQ]+) hit points?\.)", RegexOptions.Compiled);
        private readonly Regex rxGroupEmpty = new Regex(logTimeStampRegexStr + @"Not in a group", RegexOptions.Compiled);

        // ===== Game Data Constants =====
        private const string races = "Aerakyn|Arasai|Barbarian|Dark Elf|Dwarf|Erudite|Fae|Freeblood|Froglok|Gnome|Half Elf|Halfling|High Elf|Human|Iksar|Kerra|Ogre|Ratonga|Sarnak|Troll|Vah Shir|Wood Elf";
        private const string classes = "Fighter|Warrior|Berserker|Guardian|Brawler|Bruiser|Monk|Crusader|Paladin|Shadowknight|Mage|Enchanter|Coercer|Illusionist|Sorceror|Warlock|Wizard|Summoner|Conjuror|Necromancer|Priest|Cleric|Inquisitor|Templar|Druid|Fury|Warden|Shaman|Defiler|Mystic|Shaper|Channeler|Scout|Bard|Dirge|Troubador|Predator|Assassin|Ranger|Rogue|Brigand|Swashbuckler|Animalist|Beastlord";
        private const string playerNameEmpty = "N/A";

        // ===== UI Control References =====
        Label lblStatus;
        TabPage tabReactive;
        frmReactiveOverlay overlayForm;
        Button btnShowOverlay;
        Button btnHideOverlay;
        CheckBox chkCoerciveHealing;
        CheckBox chkEofRaid2Set;
        ComboBox cmbClass;
        NumericUpDown nudWarningCountThreshold;
        NumericUpDown nudWarningSecondsThreshold;
        Button btnBackgroundColor;
        Button btnTextColor;
        Button btnActiveColor;
        Button btnThresholdColor;
        NumericUpDown nudBackgroundTransparency;
        ctrlPlayer[] _playerControls;

        // ===== Game State & Player Tracking =====
        PlayerModel[] _players;
        ReactivePresenter _reactivePresenter;
        const int _whoGroupStartIndex = 1;
        const int _whoGroupMaxIndex = 5;
        int _whoGroupIndex = _whoGroupStartIndex; // 0 = not currently in a /who group sequence
        private string whogroupRegexStr = "";
        private Regex rxWhoGroup = null;
        private bool bInWhoGroup = false;
        private System.Drawing.Color _backgroundColor = DefaultBackgroundColor;
        private System.Drawing.Color _textColor = DefaultTextColor;
        private System.Drawing.Color _activeColor = DefaultActiveColor;
        private System.Drawing.Color _thresholdColor = DefaultThresholdColor;

        // ===== Computed Properties =====
        private int SingleCount => DefaultSingleCount + (chkCoerciveHealing.Checked ? 1 : 0) + (chkEofRaid2Set.Checked && cmbClass.SelectedItem?.ToString() == "Templar" ? 4 : 0);
        private int GroupCount => DefaultGroupCount + (chkCoerciveHealing.Checked ? 1 : 0);
        private int WarningCountThreshold => (int)(nudWarningCountThreshold?.Value ?? DefaultWarningCountThreshold);
        private int WarningSecondsThreshold => (int)(nudWarningSecondsThreshold?.Value ?? DefaultWarningSecondsThreshold);
        private int BackgroundTransparency => (int)(nudBackgroundTransparency?.Value ?? DefaultBackgroundTransparency);

        public void DeInitPlugin()
        {
            SaveSettings();
            _reactivePresenter?.ResetAll();
            overlayForm?.Close();
            overlayForm = null;
            lblStatus.Text = "Plugin Stopped";
        }

        private void SaveSettings()
        {
            try
            {
                var doc = new XmlDocument();
                var root = doc.CreateElement(SettingsRootElementName);
                doc.AppendChild(root);

                if (overlayForm != null)
                {
                    var overlay = doc.CreateElement(SettingsOverlayElementName);
                    overlay.SetAttribute(SettingsAttributeX, overlayForm.Location.X.ToString());
                    overlay.SetAttribute(SettingsAttributeY, overlayForm.Location.Y.ToString());
                    overlay.SetAttribute(SettingsAttributeWidth, overlayForm.Width.ToString());
                    overlay.SetAttribute(SettingsAttributeHeight, overlayForm.Height.ToString());
                    overlay.SetAttribute(SettingsAttributeVisible, overlayForm.Visible.ToString());
                    root.AppendChild(overlay);
                }

                var options = doc.CreateElement(SettingsOptionsElementName);
                options.SetAttribute(SettingsAttributeCoerciveHealing, chkCoerciveHealing.Checked.ToString());
                options.SetAttribute(SettingsAttributeEofRaid2Set, chkEofRaid2Set.Checked.ToString());
                options.SetAttribute(SettingsAttributeClass, cmbClass.SelectedItem?.ToString() ?? "Inquisitor");
                options.SetAttribute(SettingsAttributeWarningCountThreshold, WarningCountThreshold.ToString());
                options.SetAttribute(SettingsAttributeWarningSecondsThreshold, WarningSecondsThreshold.ToString());
                options.SetAttribute(SettingsAttributeBackgroundColor, System.Drawing.ColorTranslator.ToHtml(_backgroundColor));
                options.SetAttribute(SettingsAttributeTextColor, System.Drawing.ColorTranslator.ToHtml(_textColor));
                options.SetAttribute(SettingsAttributeActiveColor, System.Drawing.ColorTranslator.ToHtml(_activeColor));
                options.SetAttribute(SettingsAttributeThresholdColor, System.Drawing.ColorTranslator.ToHtml(_thresholdColor));
                options.SetAttribute(SettingsAttributeBackgroundTransparency, BackgroundTransparency.ToString());
                root.AppendChild(options);

                doc.Save(SettingsFile);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error saving settings: " + ex.Message;
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (!System.IO.File.Exists(SettingsFile))
                    return;

                var doc = new XmlDocument();
                doc.Load(SettingsFile);

                var overlayNode = doc.SelectSingleNode($"/{SettingsRootElementName}/{SettingsOverlayElementName}");
                if (overlayNode != null && overlayForm != null)
                {
                    int x = int.Parse(overlayNode.Attributes[SettingsAttributeX].Value);
                    int y = int.Parse(overlayNode.Attributes[SettingsAttributeY].Value);
                    int width = int.Parse(overlayNode.Attributes[SettingsAttributeWidth].Value);
                    int height = int.Parse(overlayNode.Attributes[SettingsAttributeHeight].Value);
                    overlayForm.Location = new System.Drawing.Point(x, y);
                    overlayForm.Size = new System.Drawing.Size(width, height);

                    var visAttr = overlayNode.Attributes[SettingsAttributeVisible];
                    if (visAttr != null && !bool.Parse(visAttr.Value))
                        overlayForm.Hide();
                }

                var optionsNode = doc.SelectSingleNode($"/{SettingsRootElementName}/{SettingsOptionsElementName}");
                if (optionsNode != null)
                {
                    var coerciveAttr = optionsNode.Attributes[SettingsAttributeCoerciveHealing];
                    if (coerciveAttr != null)
                        chkCoerciveHealing.Checked = bool.Parse(coerciveAttr.Value);

                    var eofRaid2Attr = optionsNode.Attributes[SettingsAttributeEofRaid2Set];
                    if (eofRaid2Attr != null)
                        chkEofRaid2Set.Checked = bool.Parse(eofRaid2Attr.Value);

                    var classAttr = optionsNode.Attributes[SettingsAttributeClass];
                    if (classAttr != null && cmbClass.Items.Contains(classAttr.Value))
                        cmbClass.SelectedItem = classAttr.Value;

                    var warningCountAttr = optionsNode.Attributes[SettingsAttributeWarningCountThreshold];
                    int warningCountThreshold;
                    if (warningCountAttr != null && int.TryParse(warningCountAttr.Value, out warningCountThreshold))
                    {
                        warningCountThreshold = Math.Max((int)nudWarningCountThreshold.Minimum, Math.Min((int)nudWarningCountThreshold.Maximum, warningCountThreshold));
                        nudWarningCountThreshold.Value = warningCountThreshold;
                    }

                    var warningSecondsAttr = optionsNode.Attributes[SettingsAttributeWarningSecondsThreshold];
                    int warningSecondsThreshold;
                    if (warningSecondsAttr != null && int.TryParse(warningSecondsAttr.Value, out warningSecondsThreshold))
                    {
                        warningSecondsThreshold = Math.Max((int)nudWarningSecondsThreshold.Minimum, Math.Min((int)nudWarningSecondsThreshold.Maximum, warningSecondsThreshold));
                        nudWarningSecondsThreshold.Value = warningSecondsThreshold;
                    }

                    var backgroundColorAttr = optionsNode.Attributes[SettingsAttributeBackgroundColor];
                    if (backgroundColorAttr != null)
                    {
                        try { _backgroundColor = System.Drawing.ColorTranslator.FromHtml(backgroundColorAttr.Value); } catch { }
                    }

                    var textColorAttr = optionsNode.Attributes[SettingsAttributeTextColor];
                    if (textColorAttr != null)
                    {
                        try { _textColor = System.Drawing.ColorTranslator.FromHtml(textColorAttr.Value); } catch { }
                    }

                    var activeColorAttr = optionsNode.Attributes[SettingsAttributeActiveColor];
                    if (activeColorAttr != null)
                    {
                        try { _activeColor = System.Drawing.ColorTranslator.FromHtml(activeColorAttr.Value); } catch { }
                    }

                    var thresholdColorAttr = optionsNode.Attributes[SettingsAttributeThresholdColor];
                    if (thresholdColorAttr != null)
                    {
                        try { _thresholdColor = System.Drawing.ColorTranslator.FromHtml(thresholdColorAttr.Value); } catch { }
                    }

                    var backgroundTransparencyAttr = optionsNode.Attributes[SettingsAttributeBackgroundTransparency];
                    int transparency;
                    if (backgroundTransparencyAttr != null && int.TryParse(backgroundTransparencyAttr.Value, out transparency) && nudBackgroundTransparency != null)
                    {
                        transparency = Math.Max((int)nudBackgroundTransparency.Minimum, Math.Min((int)nudBackgroundTransparency.Maximum, transparency));
                        nudBackgroundTransparency.Value = transparency;
                    }

                    ApplyAppearanceToPlayers();
                    ApplyWarningThresholdsToPlayers();
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error loading settings: " + ex.Message;
            }
        }

        private void ApplyWarningThresholdsToPlayers()
        {
            if (_playerControls == null)
                return;

            for (int i = 0; i < _playerControls.Length; i++)
                _playerControls[i].SetWarningThresholds(WarningCountThreshold, WarningSecondsThreshold);
        }

        private void ApplyAppearanceToPlayers()
        {
            if (_playerControls == null)
                return;

            if (overlayForm != null)
            {
                // ensure a fully opaque back color, then set overall form opacity
                var overlayBackColor = System.Drawing.Color.FromArgb(255, _backgroundColor); // force opaque
                overlayForm.AllowTransparency = true; // optional but clarifies intent
                overlayForm.Opacity = Math.Max(0.0, Math.Min(1.0, BackgroundTransparency / 255.0)); // 0.0 - 1.0
                overlayForm.BackColor = overlayBackColor;
            }

            for (int i = 0; i < _playerControls.Length; i++)
                _playerControls[i].SetAppearance(_backgroundColor, _textColor, _activeColor, _thresholdColor);

            if (btnBackgroundColor != null)
                btnBackgroundColor.BackColor = _backgroundColor;
            if (btnTextColor != null)
                btnTextColor.BackColor = _textColor;
            if (btnActiveColor != null)
                btnActiveColor.BackColor = _activeColor;
            if (btnThresholdColor != null)
                btnThresholdColor.BackColor = _thresholdColor;
        }

        private void PickColor(Action<System.Drawing.Color> setter)
        {
            using (var dialog = new ColorDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    setter(dialog.Color);
                    ApplyAppearanceToPlayers();
                }
            }
        }

        private void ResetAppearanceDefaults()
        {
            _backgroundColor = DefaultBackgroundColor;
            _textColor = DefaultTextColor;
            _activeColor = DefaultActiveColor;
            _thresholdColor = DefaultThresholdColor;

            if (nudBackgroundTransparency != null)
            {
                var defaultTransparency = Math.Max((decimal)nudBackgroundTransparency.Minimum, Math.Min((decimal)nudBackgroundTransparency.Maximum, DefaultBackgroundTransparency));
                nudBackgroundTransparency.Value = defaultTransparency;
            }

            ApplyAppearanceToPlayers();
        }

        public void InitPlugin(System.Windows.Forms.TabPage pluginScreenSpace, System.Windows.Forms.Label pluginStatusText)
        {
            lblStatus = pluginStatusText;
            lblStatus.Text = "Plugin Starting";

            tabReactive = pluginScreenSpace;

            this.Dock = DockStyle.Fill;

            ActGlobals.oFormActMain.OnLogLineRead += OFormActMain_OnLogLineRead;

            ActGlobals.oFormActMain.BeforeCombatAction += OFormActMain_BeforeCombatAction;
            ActGlobals.oFormActMain.AfterCombatAction += OFormActMain_AfterCombatAction;

            ActGlobals.oFormActMain.OnCombatStart += OFormActMain_OnCombatStart;
            ActGlobals.oFormActMain.OnCombatEnd += OFormActMain_OnCombatEnd;

            // MVP wiring
            _players = new PlayerModel[6]
            {
                new PlayerModel { Name = ActGlobals.charName },
                new PlayerModel { Name = playerNameEmpty },
                new PlayerModel { Name = playerNameEmpty },
                new PlayerModel { Name = playerNameEmpty },
                new PlayerModel { Name = playerNameEmpty },
                new PlayerModel { Name = playerNameEmpty },
            };

            whogroupRegexStr = @"(?<playerName>.+?) Lvl (?<level>\d+) (?<race>" + races + ") (?<class>" + classes + ")";
            rxWhoGroup = new Regex(logTimeStampRegexStr + whogroupRegexStr, RegexOptions.Compiled);

            _playerControls = new ctrlPlayer[6];
            var views = new IPlayerView[6];
            for (int i = 0; i < 6; i++)
            {
                _playerControls[i] = new ctrlPlayer();
                views[i] = _playerControls[i];
            }

            _reactivePresenter = new ReactivePresenter(_players, views);

            for (int i = 0; i < 6; i++)
                _playerControls[i].SetPresenter(_reactivePresenter.GetPlayerPresenter(i));

            // The Main flow panel for the control
            var flowPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, Margin = new Padding(0, 0, 0, 10) };

            // Create the gbOverlay groupbox to hold the Show and Hide Overlay buttons
            var gbOverlay = new GroupBox { Text = "Overlay", AutoSize = false, Padding = new Padding(3, 14, 3, 3), Width = 300, Height = 100 };
            var flowButtons = new FlowLayoutPanel { AutoSize = false, FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };

            btnShowOverlay = new Button { Text = "Show Overlay", AutoSize = false, Width = 130, Height = 32 };
            btnShowOverlay.Click += (s, e) => { overlayForm.Show(ActGlobals.oFormActMain); btnShowOverlay.Enabled = false; btnHideOverlay.Enabled = true; };

            btnHideOverlay = new Button { Text = "Hide Overlay", AutoSize = false, Width = 130, Height = 32 };
            btnHideOverlay.Click += (s, e) => { overlayForm.Hide(); btnShowOverlay.Enabled = true; btnHideOverlay.Enabled = false; };

            flowButtons.Controls.Add(btnShowOverlay);
            flowButtons.Controls.Add(btnHideOverlay);
            gbOverlay.Controls.Add(flowButtons);
            gbOverlay.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // Create the Coercive Healing checkbox and add it to a group box
            var gbCalculateCount = new GroupBox { Text = "Calculate", AutoSize = false, Padding = new Padding(3, 14, 3, 3), Width = 300, Height = 110 };
            var flowCount = new FlowLayoutPanel { AutoSize = false, FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 0) };
            chkCoerciveHealing = new CheckBox { Text = "Coercive Healing", AutoSize = false, Width = 260, Height = 30, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
            chkEofRaid2Set = new CheckBox { Text = "Templar EoF Raid 2-Set", AutoSize = false, Width = 260, Height = 30, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
            flowCount.Controls.Add(chkCoerciveHealing);
            flowCount.Controls.Add(chkEofRaid2Set);
            gbCalculateCount.Controls.Add(flowCount);

            // Create the Class selection dropdown and add it to a group box
            var gbClass = new GroupBox { Text = "Class", AutoSize = false, Padding = new Padding(3, 14, 3, 3), Width = 300, Height = 70 };

            cmbClass = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new System.Drawing.Point(3, 30), Width = 260, Height = 32 };
            cmbClass.Items.AddRange(new object[] { "Defiler", "Inquisitor", "Mystic", "Templar" });
            cmbClass.SelectedItem = "Inquisitor";

            gbClass.Controls.Add(cmbClass);

            // Create warning threshold controls and add them to a group box
            var gbWarning = new GroupBox { Text = "Warning", AutoSize = false, Padding = new Padding(3, 14, 3, 3), Width = 300, Height = 110 };
            var warningTable = new TableLayoutPanel { ColumnCount = 2, RowCount = 2, Dock = DockStyle.Fill, AutoSize = false, Padding = new Padding(0, 4, 0, 4) };
            warningTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            warningTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            warningTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            warningTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            var lblWarningCount = new Label { Text = "Count <=", AutoSize = false, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleRight, Margin = new Padding(0, 0, 6, 0) };
            nudWarningCountThreshold = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 10,
                Value = DefaultWarningCountThreshold,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 0, 4)
            };

            var lblWarningSeconds = new Label { Text = "Seconds <", AutoSize = false, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleRight, Margin = new Padding(0, 0, 6, 0) };
            nudWarningSecondsThreshold = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 30,
                Value = DefaultWarningSecondsThreshold,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 0, 4)
            };

            nudWarningCountThreshold.ValueChanged += (s, e) => ApplyWarningThresholdsToPlayers();
            nudWarningSecondsThreshold.ValueChanged += (s, e) => ApplyWarningThresholdsToPlayers();

            warningTable.Controls.Add(lblWarningCount, 0, 0);
            warningTable.Controls.Add(nudWarningCountThreshold, 1, 0);
            warningTable.Controls.Add(lblWarningSeconds, 0, 1);
            warningTable.Controls.Add(nudWarningSecondsThreshold, 1, 1);
            gbWarning.Controls.Add(warningTable);

            // Create colour controls and add them to a group box
            var gbColours = new GroupBox { Text = "Colours", AutoSize = false, Padding = new Padding(3, 14, 3, 3), Width = 300, Height = 220 };
            var coloursTable = new TableLayoutPanel { ColumnCount = 2, RowCount = 6, Dock = DockStyle.Fill, AutoSize = false };
            coloursTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            coloursTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            for (int i = 0; i < 6; i++)
                coloursTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 6F));

            var lblBackgroundColor = new Label { Text = "Background", AutoSize = false, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
            btnBackgroundColor = new Button { Text = "Select", Dock = DockStyle.Fill };
            btnBackgroundColor.Click += (s, e) => PickColor(c => _backgroundColor = c);

            var lblTextColor = new Label { Text = "Text", AutoSize = false, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
            btnTextColor = new Button { Text = "Select", Dock = DockStyle.Fill };
            btnTextColor.Click += (s, e) => PickColor(c => _textColor = c);

            var lblActiveColor = new Label { Text = "Reactive Active", AutoSize = false, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
            btnActiveColor = new Button { Text = "Select", Dock = DockStyle.Fill };
            btnActiveColor.Click += (s, e) => PickColor(c => _activeColor = c);

            var lblThresholdColor = new Label { Text = "Threshold Reached", AutoSize = false, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
            btnThresholdColor = new Button { Text = "Select", Dock = DockStyle.Fill };
            btnThresholdColor.Click += (s, e) => PickColor(c => _thresholdColor = c);

            var lblTransparency = new Label { Text = "Transparency (0 -> 255)", AutoSize = false, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
            nudBackgroundTransparency = new NumericUpDown { Minimum = 0, Maximum = 255, Value = DefaultBackgroundTransparency, Dock = DockStyle.Fill };
            nudBackgroundTransparency.ValueChanged += (s, e) => ApplyAppearanceToPlayers();

            var btnResetColours = new Button { Text = "Reset Defaults", Dock = DockStyle.Fill };
            btnResetColours.Click += (s, e) => ResetAppearanceDefaults();

            coloursTable.Controls.Add(lblBackgroundColor, 0, 0);
            coloursTable.Controls.Add(btnBackgroundColor, 1, 0);
            coloursTable.Controls.Add(lblTextColor, 0, 1);
            coloursTable.Controls.Add(btnTextColor, 1, 1);
            coloursTable.Controls.Add(lblActiveColor, 0, 2);
            coloursTable.Controls.Add(btnActiveColor, 1, 2);
            coloursTable.Controls.Add(lblThresholdColor, 0, 3);
            coloursTable.Controls.Add(btnThresholdColor, 1, 3);
            coloursTable.Controls.Add(lblTransparency, 0, 4);
            coloursTable.Controls.Add(nudBackgroundTransparency, 1, 4);
            coloursTable.Controls.Add(btnResetColours, 0, 5);
            coloursTable.SetColumnSpan(btnResetColours, 2);
            gbColours.Controls.Add(coloursTable);

            // Add the group boxes to the flow panel
            flowPanel.Controls.Add(gbOverlay);
            flowPanel.Controls.Add(gbCalculateCount);
            flowPanel.Controls.Add(gbClass);
            flowPanel.Controls.Add(gbWarning);
            flowPanel.Controls.Add(gbColours);

            this.Controls.Add(flowPanel);

            // Add the flow panel to the ACT Tab page
            tabReactive.Controls.Add(this);

            // Create the overlay form and set up event handlers
            overlayForm = new frmReactiveOverlay();
            overlayForm.SetPlayerControls(_playerControls);
            ApplyAppearanceToPlayers();
            ApplyWarningThresholdsToPlayers();
            overlayForm.VisibleChanged += (s, e) =>
            {
                btnShowOverlay.Enabled = !overlayForm.Visible;
                btnHideOverlay.Enabled = overlayForm.Visible;
            };
            overlayForm.Show(ActGlobals.oFormActMain);
            LoadSettings();
            btnShowOverlay.Enabled = !overlayForm.Visible;
            btnHideOverlay.Enabled = overlayForm.Visible;

            lblStatus.Text = "Plugin Started";
        }

        private void OFormActMain_OnLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            if (isImport) return; // Ignore imported logs

            var line = logInfo.logLine.ToString();

            CheckWhoGroup(line);

            CheckSingleReacStart(line);

            CheckSingleReacProc(line);

            CheckGroupReacStart(line);

            CheckkGroupReacProc(line);

            // CheckSingleReacExpiryOrCancel(line);
        }

        private void CheckSingleReacStart(string line)
        {
            Regex rxSingleReacStart;
            //Switch on cmbClass.selectedItem to determine which regex to use for the single reactive start
            for (int i = 0; i < _players.Length; i++)
            {
                switch (cmbClass.SelectedItem.ToString())
                {
                    case "Inquisitor":
                        // Inquisitor: "You demand retribution for any wrongs done to (YOU/player)."
                        rxSingleReacStart = new Regex(logTimeStampRegexStr + $"You demand retribution for any wrongs done to ({ActGlobals.charName}|{_players[i].Name})");
                        break;
                    case "Templar":
                        // Templar: "You pray for NAME's body and soul." or "You pray for NAMES' body and soul."
                        rxSingleReacStart = new Regex(logTimeStampRegexStr + $"You pray for ({ActGlobals.charName}|{_players[i].Name})'s? body and soul.");
                        break;
                    default:
                        return; // Unknown class, do nothing
                }
                if (rxSingleReacStart.IsMatch(line))
                {
                    _reactivePresenter.StartSingle(i, SingleCount);
                    break;
                }
            }
        }

        private void CheckSingleReacProc(string line)
        {
            Regex regex;
            switch (cmbClass.SelectedItem?.ToString())
            {
                case "Inquisitor":
                    regex = rxSingleProcInq;
                    break;
                case "Templar":
                    regex = rxSingleProcTemp;
                    break;
                default:
                    return;
            }

            if (regex.IsMatch(line))
            {
                var victim = regex.Match(line).Groups["victim"].Value;
                FindAndExecuteForVictim(victim, _reactivePresenter.UseSingle);
            }
        }

        private void CheckSingleReacExpiry(string line)
        {
            // Single reactive Expiry: "You feel the strong prayerful protection dissipate."
            if (rxSingleReacExpiry.IsMatch(line))
            {
                _reactivePresenter.ExpireSingle(0);
            }
        }


        private void CheckGroupReacStart(string line)
        {
            Regex rxGroupeReacStart;
            switch (cmbClass.SelectedItem.ToString())
            {
                case "Inquisitor":
                    rxGroupeReacStart = new Regex(logTimeStampRegexStr + $"You raise your voice in a Malevolent Diatribe.");
                    break;
                case "Templar":
                    rxGroupeReacStart = new Regex(logTimeStampRegexStr + $"You pray devoutly for Holy Intercession.");
                    break;
                default:
                    return; // Unknown class, do nothing
            }
            if (rxGroupeReacStart.IsMatch(line))
            {
                for (int i = 0; i < _players.Length; i++)
                {
                    if (_players[i].Name != playerNameEmpty)
                        _reactivePresenter.StartGroup(i, GroupCount);
                }
            }
        }

        private void CheckkGroupReacProc(string line)
        {
            Regex regex;
            switch (cmbClass.SelectedItem?.ToString())
            {
                case "Inquisitor":
                    regex = rxGroupProcInq;
                    break;
                case "Templar":
                    regex = rxGroupProcTemp;
                    break;
                default:
                    return;
            }

            if (regex.IsMatch(line))
            {
                var victim = regex.Match(line).Groups["victim"].Value;
                FindAndExecuteForVictim(victim, _reactivePresenter.UseGroup);
            }
        }

        private void CheckSingleReacExpiryOrCancel(string line)
        {
            // Single reac drops from player[0] = You feel the strong prayerful protection dissipate.
            Regex rxSingleReacDropSelf = new Regex(logTimeStampRegexStr + @"You feel the strong prayerful protection dissipate\.");
            if (rxSingleReacDropSelf.IsMatch(line))
            {
                _reactivePresenter.ExpireSingle(0);
            }

            // Single reac drops from other players = "NAME appears less sure of (him/her)self.
            Regex rxSingleReacDropOther = new Regex(logTimeStampRegexStr + @"(?<victim>.+?) appears less sure of (himself|herself)\.");
            if (rxSingleReacDropOther.IsMatch(line))
            {
                var victim = rxSingleReacDropOther.Match(line).Groups["victim"].Value;
                FindAndExecuteForVictim(victim, _reactivePresenter.ExpireSingle);
            }   
        }

        private void FindAndExecuteForVictim(string victim, Action<int> action)
        {
            for (int i = 0; i < _players.Length; i++)
            {
                if (victim == _players[i].Name || victim == "YOU")
                {
                    action(i);
                    break;
                }
            }
        }

        private void CheckWhoGroup(string line)
        {
            //Reset all if we ar not in a group
            if (rxGroupEmpty.IsMatch(line))
            {
                for (int i = 1; i < _players.Length; i++)
                    _players[i].Name = playerNameEmpty;
                _whoGroupIndex = _whoGroupStartIndex;
                bInWhoGroup = false;
                return;
            }

            // Parse the output of the EQ2 command  /whogroup
            if (rxWhoGroup.IsMatch(line))
            {
                bInWhoGroup = true;
                Match match = rxWhoGroup.Match(line);
                var playerName = match.Groups["playerName"].Value;

                // First match starts at player[1]; subsequent consecutive matches fill up to player[_whoGroupMaxIndex]
                if (_whoGroupIndex < _players.Length && _whoGroupIndex <= _whoGroupMaxIndex)
                {
                    _players[_whoGroupIndex].Name = playerName;
                    _whoGroupIndex++;
                }
            }
            else
            {
                if (bInWhoGroup)
                {
                    // A non-matching line broke the sequence — fill remaining slots with playerNameEmpty
                    for (int i = _whoGroupIndex; i <= _whoGroupMaxIndex && i < _players.Length; i++)
                        _players[i].Name = playerNameEmpty;
                    _whoGroupIndex = _whoGroupStartIndex;
                    bInWhoGroup = false;
                }
            }
        }


        private void OFormActMain_BeforeCombatAction(bool isImport, CombatActionEventArgs actionInfo)
        {

        }

        private void OFormActMain_AfterCombatAction(bool isImport, CombatActionEventArgs actionInfo)
        {

        }

        private void OFormActMain_OnCombatStart(bool isImport, CombatToggleEventArgs encounterInfo)
        {

        }

        private void OFormActMain_OnCombatEnd(bool isImport, CombatToggleEventArgs encounterInfo)
        {

        }
    }
}
