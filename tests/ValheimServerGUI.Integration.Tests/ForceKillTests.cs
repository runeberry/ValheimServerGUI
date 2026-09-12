using System;
using System.Threading.Tasks;
using Xunit;

namespace ValheimServerGUI.Integration.Tests
{
    /// <summary>
    /// The B half of the E9 A/B (the A half is <see cref="LiveLifecycleTests.GracefulStop_FlushesWorldSave"/>):
    /// a hard SIGKILL of a running server must NOT flush a world save. Without this negative leg, "graceful
    /// stop flushes a save" proves nothing — the save could be happening on a timer, not because of SIGINT.
    /// </summary>
    public sealed class ForceKillTests
    {
        private static readonly TimeSpan BootTimeout = TimeSpan.FromSeconds(150);
        private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan Settle = TimeSpan.FromSeconds(3);

        [Fact]
        public async Task ForceKill_DoesNotFlushSave()
        {
            IntegrationConfig.SkipIfServerUnconfigured();

            var worldName = "ITKill" + Guid.NewGuid().ToString("n")[..6];
            using var harness = new LiveServerHarness(IntegrationConfig.SaveDir, worldName);

            var running = await harness.BootToRunningAsync(BootTimeout);
            Assert.True(running, "Precondition: server must reach Running before the force-kill A/B is meaningful.");

            // Let the post-gen save settle, then snapshot the filesystem truth and the parsed-save count.
            await Task.Delay(Settle, TestContext.Current.CancellationToken);
            var saveBefore = harness.LatestWorldSaveTimeUtc();
            var savesBefore = harness.WorldSaves.Count;
            Assert.NotNull(saveBefore); // the world must already be on disk, or there's nothing to compare

            var stopped = await harness.ForceKillAsync(StopTimeout);
            Assert.True(stopped, "Force-killed server did not reach Stopped.");

            // Give a (non-existent) async save the same window the graceful path gets, to be fair.
            await Task.Delay(Settle, TestContext.Current.CancellationToken);
            var saveAfter = harness.LatestWorldSaveTimeUtc();
            var savesAfter = harness.WorldSaves.Count;

            Assert.Equal(saveBefore, saveAfter);       // SIGKILL flushes nothing: world mtime must not advance
            Assert.Equal(savesBefore, savesAfter);     // and no 'World saved' line is parsed after the kill
        }
    }
}
