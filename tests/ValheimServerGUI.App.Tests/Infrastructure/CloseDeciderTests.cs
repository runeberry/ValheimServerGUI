using ValheimServerGUI.App.Infrastructure;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.App.Tests.Infrastructure;

// §2.4 "safe shutdowns" close matrix.
public class CloseDeciderTests
{
    [Fact]
    public void Stopped_proceeds_without_prompting()
    {
        var prompt = new RecordingUserPrompt(answer: false);
        var decision = CloseDecider.Decide(ServerStatus.Stopped, isOsShutdown: false, prompt, "t");

        Assert.Equal(CloseDecision.Proceed, decision);
        Assert.Empty(prompt.Prompts);
    }

    [Theory]
    [InlineData(ServerStatus.Running)]
    [InlineData(ServerStatus.Starting)]
    public void Running_and_confirmed_stops_then_closes(ServerStatus status)
    {
        var prompt = new RecordingUserPrompt(answer: true);
        var decision = CloseDecider.Decide(status, isOsShutdown: false, prompt, "t");

        Assert.Equal(CloseDecision.StopThenClose, decision);
        Assert.Single(prompt.Prompts);
    }

    [Fact]
    public void Running_and_declined_cancels()
    {
        var prompt = new RecordingUserPrompt(answer: false);
        Assert.Equal(CloseDecision.Cancel, CloseDecider.Decide(ServerStatus.Running, false, prompt, "t"));
    }

    [Fact]
    public void Stopping_and_confirmed_proceeds()
    {
        var prompt = new RecordingUserPrompt(answer: true);
        Assert.Equal(CloseDecision.Proceed, CloseDecider.Decide(ServerStatus.Stopping, false, prompt, "t"));
    }

    [Fact]
    public void Stopping_and_declined_cancels()
    {
        var prompt = new RecordingUserPrompt(answer: false);
        Assert.Equal(CloseDecision.Cancel, CloseDecider.Decide(ServerStatus.Stopping, false, prompt, "t"));
    }

    [Theory]
    [InlineData(ServerStatus.Running)]
    [InlineData(ServerStatus.Stopping)]
    public void Os_shutdown_stops_then_closes_without_prompting(ServerStatus status)
    {
        var prompt = new RecordingUserPrompt(answer: false);
        var decision = CloseDecider.Decide(status, isOsShutdown: true, prompt, "t");

        Assert.Equal(CloseDecision.StopThenClose, decision);
        Assert.Empty(prompt.Prompts); // OS shutdown never prompts — it just flushes the save
    }
}
