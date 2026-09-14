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
}

public sealed class ServerManager : IServerManager
{
    /// <summary>
    /// Cap on the lines kept per server buffer — bounds memory/render cost the same way the Logs tab did
    /// before this buffer moved out of the view-model. Older lines fall off the top.
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
        var servers = _entries.Values.Select(e => e.Server).ToList();
        _entries.Clear();

        // Kick off a graceful stop on every server first (they shut down in parallel), then block until each
        // one reports Stopped before disposing. Each server's own GracefulStopTimeout force-kills a hung
        // process, so the wait is bounded, and reaching Stopped does not depend on this thread (the process
        // Exited callback flips Status on a background thread). Returning before Stopped would skip the
        // world-save flush the graceful stop provides (§16.2 / E9) — the whole point of this path.
        var pending = new List<(ValheimServer Server, ManualResetEventSlim Stopped, EventHandler<ServerStatus> Handler)>();

        foreach (var server in servers)
        {
            var stopped = new ManualResetEventSlim(false);
            EventHandler<ServerStatus> handler = (_, s) =>
            {
                if (s == ServerStatus.Stopped) stopped.Set();
            };

            server.StatusChanged += handler;
            if (server.Status == ServerStatus.Stopped) stopped.Set(); // already Stopped, or became so before we subscribed
            server.Stop(); // no-op if not running

            pending.Add((server, stopped, handler));
        }

        foreach (var (server, stopped, handler) in pending)
        {
            // A little slack over the graceful timeout so the force-kill fallback can complete.
            var budget = server.GracefulStopTimeout > TimeSpan.Zero
                ? server.GracefulStopTimeout + TimeSpan.FromSeconds(5)
                : TimeSpan.FromSeconds(35);
            stopped.Wait(budget);
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
        }
    }

    private ServerEntry GetEntry(string profileName)
        => _entries.GetOrAdd(profileName, _ => new ServerEntry { Server = _serverFactory() });

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
