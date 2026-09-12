using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ValheimServerGUI.App.Infrastructure;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// A copy-to-clipboard icon button (the WinForms <c>CopyButton</c> equivalent): shows the copy glyph,
/// and on click copies <see cref="Text"/> then flashes a green check (StatusOK) for a couple of seconds.
/// While the check shows the button is inert but keeps its normal colour (not disabled).
/// </summary>
public class CopyButton : Button
{
    private static readonly Bitmap CopyIcon = AppIcons.Get("Copy_16x");
    private static readonly Bitmap ConfirmIcon = AppIcons.Get("StatusOK_16x");

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<CopyButton, string?>(nameof(Text));

    private readonly Image _image = new() { Width = 16, Height = 16 };
    private DispatcherTimer? _timer;
    private bool _confirming;

    public CopyButton()
    {
        Classes.Add("icon");
        RenderOptions.SetBitmapInterpolationMode(_image, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);
        _image.Source = CopyIcon;
        Content = _image;
        if (ToolTip.GetTip(this) is null) ToolTip.SetTip(this, "Copy");
    }

    /// <summary>The text copied to the clipboard when the button is clicked.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>How long the confirmation checkmark shows before reverting.</summary>
    internal static TimeSpan ConfirmDuration { get; set; } = TimeSpan.FromSeconds(2);

    internal bool IsConfirming => _confirming;

    internal object? CurrentIconSource => _image.Source;

    // Use the Button theme/template + the "icon" button style; a subclass otherwise resolves no theme.
    protected override Type StyleKeyOverride => typeof(Button);

    protected override async void OnClick() => await ConfirmCopyAsync();

    internal async System.Threading.Tasks.Task ConfirmCopyAsync()
    {
        if (_confirming) return;

        _confirming = true;
        IsHitTestVisible = false;   // inert to the pointer, but not disabled (keeps its colour)
        _image.Source = ConfirmIcon;

        await ClipboardHelper.CopyTextAsync(this, Text);

        _timer?.Stop();
        _timer = new DispatcherTimer { Interval = ConfirmDuration };
        _timer.Tick += (_, _) => EndConfirm();
        _timer.Start();
    }

    private void EndConfirm()
    {
        _timer?.Stop();
        _image.Source = CopyIcon;
        IsHitTestVisible = true;
        _confirming = false;
    }
}
