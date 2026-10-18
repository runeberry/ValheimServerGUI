using ValheimServerGUI.App;
using Xunit;

namespace ValheimServerGUI.App.Tests.Services;

// The path written into the run-on-login registration must still exist after the app exits.
public sealed class LaunchPathTests
{
    [Fact]
    public void Uses_the_process_path_outside_an_AppImage()
    {
        Assert.Equal("/opt/vsg/ValheimServerGUI.App", ServiceConfiguration.ResolveLaunchPath("/opt/vsg/ValheimServerGUI.App", null));
    }

    [Fact]
    public void Uses_the_AppImage_file_rather_than_its_temporary_mount()
    {
        var path = ServiceConfiguration.ResolveLaunchPath(
            "/tmp/.mount_ValheiAclCoG/usr/bin/ValheimServerGUI.App",
            "/home/user/My Apps/ValheimServerGUI-x86_64.AppImage");

        Assert.Equal("/home/user/My Apps/ValheimServerGUI-x86_64.AppImage", path);
    }

    [Fact]
    public void Ignores_an_empty_APPIMAGE_variable()
    {
        Assert.Equal("/opt/vsg/ValheimServerGUI.App", ServiceConfiguration.ResolveLaunchPath("/opt/vsg/ValheimServerGUI.App", ""));
    }
}
