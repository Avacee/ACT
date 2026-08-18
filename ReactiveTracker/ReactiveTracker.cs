using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Xml;

using Advanced_Combat_Tracker;

using ReactiveTracker.Models;
using ReactiveTracker.Presenters;

using static System.Windows.Forms.LinkLabel;

namespace ReactiveTracker
{
    public class ReactiveTracker : UserControl, IActPluginV1
    {
        private const int DefaultSingleCount = 5;
        private const int DefaultGroupCount = 3;
        private static readonly string SettingsFile = System.IO.Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "CureFinderSettings.xml");

        Label lblStatus;
        TabPage tabReactive;
        frmReactiveOverlay overlayForm;
        Button btnShowOverlay;
        Button btnHideOverlay;
        CheckBox chkCoerciveHealing;
        CheckBox chkEofRaid2Set;
        ComboBox cmbClass;
        ctrlPlayer[] _playerControls;
        private const string playerNameEmpty = "N/A";
        PlayerModel[] _players;
        const int _whoGroupStartIndex = 1;
        const int _whoGroupMaxIndex = 5;
        int _whoGroupIndex = _whoGroupStartIndex; // 0 = not currently in a /who group sequence
        ReactivePresenter _reactivePresenter;
        const string logTimeStampRegexStr = @"\(\d{10}\)\[.{24}\] ";


        private int SingleCount => DefaultSingleCount + (chkCoerciveHealing.Checked ? 1 : 0) + (chkEofRaid2Set.Checked && cmbClass.SelectedItem?.ToString() == "Templar" ? 4 : 0);
        private int GroupCount => DefaultGroupCount + (chkCoerciveHealing.Checked ? 1 : 0);

        //Create the RegEx once
        private readonly Regex rxSingleProcInq = new Regex(logTimeStampRegexStr + @"(YOUR Vengeful Faith heals (?<victim>.+?) for (a critical of )?(?<damage>[\d,\.KMBTQ]+) hit points?\.)", RegexOptions.Compiled);
        private readonly Regex rxSingleProcTemp = new Regex(logTimeStampRegexStr + @"(YOUR Supplicant's Prayer heals (?<victim>.+?) for (a critical of )?(?<damage>[\d,\.KMBTQ]+) hit points?\.)", RegexOptions.Compiled);
        private readonly Regex rxGroupProcInq = new Regex(logTimeStampRegexStr + @"(YOUR Atoning Faith heals (?<victim>.+?) for (a critical of )?(?<damage>[\d,\.KMBTQ]+) hit points?\.)", RegexOptions.Compiled);
        private readonly Regex rxGroupProcTemp = new Regex(logTimeStampRegexStr + @"(YOUR Divine Prayer heals (?<victim>.+?) for (a critical of )?(?<damage>[\d,\.KMBTQ]+) hit points?\.)", RegexOptions.Compiled);
        private const string races = "Aerakyn|Arasai|Barbarian|Dark Elf|Dwarf|Erudite|Fae|Freeblood|Froglok|Gnome|Half Elf|Halfling|High Elf|Human|Iksar|Kerra|Ogre|Ratonga|Sarnak|Troll|Vah Shir|Wood Elf";
        private const string classes = "Fighter|Warrior|Berserker|Guardian|Brawler|Bruiser|Monk|Crusader|Paladin|Shadowknight|Mage|Enchanter|Coercer|Illusionist|Sorceror|Warlock|Wizard|Summoner|Conjuror|Necromancer|Priest|Cleric|Inquisitor|Templar|Druid|Fury|Warden|Shaman|Defiler|Mystic|Shaper|Channeler|Scout|Bard|Dirge|Troubador|Predator|Assassin|Ranger|Rogue|Brigand|Swashbuckler|Animalist|Beastlord";
        private string whogroupRegexStr = "";
        private Regex rxWhoGroup = null;
        private bool bInWhoGroup = false;

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
                var root = doc.CreateElement("CureFinderSettings");
                doc.AppendChild(root);

