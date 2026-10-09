using ValheimServerGUI.App.Controls;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

public class PlatformIconsTests
{
    [Theory]
    [InlineData("Steam", "Steam_16x")]
    [InlineData("steam", "Steam_16x")]
    [InlineData("Xbox", "XboxLive_16x")]
    public void Known_platforms_map_to_an_icon(string platform, string expected)
        => Assert.Equal(expected, PlatformIcons.IconNameForPlatform(platform));

    [Fact]
    public void Unknown_has_no_icon()
    {
        Assert.Null(PlatformIcons.IconNameForPlatform("nope"));
        Assert.Null(PlatformIcons.IconNameForPlatform(null));
    }
}
