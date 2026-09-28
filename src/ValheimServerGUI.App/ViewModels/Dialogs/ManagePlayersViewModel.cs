using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// Manage Players dialog: the app-global <b>My Accounts</b>, <b>Friends</b>, and <b>Banned</b> lists, each
/// account carrying a default role applied on every server (a server's override wins; see
/// <see cref="PlayerRoleResolver"/>). Everything is staged — the defaults map and any player-record edits
/// (<see cref="StagedPlayerRecords"/>) — until <see cref="Save"/>; Cancel writes nothing.
/// </summary>
public sealed class ManagePlayersViewModel : ModalEditViewModel, IDisposable
{
    // ===== User copy (EXACT — do not paraphrase) =====
    // Asserted verbatim by a test. {name}/{list}/{target} are literal placeholders substituted at display time.
    public const string AddPlayerTitle = "Add Player";
    public const string SelfOnListMessage =
        "Player {name} is already set as your own account. You cannot add yourself to the {list} list.";
    public const string ClaimAccountMessage =
        "Player {name} is already on the {list} list. Claim this as your account instead?";
    public const string MoveListMessage =
        "Player {name} is already on the {list} list. Move them to the {target} list instead?";

    public const string MyAccountsCaption = "Add your account here and set default permissions for any server you host.";
    public const string FriendsCaption = "Add your friends' accounts here and set default permissions for any server you host.";
    public const string NoAccountSelectedText = "Select an account to see Known Characters.";
    public const string NoKnownCharactersText = "No known characters for this account.";

    private readonly IUserPreferencesProvider _prefs;
    private readonly IPlayerDataRepository _repo;
    private readonly IRuneberryApiClient? _api;
    private readonly Dictionary<string, PlayerDefaultEntry> _defaults;
    private readonly Dictionary<KnownCharactersViewModel, string> _knownCharactersKey = new();

    public ManagePlayersViewModel(IUserPreferencesProvider prefs, IPlayerDataRepository repo, IRuneberryApiClient? api)
    {
        _prefs = prefs;
        _repo = repo;
        _api = api;
        _defaults = new Dictionary<string, PlayerDefaultEntry>(prefs.LoadPreferences().PlayerDefaults);
        Records = new StagedPlayerRecords(repo);

        MyAccounts = new PlayerListSectionViewModel(this, PlayerCategory.MyAccount, MyAccountsCaption, "My Accounts",
            NewKnownCharacters());
        Friends = new PlayerListSectionViewModel(this, PlayerCategory.Friend, FriendsCaption, "Friends' Accounts",
            NewKnownCharacters());
        Banned = new PlayerListSectionViewModel(this, PlayerCategory.Banned, null, "Banned Accounts", null);

        // Status/Since are display-only and follow the live cache.
        _repo.EntityUpdated += OnRepoPlayerChanged;
        _repo.PlayerStatusChanged += OnRepoPlayerChanged;

        LoadClean(() => { foreach (var section in Sections) Rebuild(section); });
    }

    public PlayerListSectionViewModel MyAccounts { get; }
    public PlayerListSectionViewModel Friends { get; }
    public PlayerListSectionViewModel Banned { get; }

    private IEnumerable<PlayerListSectionViewModel> Sections => new[] { MyAccounts, Friends, Banned };

    /// <summary>Staged player records; Player Details opened from here edits through this store.</summary>
    public StagedPlayerRecords Records { get; }

    /// <summary>Shows the Add Player dialog with the given role options; null on cancel. Wired by the window.</summary>
    public Func<AddPlayerOptions, Task<AddPlayerResult?>>? AddPlayerPrompt { get; set; }

    /// <summary>Shows a single-OK message (title, body). Wired by the window; no-op if unset.</summary>
    public Func<string, string, Task>? MessagePrompt { get; set; }

    /// <summary>Shows a Yes/No question (title, body); true for Yes. Wired by the window; No if unset.</summary>
    public Func<string, string, Task<bool>>? ChoicePrompt { get; set; }

    /// <summary>Raised for View Player Details (player key). The window opens it over <see cref="Records"/>.</summary>
    public event Action<string>? DetailsRequested;

    public override void ApplyDefaults() { /* Manage Players has no defaults to restore. */ }

    /// <summary>
    /// Commits the staged player records to the repo (new ones first appear there), then saves the defaults to
    /// user preferences — which re-renders every Players tab and live-applies to running servers. New records
    /// with no name get the same background lookup the join path uses.
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

