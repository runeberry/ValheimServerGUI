using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The single icon renderer: an <see cref="Image"/> whose glyph is an <see cref="AppIcons"/> key. It greys
/// itself automatically whenever it sits inside disabled chrome — because <see cref="InputElement.IsEffectivelyEnabled"/>
/// propagates from any disabled ancestor (a button, a menu item), a disabled container turns the glyph grey
/// and an enabled one keeps it in colour, with no per-site wiring. This is the WinForms "disabled icon" cue
/// applied universally (the status bar is never disabled, so its icons stay coloured).
/// </summary>
public class IconImage : Image
{
    public static readonly StyledProperty<string?> IconNameProperty =
        AvaloniaProperty.Register<IconImage, string?>(nameof(IconName));

    public IconImage()
    {
        Width = 16;
        Height = 16;
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
    }

    /// <summary>The <see cref="AppIcons"/> key of the glyph (e.g. <c>Run_16x</c>).</summary>
    public string? IconName
    {
        get => GetValue(IconNameProperty);
        set => SetValue(IconNameProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconNameProperty || change.Property == IsEffectivelyEnabledProperty)
            UpdateSource();
    }

    private void UpdateSource()
    {
        var name = IconName;
        Source = string.IsNullOrEmpty(name)
            ? null
            : IsEffectivelyEnabled ? AppIcons.Get(name) : AppIcons.GetGrayscale(name);
    }
}
