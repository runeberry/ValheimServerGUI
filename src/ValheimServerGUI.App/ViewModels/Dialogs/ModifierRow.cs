using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>One world-gen modifier (§6.5). "Normal" is the empty/omitted sentinel value.</summary>
public partial class ModifierRow : ObservableObject
{
    public const string Normal = "Normal";

    private readonly Action _onChanged;

    public ModifierRow(string key, Action onChanged)
    {
        Key = key;
        _onChanged = onChanged;
        Options = new List<string> { Normal }
            .Concat(WorldGenModifiers.AllowedValues[key])
            .ToList();
    }

    public string Key { get; }

    public IReadOnlyList<string> Options { get; }

    [ObservableProperty] private string _selected = Normal;

    /// <summary>The persisted value, or null when "Normal" (omitted).</summary>
    public string? Value => Selected == Normal ? null : Selected;

    public void SetSelectedQuiet(string? value) => Selected = value ?? Normal;

    partial void OnSelectedChanged(string value) => _onChanged();
}