                if (overlayForm != null)
                {
                    var overlay = doc.CreateElement("OverlayPosition");
                    overlay.SetAttribute("X", overlayForm.Location.X.ToString());
                    overlay.SetAttribute("Y", overlayForm.Location.Y.ToString());
                    overlay.SetAttribute("Width", overlayForm.Width.ToString());
                    overlay.SetAttribute("Height", overlayForm.Height.ToString());
                    overlay.SetAttribute("Visible", overlayForm.Visible.ToString());
                    root.AppendChild(overlay);
                }

                var options = doc.CreateElement("Options");
                options.SetAttribute("CoerciveHealing", chkCoerciveHealing.Checked.ToString());
                options.SetAttribute("EofRaid2Set", chkEofRaid2Set.Checked.ToString());
                options.SetAttribute("Class", cmbClass.SelectedItem?.ToString() ?? "Inquisitor");
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

                var overlayNode = doc.SelectSingleNode("/CureFinderSettings/OverlayPosition");
                if (overlayNode != null && overlayForm != null)
                {
                    int x = int.Parse(overlayNode.Attributes["X"].Value);
                    int y = int.Parse(overlayNode.Attributes["Y"].Value);
                    int width = int.Parse(overlayNode.Attributes["Width"].Value);
                    int height = int.Parse(overlayNode.Attributes["Height"].Value);
                    overlayForm.Location = new System.Drawing.Point(x, y);
                    overlayForm.Size = new System.Drawing.Size(width, height);

                    var visAttr = overlayNode.Attributes["Visible"];
                    if (visAttr != null && !bool.Parse(visAttr.Value))
                        overlayForm.Hide();
                }

