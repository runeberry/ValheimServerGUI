using System;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The single "?" help glyph used across the app (the WinForms <c>HelpLabel</c> equivalent): a bold accent
/// "?" whose tooltip carries <see cref="HelpText"/>. The whole control collapses when <see cref="HelpText"/>
/// is null/empty, so a row with no help text reserves no space. The glyph itself lives in the
/// <c>ControlTheme</c> in <c>HelpLabel.axaml</c>; visibility is driven here so it stays trivially testable.
/// </summary>
public class HelpLabel : TemplatedControl
{
    public static readonly StyledProperty<string?> HelpTextProperty =
        AvaloniaProperty.Register<HelpLabel, string?>(nameof(HelpText));

    static HelpLabel()
    {
        HelpTextProperty.Changed.AddClassHandler<HelpLabel>((label, _) => label.UpdateVisibility());
    }

    public HelpLabel() => UpdateVisibility();

    /// <summary>Tooltip text shown by the glyph. When null/empty the glyph is hidden.</summary>
    public string? HelpText
    {
        get => GetValue(HelpTextProperty);
        set => SetValue(HelpTextProperty, value);
    }

    private void UpdateVisibility() => IsVisible = !string.IsNullOrWhiteSpace(HelpText);
}
