using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Infrastructure;
using ValheimServerGUI.App.Startup;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Logging;
using Xunit;

namespace ValheimServerGUI.App.Tests.Startup;

// Repro guard for the "New Window -> close it -> fatal 'A task was canceled'" report: closing a secondary
// window must tear its view-model down cleanly, leaving the other window open and raising no unhandled or
// unobserved exception on any channel.
public class WindowLifecycleTests
{
    private static readonly IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    [AvaloniaFact]
    public void Closing_a_second_window_is_clean()
    {
        var logger = Core.GetRequiredService<IApplicationLogger>();
        var serverPrefs = new FakeServerPreferencesProvider(new[] { new ServerPreferences { ProfileName = "A" } });
        var userPrefs = new FakeUserPreferencesProvider();
        var shell = new ValheimServerGUI.App.Services.ShellLauncher(new Services.RecordingSystemShell(), TestLog.Silent);
        var pathResolver = Core.GetRequiredService<IValheimPathResolver>();
        Func<MainWindowViewModel> factory = () => new MainWindowViewModel(
            Core.GetRequiredService<IServerManager>(), userPrefs, serverPrefs,
            Core.GetRequiredService<IWorldPreferencesProvider>(), Core.GetRequiredService<ISteamCloudWorldProvider>(),
            Core.GetRequiredService<IIpAddressProvider>(), Core.GetRequiredService<IPlayerDataRepository>(),
            logger, new FakeSoftwareUpdateProvider(), shell, pathResolver);
        var windowManager = new WindowManager(logger);
        var coordinator = new ShellCoordinator(
            serverPrefs, userPrefs, new StartupArgsProvider(Array.Empty<string>()),
            factory, windowManager, new StartupService(new FakeSoftwareUpdateProvider(), new FakePlayerDataRepository(), TestLog.Silent),
            logger);

        var captured = new List<Exception>();
        void OnUnobserved(object? _, UnobservedTaskExceptionEventArgs e) { captured.Add(e.Exception); e.SetObserved(); }
        void OnDispatcher(object? _, DispatcherUnhandledExceptionEventArgs e) { captured.Add(e.Exception); e.Handled = true; }
        TaskScheduler.UnobservedTaskException += OnUnobserved;
        Dispatcher.UIThread.UnhandledException += OnDispatcher;
        try
        {
            var first = Assert.Single(coordinator.CreateAndShowStartupWindows());
            coordinator.OpenNewWindow();
            Assert.Equal(2, windowManager.Windows.Count);
            var second = windowManager.Windows[1];

            second.Close();
            Dispatcher.UIThread.RunJobs();

            // Flush any orphaned/faulted task so an unobserved exception would surface now.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Dispatcher.UIThread.RunJobs();

            Assert.Single(windowManager.Windows);        // the first window is still open
            Assert.True(first.IsVisible);
            Assert.Empty(captured);                       // nothing crashed on close
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobserved;
            Dispatcher.UIThread.UnhandledException -= OnDispatcher;
        }
    }
}
