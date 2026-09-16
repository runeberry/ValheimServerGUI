using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// "Add by ID" dialog: grant admin / ban / permit to a player by platform + ID, including friends who have
/// never connected. Requires a non-blank ID and at least one target list. Surfaces the ban-overrides caveat
/// inline when a ban is combined with admin/permit.
/// </summary>
public partial class AddByIdViewModel : ObservableObject
{
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit), nameof(ShowBanWarning))]
    private bool _addAdmin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit), nameof(ShowBanWarning))]
    private bool _addBanned;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit), nameof(ShowBanWarning))]
    private bool _addPermitted;

    /// <summary>OK is enabled once an ID is entered and at least one list is chosen.</summary>
    public bool CanSubmit => !string.IsNullOrWhiteSpace(PlayerId) && (AddAdmin || AddBanned || AddPermitted);

    /// <summary>A ban combined with admin/permit is contradictory-looking; warn that the ban wins.</summary>
    public bool ShowBanWarning => AddBanned && (AddAdmin || AddPermitted);

    /// <summary>The dialog result, or null when the form is incomplete.</summary>
    public AddByIdResult? BuildResult() => CanSubmit
        ? new AddByIdResult(SelectedPlatform, PlayerId.Trim(), AddAdmin, AddBanned, AddPermitted)
        : null;
}
