using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Serilog;

namespace ValheimServerGUI.Game;

/// <summary>
/// App-wide registry of per-profile <see cref="ValheimServer"/> instances (the multi-profile control panel).
/// A window re-targets its status/controls/tabs onto the selected profile's server; multiple servers run
/// independently and are shared across every window via this singleton. Each entry owns its server plus a
/// server-owned log buffer, so server log lines land in the right buffer regardless of which window started
/// the server and survive that window closing. Keyed case-insensitively to match <see cref="ServerPreferences"/>.
/// </summary>
public interface IServerManager
{
    /// <summary>Returns the server for the profile, creating it on first ask (idempotent).</summary>
    ValheimServer GetOrCreate(string profileName);

    bool TryGet(string profileName, out ValheimServer server);

    /// <summary>Every server currently held (a snapshot).</summary>
    IReadOnlyCollection<ValheimServer> All { get; }

    /// <summary>The server-owned, UI-bindable log buffer for the profile (created on first ask).</summary>
    ObservableCollection<string> GetServerLog(string profileName);

    /// <summary>The log-line handler wired into the server's options at Start (appends to the owned buffer).</summary>
    Action<string> GetLogAppender(string profileName);

    /// <summary>Stops (if running) then disposes and drops the profile's server — used on profile deletion.</summary>
    void Remove(string profileName);

    /// <summary>Gracefully stops every server and blocks until each reports Stopped, then disposes (app shutdown).</summary>
    void StopAllAndDispose();

    /// <summary>
    /// Any held server adopted list-file changes made during play into its role overrides (see
    /// <see cref="ValheimServer.PlayerRolesAdopted"/>); carries the server's profile name so the owner can persist
    /// them. May be raised off the UI thread.
    /// </summary>
    event EventHandler<PlayerRolesAdoptedEventArgs>? PlayerRolesAdopted;
}

/// <summary>A server's adopted list-file changes, tagged with the profile it runs.</summary>
public sealed record PlayerRolesAdoptedEventArgs(string ProfileName, ReconcileResult Result);

public sealed class ServerManager : IServerManager
{
    /// <summary>
    /// Cap on the lines kept per server buffer, bounding memory and render cost. Older lines fall off the top.
    /// </summary>
    private const int MaxLogLines = 5000;

    private sealed class ServerEntry
    {
        public required ValheimServer Server { get; init; }
        public ObservableCollection<string> ServerLog { get; } = new();
    }

    private readonly ConcurrentDictionary<string, ServerEntry> _entries =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<ValheimServer> _serverFactory;
    private readonly ILogger _logger;
    private readonly SynchronizationContext? _uiContext;

    public ServerManager(Func<ValheimServer> serverFactory, ILogger logger)
    {
        _serverFactory = serverFactory;
        _logger = logger;

        // Captured on the UI thread (the singleton is first resolved while the first window is built). Server
        // log lines arrive on the server's own background thread, so appending to the bound ObservableCollection
        // must marshal back to this context or Avalonia throws. Null in headless tests → append inline.
        _uiContext = SynchronizationContext.Current;
    }

    public event EventHandler<PlayerRolesAdoptedEventArgs>? PlayerRolesAdopted;

    public ValheimServer GetOrCreate(string profileName) => GetEntry(profileName).Server;

    public bool TryGet(string profileName, out ValheimServer server)
    {
        if (_entries.TryGetValue(profileName, out var entry))
        {
            server = entry.Server;
            return true;
        }

        server = null!;
        return false;
    }

    public IReadOnlyCollection<ValheimServer> All => _entries.Values.Select(e => e.Server).ToList();

    public ObservableCollection<string> GetServerLog(string profileName) => GetEntry(profileName).ServerLog;

    public Action<string> GetLogAppender(string profileName)
    {
        var entry = GetEntry(profileName);
        return line => AppendServerLine(entry, line);
    }

    public void Remove(string profileName)
    {
        if (!_entries.TryRemove(profileName, out var entry)) return;

        try
        {
            entry.Server.Dispose(); // Dispose gracefully stops a running server first.
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error disposing server for profile {profile}", profileName);
        }
    }

    public void StopAllAndDispose()
    {
        var servers = _entries.ToList();

        // Kick off a graceful stop on every server first (they shut down in parallel), then block until each
        // one reports Stopped before disposing. There is no time limit: a server saves the world on the way down
        // and is never force-killed, so returning before Stopped would lose that save. Reaching
        // Stopped does not depend on this thread (the process Exited callback flips Status on a background
        // thread). Each server stays registered until it is disposed, so while servers are still stopping a
        // second window close sees them and offers the "close anyway" prompt rather than exiting silently.
        var pending = new List<(string Profile, ValheimServer Server, ManualResetEventSlim Stopped, EventHandler<ServerStatus> Handler)>();

        foreach (var (profile, entry) in servers)
        {
            var server = entry.Server;
            var stopped = new ManualResetEventSlim(false);
            EventHandler<ServerStatus> handler = (_, s) =>
            {
                if (s == ServerStatus.Stopped) stopped.Set();
            };

            server.StatusChanged += handler;
            if (server.Status == ServerStatus.Stopped) stopped.Set(); // already Stopped, or became so before we subscribed
            server.Stop(); // no-op if not running

            pending.Add((profile, server, stopped, handler));
        }

        foreach (var (profile, server, stopped, handler) in pending)
        {
            stopped.Wait();
            server.StatusChanged -= handler;
            stopped.Dispose();

            try
            {
                server.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error disposing server on shutdown");
            }

            _entries.TryRemove(profile, out _);
        }
    }

    private ServerEntry GetEntry(string profileName)
        => _entries.GetOrAdd(profileName, _ =>
        {
            var server = _serverFactory();
            server.PlayerRolesAdopted += (_, result)
                => PlayerRolesAdopted?.Invoke(this, new PlayerRolesAdoptedEventArgs(profileName, result));
            return new ServerEntry { Server = server };
        });

    private void AppendServerLine(ServerEntry entry, string line)
    {
        void Apply()
        {
            entry.ServerLog.Add(line);
            while (entry.ServerLog.Count > MaxLogLines) entry.ServerLog.RemoveAt(0);
        }

        if (_uiContext is null || _uiContext == SynchronizationContext.Current)
            Apply();
        else
            _uiContext.Post(_ => Apply(), null);
    }
}
