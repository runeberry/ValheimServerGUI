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
