using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>One world-gen key toggle (§6.5). Keys persist independently of the preset (§15 #5).</summary>
public partial class WorldKeyToggle : ObservableObject
{
    private readonly Action _onChanged;

    public WorldKeyToggle(string key, Action onChanged)
    {
        Key = key;
        _onChanged = onChanged;
    }

    public string Key { get; }

    [ObservableProperty] private bool _isSet;

    partial void OnIsSetChanged(bool value) => _onChanged();
}
