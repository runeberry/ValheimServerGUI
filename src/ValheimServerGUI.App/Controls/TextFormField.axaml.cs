using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's single text field: a caption + help glyph over one text box, with an optional trailing slot
/// for adornments (e.g. the password copy button + show toggle). Replaces every raw <see cref="TextBox"/>
/// in the data-bound views so text inputs are sized and styled identically everywhere.
/// </summary>
public partial class TextFormField : FormFieldBase
{
    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<TextFormField, string?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> MaxLengthProperty =
        AvaloniaProperty.Register<TextFormField, int>(nameof(MaxLength));

    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<TextFormField, string?>(nameof(Watermark));

    /// <summary>When false the text is masked (password field). Default true (plain text).</summary>
    public static readonly StyledProperty<bool> ShowValueProperty =
        AvaloniaProperty.Register<TextFormField, bool>(nameof(ShowValue), defaultValue: true);

    /// <summary>Optional controls rendered to the right of the text box (copy button, show toggle, …).</summary>
    public static readonly StyledProperty<object?> TrailingContentProperty =
        AvaloniaProperty.Register<TextFormField, object?>(nameof(TrailingContent));

    public TextFormField() => AvaloniaXamlLoader.Load(this);

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int MaxLength
    {
        get => GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public bool ShowValue
    {
        get => GetValue(ShowValueProperty);
        set => SetValue(ShowValueProperty, value);
    }

    public object? TrailingContent
    {
        get => GetValue(TrailingContentProperty);
        set => SetValue(TrailingContentProperty, value);
    }
}
