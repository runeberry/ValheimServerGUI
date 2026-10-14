using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ValheimServerGUI.App.Controls;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// Universal grayscale disabled-icon cue: AppIcons.GetGrayscale desaturates while preserving alpha, and
// IconImage swaps between the colour and grey variants as its effective-enabled state changes.
public class IconGrayscaleTests
{
    private static byte[] Bgra(Bitmap bmp)
    {
        var wb = new WriteableBitmap(bmp.PixelSize, bmp.Dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var fb = wb.Lock();
        bmp.CopyPixels(fb);
        var len = fb.RowBytes * bmp.PixelSize.Height;
        var buf = new byte[len];
        Marshal.Copy(fb.Address, buf, 0, len);
        return buf;
    }

    [AvaloniaFact]
    public void GetGrayscale_is_cached_and_distinct_from_the_colour_variant()
    {
        Assert.Same(AppIcons.GetGrayscale("Run_16x"), AppIcons.GetGrayscale("Run_16x"));
        Assert.NotSame(AppIcons.Get("Run_16x"), AppIcons.GetGrayscale("Run_16x"));
    }

    [AvaloniaFact]
    public void GetGrayscale_desaturates_every_pixel_and_preserves_alpha()
    {
        var colour = Bgra(AppIcons.Get("Run_16x"));
        var grey = Bgra(AppIcons.GetGrayscale("Run_16x"));
        Assert.Equal(colour.Length, grey.Length);

        var sourceHadColour = false;
        for (var i = 0; i + 3 < grey.Length; i += 4)
        {
            Assert.Equal(grey[i], grey[i + 1]);       // B == G
            Assert.Equal(grey[i + 1], grey[i + 2]);   // G == R  → fully desaturated
            Assert.Equal(colour[i + 3], grey[i + 3]); // alpha preserved (silhouette + anti-aliasing intact)

            if (colour[i] != colour[i + 1] || colour[i + 1] != colour[i + 2]) sourceHadColour = true;
        }

        // The probe only means something if the source was actually coloured to begin with.
        Assert.True(sourceHadColour);
    }

    [AvaloniaFact]
    public void IconImage_swaps_between_colour_and_grey_on_effective_enabled()
    {
        var icon = new IconImage { IconName = "Run_16x" };
        Assert.Same(AppIcons.Get("Run_16x"), icon.Source);          // enabled → colour

        icon.IsEnabled = false;
        Assert.Same(AppIcons.GetGrayscale("Run_16x"), icon.Source);  // disabled → grey

        icon.IsEnabled = true;
        Assert.Same(AppIcons.Get("Run_16x"), icon.Source);          // re-enabled → colour again
    }
}
