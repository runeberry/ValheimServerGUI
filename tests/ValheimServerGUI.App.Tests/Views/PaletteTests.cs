using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Services;
using ValheimServerGUI.App.Tests.Services;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Logging;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// The layered surface palette resolves in both theme variants, the three layers are distinct, and none is
// pure black (the "no solid black anywhere" requirement). The window paints the base layer.
public class PaletteTests
{
    private static readonly System.IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    private static readonly Color Black = Color.FromArgb(0xFF, 0, 0, 0);

    private static Color Resolve(Window w, string key)
    {
        Assert.True(w.TryFindResource(key, w.ActualThemeVariant, out var v), $"{key} did not resolve");
        return ((ISolidColorBrush)v!).Color;
    }

    private static Window ShowWindow()
    {
        var shell = new ShellLauncher(new RecordingSystemShell(), TestLog.Silent);
        var vm = new MainWindowViewModel(
            Core.GetRequiredService<IServerManager>(), new FakeUserPreferencesProvider(),
            new FakeServerPreferencesProvider(), Core.GetRequiredService<IWorldPreferencesProvider>(),
            Core.GetRequiredService<ISteamCloudWorldProvider>(), Core.GetRequiredService<IIpAddressProvider>(),
            Core.GetRequiredService<IPlayerDataRepository>(), Core.GetRequiredService<IApplicationLogger>(),
            new FakeSoftwareUpdateProvider(), shell, Core.GetRequiredService<IValheimPathResolver>(),
            Core.GetRequiredService<IPlayerListImportService>(), Core.GetRequiredService<IRuneberryApiClient>());
        var window = new ValheimServerGUI.App.Views.MainWindow(vm);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Layers_are_distinct_and_never_pure_black(string variantName)
    {
        var window = ShowWindow();
        window.RequestedThemeVariant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Dispatcher.UIThread.RunJobs();

        var darkest = Resolve(window, "LayerDarkest");
        var baseColor = Resolve(window, "LayerBase");
        var content = Resolve(window, "LayerContent");

        Assert.NotEqual(Black, darkest);
        Assert.NotEqual(Black, baseColor);
        Assert.NotEqual(Black, content);
        Assert.NotEqual(darkest, baseColor);
        Assert.NotEqual(baseColor, content);

        // App background blends with the menu/footer (darkest layer).
        Assert.Equal(darkest, (window.Background as ISolidColorBrush)!.Color);
    }
}
