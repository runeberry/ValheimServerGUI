using System;
using Avalonia;
using ValheimServerGUI.App.Infrastructure;

namespace ValheimServerGUI.App;

internal static class Program
{
    // Avalonia + SynchronizationContext-reliant code must not run before AppMain; keep Main minimal.
    [STAThread]
    public static void Main(string[] args)
    {
        // Single-instance, multi-window (§2.2): a second launch forwards its args to the primary and exits.
        var singleInstance = new SingleInstanceManager();
        if (!singleInstance.TryAcquire())
        {
            singleInstance.ForwardArgs(args);
            singleInstance.Dispose();
            return;
        }

        var services = ServiceConfiguration.BuildServiceProvider(args, singleInstance);
        BuildAvaloniaApp(services).StartWithClassicDesktopLifetime(args);
    }

    // Composition root: the running app is constructed with the fully-built provider (see App).
    public static AppBuilder BuildAvaloniaApp(IServiceProvider services)
        => AppBuilder.Configure(() => new App(services))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    // Parameterless overload for the Avalonia visual designer only. The designer never runs the real
    // DI graph, so it gets an App that lazily builds a default provider (see App.Services).
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
