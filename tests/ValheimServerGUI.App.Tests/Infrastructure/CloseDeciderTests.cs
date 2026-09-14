using ValheimServerGUI.App.Infrastructure;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.App.Tests.Infrastructure;

// §2.4 "safe shutdowns", now aggregated over every running server's status.
public class CloseDeciderTests
{
    private static CloseDecision Decide(bool osShutdown, bool answer, params ServerStatus[] statuses)
        => CloseDecider.Decide(statuses, osShutdown, new RecordingUserPrompt(answer), "t");

    [Fact]
    public void No_servers_proceeds_without_prompting()
    {
        var prompt = new RecordingUserPrompt(answer: false);
        Assert.Equal(CloseDecision.Proceed, CloseDecider.Decide(new ServerStatus[0], false, prompt, "t"));
        Assert.Empty(prompt.Prompts);
    }

    [Fact]
    public void All_stopped_proceeds_without_prompting()
    {
        var prompt = new RecordingUserPrompt(answer: false);
        var decision = CloseDecider.Decide(new[] { ServerStatus.Stopped, ServerStatus.Stopped }, false, prompt, "t");

        Assert.Equal(CloseDecision.Proceed, decision);
        Assert.Empty(prompt.Prompts);
    }

    [Theory]
    [InlineData(ServerStatus.Running)]
    [InlineData(ServerStatus.Starting)]
    public void Any_active_and_confirmed_stops_then_closes(ServerStatus active)
    {
        var prompt = new RecordingUserPrompt(answer: true);
        var decision = CloseDecider.Decide(new[] { ServerStatus.Stopped, active }, false, prompt, "t");

        Assert.Equal(CloseDecision.StopThenClose, decision);
        Assert.Single(prompt.Prompts);
    }

    [Fact]
    public void Any_active_and_declined_cancels()
    {
        Assert.Equal(CloseDecision.Cancel, Decide(osShutdown: false, answer: false, ServerStatus.Running));
    }

    [Fact]
    public void Only_stopping_and_confirmed_proceeds()
    {
        Assert.Equal(CloseDecision.Proceed, Decide(osShutdown: false, answer: true, ServerStatus.Stopping));
    }

    [Fact]
    public void Only_stopping_and_declined_cancels()
    {
        Assert.Equal(CloseDecision.Cancel, Decide(osShutdown: false, answer: false, ServerStatus.Stopping));
    }

    [Fact]
    public void Active_wins_over_stopping_for_the_prompt_branch()
    {
        // A running server present alongside a stopping one → the "stop and exit?" branch (StopThenClose),
        // not the "shutting down, exit anyway?" branch.
        var prompt = new RecordingUserPrompt(answer: true);
        var decision = CloseDecider.Decide(new[] { ServerStatus.Stopping, ServerStatus.Running }, false, prompt, "t");

        Assert.Equal(CloseDecision.StopThenClose, decision);
    }

    [Theory]
    [InlineData(ServerStatus.Running)]
    [InlineData(ServerStatus.Stopping)]
    public void Os_shutdown_stops_then_closes_without_prompting(ServerStatus status)
    {
        var prompt = new RecordingUserPrompt(answer: false);
        var decision = CloseDecider.Decide(new[] { status }, isOsShutdown: true, prompt, "t");

        Assert.Equal(CloseDecision.StopThenClose, decision);
        Assert.Empty(prompt.Prompts); // OS shutdown never prompts — it just flushes the saves
    }
}