        var result = await AddPlayerPrompt(OptionsFor(section.Category));
        if (result is null) return;
        if (!PlayerPlatforms.TryGetValidPlatform(result.Platform, out var platform) || platform is null) return;
        if (string.IsNullOrWhiteSpace(result.PlayerId)) return;

        var playerId = result.PlayerId.Trim();
        var key = $"{platform}:{playerId}";
        var role = section.Category == PlayerCategory.Banned ? PlayerRole.Banned : result.Role;

        if (_defaults.TryGetValue(key, out var existing) && existing.Category != section.Category)
        {
            var shownName = result.PlayerName ?? Records.FindById(key)?.PlayerName ?? PlayerRowViewModel.FallbackName(playerId);
            if (!await ConfirmCrossListAsync(existing.Category, section.Category, shownName)) return;
        }

        // Create or annotate the record so the account has a name/row; untouched existing records stay unstaged.
        var record = Records.FindById(key);
        if (record is null)
        {
            record = new PlayerInfo
            {
                Platform = platform,
                PlatformRaw = platform,
                PlayerId = playerId,
                PlayerStatus = PlayerStatus.Offline,
                LastStatusChange = DateTimeOffset.UtcNow,
            };
            record.PlayerName = result.PlayerName;
            Records.Upsert(record);
        }
        else if (result.PlayerName is not null)
        {
            record.PlayerName = result.PlayerName;
            Records.Upsert(record);
        }

        // Adding to the same list updates the role (and name); a confirmed cross-list add moves the account.
        _defaults[key] = new PlayerDefaultEntry(section.Category, role, record.PlatformRaw ?? platform).Normalized();
        IsDirty = true;

