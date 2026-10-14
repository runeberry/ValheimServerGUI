using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Serilog.Events;
using ValheimServerGUI.Core.Tests.Fakes;
using ValheimServerGUI.Tools.Processes;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Tools;

// A graceful stop is delivered by a helper process (taskkill / kill). When the helper fails, the server keeps
// running with no other sign of why, so the failure and the helper's own output must reach the app log.
public class ProcessProviderTests
{
    private const string ServerKey = "server";

    private readonly FakeApplicationLogger _log = new();

    // The "server" is this test process: the injected stop command never signals it, and GetProcess only needs
    // a live, tracked process.
    private ProcessProvider NewProvider((string Command, string Arguments) stopCommand)
    {
        var provider = new ProcessProvider(_log, _ => stopCommand);
        provider.AddProcess(ServerKey, Process.GetCurrentProcess());
        return provider;
    }

    // The same fixture command on either OS: print a line to stderr, then exit with the given code.
    private static (string Command, string Arguments) Shell(int exitCode) => OperatingSystem.IsWindows()
        ? ("cmd", $"/c echo refused by test 1>&2 & exit /b {exitCode}")
        : ("sh", $"-c \"echo refused by test >&2; exit {exitCode}\"");

    private static string StopKey => $"stop-{Environment.ProcessId}";

    private static void WaitFor(Func<bool> condition, int timeoutMs = 5000)
    {
        var sw = Stopwatch.StartNew();
        while (!condition() && sw.ElapsedMilliseconds < timeoutMs) Thread.Sleep(10);
    }

    [Fact]
    public void A_refused_stop_request_is_logged_with_the_helper_output()
    {
        var provider = NewProvider(Shell(3));

        provider.SafelyKillProcess(ServerKey);
        WaitFor(() => _log.Events.Any(e => e.Level == LogEventLevel.Warning));

        var warning = Assert.Single(_log.Events, e => e.Level == LogEventLevel.Warning);
        var message = warning.RenderMessage();
        Assert.Contains(Environment.ProcessId.ToString(), message);
        Assert.Contains("exited with code 3", message);
        Assert.Contains("refused by test", message);
    }

    [Fact]
    public void A_successful_stop_request_logs_nothing()
    {
        var provider = NewProvider(Shell(0));

        provider.SafelyKillProcess(ServerKey);
        WaitFor(() => provider.GetProcess(StopKey) is null);
        Thread.Sleep(200); // let the helper's Exited handler run

        Assert.Empty(_log.Events);
    }

    [Fact]
    public void A_stop_helper_that_cannot_start_is_logged_as_an_error_and_untracked()
    {
        var provider = NewProvider(("vsg-no-such-stop-command", string.Empty));

        provider.SafelyKillProcess(ServerKey); // must not throw out of Stop

        var error = Assert.Single(_log.Events);
        Assert.Equal(LogEventLevel.Error, error.Level);
        Assert.NotNull(error.Exception);
        Assert.Null(provider.GetProcess(StopKey));
    }
}
