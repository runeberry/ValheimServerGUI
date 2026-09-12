using System;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace ValheimServerGUI.App;

internal static class Program
{
    // Avalonia + SynchronizationContext-reliant code must not run before AppMain; keep Main minimal.
    [STAThread]
    public static void Main(string[] args)
    {
        var services = ServiceConfiguration.BuildServiceProvider(args);
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
