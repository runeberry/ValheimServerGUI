using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ValheimServerGUI.Controls;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class WorldPreferencesForm : Form
    {
        private readonly IWorldPreferencesProvider WorldPrefsProvider;
        private readonly ILogger Logger;

        private string WorldName { get; set; }

        private const string KeywordNoPreset = "No Preset";
        private const string KeywordNoModifierValue = "";

        #region Modifier value mappings

        private static readonly Dictionary<string, string> PresetMapping = new()
        {
            { KeywordNoPreset, "" },
            { "Casual", WorldGenPresets.Casual },
            { "Easy", WorldGenPresets.Easy },
            { "Normal", WorldGenPresets.Normal },
            { "Hard", WorldGenPresets.Hard },
            { "Hardcore", WorldGenPresets.Hardcore },
            { "Immersive", WorldGenPresets.Immersive },
            { "Hammer", WorldGenPresets.Hammer },
        };

        private static readonly Dictionary<string, string> ModifierCombatMapping = new()
        {
            { KeywordNoModifierValue, "" },
            { "Very Easy", WorldGenModifiers.Values.CombatVeryEasy },
            { "Easy", WorldGenModifiers.Values.CombatEasy },
            { "Hard", WorldGenModifiers.Values.CombatHard },
            { "Very Hard", WorldGenModifiers.Values.CombatVeryHard },
        };

        private static readonly Dictionary<string, string> ModifierDeathPenaltyMapping = new()
        {
            { KeywordNoModifierValue, "" },
            { "Casual", WorldGenModifiers.Values.DeathPenaltyCasual },
            { "Very Easy", WorldGenModifiers.Values.DeathPenaltyVeryEasy },
            { "Easy", WorldGenModifiers.Values.DeathPenaltyEasy },
            { "Hard", WorldGenModifiers.Values.DeathPenaltyHard },
            { "Hardcore", WorldGenModifiers.Values.DeathPenaltyHardcore },
        };

        private static readonly Dictionary<string, string> ModifierResourcesMapping = new()
        {
            { KeywordNoModifierValue, "" },
            { "Much Less", WorldGenModifiers.Values.ResourcesMuchLess },
            { "Less", WorldGenModifiers.Values.ResourcesLess },
            { "More", WorldGenModifiers.Values.ResourcesMore },
            { "Much More", WorldGenModifiers.Values.ResourcesMuchMore },
            { "Most", WorldGenModifiers.Values.ResourcesMost },
        };

        private static readonly Dictionary<string, string> ModifierRaidsMapping = new()
        {
            { KeywordNoModifierValue, "" },
            { "None", WorldGenModifiers.Values.RaidsNone },
            { "Much Less", WorldGenModifiers.Values.RaidsMuchLess },
            { "Less", WorldGenModifiers.Values.RaidsLess },
            { "More", WorldGenModifiers.Values.RaidsMore },
            { "Much More", WorldGenModifiers.Values.RaidsMuchMore },
        };

        private static readonly Dictionary<string, string> ModifierPortalsMapping = new()
        {
            { KeywordNoModifierValue, "" },
            { "Casual", WorldGenModifiers.Values.PortalsCasual },
            { "Hard", WorldGenModifiers.Values.PortalsHard },
            { "Very Hard", WorldGenModifiers.Values.PortalsVeryHard },
        };

        #endregion

        public WorldPreferencesForm(IWorldPreferencesProvider worldPrefsProvider, ILogger logger)
        {
            WorldPrefsProvider = worldPrefsProvider;
            Logger = logger;

            InitializeComponent();
            this.AddApplicationIcon();
        }

        /// <summary>
        /// Call this before showing the form.
        /// </summary>
        public void SetWorld(string worldName)
        {
            WorldName = worldName;
            Text = $"World Settings - {WorldName}";
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (string.IsNullOrWhiteSpace(WorldName))
            {
                Logger.Error("Unable to open world settings. No world name has been set.");
                Close();
                return;
            }

            InitializeDropDownOptions();
            SetFormDefaultValues();
            LoadFormFromPreferences();
        }

        private void InitializeDropDownOptions()
        {
            PresetFormField.DataSource = PresetMapping.Keys.ToList();

            ModifierCombatFormField.DataSource = ModifierCombatMapping.Keys.ToList();
            ModifierDeathPenaltyFormField.DataSource = ModifierDeathPenaltyMapping.Keys.ToList();
            ModifierResourcesFormField.DataSource = ModifierResourcesMapping.Keys.ToList();
            ModifierRaidsFormField.DataSource = ModifierRaidsMapping.Keys.ToList();
            ModifierPortalsFormField.DataSource = ModifierPortalsMapping.Keys.ToList();
        }

        private void SetFormDefaultValues()
        {
            PresetFormField.Value = KeywordNoPreset;

            ModifierCombatFormField.Value = KeywordNoModifierValue;
            ModifierDeathPenaltyFormField.Value = KeywordNoModifierValue;
            ModifierResourcesFormField.Value = KeywordNoModifierValue;
            ModifierRaidsFormField.Value = KeywordNoModifierValue;
            ModifierPortalsFormField.Value = KeywordNoModifierValue;

            KeyNoBuildCostFormField.Value = false;
            KeyPlayerEventsFormField.Value = false;
            KeyPassiveMobsFormField.Value = false;
            KeyNoMapFormField.Value = false;
        }

        #region Load methods

        private void LoadFormFromPreferences()
        {
            var prefs = WorldPrefsProvider.LoadPreferences(WorldName);

            // No preferences exist for this world, so there are no values to initialize
            // A new prefs object will be saved when OK is clicked
            if (prefs == null) return;

            if (!string.IsNullOrWhiteSpace(prefs.Preset))
            {
                LoadPresetFromPreferences(prefs, PresetFormField, PresetMapping.Invert());
            }
            else
            {
                LoadModifierFromPreferences(prefs, WorldGenModifiers.Combat, ModifierCombatFormField, ModifierCombatMapping.Invert());
                LoadModifierFromPreferences(prefs, WorldGenModifiers.DeathPenalty, ModifierDeathPenaltyFormField, ModifierDeathPenaltyMapping.Invert());
                LoadModifierFromPreferences(prefs, WorldGenModifiers.Resources, ModifierResourcesFormField, ModifierResourcesMapping.Invert());
                LoadModifierFromPreferences(prefs, WorldGenModifiers.Raids, ModifierRaidsFormField, ModifierRaidsMapping.Invert());
                LoadModifierFromPreferences(prefs, WorldGenModifiers.Portals, ModifierPortalsFormField, ModifierPortalsMapping.Invert());
            }

            LoadKeyFromPreferences(prefs, WorldGenKeys.NoBuildCost, KeyNoBuildCostFormField);
            LoadKeyFromPreferences(prefs, WorldGenKeys.PlayerEvents, KeyPlayerEventsFormField);
            LoadKeyFromPreferences(prefs, WorldGenKeys.PassiveMobs, KeyPassiveMobsFormField);
            LoadKeyFromPreferences(prefs, WorldGenKeys.NoMap, KeyNoMapFormField);
        }

        private void LoadPresetFromPreferences(WorldPreferences prefs, DropdownFormField dropdown, Dictionary<string, string> mapping)
        {
            // Clear the existing value
            dropdown.Value = KeywordNoPreset;

            // Return early if no preset is defined on the prefs object
            if (string.IsNullOrWhiteSpace(prefs.Preset)) return;

            else if (mapping.TryGetValue(prefs.Preset, out var presetDisplayName))
            {
                dropdown.Value = presetDisplayName;
            }
            else
            {
                // Would occur if a user manually wrote an unknown preset name to the prefs file
                Logger.Error("Unable to load preset: no value is mapped to '{presetName}'", prefs.Preset);
            }
        }

        private void LoadModifierFromPreferences(WorldPreferences prefs, string modifierName, DropdownFormField dropdown, Dictionary<string, string> mapping)
        {
            // Clear the existing value
            dropdown.Value = KeywordNoModifierValue;

            // Return early if this modifier is not set on the prefs object
            if (prefs.Modifiers == null || !prefs.Modifiers.TryGetValue(modifierName, out var modifierValue)) return;

            if (mapping.TryGetValue(modifierValue, out var modifierValueDisplayName))
            {
                dropdown.Value = modifierValueDisplayName;
            }
            else
            {
                // Would occur if a user manually wrote an invalid value for this modifier in the prefs file
                Logger.Error("Unable to load modifier: no value is mapped to '{modifierValue}'", modifierValue);
            }
        }

        private static void LoadKeyFromPreferences(WorldPreferences prefs, string keyName, CheckboxFormField checkbox)
        {
            checkbox.Value = prefs.Keys != null && prefs.Keys.Contains(keyName);
        }

        #endregion

        #region Save methods

        private void SaveFormToPreferences()
        {
            var prefs = WorldPrefsProvider.LoadPreferences(WorldName);
            prefs ??= new WorldPreferences
            {
                WorldName = WorldName,
            };

            prefs.Preset = null;
            prefs.Modifiers.Clear();
            prefs.Keys.Clear();

            if (PresetFormField.Value != KeywordNoPreset)
            {
                SavePresetToPreferences(prefs, PresetFormField, PresetMapping);
            }
            else
            {
                SaveModifierToPreferences(prefs, WorldGenModifiers.Combat, ModifierCombatFormField, ModifierCombatMapping);
                SaveModifierToPreferences(prefs, WorldGenModifiers.DeathPenalty, ModifierDeathPenaltyFormField, ModifierDeathPenaltyMapping);
                SaveModifierToPreferences(prefs, WorldGenModifiers.Resources, ModifierResourcesFormField, ModifierResourcesMapping);
                SaveModifierToPreferences(prefs, WorldGenModifiers.Raids, ModifierRaidsFormField, ModifierRaidsMapping);
                SaveModifierToPreferences(prefs, WorldGenModifiers.Portals, ModifierPortalsFormField, ModifierPortalsMapping);

                SaveKeyToPreferences(prefs, WorldGenKeys.NoBuildCost, KeyNoBuildCostFormField);
                SaveKeyToPreferences(prefs, WorldGenKeys.PlayerEvents, KeyPlayerEventsFormField);
                SaveKeyToPreferences(prefs, WorldGenKeys.PassiveMobs, KeyPassiveMobsFormField);
                SaveKeyToPreferences(prefs, WorldGenKeys.NoMap, KeyNoMapFormField);
            }

            WorldPrefsProvider.SavePreferences(prefs);
        }

        private void SavePresetToPreferences(WorldPreferences prefs, DropdownFormField dropdown, Dictionary<string, string> mapping)
        {
            if (dropdown.Value == null || dropdown.Value == KeywordNoPreset) return;

            if (mapping.TryGetValue(dropdown.Value, out var value))
            {
                prefs.Preset = value;
            }
            else
            {
                // Would only occur if the mapping in this file is missing an entry
                Logger.Error("Unable to save preset: no value is mapped to '{presetName}'", dropdown.Value);
            }
        }

        private void SaveModifierToPreferences(WorldPreferences prefs, string modifierName, DropdownFormField dropdown, Dictionary<string, string> mapping)
        {
            if (dropdown.Value == null || dropdown.Value == KeywordNoModifierValue) return;

            if (mapping.TryGetValue(dropdown.Value, out var value))
            {
                prefs.Modifiers[modifierName] = value;
            }
            else
            {
                // Would only occur if the mapping in this file is missing an entry
                Logger.Error("Unable to save modifier: no value is mapped to '{valueName}' for modifier '{modifierName}'", dropdown.Value, modifierName);
            }
        }

        private static void SaveKeyToPreferences(WorldPreferences prefs, string keyName, CheckboxFormField checkbox)
        {
            if (!checkbox.Value) return;

            prefs.Keys.Add(keyName);
        }

        #endregion

        #region Form events

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            SaveFormToPreferences();
            Close();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ButtonDefaults_Click(object sender, EventArgs e)
        {
            SetFormDefaultValues();
        }

        #endregion
    }
}
