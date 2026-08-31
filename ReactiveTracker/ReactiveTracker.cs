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
        private static readonly string SettingsFile = System.IO.Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "ReactiveTrackerSettings.xml");

        // ===== Regex Patterns for Log Parsing =====
        private const string logTimeStampRegexStr = @"\(\d{10}\)\[.{24}\] ";
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

        // ===== Computed Properties =====
        private int SingleCount => DefaultSingleCount + (chkCoerciveHealing.Checked ? 1 : 0) + (chkEofRaid2Set.Checked && cmbClass.SelectedItem?.ToString() == "Templar" ? 4 : 0);
        private int GroupCount => DefaultGroupCount + (chkCoerciveHealing.Checked ? 1 : 0);
        private int WarningCountThreshold => (int)(nudWarningCountThreshold?.Value ?? DefaultWarningCountThreshold);
        private int WarningSecondsThreshold => (int)(nudWarningSecondsThreshold?.Value ?? DefaultWarningSecondsThreshold);

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

            // Create the Show and Hide Overlay buttons and add them to a group box
            btnShowOverlay = new Button { Text = "Show Overlay", AutoSize = true };
            btnShowOverlay.Click += (s, e) => { overlayForm.Show(ActGlobals.oFormActMain); btnShowOverlay.Enabled = false; btnHideOverlay.Enabled = true; };

            btnHideOverlay = new Button { Text = "Hide Overlay", AutoSize = true };
            btnHideOverlay.Click += (s, e) => { overlayForm.Hide(); btnShowOverlay.Enabled = true; btnHideOverlay.Enabled = false; };

            var gbOverlay = new GroupBox { Text = "Overlay", AutoSize = false, Padding = new Padding(3, 14, 3, 3), Width = 300, Height = 80 };
            var flowButtons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Location = new System.Drawing.Point(3, 16) };
            flowButtons.Controls.Add(btnShowOverlay);
            flowButtons.Controls.Add(btnHideOverlay);
            gbOverlay.Controls.Add(flowButtons);
            gbOverlay.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // Create the Coercive Healing checkbox and add it to a group box
            var gbCalculateCount = new GroupBox { Text = "Calculate", AutoSize = false, Padding = new Padding(3, 14, 3, 3), Width = 300, Height = 80 };
            var flowCount = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, Location = new System.Drawing.Point(3, 16) };
            chkCoerciveHealing = new CheckBox { Text = "Coercive Healing", AutoSize = true };
            chkEofRaid2Set = new CheckBox { Text = "Templar EoF Raid 2-Set", AutoSize = true };
            flowCount.Controls.Add(chkCoerciveHealing);
            flowCount.Controls.Add(chkEofRaid2Set);
            gbCalculateCount.Controls.Add(flowCount);

            // Create the Class selection dropdown and add it to a group box
            var gbClass = new GroupBox { Text = "Class", AutoSize = false, Padding = new Padding(3, 14, 3, 3), Width = 300, Height = 80 };

            cmbClass = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new System.Drawing.Point(3, 16), Width = 200 };
            cmbClass.Items.AddRange(new object[] { "Defiler", "Inquisitor", "Mystic", "Templar" });
            cmbClass.SelectedItem = "Inquisitor";

            gbClass.Controls.Add(cmbClass);

            // Create warning threshold controls and add them to a group box
            var gbWarning = new GroupBox { Text = "Warning", AutoSize = false, Padding = new Padding(3, 14, 3, 3), Width = 300, Height = 80 };
            var warningTable = new TableLayoutPanel { ColumnCount = 2, RowCount = 2, Dock = DockStyle.Fill, AutoSize = false };
            warningTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            warningTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            warningTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            warningTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            var lblWarningCount = new Label { Text = "Count <=", AutoSize = false, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
            nudWarningCountThreshold = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 10,
                Value = DefaultWarningCountThreshold,
                Dock = DockStyle.Fill
            };

            var lblWarningSeconds = new Label { Text = "Seconds <", AutoSize = false, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
            nudWarningSecondsThreshold = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 30,
                Value = DefaultWarningSecondsThreshold,
                Dock = DockStyle.Fill
            };

            nudWarningCountThreshold.ValueChanged += (s, e) => ApplyWarningThresholdsToPlayers();
            nudWarningSecondsThreshold.ValueChanged += (s, e) => ApplyWarningThresholdsToPlayers();

            warningTable.Controls.Add(lblWarningCount, 0, 0);
            warningTable.Controls.Add(nudWarningCountThreshold, 1, 0);
            warningTable.Controls.Add(lblWarningSeconds, 0, 1);
            warningTable.Controls.Add(nudWarningSecondsThreshold, 1, 1);
            gbWarning.Controls.Add(warningTable);

            // Add the group boxes to the flow panel
            flowPanel.Controls.Add(gbOverlay);
            flowPanel.Controls.Add(gbCalculateCount);
            flowPanel.Controls.Add(gbClass);
            flowPanel.Controls.Add(gbWarning);

            this.Controls.Add(flowPanel);

            // Add the flow panel to the ACT Tab page
            tabReactive.Controls.Add(this);

            // Create the overlay form and set up event handlers
            overlayForm = new frmReactiveOverlay();
            overlayForm.SetPlayerControls(_playerControls);
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

            CheckSingleReacStart(line);

            CheckSingleReacProc(line);

            CheckGroupReacStart(line);

            CheckkGroupReacProc(line);

            CheckWhoGroup(line);

            // CheckSingleReacExpiry(line);

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
            if (rxSingleReacExpiry.IsMatch(line ))
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
