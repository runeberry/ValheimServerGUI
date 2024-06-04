using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ValheimServerGUI.Controls;
using ValheimServerGUI.Game;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class WorldPreferencesForm : Form
    {
        private readonly IWorldPreferencesProvider WorldPrefsProvider;
        private readonly ILogger Logger;

        private string WorldName { get; set; }
        private bool PresetReactToFormChanges { get; set; }
        private bool EnablePresetModifierPopulation { get; set; } = true;

        #region Modifier value mappings

        private const string NoValue = "";

        private static class DisplayNames
        {
            public const string NoPreset = "Custom (No Preset)";
            public const string NoModifier = "Normal";

            public const string PresetEasy = "Easy";
            public const string PresetNormal = "Normal";
            public const string PresetHard = "Hard";
            public const string PresetHardcore = "Hardcore";
            public const string PresetCasual = "Casual";
            public const string PresetHammer = "Hammer Mode (Creative)";
            public const string PresetImmersive = "Immersive";

            public const string CombatVeryEasy = "Very Easy";
            public const string CombatEasy = "Easy";
            public const string CombatHard = "Hard";
            public const string CombatVeryHard = "Very Hard";

            public const string DeathPenaltyCasual = "Casual";
            public const string DeathPenaltyVeryEasy = "Very Easy";
            public const string DeathPenaltyEasy = "Easy";
            public const string DeathPenaltyHard = "Hard";
            public const string DeathPenaltyHardcore = "Hardcore";

            public const string ResourcesMuchLess = "Much Less (0.5x)";
            public const string ResourcesLess = "Less (0.75x)";
            public const string ResourcesMore = "More (1.5x)";
            public const string ResourcesMuchMore = "Much More (2x)";
            public const string ResourcesMost = "Most (3x)";

            public const string RaidsNone = "None";
            public const string RaidsMuchLess = "Much Less";
            public const string RaidsLess = "Less";
            public const string RaidsMore = "More";
            public const string RaidsMuchMore = "Much More";

            public const string PortalsCasual = "Casual (Portal items)";
            public const string PortalsHard = "Hard (No boss portals)";
            public const string PortalsVeryHard = "Very Hard (No portals)";
        }

        private static readonly Dictionary<string, string> PresetMappingByDisplayName = new()
        {
            { DisplayNames.NoPreset, NoValue },
            { DisplayNames.PresetEasy, WorldGenPresets.Easy },
            { DisplayNames.PresetNormal, WorldGenPresets.Normal },
            { DisplayNames.PresetHard, WorldGenPresets.Hard },
            { DisplayNames.PresetHardcore, WorldGenPresets.Hardcore },
            { DisplayNames.PresetCasual, WorldGenPresets.Casual },
            { DisplayNames.PresetHammer, WorldGenPresets.Hammer },
            { DisplayNames.PresetImmersive, WorldGenPresets.Immersive },
        };
        private static readonly Dictionary<string, string> PresetMappingByValue = PresetMappingByDisplayName.Invert();

        private static readonly Dictionary<string, string> ModifierCombatMappingByDisplayName = new()
        {
            { DisplayNames.CombatVeryEasy, WorldGenModifiers.Values.CombatVeryEasy },
            { DisplayNames.CombatEasy, WorldGenModifiers.Values.CombatEasy },
            { DisplayNames.NoModifier, NoValue },
            { DisplayNames.CombatHard, WorldGenModifiers.Values.CombatHard },
            { DisplayNames.CombatVeryHard, WorldGenModifiers.Values.CombatVeryHard },
        };
        private static readonly Dictionary<string, string> ModifierCombatMappingByValue = ModifierCombatMappingByDisplayName.Invert();

        private static readonly Dictionary<string, string> ModifierDeathPenaltyMappingByDisplayName = new()
        {
            { DisplayNames.DeathPenaltyCasual, WorldGenModifiers.Values.DeathPenaltyCasual },
            { DisplayNames.DeathPenaltyVeryEasy, WorldGenModifiers.Values.DeathPenaltyVeryEasy },
            { DisplayNames.DeathPenaltyEasy, WorldGenModifiers.Values.DeathPenaltyEasy },
            { DisplayNames.NoModifier, NoValue },
            { DisplayNames.DeathPenaltyHard, WorldGenModifiers.Values.DeathPenaltyHard },
            { DisplayNames.DeathPenaltyHardcore, WorldGenModifiers.Values.DeathPenaltyHardcore },
        };
        private static readonly Dictionary<string, string> ModifierDeathPenaltyMappingByValue = ModifierDeathPenaltyMappingByDisplayName.Invert();

        private static readonly Dictionary<string, string> ModifierResourcesMappingByDisplayName = new()
        {
            { DisplayNames.ResourcesMuchLess, WorldGenModifiers.Values.ResourcesMuchLess },
            { DisplayNames.ResourcesLess, WorldGenModifiers.Values.ResourcesLess },
            { DisplayNames.NoModifier, NoValue },
            { DisplayNames.ResourcesMore, WorldGenModifiers.Values.ResourcesMore },
            { DisplayNames.ResourcesMuchMore, WorldGenModifiers.Values.ResourcesMuchMore },
            { DisplayNames.ResourcesMost, WorldGenModifiers.Values.ResourcesMost },
        };
        private static readonly Dictionary<string, string> ModifierResourcesMappingByValue = ModifierResourcesMappingByDisplayName.Invert();

        private static readonly Dictionary<string, string> ModifierRaidsMappingByDisplayName = new()
        {
            { DisplayNames.RaidsNone, WorldGenModifiers.Values.RaidsNone },
            { DisplayNames.RaidsMuchLess, WorldGenModifiers.Values.RaidsMuchLess },
            { DisplayNames.RaidsLess, WorldGenModifiers.Values.RaidsLess },
            { DisplayNames.NoModifier, NoValue },
            { DisplayNames.RaidsMore, WorldGenModifiers.Values.RaidsMore },
            { DisplayNames.RaidsMuchMore, WorldGenModifiers.Values.RaidsMuchMore },
        };
        private static readonly Dictionary<string, string> ModifierRaidsMappingByValue = ModifierRaidsMappingByDisplayName.Invert();

        private static readonly Dictionary<string, string> ModifierPortalsMappingByDisplayName = new()
        {
            { DisplayNames.PortalsCasual, WorldGenModifiers.Values.PortalsCasual },
            { DisplayNames.NoModifier, NoValue },
            { DisplayNames.PortalsHard, WorldGenModifiers.Values.PortalsHard },
            { DisplayNames.PortalsVeryHard, WorldGenModifiers.Values.PortalsVeryHard },
        };
        private static readonly Dictionary<string, string> ModifierPortalsMappingByValue = ModifierPortalsMappingByDisplayName.Invert();

        private void SetFormValuesFromPreset()
        {
            if (!EnablePresetModifierPopulation) return;

            var currentPresetName = PresetFormField.Value;
            if (currentPresetName == DisplayNames.NoPreset) return;

            PresetReactToFormChanges = false;
            SetFormDefaultValues();

            if (!PresetMappingByDisplayName.TryGetValue(currentPresetName, out var currentPresetValue)) return;

            switch (currentPresetValue)
            {
                case WorldGenPresets.Easy:
                    ModifierCombatFormField.Value = DisplayNames.CombatEasy;
                    ModifierRaidsFormField.Value = DisplayNames.RaidsLess;
                    break;
                case WorldGenPresets.Normal:
                    // no change from defaults
                    break;
                case WorldGenPresets.Hard:
                    ModifierCombatFormField.Value = DisplayNames.CombatHard;
                    ModifierRaidsFormField.Value = DisplayNames.RaidsMore;
                    break;
                case WorldGenPresets.Hardcore:
                    ModifierCombatFormField.Value = DisplayNames.CombatVeryHard;
                    ModifierDeathPenaltyFormField.Value = DisplayNames.DeathPenaltyHardcore;
                    ModifierRaidsFormField.Value = DisplayNames.RaidsMore;
                    ModifierPortalsFormField.Value = DisplayNames.PortalsHard;
                    KeyNoMapFormField.Value = true;
                    break;
                case WorldGenPresets.Casual:
                    ModifierCombatFormField.Value = DisplayNames.CombatVeryEasy;
                    ModifierDeathPenaltyFormField.Value = DisplayNames.DeathPenaltyCasual;
                    ModifierResourcesFormField.Value = DisplayNames.ResourcesMore;
                    ModifierRaidsFormField.Value = DisplayNames.RaidsNone;
                    ModifierPortalsFormField.Value = DisplayNames.PortalsCasual;
                    KeyPlayerEventsFormField.Value = true;
                    KeyPassiveMobsFormField.Value = true;
                    break;
                case WorldGenPresets.Hammer:
                    ModifierRaidsFormField.Value = DisplayNames.RaidsNone;
                    KeyNoBuildCostFormField.Value = true;
                    KeyPassiveMobsFormField.Value = true;
                    break;
                case WorldGenPresets.Immersive:
                    ModifierPortalsFormField.Value = DisplayNames.PortalsVeryHard;
                    KeyNoMapFormField.Value = true;
                    KeyFireFormField.Value = true;
                    break;
                default:
                    break;
            }

            // Preset must be set last so that changes to modifier fields do not trigger
            // the preset to be returned to "Custom"
            EnablePresetModifierPopulation = false;
            PresetFormField.Value = currentPresetName;
            EnablePresetModifierPopulation = true;
            PresetReactToFormChanges = true;
        }

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
            InitializeFormEvents();
            SetFormDefaultValues();
            LoadFormFromPreferences();
        }

        private void InitializeDropDownOptions()
        {
            PresetFormField.DataSource = PresetMappingByDisplayName.Keys.ToList();

            ModifierCombatFormField.DataSource = ModifierCombatMappingByDisplayName.Keys.ToList();
            ModifierDeathPenaltyFormField.DataSource = ModifierDeathPenaltyMappingByDisplayName.Keys.ToList();
            ModifierResourcesFormField.DataSource = ModifierResourcesMappingByDisplayName.Keys.ToList();
            ModifierRaidsFormField.DataSource = ModifierRaidsMappingByDisplayName.Keys.ToList();
            ModifierPortalsFormField.DataSource = ModifierPortalsMappingByDisplayName.Keys.ToList();
        }

        private void InitializeFormEvents()
        {
            PresetFormField.ValueChanged += PresetFormField_ValueChanged;

            ModifierCombatFormField.ValueChanged += ModifierFormField_ValueChanged;
            ModifierDeathPenaltyFormField.ValueChanged += ModifierFormField_ValueChanged;
            ModifierResourcesFormField.ValueChanged += ModifierFormField_ValueChanged;
            ModifierRaidsFormField.ValueChanged += ModifierFormField_ValueChanged;
            ModifierPortalsFormField.ValueChanged += ModifierFormField_ValueChanged;

            KeyNoBuildCostFormField.ValueChanged += KeyFormField_ValueChanged;
            KeyPlayerEventsFormField.ValueChanged += KeyFormField_ValueChanged;
            KeyPassiveMobsFormField.ValueChanged += KeyFormField_ValueChanged;
            KeyNoMapFormField.ValueChanged += KeyFormField_ValueChanged;
            KeyFireFormField.ValueChanged += KeyFormField_ValueChanged;
        }

        private void SetFormDefaultValues()
        {
            PresetFormField.Value = DisplayNames.NoPreset;

            ModifierCombatFormField.Value = DisplayNames.NoModifier;
            ModifierDeathPenaltyFormField.Value = DisplayNames.NoModifier;
            ModifierResourcesFormField.Value = DisplayNames.NoModifier;
            ModifierRaidsFormField.Value = DisplayNames.NoModifier;
            ModifierPortalsFormField.Value = DisplayNames.NoModifier;

            KeyNoBuildCostFormField.Value = false;
            KeyPlayerEventsFormField.Value = false;
            KeyPassiveMobsFormField.Value = false;
            KeyNoMapFormField.Value = false;
            KeyFireFormField.Value = false;
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
                LoadPresetFromPreferences(prefs, PresetFormField, PresetMappingByValue);
                SetFormValuesFromPreset();
            }
            else
            {
                LoadModifierFromPreferences(prefs, WorldGenModifiers.Combat, ModifierCombatFormField, ModifierCombatMappingByValue);
                LoadModifierFromPreferences(prefs, WorldGenModifiers.DeathPenalty, ModifierDeathPenaltyFormField, ModifierDeathPenaltyMappingByValue);
                LoadModifierFromPreferences(prefs, WorldGenModifiers.Resources, ModifierResourcesFormField, ModifierResourcesMappingByValue);
                LoadModifierFromPreferences(prefs, WorldGenModifiers.Raids, ModifierRaidsFormField, ModifierRaidsMappingByValue);
                LoadModifierFromPreferences(prefs, WorldGenModifiers.Portals, ModifierPortalsFormField, ModifierPortalsMappingByValue);

                LoadKeyFromPreferences(prefs, WorldGenKeys.NoBuildCost, KeyNoBuildCostFormField);
                LoadKeyFromPreferences(prefs, WorldGenKeys.PlayerEvents, KeyPlayerEventsFormField);
                LoadKeyFromPreferences(prefs, WorldGenKeys.PassiveMobs, KeyPassiveMobsFormField);
                LoadKeyFromPreferences(prefs, WorldGenKeys.NoMap, KeyNoMapFormField);
                LoadKeyFromPreferences(prefs, WorldGenKeys.Fire, KeyFireFormField);
            }
        }

        private void LoadPresetFromPreferences(WorldPreferences prefs, DropdownFormField dropdown, Dictionary<string, string> mapping)
        {
            // Clear the existing value
            dropdown.Value = DisplayNames.NoPreset;

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
            dropdown.Value = DisplayNames.NoModifier;

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

            if (PresetFormField.Value != DisplayNames.NoPreset)
            {
                SavePresetToPreferences(prefs, PresetFormField, PresetMappingByDisplayName);
            }
            else
            {
                SaveModifierToPreferences(prefs, WorldGenModifiers.Combat, ModifierCombatFormField, ModifierCombatMappingByDisplayName);
                SaveModifierToPreferences(prefs, WorldGenModifiers.DeathPenalty, ModifierDeathPenaltyFormField, ModifierDeathPenaltyMappingByDisplayName);
                SaveModifierToPreferences(prefs, WorldGenModifiers.Resources, ModifierResourcesFormField, ModifierResourcesMappingByDisplayName);
                SaveModifierToPreferences(prefs, WorldGenModifiers.Raids, ModifierRaidsFormField, ModifierRaidsMappingByDisplayName);
                SaveModifierToPreferences(prefs, WorldGenModifiers.Portals, ModifierPortalsFormField, ModifierPortalsMappingByDisplayName);

                SaveKeyToPreferences(prefs, WorldGenKeys.NoBuildCost, KeyNoBuildCostFormField);
                SaveKeyToPreferences(prefs, WorldGenKeys.PlayerEvents, KeyPlayerEventsFormField);
                SaveKeyToPreferences(prefs, WorldGenKeys.PassiveMobs, KeyPassiveMobsFormField);
                SaveKeyToPreferences(prefs, WorldGenKeys.NoMap, KeyNoMapFormField);
                SaveKeyToPreferences(prefs, WorldGenKeys.Fire, KeyFireFormField);
            }

            WorldPrefsProvider.SavePreferences(prefs);
        }

        private void SavePresetToPreferences(WorldPreferences prefs, DropdownFormField dropdown, Dictionary<string, string> mapping)
        {
            if (dropdown.Value == DisplayNames.NoPreset) return;

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
            if (dropdown.Value == DisplayNames.NoModifier) return;

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

        private void PresetFormField_ValueChanged(object sender, string e)
        {
            if (PresetFormField.Value == DisplayNames.NoPreset) return;

            SetFormValuesFromPreset();
        }

        private void UpdatePresetOnValueChange()
        {
            if (PresetFormField.Value == DisplayNames.NoPreset || !PresetReactToFormChanges) return;

            // If any modifiers change after a preset was selected, then that preset is no longer valid
            PresetReactToFormChanges = false;
            PresetFormField.Value = DisplayNames.NoPreset;
        }

        private void KeyFormField_ValueChanged(object sender, bool e)
        {
            UpdatePresetOnValueChange();
        }

        private void ModifierFormField_ValueChanged(object sender, string e)
        {
            UpdatePresetOnValueChange();
        }

        private void WikiLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenHelper.OpenWebAddress(Resources.UrlValheimWikiWorldModifiers);
        }

        private void ModifiersLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenHelper.OpenWebAddress(Resources.UrlHelpWorldModifiers);
        }

        #endregion
    }
}
