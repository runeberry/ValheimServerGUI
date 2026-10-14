using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// World Preferences editor (§6.5). Preset OR modifiers (never both): choosing a preset persists as
/// <c>-preset</c> only; editing any modifier reverts the preset to Custom (the v2.4 feedback-loop guard,
/// applied without a binding storm via a suppression flag). Keys persist independently of the preset
/// (§15 #5 — matches what GenerateArgs emits).
/// </summary>
public partial class WorldPreferencesViewModel : ModalEditViewModel
{
    public const string CustomPreset = "Custom";

    private readonly IWorldPreferencesProvider _provider;
    private readonly IShellLauncher _shell;
    private readonly string _worldName;
    private bool _suppressRevert;

    public WorldPreferencesViewModel(IWorldPreferencesProvider provider, string worldName, IShellLauncher shell)
    {
        _provider = provider;
        _shell = shell;
        _worldName = worldName;

        Presets = new ObservableCollection<string>(WorldGenDisplay.PresetDisplays);

        Modifiers = new ObservableCollection<ModifierRow>(
            WorldGenModifiers.All.Select(key => new ModifierRow(key, OnModifierChanged)));
        Keys = new ObservableCollection<WorldKeyToggle>(
            WorldGenKeys.All.Select(key => new WorldKeyToggle(key, OnKeyChanged)));

        Load();
    }

    /// <summary>Friendly preset names shown in the dropdown (see <see cref="SelectedPresetDisplay"/>).</summary>
    public ObservableCollection<string> Presets { get; }
    public ObservableCollection<ModifierRow> Modifiers { get; }
    public ObservableCollection<WorldKeyToggle> Keys { get; }

    public string WorldName => _worldName;

    /// <summary>The selected preset TOKEN (or <see cref="CustomPreset"/>); the dialog binds the friendly
    /// <see cref="SelectedPresetDisplay"/> instead.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPresetSelected))]
    [NotifyPropertyChangedFor(nameof(SelectedPresetDisplay))]
    private string _selectedPreset = CustomPreset;

    /// <summary>Friendly preset name for the dropdown, mapped to/from the token in <see cref="SelectedPreset"/>.</summary>
    public string SelectedPresetDisplay
    {
        get => WorldGenDisplay.PresetDisplay(SelectedPreset);
        set => SelectedPreset = WorldGenDisplay.PresetToken(value);
    }

    /// <summary>True when a real preset is selected (modifiers are then ignored on save).</summary>
    public bool IsPresetSelected => SelectedPreset != CustomPreset;

    // World Preferences wiki links (parity with the WinForms LinkLabels).
    [RelayCommand] private void OpenWorldModifiersWiki() => _shell.OpenWebAddress(AppConstants.UrlValheimWikiWorldModifiers);
    [RelayCommand] private void OpenWorldModifiersHelp() => _shell.OpenWebAddress(AppConstants.UrlHelpWorldModifiers);

    private void Load() => LoadClean(() =>
    {
        _suppressRevert = true;
        try
        {
            var prefs = _provider.LoadPreferences(_worldName) ?? new WorldPreferences { WorldName = _worldName };

            SelectedPreset = string.IsNullOrEmpty(prefs.Preset) ? CustomPreset : prefs.Preset;

            foreach (var row in Modifiers)
                row.SetSelectedQuiet(prefs.Modifiers.TryGetValue(row.Key, out var v) ? v : null);

            var keys = prefs.Keys ?? new HashSet<string>();
            foreach (var toggle in Keys)
                toggle.IsSet = keys.Contains(toggle.Key);
        }
        finally
        {
            _suppressRevert = false;
        }
    });

    public override void ApplyDefaults()
    {
        _suppressRevert = true;
        try
        {
            SelectedPreset = CustomPreset;
            foreach (var row in Modifiers) row.SetSelectedQuiet(null);
            foreach (var toggle in Keys) toggle.IsSet = false;
        }
        finally
        {
            _suppressRevert = false;
        }
        IsDirty = true;
    }

    public void Save()
    {
        var prefs = _provider.LoadPreferences(_worldName) ?? new WorldPreferences { WorldName = _worldName };

        if (IsPresetSelected)
        {
            prefs.Preset = SelectedPreset;
            prefs.Modifiers = new Dictionary<string, string>(); // a preset persists as -preset only
        }
        else
        {
            prefs.Preset = null;
            prefs.Modifiers = Modifiers
                .Where(m => m.Value is not null)
                .ToDictionary(m => m.Key, m => m.Value!);
        }

        // Keys always persist, regardless of preset/custom (§15 #5).
        prefs.Keys = Keys.Where(k => k.IsSet).Select(k => k.Key).ToHashSet();

        _provider.SavePreferences(prefs);
    }

    // Editing a modifier reverts the preset to Custom (feedback-loop guarded against load/defaults).
    private void OnModifierChanged()
    {
        if (_suppressRevert) return;
        SelectedPreset = CustomPreset;
        IsDirty = true;
    }

    private void OnKeyChanged()
    {
        if (_suppressRevert) return;
        IsDirty = true;
    }
}
