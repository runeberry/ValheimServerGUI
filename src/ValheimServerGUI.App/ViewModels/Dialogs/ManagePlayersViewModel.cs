using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// Manage Players dialog: every known player (the same cache the Players tab reads) with their app-global
/// <b>default role</b>, applied on every server unless a server overrides it (see <see cref="PlayerRoleResolver"/>).
/// Players whose default role is Banned sit on the <b>Banned</b> tab; everyone else on <b>Player Accounts</b>.
/// Each tab shows its selected account's Known Characters, and the account name can be edited here. This is
/// also where a single player's details open (<see cref="FocusPlayer"/>). Everything is staged — the default
/// roles and any player-record edits/removals (<see cref="StagedPlayerRecords"/>) — until <see cref="Save"/>;
/// Cancel writes nothing.
/// </summary>
public sealed partial class ManagePlayersViewModel : ModalEditViewModel, IDisposable
{
    private readonly IUserPreferencesProvider _prefs;
    private readonly IPlayerDataRepository _repo;
    private readonly IRuneberryApiClient? _api;
    private readonly Dictionary<string, PlayerDefaultEntry> _defaults;
    private readonly StagedPlayerRecords _records;
    private readonly DispatcherTimer _sinceTimer;

    public ManagePlayersViewModel(IUserPreferencesProvider prefs, IPlayerDataRepository repo, IRuneberryApiClient? api)
    {
        _prefs = prefs;
        _repo = repo;
        _api = api;
        _defaults = new Dictionary<string, PlayerDefaultEntry>(prefs.LoadPreferences().PlayerDefaults);
        _records = new StagedPlayerRecords(repo);

        PlayerAccounts = new PlayerListSectionViewModel(this, isBanned: false, NewKnownCharacters());
        Banned = new PlayerListSectionViewModel(this, isBanned: true, NewKnownCharacters());
        IgnoreForDirty(nameof(SelectedTabIndex));

        // The tables follow the live cache: players joining/leaving/renamed while the dialog is open.
        _repo.EntityUpdated += OnRepoPlayerChanged;
        _repo.PlayerStatusChanged += OnRepoPlayerChanged;
        _repo.EntityRemoved += OnRepoPlayerChanged;
        _repo.DataUpdated += OnRepoDataChanged;
        _repo.DataReady += OnRepoDataChanged;

        LoadClean(RebuildAll);

        // Keep the Known Characters "Since" column current while the dialog is open.
        _sinceTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _sinceTimer.Tick += (_, _) => RefreshSince(DateTimeOffset.Now);
        _sinceTimer.Start();
    }

    /// <summary>Every known player whose default role is not Banned.</summary>
    public PlayerListSectionViewModel PlayerAccounts { get; }

    /// <summary>Every known player whose default role is Banned.</summary>
    public PlayerListSectionViewModel Banned { get; }

    private IEnumerable<PlayerListSectionViewModel> Sections => new[] { PlayerAccounts, Banned };

    /// <summary>The shown tab: 0 = Player Accounts, 1 = Banned. View state (never marks the dialog dirty).</summary>
    [ObservableProperty]
    private int _selectedTabIndex;

    /// <summary>Shows the Add Player dialog with the given options; null on cancel. Wired by the window.</summary>
    public Func<AddPlayerOptions, Task<AddPlayerResult?>>? AddPlayerPrompt { get; set; }

    /// <summary>Prompts for a player name, given the current one (empty when unknown); null on cancel. Wired by
    /// the window.</summary>
    public Func<string, Task<string?>>? EditNamePrompt { get; set; }

    /// <summary>Opens the tab holding <paramref name="key"/> and selects that player (the "View Player Details"
    /// entry point). Does nothing for a player this dialog doesn't list.</summary>
    public void FocusPlayer(string key)
    {
        var section = SectionFor(key);
        if (section.Accounts.FirstOrDefault(r => r.Key == key) is not { } row) return;

        SelectedTabIndex = section.IsBanned ? 1 : 0;
        section.SelectedAccount = row;
    }

    public override void ApplyDefaults() { /* Manage Players has no defaults to restore. */ }

