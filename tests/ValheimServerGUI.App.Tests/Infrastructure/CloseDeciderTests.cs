using System.Collections.Generic;
using System.Threading.Tasks;
using ValheimServerGUI.App.Infrastructure;
using ValheimServerGUI.Game;
using Xunit;
using ValheimServerGUI.Localization;

namespace ValheimServerGUI.App.Tests.Infrastructure;

// §2.4 "safe shutdowns": the exit decision when the last window closes, aggregated over every server's status.
public class CloseDeciderTests
{
    private readonly List<string> _asked = new();

    private Task<CloseDecision> Decide(bool answer, params ServerStatus[] statuses)
        => CloseDecider.DecideAsync(statuses, message =>
        {
            _asked.Add(message);
            return Task.FromResult(answer);
        });

    [Fact]
    public async Task No_servers_proceeds_without_asking()
    {
        Assert.Equal(CloseDecision.Proceed, await Decide(answer: false));
        Assert.Empty(_asked);
    }

    [Fact]
    public async Task All_stopped_proceeds_without_asking()
    {
        Assert.Equal(CloseDecision.Proceed, await Decide(answer: false, ServerStatus.Stopped, ServerStatus.Stopped));
        Assert.Empty(_asked);
    }

    [Theory]
    [InlineData(ServerStatus.Running)]
    [InlineData(ServerStatus.Starting)]
    public async Task Any_active_and_confirmed_stops_then_closes(ServerStatus active)
    {
        Assert.Equal(CloseDecision.StopThenClose, await Decide(answer: true, ServerStatus.Stopped, active));
        Assert.Equal(new[] { Strings.Prompt_ExitWhileRunning }, _asked);
    }

    [Fact]
    public async Task Any_active_and_declined_cancels()
    {
        Assert.Equal(CloseDecision.Cancel, await Decide(answer: false, ServerStatus.Running));
    }

    [Fact]
    public async Task Only_stopping_and_confirmed_proceeds()
    {
        Assert.Equal(CloseDecision.Proceed, await Decide(answer: true, ServerStatus.Stopping));
        Assert.Equal(new[] { Strings.Prompt_ExitWhileStopping }, _asked);
    }

    [Fact]
    public async Task Only_stopping_and_declined_cancels()
    {
        Assert.Equal(CloseDecision.Cancel, await Decide(answer: false, ServerStatus.Stopping));
    }

    // A running server alongside a stopping one → the "stop and exit?" question (StopThenClose), not the
    // "shutting down, exit anyway?" one.
    [Fact]
    public async Task Active_wins_over_stopping_for_the_question()
    {
        Assert.Equal(CloseDecision.StopThenClose, await Decide(answer: true, ServerStatus.Stopping, ServerStatus.Running));
        Assert.Equal(new[] { Strings.Prompt_ExitWhileRunning }, _asked);
    }
}
