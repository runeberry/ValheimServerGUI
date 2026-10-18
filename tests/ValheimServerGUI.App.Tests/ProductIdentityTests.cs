using System.Reflection;
using Xunit;

namespace ValheimServerGUI.App.Tests;

// The exe's version info (what Windows lists in Settings > Startup apps and Task Manager) names the app the same
// way the window title and the Linux autostart entry do.
public class ProductIdentityTests
{
    private static readonly Assembly App = typeof(AppConstants).Assembly;

    [Fact]
    public void File_description_is_the_product_name()
        => Assert.Equal(AppConstants.ProductName, App.GetCustomAttribute<AssemblyTitleAttribute>()?.Title);

    [Fact]
    public void Product_is_the_product_name()
        => Assert.Equal(AppConstants.ProductName, App.GetCustomAttribute<AssemblyProductAttribute>()?.Product);
}
