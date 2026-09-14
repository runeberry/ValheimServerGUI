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
    public void Server_view_reflects_the_targeted_buffer()
    {
        var vm = Build();
        var buffer = new System.Collections.ObjectModel.ObservableCollection<string>();
        vm.SetServerLog(buffer);
        buffer.Add("world loaded");

        Assert.Equal(LogViews.Server, vm.SelectedView);
        Assert.Contains("world loaded", vm.CurrentLines);
    }

    [AvaloniaFact]
    public void Set_server_log_repoints_the_server_view()
    {
        var vm = Build();
        var first = new System.Collections.ObjectModel.ObservableCollection<string> { "from-A" };
        var second = new System.Collections.ObjectModel.ObservableCollection<string> { "from-B" };

        vm.SetServerLog(first);
        Assert.Contains("from-A", vm.CurrentLines);

        var raised = false;
        vm.PropertyChanged += (_, e) => raised |= e.PropertyName == nameof(LogsViewModel.CurrentLines);
        vm.SetServerLog(second); // profile switch re-points the Server buffer

        Assert.True(raised);
        Assert.DoesNotContain("from-A", vm.CurrentLines);
        Assert.Contains("from-B", vm.CurrentLines);
    }

    [AvaloniaFact]
    public void Switching_view_changes_current_lines()
    {
        var vm = Build();
        vm.SetServerLog(new System.Collections.ObjectModel.ObservableCollection<string> { "server-line" });

        var raised = false;
        vm.PropertyChanged += (_, e) => raised |= e.PropertyName == nameof(LogsViewModel.CurrentLines);
        vm.SelectedView = LogViews.Application;

        Assert.True(raised);
        Assert.DoesNotContain("server-line", vm.CurrentLines); // now showing the Application buffer
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
