using System;
using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// Central registry for the restored WinForms icon set (see <c>Assets/Icons/README.md</c>). Loads each
/// PNG from the app's Avalonia resources once, caches it, and hands the same <see cref="Bitmap"/> to both
/// XAML (via the <c>{c:Icon Name_16x}</c> markup extension) and code (converters, view-models) so there is
/// a single source of truth for "which asset is this icon" keyed by the file's base name.
/// </summary>
public static class AppIcons
{
    private const string BaseUri = "avares://ValheimServerGUI.App/Assets/Icons/";

    private static readonly ConcurrentDictionary<string, Bitmap> Cache = new();

    /// <summary>Returns the cached bitmap for the icon whose PNG base name is <paramref name="name"/>.</summary>
    public static Bitmap Get(string name)
        => Cache.GetOrAdd(name, static n => new Bitmap(AssetLoader.Open(new Uri(BaseUri + n + ".png"))));
}

/// <summary>
/// XAML markup extension: <c>Source="{c:Icon Run_16x}"</c> resolves to the shared <see cref="Bitmap"/>
/// from <see cref="AppIcons"/>. The moral equivalent of <c>{StaticResource IconRun}</c>, but type-loaded
/// through the same registry the converters use.
/// </summary>
public sealed class IconExtension : MarkupExtension
{
    public IconExtension() { }

    public IconExtension(string name) => Name = name;

    public string Name { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider) => AppIcons.Get(Name);
}
