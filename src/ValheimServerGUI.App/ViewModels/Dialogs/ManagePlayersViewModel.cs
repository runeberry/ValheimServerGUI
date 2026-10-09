using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// Manage Players dialog: every known player (the same cache the Players tab reads) with their app-global
/// <b>default role</b>, applied on every server unless a server overrides it (see <see cref="PlayerRoleResolver"/>).
/// Players whose default role is Banned sit on the <b>Banned</b> tab; everyone else on <b>Player Accounts</b>.
/// Everything is staged — the default roles and any player-record edits/removals (<see cref="StagedPlayerRecords"/>)
/// — until <see cref="Save"/>; Cancel writes nothing.
/// </summary>
public sealed class ManagePlayersViewModel : ModalEditViewModel, IDisposable
{
    private readonly IUserPreferencesProvider _prefs;
    private readonly IPlayerDataRepository _repo;
    private readonly IRuneberryApiClient? _api;
    private readonly Dictionary<string, PlayerDefaultEntry> _defaults;
    private string? _knownCharactersKey;

    public ManagePlayersViewModel(IUserPreferencesProvider prefs, IPlayerDataRepository repo, IRuneberryApiClient? api)
    {
        _prefs = prefs;
        _repo = repo;
        _api = api;
        _defaults = new Dictionary<string, PlayerDefaultEntry>(prefs.LoadPreferences().PlayerDefaults);
        Records = new StagedPlayerRecords(repo);

        var knownCharacters = new KnownCharactersViewModel(Strings.ManagePlayers_NoAccountSelected, Strings.ManagePlayers_NoKnownCharacters);
        knownCharacters.Edited += OnKnownCharactersEdited;
        PlayerAccounts = new PlayerListSectionViewModel(this, isBanned: false, knownCharacters);
        Banned = new PlayerListSectionViewModel(this, isBanned: true, knownCharacters: null);

        // The tables follow the live cache: players joining/leaving/renamed while the dialog is open.
        _repo.EntityUpdated += OnRepoPlayerChanged;
        _repo.PlayerStatusChanged += OnRepoPlayerChanged;
        _repo.EntityRemoved += OnRepoPlayerChanged;
        _repo.DataUpdated += OnRepoDataChanged;
        _repo.DataReady += OnRepoDataChanged;

        LoadClean(RebuildAll);
    }

    /// <summary>Every known player whose default role is not Banned.</summary>
    public PlayerListSectionViewModel PlayerAccounts { get; }

    /// <summary>Every known player whose default role is Banned.</summary>
    public PlayerListSectionViewModel Banned { get; }

    private IEnumerable<PlayerListSectionViewModel> Sections => new[] { PlayerAccounts, Banned };

    /// <summary>Staged player records; Player Details opened from here edits through this store.</summary>
    public StagedPlayerRecords Records { get; }

    /// <summary>Shows the Add Player dialog with the given options; null on cancel. Wired by the window.</summary>
    public Func<AddPlayerOptions, Task<AddPlayerResult?>>? AddPlayerPrompt { get; set; }

    /// <summary>Raised for View Player Details (player key). The window opens it over <see cref="Records"/>.</summary>
    public event Action<string>? DetailsRequested;

    public override void ApplyDefaults() { /* Manage Players has no defaults to restore. */ }

    /// <summary>
    /// Commits the staged player records to the repo (removals, then new/edited records), then saves the default
    /// roles to user preferences — which re-renders every Players tab and live-applies to running servers. New
    /// records with no name get the same background lookup the join path uses.
    /// </summary>
    public void Save()
    {
        var created = Records.CommitTo(_repo);

        var prefs = _prefs.LoadPreferences();
        prefs.PlayerDefaults = new Dictionary<string, PlayerDefaultEntry>(_defaults);
        _prefs.SavePreferences(prefs);

        if (_api is null) return;
        foreach (var player in created.Where(p => string.IsNullOrWhiteSpace(p.PlayerName)))
            _ = _api.RequestPlayerInfoAsync(player.Platform ?? string.Empty, player.PlayerId ?? string.Empty);
    }

    // ===== Section callbacks =====

    internal async Task AddAsync(PlayerListSectionViewModel section)
    {
        if (AddPlayerPrompt is null) return;

        var result = await AddPlayerPrompt(section.IsBanned ? AddPlayerOptions.ForBanned : AddPlayerOptions.ForPlayerAccounts);
        if (result is null) return;

        // The same routine the Players tab uses, over this dialog's staged records + default roles.
        var outcome = AddPlayerFlow.Apply(result, Records, _defaults, setServerOverride: null);
        if (outcome is null) return;

        IsDirty = true;
        RebuildAll();
        Select(outcome.Player.Key);
    }

    internal void RemoveAccount(string key)
    {
        // Forget the player entirely: the cached record and its default role.
        Records.Remove(key);
        _defaults.Remove(key);
        IsDirty = true;
        RebuildAll();
    }

    /// <summary>Sets a player's default role; None clears it. Banned moves the player to the Banned tab (and
    /// any other role moves them back).</summary>
    internal void SetDefaultRole(string key, PlayerRole role)
    {
        var current = _defaults.TryGetValue(key, out var entry) ? entry.DefaultRole : PlayerRole.None;
        if (current == role) return;

        if (role == PlayerRole.None)
            _defaults.Remove(key);
        else
            _defaults[key] = new PlayerDefaultEntry(role, entry?.PlatformRaw ?? Records.FindById(key)?.PlatformRaw);
        IsDirty = true;

        var section = SectionFor(key);
        if (section.Accounts.FirstOrDefault(r => r.Key == key) is { } row)
        {
            // Same tab: update in place (keeps the selection). A move across tabs rebuilds both.
            row.DisplayRole = DefaultRoleOf(key);
            section.RefreshRoleFlags();
        }
        else
        {
            RebuildAll();
        }
    }

