using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ValheimServerGUI.App.Startup;
using ValheimServerGUI.App.Tests.Fakes;
using Xunit;

namespace ValheimServerGUI.App.Tests.Startup;

public class StartupServiceTests
{
    private sealed class ProgressRecorder : IProgress<StartupProgress>
    {
        public List<StartupProgress> Reports { get; } = new();
        public void Report(StartupProgress value) => Reports.Add(value);
    }

    private static StartupService Make(FakeSoftwareUpdateProvider update, FakePlayerDataRepository players)
        => new(update, players, TestLog.Silent);

    [Fact]
    public async Task RunAsync_checks_updates_and_loads_players_once()
    {
        var update = new FakeSoftwareUpdateProvider();
        var players = new FakePlayerDataRepository();

        await Make(update, players).RunAsync();

        Assert.Equal(1, update.CheckCount);
        Assert.False(update.LastIsManual); // startup check is non-manual
        Assert.Equal(1, players.LoadCount); // roster hydrated exactly once
    }

    [Fact]
    public async Task RunAsync_reports_progress_reaching_complete()
    {
        var recorder = new ProgressRecorder();

        await Make(new FakeSoftwareUpdateProvider(), new FakePlayerDataRepository()).RunAsync(recorder);

        Assert.NotEmpty(recorder.Reports);
        Assert.Equal(1.0, recorder.Reports[^1].Fraction);
    }

    [Fact]
    public async Task RunAsync_is_resilient_to_a_failing_task()
    {
        var update = new FakeSoftwareUpdateProvider { ThrowOnCheck = true };
        var players = new FakePlayerDataRepository();

        await Make(update, players).RunAsync(); // must not throw

        Assert.Equal(1, players.LoadCount); // player load still ran after the update check failed
    }
}
