using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// A single labelled form row: [label] [input] [help glyph]. The reusable wrapper the v2.4
/// <c>IFormField&lt;T&gt;</c> kit maps to (§11.2) — <see cref="HeaderedContentControl.Header"/> is the
/// label, <see cref="ContentControl.Content"/> is the input control, and <see cref="HelpText"/> drives a
/// "?" glyph whose tooltip is collapsed when the text is empty.
/// </summary>
public class FormRow : HeaderedContentControl
{
    public static readonly StyledProperty<string?> HelpTextProperty =
        AvaloniaProperty.Register<FormRow, string?>(nameof(HelpText));

    /// <summary>Tooltip shown by the trailing "?" glyph. When null/empty the glyph is hidden.</summary>
    public string? HelpText
    {
        get => GetValue(HelpTextProperty);
        set => SetValue(HelpTextProperty, value);
    }
}