    /// <summary>
    /// Commits the staged player records to the repo (removals, then new/edited records), then saves the default
    /// roles to user preferences — which re-renders every Players tab and live-applies to running servers. New
    /// records with no name get the same background lookup the join path uses.
    /// </summary>
    public void Save()
    {
        var created = _records.CommitTo(_repo);

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
        var outcome = AddPlayerFlow.Apply(result, _records, _defaults, setServerOverride: null);
        if (outcome is null) return;

        IsDirty = true;
        RebuildAll();
        Select(outcome.Player.Key);
    }

    internal void RemoveAccount(string key)
    {
        // Forget the player entirely: the cached record and its default role.
        _records.Remove(key);
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
            _defaults[key] = new PlayerDefaultEntry(role, entry?.PlatformRaw ?? _records.FindById(key)?.PlatformRaw);
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

    /// <summary>Prompts for a new account name. Cancel leaves it unchanged; a blank name clears it back to
    /// unknown (so a later lookup can fill it in).</summary>
    internal async Task EditNameAsync(string key)
    {
        if (EditNamePrompt is null || EditableRecord(key) is not { } record) return;

        var name = await EditNamePrompt(record.PlayerName ?? string.Empty);
        if (name is null) return;

        record.PlayerName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        _records.Upsert(record);
        IsDirty = true;
        RefreshRow(key);
    }

    /// <summary>Re-formats both tabs' Known Characters "Since" column against <paramref name="now"/>.</summary>
    internal void RefreshSince(DateTimeOffset now)
    {
        foreach (var section in Sections) section.KnownCharacters.RefreshSince(now);
    }

    internal void OnSelectionChanged(PlayerListSectionViewModel section)
        => section.KnownCharacters.Load(section.SelectedAccount is { } row ? EditableRecord(row.Key) : null);

    // ===== Non-public =====

    private KnownCharactersViewModel NewKnownCharacters()
    {
        var knownCharacters = new KnownCharactersViewModel(Strings.ManagePlayers_NoAccountSelected, Strings.ManagePlayers_NoKnownCharacters);
        knownCharacters.Edited += OnKnownCharactersEdited;
        return knownCharacters;
    }

    // A character edit writes straight into the staged record for the account the table shows.
    private void OnKnownCharactersEdited(object? sender, EventArgs e)
    {
        var section = Sections.First(s => ReferenceEquals(s.KnownCharacters, sender));
        if (section.SelectedAccount?.Key is not { } key || EditableRecord(key) is not { } record) return;

        section.KnownCharacters.WriteTo(record);
        _records.Upsert(record);
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
    private bool IsListed(string key) => _records.FindById(key) is not null || _defaults.ContainsKey(key);

    private void RebuildAll()
    {
        var keys = _records.KnownKeys.Union(_defaults.Keys).ToList();
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

    private PlayerInfo? DisplayRecord(string key) => _records.FindForDisplay(key);

    // The record an editor works on: the staged/cloned record, or a stub for a defaulted player with no cached
    // record yet (writing it back through _records creates the record on Save).
    private PlayerInfo? EditableRecord(string key)
        => _records.FindById(key) ?? (_defaults.ContainsKey(key) ? Stub(key) : null);

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

    // Updates a row (and, when it is selected, its characters' Status/Since) in place; a player new to (or gone
    // from) the tables rebuilds them instead.
    private void RefreshRow(string key)
    {
        var section = SectionFor(key);
        var row = section.Accounts.FirstOrDefault(r => r.Key == key);
        var listed = IsListed(key);

        if (row is null || !listed)
        {
            if ((row is not null) != listed) RebuildAll();
            return;
        }
        if (DisplayRecord(key) is not { } record) return;

        row.Update(record);
        if (ReferenceEquals(section.SelectedAccount, row)) section.KnownCharacters.Refresh(record);
    }

    public void Dispose()
    {
        _sinceTimer.Stop();
        _repo.EntityUpdated -= OnRepoPlayerChanged;
        _repo.PlayerStatusChanged -= OnRepoPlayerChanged;
        _repo.EntityRemoved -= OnRepoPlayerChanged;
        _repo.DataUpdated -= OnRepoDataChanged;
        _repo.DataReady -= OnRepoDataChanged;
    }
}
