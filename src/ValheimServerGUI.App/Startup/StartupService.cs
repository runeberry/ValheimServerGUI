using System;
using System.Threading.Tasks;
using Serilog;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.Startup;

/// <summary>
/// Runs the async startup tasks the v2.4 <c>SplashForm</c> ran before opening the main windows (§2.1):
/// the update check and the one-time player-roster hydration (<see cref="IPlayerDataRepository.LoadAsync"/>
/// MUST be called once at startup). Each task is isolated so a failure (e.g. no network for the update
/// check) reports progress and continues rather than aborting the launch.
/// </summary>
public sealed class StartupService
{
    private readonly ISoftwareUpdateProvider _updateProvider;
    private readonly IPlayerDataRepository _playerData;
    private readonly ILogger _logger;

    public StartupService(ISoftwareUpdateProvider updateProvider, IPlayerDataRepository playerData, ILogger logger)
    {
        _updateProvider = updateProvider;
        _playerData = playerData;
        _logger = logger;
    }

    public async Task RunAsync(IProgress<StartupProgress>? progress = null)
    {
        progress?.Report(new StartupProgress("Checking for updates…", 0.15));
        await RunSafelyAsync("update check", () => _updateProvider.CheckForUpdatesAsync(isManualCheck: false));

        progress?.Report(new StartupProgress("Loading player data…", 0.6));
        await RunSafelyAsync("player-data load", () => _playerData.LoadAsync());

        progress?.Report(new StartupProgress("Ready", 1.0));
    }

    private async Task RunSafelyAsync(string what, Func<Task> task)
    {
        try
        {
            await task();
        }
        catch (Exception ex)
        {
            // A startup task failing must never block the app from opening.
            _logger.Error(ex, "Startup task failed: {Task}", what);
        }
    }
}
