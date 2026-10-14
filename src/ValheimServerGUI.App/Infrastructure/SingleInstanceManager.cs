using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ValheimServerGUI.App.Infrastructure;

/// <summary>
/// Enforces single-instance, multi-window (§2.2) <b>per user</b>: an exclusive lock file in this user's
/// own app config directory elects the primary process; a second launch forwards its args to the primary
/// over a per-user named pipe (a Unix domain socket on Linux) and exits, and the primary raises
/// <see cref="ArgsReceived"/> so it can open/focus a window. Best-effort — if the IPC fails, the second
/// instance still exits (the MVP "block the second instance" fallback), never clobbering the user's
/// <c>userprefs.json</c>.
/// <para>
/// A lock file (not a named <see cref="Mutex"/>) is used deliberately: a .NET named mutex on Linux is
/// backed by a user-private <c>/tmp/.dotnet/shm</c> directory shared machine-wide, so on a multi-user box
/// whoever runs .NET first owns it and every other user gets an <c>EACCES</c> that crashes startup. The
/// lock file lives under each user's own config dir, so instances are isolated per user with no shared
/// global state, and the lock is released automatically if the process dies.
/// </para>
/// </summary>
internal sealed class SingleInstanceManager : IDisposable
{
    private const string LockFileName = "singleton.lock";

    private readonly CancellationTokenSource _cts = new();
    private readonly string _pipeName = $"ValheimServerGUI.Ipc.{UserToken()}";
    private FileStream? _lock;

    public bool IsPrimary { get; private set; }

    /// <summary>Raised on a background thread when a second launch forwards its args to this (primary) process.</summary>
    public event Action<string[]>? ArgsReceived;

    /// <summary>
    /// Returns true if this process is the primary (acquired the per-user lock); false only when another
    /// instance of this app for the same user already holds it. Any other failure degrades to primary so
    /// the single-instance guard never blocks the app from launching.
    /// </summary>
    public bool TryAcquire()
    {
        try
        {
            var dir = GetInstanceDirectory();
            Directory.CreateDirectory(dir);
            // FileShare.None takes an exclusive lock (an advisory flock on Unix). A second instance's open
            // then throws IOException, which is our "someone else is primary" signal.
            _lock = new FileStream(
                Path.Combine(dir, LockFileName), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            IsPrimary = true;
        }
        catch (IOException)
        {
            // Another instance (same user) holds the lock -> we are secondary.
            IsPrimary = false;
        }
        catch (Exception)
        {
            // Perms / unexpected: never block startup on the guard -- launch as primary.
            IsPrimary = true;
        }

        return IsPrimary;
    }

    /// <summary>Per-user, app-owned directory for the lock file (LocalAppData on Windows, XDG config on Unix).</summary>
    private static string GetInstanceDirectory()
    {
        string baseDir;
        if (OperatingSystem.IsWindows())
        {
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        else
        {
            baseDir = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? string.Empty;
            if (string.IsNullOrEmpty(baseDir))
                baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        }

        return Path.Combine(baseDir, AppConstants.StartupKey);
    }

    /// <summary>
    /// Short stable per-user token so the IPC pipe name is distinct between users on the same machine
    /// (the pipe maps to a shared <c>/tmp/CoreFxPipe_*</c> path on Linux, which would otherwise collide).
    /// </summary>
    private static string UserToken()
    {
        var seed = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(seed)) seed = Environment.UserName ?? "default";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return Convert.ToHexString(hash, 0, 4);
    }

    /// <summary>Secondary process: hand our args to the primary. Silent best-effort.</summary>
    public void ForwardArgs(string[] args)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
            client.Connect(2000);
            using var writer = new StreamWriter(client) { AutoFlush = true };
            writer.WriteLine(args.Length);
            foreach (var arg in args)
                writer.WriteLine(arg);
        }
        catch
        {
            // Primary may be mid-startup or the pipe unavailable; nothing more we can do — just exit.
        }
    }

    /// <summary>Primary process: begin accepting forwarded args from later launches.</summary>
    public void StartListening()
    {
        if (!IsPrimary) return;
        _ = Task.Run(() => ListenLoopAsync(_cts.Token));
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    _pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(ct);

                using var reader = new StreamReader(server);
                var countLine = await reader.ReadLineAsync(ct);
                if (!int.TryParse(countLine, out var count) || count < 0) continue;

                var args = new List<string>(count);
                for (var i = 0; i < count; i++)
                {
                    var line = await reader.ReadLineAsync(ct);
                    if (line is not null) args.Add(line);
                }

                ArgsReceived?.Invoke(args.ToArray());
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // A malformed/aborted connection shouldn't stop us serving the next one.
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        // Closing the stream releases the exclusive lock; the OS also releases it if the process dies.
        _lock?.Dispose();
    }
}
