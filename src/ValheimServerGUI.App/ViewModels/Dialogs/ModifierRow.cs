using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>One world-gen modifier (§6.5). The dropdown shows friendly names (via <see cref="WorldGenDisplay"/>);
/// the persisted <see cref="Value"/> is the raw token, or null for the "Normal" (omitted) sentinel.</summary>
public partial class ModifierRow : ObservableObject
{
    private readonly Action _onChanged;

    public ModifierRow(string key, Action onChanged)
    {
        Key = key;
        _onChanged = onChanged;
        DisplayName = WorldGenDisplay.ModifierName(key);
        HelpText = WorldGenDisplay.ModifierHelp(key);
        Options = WorldGenDisplay.ModifierValueDisplays(key);
    }

    /// <summary>The raw modifier token (e.g. <c>combat</c>).</summary>
    public string Key { get; }

    /// <summary>Friendly modifier name shown as the row label (e.g. "Combat").</summary>
    public string DisplayName { get; }

    /// <summary>Descriptive help shown by the row's "?" glyph.</summary>
    public string HelpText { get; }

    /// <summary>Friendly value names shown in the dropdown (includes "Normal").</summary>
    public IReadOnlyList<string> Options { get; }

    /// <summary>The selected friendly value name (dropdown value).</summary>
    [ObservableProperty] private string _selected = WorldGenDisplay.NormalModifier;

    /// <summary>The persisted token, or null when "Normal" (omitted).</summary>
    public string? Value => WorldGenDisplay.ModifierValueToken(Key, Selected);

    public void SetSelectedQuiet(string? token) => Selected = WorldGenDisplay.ModifierValueDisplay(Key, token);

    partial void OnSelectedChanged(string value) => _onChanged();
}
