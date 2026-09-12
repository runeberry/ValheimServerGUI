using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.Tests.Fakes;

/// <summary>
/// Minimal in-memory <see cref="IPlayerDataRepository"/>. Records <see cref="LoadAsync"/> calls and lets a
/// test push players + raise the live-update events the Players tab (Wave 5) binds.
/// </summary>
internal sealed class FakePlayerDataRepository : IPlayerDataRepository
{
    private readonly List<PlayerInfo> _players = new();

    public int LoadCount { get; private set; }
    public bool ThrowOnLoad { get; set; }

    public event EventHandler? DataReady;
    public event EventHandler? DataUpdated;
    public event EventHandler? DataCleared;
    public event EventHandler<PlayerInfo>? EntityUpdated;
    public event EventHandler<PlayerInfo>? EntityRemoved;
    public event EventHandler<PlayerInfo>? PlayerStatusChanged;

    public IEnumerable<PlayerInfo> Data => _players.ToList();

    public Task LoadAsync()
    {
        LoadCount++;
        if (ThrowOnLoad) throw new InvalidOperationException("load failed");
        DataReady?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    // --- test helpers ---
    public void PushUpdate(PlayerInfo player)
    {
        _players.RemoveAll(p => p.Key == player.Key);
        _players.Add(player);
        EntityUpdated?.Invoke(this, player);
        DataUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void RaiseStatusChanged(PlayerInfo player) => PlayerStatusChanged?.Invoke(this, player);

    // --- IDataRepository<PlayerInfo> ---
    public PlayerInfo? FindById(string id) => _players.FirstOrDefault(p => p.Key == id);
    public void Upsert(PlayerInfo entity) => PushUpdate(entity);
    public void UpsertBulk(IEnumerable<PlayerInfo> entities) { foreach (var e in entities) PushUpdate(e); }
    public void Remove(string key)
    {
        var found = FindById(key);
        if (found is null) return;
        _players.Remove(found);
        EntityRemoved?.Invoke(this, found);
    }
    public void Remove(PlayerInfo entity) => Remove(entity.Key);
    public void RemoveBulk(IEnumerable<string> keys) { foreach (var k in keys) Remove(k); }
    public void RemoveBulk(IEnumerable<PlayerInfo> entities) { foreach (var e in entities) Remove(e); }
    public void RemoveAll() { _players.Clear(); DataCleared?.Invoke(this, EventArgs.Empty); }

    // --- IPlayerDataRepository ---
    public IEnumerable<PlayerInfo> FindPlayersByQuery(PlayerDataQuery query) => _players.ToList();
    public PlayerInfo? SetPlayerJoining(PlayerDataQuery query) => null;
    public PlayerInfo? SetPlayerOnline(string characterName, string zdoId) => null;
    public void SetPlayerLeaving(PlayerDataQuery query) { }
    public void SetPlayerOffline(PlayerDataQuery query) { }
}
