using System;
using System.Collections.Generic;
using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.App.ViewModels;

/// <summary>
/// The single add-a-player routine behind every Add Player entry point (Players tab, Manage Players tables).
/// It creates or annotates the player's record and applies the chosen role either as the player's default role
/// or as the current server's override. Callers supply the stores, so the Players tab writes live while Manage
/// Players writes into its staged copies.
/// </summary>
public static class AddPlayerFlow
{
    /// <summary>The outcome: the player's record and whether it was new (so the caller can look up its name).</summary>
    public sealed record Outcome(PlayerInfo Player, bool IsNewRecord);

    /// <param name="result">The dialog result.</param>
    /// <param name="records">Where the player's record is read and written.</param>
    /// <param name="defaults">The player default roles to update (key = <see cref="PlayerInfo.Key"/>).</param>
    /// <param name="setServerOverride">Sets (or clears, with null) the current server's override; null when there
    /// is no server context (Manage Players), in which case the result always applies as a default.</param>
    /// <returns>The outcome, or null when the result names no valid platform/ID.</returns>
    public static Outcome? Apply(
        AddPlayerResult result,
        IPlayerRecordStore records,
        IDictionary<string, PlayerDefaultEntry> defaults,
        Action<PlayerInfo, PlayerRole?>? setServerOverride)
    {
        if (!PlayerPlatforms.TryGetValidPlatform(result.Platform, out var platform) || platform is null) return null;
        if (string.IsNullOrWhiteSpace(result.PlayerId)) return null;

        var playerId = result.PlayerId.Trim();
        var key = $"{platform}:{playerId}";

        // Create/annotate the record so the player appears in the tables. For a manually-entered ID the normalized
        // platform name IS the canonical write token, so PlatformRaw = the platform name.
        var existing = records.FindById(key);
        var player = existing ?? new PlayerInfo
        {
            Platform = platform,
            PlatformRaw = platform,
            PlayerId = playerId,
            PlayerStatus = PlayerStatus.Offline,
            LastStatusChange = DateTimeOffset.UtcNow,
        };
        if (string.IsNullOrWhiteSpace(player.PlatformRaw)) player.PlatformRaw = platform;
        if (result.PlayerName is { } name) player.PlayerName = name;

        if (result.AsDefault || setServerOverride is null)
        {
            // None means "no default role".
            if (result.Role == PlayerRole.None) defaults.Remove(key);
            else defaults[key] = new PlayerDefaultEntry(result.Role, player.PlatformRaw);

            // From a server, the new default should actually apply there: drop any override that would mask it.
            setServerOverride?.Invoke(player, null);
        }
        else if (result.Role == PlayerRole.None)
        {
            // No role on this server: pin None over a default role; otherwise just clear any override.
            setServerOverride(player, defaults.ContainsKey(key) ? PlayerRole.None : null);
        }
        else
        {
            setServerOverride(player, result.Role);
        }

        records.Upsert(player);
        return new Outcome(player, existing is null);
    }
}
