using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace ValheimServerGUI.App.Infrastructure;

/// <summary>
/// Enforces single-instance, multi-window (§2.2): a named system mutex elects the primary process; a
/// second launch forwards its args to the primary over a named pipe (cross-platform: a Unix domain socket
/// on Linux) and exits, and the primary raises <see cref="ArgsReceived"/> so it can open/focus a window.
/// Best-effort — if the IPC fails, the second instance still exits (the MVP "block the second instance"
/// fallback), never clobbering the shared <c>userprefs.json</c>.
/// </summary>
internal sealed class SingleInstanceManager : IDisposable
{
    private const string MutexName = "ValheimServerGUI.SingleInstance";
    private const string PipeName = "ValheimServerGUI.Ipc";

    private readonly CancellationTokenSource _cts = new();
    private Mutex? _mutex;

    public bool IsPrimary { get; private set; }

    /// <summary>Raised on a background thread when a second launch forwards its args to this (primary) process.</summary>
    public event Action<string[]>? ArgsReceived;

    /// <summary>Returns true if this process is the primary (mutex acquired); false if another instance holds it.</summary>
    public bool TryAcquire()
    {
        try
        {
            _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
            IsPrimary = createdNew;
        }
        catch (AbandonedMutexException)
        {
            // Previous owner crashed without releasing; we now own it.
            IsPrimary = true;
        }

        return IsPrimary;
    }

    /// <summary>Secondary process: hand our args to the primary. Silent best-effort.</summary>
    public void ForwardArgs(string[] args)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
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
                    PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
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
        try
        {
            if (IsPrimary) _mutex?.ReleaseMutex();
        }
        catch
        {
            // Not the owner / already released.
        }
        _mutex?.Dispose();
    }
}
