using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// World Preferences editor. The modifier and key fields are the truth, and they are what Save persists. The preset
/// dropdown is derived from them: it selects the preset the fields match exactly, and otherwise shows the game-style
/// name ("Hard+", "Custom (No Preset)") as its empty-state text. Choosing a preset sets every field to its values.
/// </summary>
public partial class WorldPreferencesViewModel : ModalEditViewModel
{
    private readonly IWorldPreferencesProvider _provider;
    private readonly IShellLauncher _shell;
    private readonly string _worldName;

    // Set while several fields change together, so the preset is derived once from the finished values.
    private bool _applying;

    public WorldPreferencesViewModel(IWorldPreferencesProvider provider, string worldName, IShellLauncher shell)
    {
        _provider = provider;
        _shell = shell;
        _worldName = worldName;

        Presets = WorldGenDisplay.PresetDisplays;

        Modifiers = new ObservableCollection<ModifierRow>(
            WorldGenModifiers.All.Select(key => new ModifierRow(key, OnFieldChanged)));
        Keys = new ObservableCollection<WorldKeyToggle>(
            WorldGenKeys.All.Select(key => new WorldKeyToggle(key, OnFieldChanged)));

        Load();
    }

    /// <summary>Friendly preset names shown in the dropdown. "Custom" is never one of them.</summary>
    public IReadOnlyList<string> Presets { get; }
    public ObservableCollection<ModifierRow> Modifiers { get; }
    public ObservableCollection<WorldKeyToggle> Keys { get; }

    public string WorldName => _worldName;

    /// <summary>The preset the fields match exactly, or null. Setting a preset applies its values to the fields.</summary>
    public string? SelectedPresetDisplay
    {
        get => PresetLabel is { HasExtras: false } label ? WorldGenDisplay.PresetDisplay(label.Preset) : null;
        set
        {
            if (WorldGenPresets.Find(WorldGenDisplay.PresetToken(value)) is { } preset) Apply(preset.Settings);
        }
    }

    /// <summary>The dropdown's text when no preset matches exactly: "Hard+" or "Custom (No Preset)".</summary>
    public string PresetPlaceholder => WorldGenDisplay.PresetLabelText(PresetLabel);

    private WorldGenPresetLabel PresetLabel => WorldGenPresets.Label(CurrentSettings());

    /// <summary>The settings the fields currently show.</summary>
    public WorldGenSettings CurrentSettings() => new(
        Modifiers.Where(m => m.Value is not null).ToDictionary(m => m.Key, m => m.Value!),
        Keys.Where(k => k.IsSet).Select(k => k.Key));

    [RelayCommand] private void OpenWorldModifiersWiki() => _shell.OpenWebAddress(AppConstants.UrlValheimWikiWorldModifiers);

    private void Load() => LoadClean(() =>
        Apply((_provider.LoadPreferences(_worldName) ?? new WorldPreferences()).ToSettings()));

    public override void ApplyDefaults()
    {
        Apply(new WorldGenSettings());
        IsDirty = true;
    }

    public void Save()
    {
        var prefs = _provider.LoadPreferences(_worldName) ?? new WorldPreferences { WorldName = _worldName };
        var settings = CurrentSettings();

        prefs.Modifiers = new Dictionary<string, string>(settings.Modifiers);
        prefs.Keys = settings.Keys.ToHashSet();

        _provider.SavePreferences(prefs);
    }

    private void Apply(WorldGenSettings settings)
    {
        _applying = true;
        try
        {
            foreach (var row in Modifiers)
                row.SetSelectedQuiet(settings.Modifiers.TryGetValue(row.Key, out var value) ? value : null);
            foreach (var toggle in Keys)
                toggle.IsSet = settings.Keys.Contains(toggle.Key);
        }
        finally
        {
            _applying = false;
        }
        OnFieldChanged();
    }

    // The fields live on the row objects, so their edits are reported here: re-derive the preset, which also marks
    // the dialog dirty (outside a load).
    private void OnFieldChanged()
    {
        if (_applying) return;
        OnPropertyChanged(nameof(SelectedPresetDisplay));
        OnPropertyChanged(nameof(PresetPlaceholder));
    }
}
