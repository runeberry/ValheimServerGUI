using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>One world-gen key toggle (§6.5). Keys persist independently of the preset (§15 #5). The checkbox
/// shows a friendly name + help (via <see cref="WorldGenDisplay"/>); <see cref="Key"/> is the raw token.</summary>
public partial class WorldKeyToggle : ObservableObject
{
    private readonly Action _onChanged;

    public WorldKeyToggle(string key, Action onChanged)
    {
        Key = key;
        DisplayName = WorldGenDisplay.KeyName(key);
        HelpText = WorldGenDisplay.KeyHelp(key);
        _onChanged = onChanged;
    }

    /// <summary>The raw key token (e.g. <c>nomap</c>).</summary>
    public string Key { get; }

    /// <summary>Friendly key name shown as the checkbox label (e.g. "No map").</summary>
    public string DisplayName { get; }

    /// <summary>Descriptive help shown by the checkbox's "?" glyph.</summary>
    public string HelpText { get; }

    [ObservableProperty] private bool _isSet;

    partial void OnIsSetChanged(bool value) => _onChanged();
}
