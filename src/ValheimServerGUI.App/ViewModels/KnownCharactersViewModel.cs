using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.ViewModels;

/// <summary>
/// The editable "Known Characters" table for one player (name + derived Status/Since; added names are flagged
/// <c>matchConfident=true</c>). Shared by Player Details and the Manage Players account lists. The owner loads a
/// player, listens to <see cref="Edited"/> to mark itself dirty, and flushes with <see cref="WriteTo"/>.
/// </summary>
public partial class KnownCharactersViewModel : ObservableObject
{
    private readonly string? _noPlayerText;
    private readonly string? _noCharactersText;

    /// <param name="noPlayerText">Empty-state hint while no player is loaded (null = none).</param>
    /// <param name="noCharactersText">Empty-state hint when the loaded player has no characters (null = none).</param>
    public KnownCharactersViewModel(string? noPlayerText = null, string? noCharactersText = null)
    {
        _noPlayerText = noPlayerText;
        _noCharactersText = noCharactersText;
        Characters.CollectionChanged += (_, _) => OnPropertyChanged(nameof(EmptyText));
    }

    public ObservableCollection<CharacterRowViewModel> Characters { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCharacterCommand))]
    private CharacterRowViewModel? _selectedCharacter;

    /// <summary>True while a player is loaded; the add/rename/remove actions need one.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EmptyText))]
    private bool _hasPlayer;

    /// <summary>The table's empty-state hint, or null when rows are shown (or no hint applies).</summary>
    public string? EmptyText => !HasPlayer ? _noPlayerText : Characters.Count == 0 ? _noCharactersText : null;

    /// <summary>Raised on every user edit (add/rename/remove) so the owner can mark itself dirty.</summary>
    public event EventHandler? Edited;

    /// <summary>Replaces the table with <paramref name="player"/>'s characters (null clears it).</summary>
    public void Load(PlayerInfo? player)
    {
        SelectedCharacter = null;
        Characters.Clear();
        HasPlayer = player is not null;
        if (player is null) return;

        var now = DateTimeOffset.Now;
        foreach (var c in player.Characters ?? new List<PlayerInfo.CharacterInfo>())
        {
            if (string.IsNullOrWhiteSpace(c.CharacterName)) continue;
            var row = new CharacterRowViewModel(c.CharacterName!, c.MatchConfident, c.LastSeen);
            row.Refresh(player, now);
            Characters.Add(row);
        }
    }

    /// <summary>Re-derives each row's Status/Since from the player's current state, keeping unsaved edits.</summary>
    public void Refresh(PlayerInfo player)
    {
        var now = DateTimeOffset.Now;
        foreach (var row in Characters) row.Refresh(player, now);
    }

    /// <summary>Writes the table back onto <paramref name="player"/>'s character list.</summary>
    public void WriteTo(PlayerInfo player)
        => player.Characters = Characters
            .Select(row => new PlayerInfo.CharacterInfo
            {
                CharacterName = row.CharacterName,
                // Added names are confident; loaded ones keep their original flag and last-seen time.
                MatchConfident = row.MatchConfident,
                LastSeen = row.LastSeen,
            })
            .ToList();

    public void AddCharacter(string name)
    {
        if (!HasPlayer || string.IsNullOrWhiteSpace(name) || Characters.Any(c => c.CharacterName == name)) return;
        Characters.Add(new CharacterRowViewModel(name));
        Edited?.Invoke(this, EventArgs.Empty);
    }

    public void RenameCharacter(string oldName, string newName)
    {
        var row = Characters.FirstOrDefault(c => c.CharacterName == oldName);
        if (row is null || string.IsNullOrWhiteSpace(newName)) return;
        row.CharacterName = newName;
        Edited?.Invoke(this, EventArgs.Empty);
    }

    private bool CanRemoveCharacter => SelectedCharacter is not null;

    [RelayCommand(CanExecute = nameof(CanRemoveCharacter))]
    private void RemoveCharacter()
    {
        if (SelectedCharacter is { } row && Characters.Remove(row))
            Edited?.Invoke(this, EventArgs.Empty);
    }
}
