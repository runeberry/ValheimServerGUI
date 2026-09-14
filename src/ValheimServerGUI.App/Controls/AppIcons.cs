using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
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
    private static readonly ConcurrentDictionary<string, Bitmap> GrayscaleCache = new();

    /// <summary>Returns the cached bitmap for the icon whose PNG base name is <paramref name="name"/>.</summary>
    public static Bitmap Get(string name)
        => Cache.GetOrAdd(name, static n => new Bitmap(AssetLoader.Open(new Uri(BaseUri + n + ".png"))));

    /// <summary>
    /// Returns a cached desaturated (grey) variant of the icon — the WinForms "disabled" cue. Each pixel's
    /// colour is replaced with its luminance while its alpha is preserved, so the glyph's silhouette and
    /// anti-aliasing stay intact. Luminance is taken directly from the premultiplied bytes: premultiplied
    /// channels already equal alpha·straight, so their luminance equals the premultiplied grey — no un/re-
    /// premultiply needed.
    /// </summary>
    public static Bitmap GetGrayscale(string name) => GrayscaleCache.GetOrAdd(name, static n => Desaturate(Get(n)));

    private static Bitmap Desaturate(Bitmap source)
    {
        var size = source.PixelSize;
        var writable = new WriteableBitmap(size, source.Dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);

        using (var fb = writable.Lock())
        {
            // CopyPixels(ILockedFramebuffer) converts the source into the framebuffer's Bgra8888/Premul format.
            source.CopyPixels(fb);

            var length = fb.RowBytes * size.Height;
            var buffer = new byte[length];
            Marshal.Copy(fb.Address, buffer, 0, length);

            // Bgra8888: [0]=B [1]=G [2]=R [3]=A. Rec.601 luminance; alpha untouched.
            for (var i = 0; i + 3 < length; i += 4)
            {
                int b = buffer[i], g = buffer[i + 1], r = buffer[i + 2];
                var l = (byte)((r * 299 + g * 587 + b * 114) / 1000);
                buffer[i] = buffer[i + 1] = buffer[i + 2] = l;
            }

            Marshal.Copy(buffer, 0, fb.Address, length);
        }

        return writable;
    }
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
