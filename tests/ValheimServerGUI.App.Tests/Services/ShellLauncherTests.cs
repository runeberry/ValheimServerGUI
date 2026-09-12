using ValheimServerGUI.App.Services;
using Xunit;

namespace ValheimServerGUI.App.Tests.Services;

// E54: open folder/URL, invalid path, non-http scheme — validated + correct per-OS launcher.
public class ShellLauncherTests
{
    private static ShellLauncher Make(RecordingSystemShell shell, bool isWindows)
        => new(shell, TestLog.Silent, isWindows);

    [Fact]
    public void OpenWebAddress_linux_uses_xdg_open()
    {
        var shell = new RecordingSystemShell();
        Make(shell, isWindows: false).OpenWebAddress("https://example.com/");

        var call = Assert.Single(shell.Started);
        Assert.Equal("xdg-open", call.FileName);
        Assert.Equal(new[] { "https://example.com/" }, call.Args);
    }

    [Fact]
    public void OpenWebAddress_windows_uses_shell_execute()
    {
        var shell = new RecordingSystemShell();
        Make(shell, isWindows: true).OpenWebAddress("https://example.com/");

        Assert.Equal("https://example.com/", Assert.Single(shell.ShellOpened));
        Assert.Empty(shell.Started);
    }

    [Theory]
    [InlineData("ftp://example.com/")]
    [InlineData("javascript:alert(1)")]
    [InlineData("not a url")]
    [InlineData("")]
    public void OpenWebAddress_rejects_non_http_scheme(string url)
    {
        var shell = new RecordingSystemShell();
        Make(shell, isWindows: false).OpenWebAddress(url);

        Assert.Empty(shell.Started);
        Assert.Empty(shell.ShellOpened);
    }

    [Fact]
    public void OpenDirectory_opens_existing_directory()
    {
        var shell = new RecordingSystemShell();
        shell.ExistingDirectories.Add("/srv/worlds");
        Make(shell, isWindows: false).OpenDirectory("/srv/worlds");

        Assert.Equal("/srv/worlds", Assert.Single(shell.Started).Args[0]);
    }

    [Fact]
    public void OpenDirectory_opens_containing_dir_of_a_file()
    {
        var shell = new RecordingSystemShell();
        shell.ExistingFiles.Add("/srv/worlds/Dedicated.fwl");
        Make(shell, isWindows: false).OpenDirectory("/srv/worlds/Dedicated.fwl");

        Assert.Equal("/srv/worlds", Assert.Single(shell.Started).Args[0]);
    }

    [Fact]
    public void OpenDirectory_rejects_nonexistent_path()
    {
        var shell = new RecordingSystemShell();
        Make(shell, isWindows: false).OpenDirectory("/does/not/exist");

        Assert.Empty(shell.Started);
        Assert.Empty(shell.ShellOpened);
    }

    [Fact]
    public void OpenWebAddress_falls_back_to_gio_when_xdg_open_fails()
    {
        var shell = new RecordingSystemShell { ThrowOnceOnStart = true };
        Make(shell, isWindows: false).OpenWebAddress("https://example.com/");

        var call = Assert.Single(shell.Started);
        Assert.Equal("gio", call.FileName);
        Assert.Equal(new[] { "open", "https://example.com/" }, call.Args);
    }
}