    internal void RequestDetails(string key)
    {
        // Player Details edits an existing record; give a defaulted player with no cached record a staged stub.
        if (Records.FindById(key) is null && EditableRecord(key) is { } stub)
            Records.Upsert(stub);

        DetailsRequested?.Invoke(key);
    }

    /// <summary>Called after Player Details saved into <see cref="Records"/>: refresh the row and characters.</summary>
    public void OnDetailsSaved(string key)
    {
        IsDirty = true;
        RefreshRow(key);
        if (PlayerAccounts.SelectedAccount?.Key == key)
            PlayerAccounts.KnownCharacters!.Load(Records.FindById(key));
    }

    internal void OnSelectionChanged(PlayerListSectionViewModel section)
    {
        if (section.KnownCharacters is not { } kc) return;

        _knownCharactersKey = section.SelectedAccount?.Key;
        kc.Load(_knownCharactersKey is null ? null : EditableRecord(_knownCharactersKey));
    }

    // ===== Non-public =====

    // A character edit writes straight into the staged record for the account the table shows.
    private void OnKnownCharactersEdited(object? sender, EventArgs e)
    {
        if (sender is not KnownCharactersViewModel kc || _knownCharactersKey is not { } key) return;
        if (EditableRecord(key) is not { } record) return;

        kc.WriteTo(record);
        Records.Upsert(record);
        IsDirty = true;
        RefreshRow(key);
    }

    private PlayerRole? DefaultRoleOf(string key) => _defaults.TryGetValue(key, out var entry) ? entry.DefaultRole : null;

    private bool IsBannedByDefault(string key) => DefaultRoleOf(key) == PlayerRole.Banned;

    private PlayerListSectionViewModel SectionFor(string key) => IsBannedByDefault(key) ? Banned : PlayerAccounts;

    private void Select(string key)
    {
        var section = SectionFor(key);
        section.SelectedAccount = section.Accounts.FirstOrDefault(r => r.Key == key);
    }

    // Known players (live cache + staged adds − staged removals), plus any default role whose player has no cached
    // record yet, so a default is never invisible.
    private bool IsListed(string key) => Records.FindById(key) is not null || _defaults.ContainsKey(key);

    private void RebuildAll()
    {
        var keys = Records.KnownKeys.Union(_defaults.Keys).ToList();
        foreach (var section in Sections) Rebuild(section, keys);
    }

    // Rebuilds a tab's rows, keeping the selection when the account is still on it.
    private void Rebuild(PlayerListSectionViewModel section, IReadOnlyList<string> keys)
    {
        var selectedKey = section.SelectedAccount?.Key;
        section.SelectedAccount = null;
        section.Accounts.Clear();

        var rows = keys
            .Where(key => IsBannedByDefault(key) == section.IsBanned)
            .Select(key =>
            {
                var row = new PlayerRowViewModel(DisplayRecord(key) ?? Stub(key));
                row.DisplayRole = DefaultRoleOf(key);
                return row;
            })
            .OrderBy(r => r.AccountName ?? r.DisplayName, StringComparer.CurrentCultureIgnoreCase);
        foreach (var row in rows) section.Accounts.Add(row);

        section.SelectedAccount = section.Accounts.FirstOrDefault(r => r.Key == selectedKey);
    }

    private PlayerInfo? DisplayRecord(string key) => Records.FindForDisplay(key);

    // The record an editor works on: the staged/cloned record, or a stub for a defaulted player with no cached
    // record yet (writing it back through Records creates the record on Save).
    private PlayerInfo? EditableRecord(string key)
        => Records.FindById(key) ?? (_defaults.ContainsKey(key) ? Stub(key) : null);

    // A defaulted player with no cached record.
    private PlayerInfo Stub(string key)
    {
        var separator = key.IndexOf(':');
        var platform = separator > 0 ? key[..separator] : null;
        var playerId = separator > 0 ? key[(separator + 1)..] : key;
        var platformRaw = _defaults.TryGetValue(key, out var entry) ? entry.PlatformRaw : null;
        return new PlayerInfo { Platform = platform, PlatformRaw = platformRaw ?? platform, PlayerId = playerId };
    }

    private void OnRepoPlayerChanged(object? sender, PlayerInfo player) => OnUi(() => RefreshRow(player.Key));

    private void OnRepoDataChanged(object? sender, EventArgs e) => OnUi(RebuildAll);

    private static void OnUi(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess()) action();
        else Dispatcher.UIThread.Post(action);
    }

    // Updates a row in place; a player new to (or gone from) the tables rebuilds them instead.
    private void RefreshRow(string key)
    {
        var row = SectionFor(key).Accounts.FirstOrDefault(r => r.Key == key);
        var listed = IsListed(key);

        if (row is null || !listed)
        {
            if ((row is not null) != listed) RebuildAll();
            return;
        }
        if (DisplayRecord(key) is { } record) row.Update(record);
    }

    public void Dispose()
    {
        _repo.EntityUpdated -= OnRepoPlayerChanged;
        _repo.PlayerStatusChanged -= OnRepoPlayerChanged;
        _repo.EntityRemoved -= OnRepoPlayerChanged;
        _repo.DataUpdated -= OnRepoDataChanged;
        _repo.DataReady -= OnRepoDataChanged;
    }
}
