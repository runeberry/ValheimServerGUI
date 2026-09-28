using System.Collections.Generic;
using System.Linq;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.ViewModels;

/// <summary>
/// Where an editor reads and writes a player's cached record (name + known characters). The live repository
/// is the default; the Manage Players dialog substitutes a staged copy so its outer Save/Cancel stays
/// authoritative over edits made in nested dialogs.
/// </summary>
public interface IPlayerRecordStore
{
    PlayerInfo? FindById(string key);

    void Upsert(PlayerInfo player);
}

/// <summary>The live player cache as an <see cref="IPlayerRecordStore"/> (writes land immediately).</summary>
public sealed class RepoPlayerRecordStore : IPlayerRecordStore
{
    private readonly IPlayerDataRepository _repo;

    public RepoPlayerRecordStore(IPlayerDataRepository repo) => _repo = repo;

    public PlayerInfo? FindById(string key) => _repo.FindById(key);

    public void Upsert(PlayerInfo player) => _repo.Upsert(player);
}

/// <summary>
/// A staged view over the live player cache for a dialog with its own Save/Cancel (Manage Players). Reads fall
/// through to the repo as <b>clones</b>, so an editor mutating a record never touches the live cache; writes and
/// removals are held here until <see cref="CommitTo"/>. Only the user-editable fields (name + known characters) are merged
/// onto the live record at commit, so live status and characters recorded meanwhile are not overwritten
/// wholesale by a stale snapshot.
/// </summary>
public sealed class StagedPlayerRecords : IPlayerRecordStore
{
    private readonly IPlayerDataRepository _repo;
    private readonly Dictionary<string, PlayerInfo> _staged = new();
    private readonly HashSet<string> _removed = new();

    public StagedPlayerRecords(IPlayerDataRepository repo) => _repo = repo;

    public PlayerInfo? FindById(string key)
    {
        if (_removed.Contains(key)) return null;
        return _staged.TryGetValue(key, out var staged) ? staged : Clone(_repo.FindById(key));
    }

    public void Upsert(PlayerInfo player)
    {
        _removed.Remove(player.Key);
        _staged[player.Key] = player;
    }

    /// <summary>Stages forgetting a player (the record is deleted from the repo at commit).</summary>
    public void Remove(string key)
    {
        _staged.Remove(key);
        _removed.Add(key);
    }

    /// <summary>Every known player's key: the live cache plus staged additions, minus staged removals.</summary>
    public IEnumerable<string> KnownKeys => _repo.Data.Select(p => p.Key)
        .Concat(_staged.Keys)
        .Distinct()
        .Where(k => !_removed.Contains(k));

    /// <summary>
    /// The record to show for a key: the live record (current status) with any staged name/characters laid
    /// over it, or the staged record alone when it is new. Null when neither exists.
    /// </summary>
    public PlayerInfo? FindForDisplay(string key)
    {
        if (_removed.Contains(key)) return null;
        var live = _repo.FindById(key);
        if (!_staged.TryGetValue(key, out var staged)) return live;
        if (live is null) return staged;

        var merged = Clone(live)!;
        merged.PlayerName = staged.PlayerName;
        merged.Characters = staged.Characters;
        return merged;
    }

    /// <summary>
    /// Applies the staged removals, then writes every staged record to the repo: new records as-is, existing ones
    /// by merging the editable fields onto the live record. Returns the records that were new to the repo.
    /// </summary>
    public IReadOnlyList<PlayerInfo> CommitTo(IPlayerDataRepository repo)
    {
        foreach (var key in _removed) repo.Remove(key);
        _removed.Clear();

        var created = new List<PlayerInfo>();
        foreach (var staged in _staged.Values)
        {
            if (repo.FindById(staged.Key) is { } live)
            {
                live.PlayerName = staged.PlayerName;
                live.Characters = staged.Characters;
                repo.Upsert(live);
            }
            else
            {
                repo.Upsert(staged);
                created.Add(staged);
            }
        }
        _staged.Clear();
        return created;
    }

    private static PlayerInfo? Clone(PlayerInfo? p) => p is null ? null : new PlayerInfo
    {
        Platform = p.Platform,
        PlatformRaw = p.PlatformRaw,
        PlayerId = p.PlayerId,
        PlayerName = p.PlayerName,
        LastStatusChange = p.LastStatusChange,
        LastStatusCharacter = p.LastStatusCharacter,
        PlayerStatus = p.PlayerStatus,
        ZdoId = p.ZdoId,
        Characters = p.Characters?
            .Select(c => new PlayerInfo.CharacterInfo
            {
                CharacterName = c.CharacterName,
                MatchConfident = c.MatchConfident,
                LastSeen = c.LastSeen,
            })
            .ToList(),
    };
}
