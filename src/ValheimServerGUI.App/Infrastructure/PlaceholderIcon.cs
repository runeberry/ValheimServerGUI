using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace ValheimServerGUI.App.Infrastructure;

/// <summary>
/// A drawn placeholder window/tray icon, so the tray has something to show before a real icon asset ships
/// in Phase 2.5. Rendered once and cached (the backing bitmap is kept alive as the icon's source).
/// </summary>
internal static class PlaceholderIcon
{
    private static WindowIcon? _cached;
    private static RenderTargetBitmap? _backing;

    public static WindowIcon Create()
    {
        if (_cached is not null) return _cached;

        _backing = new RenderTargetBitmap(new PixelSize(32, 32), new Vector(96, 96));
        using (var ctx = _backing.CreateDrawingContext())
        {
            ctx.DrawEllipse(new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)), null,
                new Point(16, 16), 15, 15);
        }

        _cached = new WindowIcon(_backing);
        return _cached;
    }
}
