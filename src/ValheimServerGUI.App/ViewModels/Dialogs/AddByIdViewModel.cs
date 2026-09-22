using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// "Add by ID" dialog: grant a single <see cref="PlayerRole"/> to a player by platform + ID, including friends
/// who have never connected. The role options offered mirror the Players tab for the active profile's mode —
/// Admin always, plus Banned (open mode) or Permitted (permitted-list mode) — so the two never disagree.
/// Requires a non-blank ID and a selected role.
/// </summary>
public partial class AddByIdViewModel : ObservableObject
{
    private readonly bool _usePermittedList;
    private PlayerRole? _selectedRole;

    public AddByIdViewModel(bool usePermittedList) => _usePermittedList = usePermittedList;

    /// <summary>The platforms a manual entry can target (matches <see cref="PlayerPlatforms.All"/>).</summary>
    public IReadOnlyList<string> Platforms { get; } = new[]
    {
        PlayerPlatforms.Steam,
        PlayerPlatforms.Xbox,
        PlayerPlatforms.PlayStation,
        PlayerPlatforms.Nintendo,
    };

    [ObservableProperty] private string _selectedPlatform = PlayerPlatforms.Steam;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    private string _playerId = string.Empty;

    // The single chosen role, or null until one is picked. The three IsXxx bools below derive from it so they
    // bind two-way to the grouped c:RadioFormField controls (RadioButton grouping clears the others for us).
    public PlayerRole? SelectedRole
    {
        get => _selectedRole;
        private set
        {
            if (_selectedRole == value) return;
            _selectedRole = value;
            OnPropertyChanged(nameof(SelectedRole));
            OnPropertyChanged(nameof(IsAdmin));
            OnPropertyChanged(nameof(IsBanned));
            OnPropertyChanged(nameof(IsPermitted));
            OnPropertyChanged(nameof(CanSubmit));
        }
    }

    /// <summary>Two-way radio bindings: setting one true selects that role; a grouping-driven false is ignored.</summary>
    public bool IsAdmin
    {
        get => _selectedRole == PlayerRole.Admin;
        set { if (value) SelectedRole = PlayerRole.Admin; }
    }

    public bool IsBanned
    {
        get => _selectedRole == PlayerRole.Banned;
        set { if (value) SelectedRole = PlayerRole.Banned; }
    }

    public bool IsPermitted
    {
        get => _selectedRole == PlayerRole.Permitted;
        set { if (value) SelectedRole = PlayerRole.Permitted; }
    }

    // Which role options are relevant to the active mode (mirrors PlayersViewModel.ShowBanToggle/ShowPermitToggle).
    // Admin is always shown.
    public bool ShowBanned => !_usePermittedList;
    public bool ShowPermitted => _usePermittedList;

    /// <summary>OK is enabled once an ID is entered and a role is chosen.</summary>
    public bool CanSubmit => !string.IsNullOrWhiteSpace(PlayerId) && SelectedRole is not null;

    /// <summary>The dialog result, or null when the form is incomplete.</summary>
    public AddByIdResult? BuildResult() => CanSubmit
        ? new AddByIdResult(SelectedPlatform, PlayerId.Trim(), SelectedRole!.Value)
        : null;
}