        foreach (var s in Sections) Rebuild(s);
        section.SelectedAccount = section.Accounts.FirstOrDefault(r => r.Key == key);
    }

    internal void RemoveAccount(PlayerListSectionViewModel section, string key)
    {
        // Removes list membership only; the player's cached record is kept.
        if (!_defaults.Remove(key)) return;
        IsDirty = true;
        Rebuild(section);
    }

    internal void SetDefaultRole(string key, PlayerRole role)
    {
        if (!_defaults.TryGetValue(key, out var entry) || entry.DefaultRole == role) return;
        _defaults[key] = (entry with { DefaultRole = role }).Normalized();
        IsDirty = true;

        var section = SectionFor(entry.Category);
        if (section.Accounts.FirstOrDefault(r => r.Key == key) is { } row) row.DisplayRole = _defaults[key].DefaultRole;
        section.RefreshRoleLabels();
    }

    internal void RequestDetails(string key)
    {
        // Player Details edits an existing record; give a listed account with no cached record a staged stub.
        if (Records.FindById(key) is null && EditableRecord(key) is { } stub)
            Records.Upsert(stub);

        DetailsRequested?.Invoke(key);
    }

    /// <summary>Called after Player Details saved into <see cref="Records"/>: refresh rows and characters.</summary>
    public void OnDetailsSaved(string key)
    {
        IsDirty = true;
        if (!_defaults.TryGetValue(key, out var entry)) return;

        RefreshRow(key);
        var section = SectionFor(entry.Category);
        if (section.KnownCharacters is { } kc && section.SelectedAccount?.Key == key)
            kc.Load(Records.FindById(key));
    }

    internal void OnSelectionChanged(PlayerListSectionViewModel section)
    {
        if (section.KnownCharacters is not { } kc) return;

        var key = section.SelectedAccount?.Key;
        if (key is null) _knownCharactersKey.Remove(kc);
        else _knownCharactersKey[kc] = key;
        kc.Load(key is null ? null : EditableRecord(key));
    }

    // ===== Non-public =====

    private KnownCharactersViewModel NewKnownCharacters()
    {
        var kc = new KnownCharactersViewModel(NoAccountSelectedText, NoKnownCharactersText);
        kc.Edited += OnKnownCharactersEdited;
        return kc;
    }

    // A character edit writes straight into the staged record for the account the table shows.
    private void OnKnownCharactersEdited(object? sender, EventArgs e)
    {
        if (sender is not KnownCharactersViewModel kc || !_knownCharactersKey.TryGetValue(kc, out var key)) return;
        if (EditableRecord(key) is not { } record) return;

        kc.WriteTo(record);
        Records.Upsert(record);
        IsDirty = true;

        var section = Sections.First(s => ReferenceEquals(s.KnownCharacters, kc));
        if (section.Accounts.FirstOrDefault(r => r.Key == key) is { } row && DisplayRecord(key) is { } display)
            row.Update(display);
    }

    private async Task<bool> ConfirmCrossListAsync(PlayerCategory from, PlayerCategory to, string name)
    {
        if (from == PlayerCategory.MyAccount)
        {
            if (MessagePrompt is not null)
                await MessagePrompt(AddPlayerTitle, SelfOnListMessage.Replace("{name}", name).Replace("{list}", ListName(to)));
            return false;
        }

        var body = to == PlayerCategory.MyAccount
            ? ClaimAccountMessage.Replace("{name}", name).Replace("{list}", ListName(from))
            : MoveListMessage.Replace("{name}", name).Replace("{list}", ListName(from)).Replace("{target}", ListName(to));
        return ChoicePrompt is not null && await ChoicePrompt(AddPlayerTitle, body);
    }

    private static string ListName(PlayerCategory category) => category switch
    {
        PlayerCategory.MyAccount => "My Accounts",
        PlayerCategory.Friend => "Friends",
        _ => "Banned",
    };

    private static AddPlayerOptions OptionsFor(PlayerCategory category) => category switch
    {
        PlayerCategory.MyAccount => AddPlayerOptions.ForMyAccounts,
        PlayerCategory.Friend => AddPlayerOptions.ForFriends,
        _ => AddPlayerOptions.ForBanned,
    };

    private PlayerListSectionViewModel SectionFor(PlayerCategory category) => category switch
    {
        PlayerCategory.MyAccount => MyAccounts,
        PlayerCategory.Friend => Friends,
        _ => Banned,
    };

    // Rebuilds a section's rows from the staged defaults, keeping the selection when the account is still there.
    private void Rebuild(PlayerListSectionViewModel section)
    {
        var selectedKey = section.SelectedAccount?.Key;
        section.SelectedAccount = null;
        section.Accounts.Clear();

        var rows = _defaults
            .Where(kvp => kvp.Value.Category == section.Category)
            .Select(kvp =>
            {
                var row = new PlayerRowViewModel(DisplayRecord(kvp.Key) ?? Stub(kvp.Key, kvp.Value));
                row.DisplayRole = kvp.Value.DefaultRole;
                return row;
            })
            .OrderBy(r => r.DisplayName, StringComparer.CurrentCultureIgnoreCase);
        foreach (var row in rows) section.Accounts.Add(row);

        section.SelectedAccount = section.Accounts.FirstOrDefault(r => r.Key == selectedKey);
    }

    private PlayerInfo? DisplayRecord(string key) => Records.FindForDisplay(key);

    // The record an editor works on: the staged/cloned record, or a stub for a listed account with no cached
    // record yet (writing it back through Records creates the record on Save).
    private PlayerInfo? EditableRecord(string key)
        => Records.FindById(key) ?? (_defaults.TryGetValue(key, out var entry) ? Stub(key, entry) : null);

    // A listed account with no cached record (never joined, or removed from the Players tab).
    private static PlayerInfo Stub(string key, PlayerDefaultEntry entry)
    {
        var separator = key.IndexOf(':');
        var platform = separator > 0 ? key[..separator] : null;
        var playerId = separator > 0 ? key[(separator + 1)..] : key;
        return new PlayerInfo { Platform = platform, PlatformRaw = entry.PlatformRaw ?? platform, PlayerId = playerId };
    }

    private void OnRepoPlayerChanged(object? sender, PlayerInfo player)
    {
        if (Dispatcher.UIThread.CheckAccess()) RefreshRow(player.Key);
        else Dispatcher.UIThread.Post(() => RefreshRow(player.Key));
    }

    private void RefreshRow(string key)
    {
        if (!_defaults.TryGetValue(key, out var entry)) return;
        var section = SectionFor(entry.Category);
        if (section.Accounts.FirstOrDefault(r => r.Key == key) is { } row && DisplayRecord(key) is { } record)
            row.Update(record);
    }

    public void Dispose()
    {
        _repo.EntityUpdated -= OnRepoPlayerChanged;
        _repo.PlayerStatusChanged -= OnRepoPlayerChanged;
    }
}
