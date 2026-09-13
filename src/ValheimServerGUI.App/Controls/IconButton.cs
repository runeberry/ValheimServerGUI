using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's standard 16×16 icon button (the WinForms <c>IconButton</c> equivalent): a <see cref="Button"/>
/// styled with the <c>icon</c> class whose glyph is an <see cref="AppIcons"/> key. When
/// <see cref="ConfirmIconName"/> is set, clicking flashes that confirm glyph for a couple of seconds — inert
/// to the pointer while it shows, but not disabled (it keeps its colour). The action itself runs through the
/// bound <see cref="Button.Command"/>. Instantiable directly for one-offs; the presets (<see cref="OpenButton"/>,
/// <see cref="RefreshButton"/>, <see cref="SettingsButton"/>, <see cref="EditButton"/>, <see cref="CopyButton"/>)
/// just preset the icon/confirm/tooltip.
/// </summary>
public class IconButton : Button
{
    public static readonly StyledProperty<string?> IconNameProperty =
        AvaloniaProperty.Register<IconButton, string?>(nameof(IconName));

    public static readonly StyledProperty<string?> ConfirmIconNameProperty =
        AvaloniaProperty.Register<IconButton, string?>(nameof(ConfirmIconName));

    private readonly Image _image = new() { Width = 16, Height = 16 };
    private DispatcherTimer? _timer;
    private bool _confirming;

    public IconButton()
    {
        Classes.Add("icon");
        RenderOptions.SetBitmapInterpolationMode(_image, BitmapInterpolationMode.HighQuality);
        Content = _image;
    }

    /// <summary>The <see cref="AppIcons"/> key of the button's glyph (e.g. <c>OpenFolder_16x</c>).</summary>
    public string? IconName
    {
        get => GetValue(IconNameProperty);
        set => SetValue(IconNameProperty, value);
    }

    /// <summary>Optional <see cref="AppIcons"/> key flashed for <see cref="ConfirmDuration"/> after a click.</summary>
    public string? ConfirmIconName
    {
        get => GetValue(ConfirmIconNameProperty);
        set => SetValue(ConfirmIconNameProperty, value);
    }

    /// <summary>How long the confirmation glyph shows before reverting.</summary>
    internal static TimeSpan ConfirmDuration { get; set; } = TimeSpan.FromSeconds(2);

    internal bool IsConfirming => _confirming;

    internal object? CurrentIconSource => _image.Source;

    // Use the Button theme/template + the "icon" button style; a subclass otherwise resolves no theme.
    protected override Type StyleKeyOverride => typeof(Button);

    protected override void OnClick()
    {
        base.OnClick();  // fires the bound Command
        ShowConfirm();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconNameProperty) UpdateBaseIcon();
    }

    /// <summary>Swaps to the confirm glyph and goes inert (but not disabled) for <see cref="ConfirmDuration"/>.</summary>
    protected void ShowConfirm()
    {
        if (_confirming || string.IsNullOrEmpty(ConfirmIconName)) return;

        _confirming = true;
        IsHitTestVisible = false;   // inert to the pointer, but not disabled (keeps its colour)
        _image.Source = AppIcons.Get(ConfirmIconName);

        _timer?.Stop();
        _timer = new DispatcherTimer { Interval = ConfirmDuration };
        _timer.Tick += (_, _) => EndConfirm();
        _timer.Start();
    }

    private void EndConfirm()
    {
        _timer?.Stop();
        _confirming = false;
        IsHitTestVisible = true;
        UpdateBaseIcon();
    }

    private void UpdateBaseIcon()
    {
        if (_confirming) return;
        _image.Source = string.IsNullOrEmpty(IconName) ? null : AppIcons.Get(IconName);
    }
}
