using Serilog;

namespace ValheimServerGUI.App.Tests;

/// <summary>A no-op Serilog logger for seams that only log diagnostics.</summary>
internal static class TestLog
{
    public static ILogger Silent { get; } = new LoggerConfiguration().CreateLogger();
}
