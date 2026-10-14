using Serilog;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.Services;

/// <summary>
/// Shell implementation of the Core <see cref="IStartupManager"/> seam. Delegates to the per-OS
/// <see cref="IStartupStrategy"/> chosen at composition time. Failures are logged and swallowed — an
/// autostart write should never crash the preferences dialog.
/// </summary>
internal sealed class StartupManager : IStartupManager
{
    private readonly IStartupStrategy _strategy;
    private readonly ILogger _logger;

    public StartupManager(IStartupStrategy strategy, ILogger logger)
    {
        _strategy = strategy;
        _logger = logger;
    }

    public bool ApplyStartupSetting(bool runOnStartup)
    {
        try
        {
            return _strategy.Apply(runOnStartup);
        }
        catch (System.Exception ex)
        {
            _logger.Error(ex, "Failed to apply run-on-startup setting ({RunOnStartup})", runOnStartup);
            return false;
        }
    }
}