                var optionsNode = doc.SelectSingleNode("/CureFinderSettings/Options");
                if (optionsNode != null)
                {
                    var coerciveAttr = optionsNode.Attributes["CoerciveHealing"];
                    if (coerciveAttr != null)
                        chkCoerciveHealing.Checked = bool.Parse(coerciveAttr.Value);

                    var eofRaid2Attr = optionsNode.Attributes["EofRaid2Set"];
                    if (eofRaid2Attr != null)
                        chkEofRaid2Set.Checked = bool.Parse(eofRaid2Attr.Value);

                    var classAttr = optionsNode.Attributes["Class"];
                    if (classAttr != null && cmbClass.Items.Contains(classAttr.Value))
                        cmbClass.SelectedItem = classAttr.Value;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error loading settings: " + ex.Message;
            }
        }

        public void InitPlugin(System.Windows.Forms.TabPage pluginScreenSpace, System.Windows.Forms.Label pluginStatusText)
        {
            lblStatus = pluginStatusText;
            lblStatus.Text = "Plugin Started";

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
            var flowPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown };

            // Create the Show and Hide Overlay buttons and add them to a group box
            btnShowOverlay = new Button { Text = "Show Overlay", AutoSize = true };
            btnShowOverlay.Click += (s, e) => { overlayForm.Show(ActGlobals.oFormActMain); btnShowOverlay.Enabled = false; btnHideOverlay.Enabled = true; };

            btnHideOverlay = new Button { Text = "Hide Overlay", AutoSize = true };
            btnHideOverlay.Click += (s, e) => { overlayForm.Hide(); btnShowOverlay.Enabled = true; btnHideOverlay.Enabled = false; };

            var gbOverlay = new GroupBox { Text = "Overlay", AutoSize = true, Padding = new Padding(3, 14, 3, 3) };
            var flowButtons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Location = new System.Drawing.Point(3, 16) };
            flowButtons.Controls.Add(btnShowOverlay);
            flowButtons.Controls.Add(btnHideOverlay);
            gbOverlay.Controls.Add(flowButtons);
            gbOverlay.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            gbOverlay.Font = new System.Drawing.Font(gbOverlay.Font, System.Drawing.FontStyle.Bold);

            // Create the Coercive Healing checkbox and add it to a group box
            var gbCount = new GroupBox { Text = "Calculate", AutoSize = true, Padding = new Padding(3, 14, 3, 3) };
            var flowCount = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, Location = new System.Drawing.Point(3, 16) };
            chkCoerciveHealing = new CheckBox { Text = "Coercive Healing", AutoSize = true };
            chkEofRaid2Set = new CheckBox { Text = "Templar EoF Raid 2-Set", AutoSize = true };
            flowCount.Controls.Add(chkCoerciveHealing);
            flowCount.Controls.Add(chkEofRaid2Set);
            gbCount.Controls.Add(flowCount);

            // Create the Class selection dropdown and add it to a group box
            var gbClass = new GroupBox { Text = "Class", AutoSize = true, Padding = new Padding(3, 14, 3, 3) };

            cmbClass = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new System.Drawing.Point(3, 16) };
            cmbClass.Items.AddRange(new object[] { "Defiler", "Inquisitor", "Mystic", "Templar" });
            cmbClass.SelectedItem = "Inquisitor";

            gbClass.Controls.Add(cmbClass);

            // Add the group boxes to the flow panel
            flowPanel.Controls.Add(gbOverlay);
            flowPanel.Controls.Add(gbCount);
            flowPanel.Controls.Add(gbClass);

            this.Controls.Add(flowPanel);

            // Add the flow panel to the ACT Tab page
            tabReactive.Controls.Add(this);

            // Create the overlay form and set up event handlers
            overlayForm = new frmReactiveOverlay();
            overlayForm.SetPlayerControls(_playerControls);
            overlayForm.VisibleChanged += (s, e) =>
            {
                btnShowOverlay.Enabled = !overlayForm.Visible;
                btnHideOverlay.Enabled = overlayForm.Visible;
            };
            overlayForm.Show(ActGlobals.oFormActMain);
            LoadSettings();
            btnShowOverlay.Enabled = !overlayForm.Visible;
            btnHideOverlay.Enabled = overlayForm.Visible;
        }

        private void OFormActMain_OnLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            if (isImport) return; // Ignore imported logs

            //Debug.WriteLine(logInfo.logLine.ToString());
            var line = logInfo.logLine.ToString();

            CheckSingleReacStart(line);

            CheckSingleReacProc(line);

            CheckGroupReacStart(line);

            CheckkGroupReacProc(line);

            CheckWhoGroup(line);
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
                        // Inquisitor: "You demand retribution for any wrongs done to YOU."
                        rxSingleReacStart = new Regex(logTimeStampRegexStr + $"You demand retribution for any wrongs done to ({ActGlobals.charName}|{_players[i].Name})");
                        break;
                    case "Templar":
                        // Templar: "You demand retribution for any wrongs done to YOU."
                        rxSingleReacStart = new Regex(logTimeStampRegexStr + $"You pray for ({ActGlobals.charName}'s|{_players[i].Name}) body and soul.");
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
            // Single reactive Proc: Normal: YOUR Vengeful Faith heals YOU for X hit points.
            // Single reactive Proc: Critical: YOUR Vengeful Faith heals YOU for a critical of X hit points.

            switch (cmbClass.SelectedItem.ToString())
            {
                case "Inquisitor":
                    if (rxSingleProcInq.IsMatch(line))
                    {
                        var match = rxSingleProcInq.Match(line);
                        var victim = match.Groups["victim"].Value;
                        for (int i = 0; i < _players.Length; i++)
                        {
                            if (victim == _players[i].Name || victim == "YOU")
                            {
                                _reactivePresenter.UseSingle(i);
                                break;
                            }
                        }
                    }
                    break;
                case "Templar":
                    if (rxSingleProcTemp.IsMatch(line))
                    {
                        var match = rxSingleProcTemp.Match(line);
                        var victim = match.Groups["victim"].Value;
                        for (int i = 0; i < _players.Length; i++)
                        {
                            if (victim == _players[i].Name || victim == "YOU")
                            {
                                _reactivePresenter.UseSingle(i);
                                break;
                            }
                        }
                    }
                    break;
            }
        }

        private void CheckGroupReacStart(string line)
        {
            // Group reactive: "You raise your voice in a Malevolent Diatribe."
            //if (line.Contains($"You pray devoutly for Holy Intercession."))
            //{
            //    for (int i = 0; i < _players.Length; i++)
            //        _reactivePresenter.StartGroup(i, GroupCount);
            //}
            Regex rxGroupeReacStart;
            //Switch on cmbClass.selectedItem to determine which regex to use for the single reactive start
            switch (cmbClass.SelectedItem.ToString())
            {
                case "Inquisitor":
                    rxGroupeReacStart = new Regex(logTimeStampRegexStr + $"You raise your voice in a Malevolent Diatribe.");
                    break;
                case "Templar":
                    // Templar: "You demand retribution for any wrongs done to YOU."
                    rxGroupeReacStart = new Regex(logTimeStampRegexStr + $"You pray devoutly for Holy Intercession.");
                    break;
                default:
                    return; // Unknown class, do nothing
            }
            if (rxGroupeReacStart.IsMatch(line))
            {
                for (int i = 0; i < _players.Length; i++)
                {
                    _reactivePresenter.StartGroup(i, GroupCount);
                }
            }
        }

        private void CheckkGroupReacProc(string line)
        {
            // Group reactive Proc: Normal: YOUR Atoning Faith heals YOU for X hit points.
            // Group reactive Proc: Critical: YOUR Atoning Faith heals YOU for a critical of X hit points.
            switch (cmbClass.SelectedItem.ToString())
            {
                case "Inquisitor":
                    {
                        if (rxGroupProcInq.IsMatch(line))
                        {
                            var match = rxGroupProcInq.Match(line);
                            var victim = match.Groups["victim"].Value;
                            for (int i = 0; i < _players.Length; i++)
                            {
                                if (victim == _players[i].Name || victim == "YOU")
                                {
                                    _reactivePresenter.UseGroup(i);
                                    break;
                                }
                            }
                        }
                        break;
                    }
                case "Templar":
                    {
                        if (rxGroupProcTemp.IsMatch(line))
                        {
                            var match = rxGroupProcTemp.Match(line);
                            var victim = match.Groups["victim"].Value;
                            for (int i = 0; i < _players.Length; i++)
                            {
                                if (victim == _players[i].Name || victim == "YOU")
                                {
                                    _reactivePresenter.UseGroup(i);
                                    break;
                                }
                            }
                        }
                        break;
                    }

            }
        }


        private void CheckWhoGroup(string line)
        {
            //Whogroup

            if (rxWhoGroup.IsMatch(line))
            {
                if (bInWhoGroup)
                    Debug.WriteLine($"WhoGroup started");
                else
                    Debug.WriteLine($"WhoGroup continuing");

                bInWhoGroup = true;
                Match match = rxWhoGroup.Match(line);
                var playerName = match.Groups["playerName"].Value;

                // First match starts at player[1]; subsequent consecutive matches fill up to player[_whoGroupMaxIndex]
                if (_whoGroupIndex < _players.Length && _whoGroupIndex <= _whoGroupMaxIndex)
                {
                    _players[_whoGroupIndex].Name = playerName;
                    _whoGroupIndex++;
                }

                Debug.WriteLine($"WhoGroup {_whoGroupIndex - 1} name is {playerName}");
            }
            else
            {
                if (bInWhoGroup)
                {
                    // A non-matching line broke the sequence — fill remaining slots with Player X
                    for (int i = _whoGroupIndex; i <= _whoGroupMaxIndex && i < _players.Length; i++)
                        _players[i].Name = playerNameEmpty;
                    _whoGroupIndex = _whoGroupStartIndex;
                    bInWhoGroup = false;
                    for (int i = 0; i < _players.Length; i++)
                        Debug.WriteLine(_players[i].Name);
                    Debug.WriteLine($"WhoGroup ended");
                }
            }
        }


        private void OFormActMain_BeforeCombatAction(bool isImport, CombatActionEventArgs actionInfo)
        {

        }

        private void OFormActMain_AfterCombatAction(bool isImport, CombatActionEventArgs actionInfo)
        {
            //Debug.WriteLine($"Attacker:{actionInfo.attacker} Victim:{actionInfo.victim}");
        }

        private void OFormActMain_OnCombatStart(bool isImport, CombatToggleEventArgs encounterInfo)
        {

        }

        private void OFormActMain_OnCombatEnd(bool isImport, CombatToggleEventArgs encounterInfo)
        {
            //_reactivePresenter?.ResetAll();
        }
    }
}
