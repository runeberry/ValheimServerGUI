using Avalonia.Headless.XUnit;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Services;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Logging;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

public class LogsViewModelTests
{
    private static readonly System.IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    private static LogsViewModel Build()
    {
        var shell = new ShellLauncher(new Services.RecordingSystemShell(), TestLog.Silent);
        return new LogsViewModel(
            Core.GetRequiredService<IApplicationLogger>(),
            shell,
            Core.GetRequiredService<IValheimPathResolver>());
    }

    [AvaloniaFact]
    public void Server_lines_go_to_the_server_view()
    {
        var vm = Build();
        vm.AppendServerLine("world loaded");

        Assert.Equal(LogViews.Server, vm.SelectedView);
        Assert.Contains("world loaded", vm.CurrentLines);
    }

    [AvaloniaFact]
    public void Switching_view_changes_current_lines()
    {
        var vm = Build();
        vm.AppendServerLine("server-line");

        var raised = false;
        vm.PropertyChanged += (_, e) => raised |= e.PropertyName == nameof(LogsViewModel.CurrentLines);
        vm.SelectedView = LogViews.Application;

        Assert.True(raised);
        Assert.DoesNotContain("server-line", vm.CurrentLines); // now showing the Application buffer
    }

    [AvaloniaFact]
    public void Clear_clears_only_the_current_view()
    {
        var vm = Build();
        vm.AppendServerLine("keep-me-on-server");
        vm.SelectedView = LogViews.Application;

        vm.ClearLogsCommand.Execute(null); // clears Application view only

        vm.SelectedView = LogViews.Server;
        Assert.Contains("keep-me-on-server", vm.CurrentLines);
    }

    [AvaloniaFact]
    public void Lines_are_capped_so_the_buffer_stays_bounded()
    {
        var vm = Build();
        for (var i = 0; i < 5100; i++) vm.AppendServerLine($"line {i}");

        Assert.Equal(5000, vm.CurrentLines.Count);
        Assert.DoesNotContain("line 0", vm.CurrentLines);      // oldest dropped off the top
        Assert.Contains("line 5099", vm.CurrentLines);          // newest kept
    }

    [AvaloniaFact]
    public void Save_empty_view_warns()
    {
        var vm = Build();
        string? warning = null;
        vm.Warning += m => warning = m;

        vm.SaveLogsCommand.Execute(null); // Server view is empty

        Assert.NotNull(warning);
    }
}
