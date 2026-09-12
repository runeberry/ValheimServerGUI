using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(ValheimServerGUI.App.Tests.TestAppBuilder))]

// The Avalonia headless platform is single-threaded; running tests in parallel across it corrupts the
// shared dispatcher/render loop. Serialize the whole suite.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace ValheimServerGUI.App.Tests;

/// <summary>
/// Boots the real <see cref="global::ValheimServerGUI.App.App"/> (Fluent theme and all) on the in-process
/// headless platform. <c>UseHeadlessDrawing = false</c> + Skia means rendered frames are real pixels, so
/// the display path can be asserted.
/// </summary>
public static class TestAppBuilder
{
    // Pin the UI culture so any localized/formatted assertion is deterministic regardless of the machine
    // locale. Runs at assembly load, before xUnit creates any worker thread, so the default-thread culture
    // covers every worker too.
    [ModuleInitializer]
    internal static void PinCulture()
    {
        var en = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = en;
        CultureInfo.DefaultThreadCurrentUICulture = en;
        CultureInfo.CurrentCulture = en;
        CultureInfo.CurrentUICulture = en;
    }

    // Redirect the app-data / XDG roots to a throwaway temp dir for the whole suite, so any test that
    // touches the preference providers or the data repository writes there instead of the developer's real
    // ~/.local/share / ~/.config. Runs at assembly load, before any path resolver is constructed from DI.
    [ModuleInitializer]
    internal static void RedirectAppDataRoots()
    {
        var root = Path.Combine(Path.GetTempPath(), "valheim-server-gui-tests", Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("XDG_DATA_HOME", Path.Combine(root, "data"));
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", Path.Combine(root, "config"));
        Environment.SetEnvironmentVariable("XDG_STATE_HOME", Path.Combine(root, "state"));
        Environment.SetEnvironmentVariable("XDG_CACHE_HOME", Path.Combine(root, "cache"));
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<global::ValheimServerGUI.App.App>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
