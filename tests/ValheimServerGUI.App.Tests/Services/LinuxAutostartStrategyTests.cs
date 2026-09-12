using System;
using System.IO;
using ValheimServerGUI.App.Services;
using Xunit;

namespace ValheimServerGUI.App.Tests.Services;

// E53: autostart enable/disable/stale-path/idempotent — the Linux .desktop branch (Windows branch is
// registry-only and covered by a Win-guarded test).
public sealed class LinuxAutostartStrategyTests : IDisposable
{
    private readonly string _dir;

    public LinuxAutostartStrategyTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vsg-autostart-" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private LinuxAutostartStrategy Make(string exec = "/opt/vsg/app")
        => new(_dir, exec, "Valheim Server GUI", "ValheimServerGUI");

    private string FilePath => Path.Combine(_dir, "ValheimServerGUI.desktop");

    [Fact]
    public void Enable_writes_desktop_entry()
    {
        var changed = Make().Apply(runOnStartup: true);

        Assert.True(changed);
        Assert.True(File.Exists(FilePath));
        var text = File.ReadAllText(FilePath);
        Assert.Contains("[Desktop Entry]", text);
        Assert.Contains("Exec=\"/opt/vsg/app\"", text);
    }

    [Fact]
    public void Enable_is_idempotent()
    {
        var strat = Make();
        Assert.True(strat.Apply(true));
        Assert.False(strat.Apply(true)); // already correct → no change
    }

    [Fact]
    public void Enable_repairs_stale_exec_path()
    {
        Make(exec: "/old/path").Apply(true);
        var changed = Make(exec: "/new/path").Apply(true);

        Assert.True(changed);
        Assert.Contains("Exec=\"/new/path\"", File.ReadAllText(FilePath));
    }

    [Fact]
    public void Disable_removes_entry()
    {
        var strat = Make();
        strat.Apply(true);
        Assert.True(strat.Apply(false));
        Assert.False(File.Exists(FilePath));
    }

    [Fact]
    public void Disable_when_absent_is_noop()
    {
        Assert.False(Make().Apply(false));
    }
}
