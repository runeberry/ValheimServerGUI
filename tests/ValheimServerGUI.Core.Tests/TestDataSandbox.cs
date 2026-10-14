using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace ValheimServerGUI.Core.Tests;

internal static class TestDataSandbox
{
    // Redirect the app-data roots to a throwaway temp dir for the whole suite (as App.Tests'
    // TestAppBuilder.RedirectAppDataRoots does), so a test that resolves real Core services (loggers,
    // preference providers, data repositories) never reads or writes the developer's real VSG data: XDG_* for
    // the Linux resolver, USERPROFILE for the Windows resolver. Setting USERPROFILE only changes %USERPROFILE%
    // expansion in this process; Environment.GetFolderPath ignores it. Runs at assembly load, before any path
    // resolver is constructed from DI.
    [ModuleInitializer]
    internal static void RedirectAppDataRoots()
    {
        var root = Path.Combine(Path.GetTempPath(), "valheim-server-gui-tests", Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("XDG_DATA_HOME", Path.Combine(root, "data"));
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", Path.Combine(root, "config"));
        Environment.SetEnvironmentVariable("XDG_STATE_HOME", Path.Combine(root, "state"));
        Environment.SetEnvironmentVariable("XDG_CACHE_HOME", Path.Combine(root, "cache"));
        Environment.SetEnvironmentVariable("USERPROFILE", Path.Combine(root, "userprofile"));
    }
}
