using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ValheimServerGUI.App.Controls;
using Xunit;
using AppGroupBox = ValheimServerGUI.App.Controls.GroupBox;

namespace ValheimServerGUI.App.Tests.Views;

// Guards the styling primitives added in the V3 look pass: the custom controls instantiate headlessly,
// and every icon the converters / menus reference resolves to a real bitmap (a bad avares name would
// otherwise only surface when that specific menu opened or that row bound at runtime).
public class ControlsAndIconsTests
{
    [AvaloniaFact]
    public void GroupBox_and_FormRow_instantiate_with_header_and_content()
    {
        var group = new AppGroupBox { Header = "World", Content = new TextBox() };
        Assert.Equal("World", group.Header);
        Assert.IsType<TextBox>(group.Content);

        var row = new FormRow { Header = "Server Name", HelpText = "help", Content = new TextBox() };
        Assert.Equal("Server Name", row.Header);
        Assert.Equal("help", row.HelpText);
        Assert.IsType<TextBox>(row.Content);
    }

    [AvaloniaTheory]
    // Server-status + update-status bar icons (converter-driven).
    [InlineData("StatusPause_grey_16x")]
    [InlineData("UnsyncedCommits_16x_Horiz")]
    [InlineData("StatusRun_16x")]
    [InlineData("Loading_Blue_16x")]
    [InlineData("StatusOK_16x")]
    [InlineData("StatusWarning_16x")]
    [InlineData("StatusCriticalError_16x")]
    // Player platform icons (converter-driven).
    [InlineData("Steam_16x")]
    [InlineData("XboxLive_16x")]
    // Menu icons.
    [InlineData("AddImmediateWindow_16x")]
    [InlineData("NewFile_16x")]
    [InlineData("Save_16x")]
    [InlineData("OpenFile_16x")]
    [InlineData("Cancel_16x")]
    [InlineData("Settings_16x")]
    [InlineData("FolderInformation_16x")]
    [InlineData("OpenFolder_16x")]
    [InlineData("OpenWeb_16x")]
    [InlineData("NewBug_16x")]
    // Button + brand icons.
    [InlineData("Run_16x")]
    [InlineData("Restart_16x")]
    [InlineData("Stop_16x")]
    [InlineData("Copy_16x")]
    [InlineData("DiscordLogo")]
    [InlineData("GitHubLogo")]
    [InlineData("DonateLogo")]
    [InlineData("RuneberryLogo")]
    [InlineData("vsg_logo_16")]
    public void Icon_loads_from_resources(string name)
    {
        var bitmap = AppIcons.Get(name);
        Assert.NotNull(bitmap);
        Assert.True(bitmap.PixelSize.Width > 0 && bitmap.PixelSize.Height > 0);
    }
}
