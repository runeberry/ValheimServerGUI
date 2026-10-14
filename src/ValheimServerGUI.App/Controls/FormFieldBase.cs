using Avalonia;
using Avalonia.Controls;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// Base for the app's data-bound field controls — the Avalonia equivalent of the WinForms
/// <c>IFormField</c> contract (a <c>LabelText</c> + <c>HelpText</c> caption around a single input). Concrete
/// fields (<see cref="TextFormField"/>, <see cref="NumericFormField"/>, <see cref="DropdownFormField"/>,
/// <see cref="FilenameFormField"/>, <see cref="CheckBoxFormField"/>) add a two-way-bindable <c>Value</c> of
/// their own type plus any field-specific knobs. Wrapping every editable field in one of these keeps the
/// look and the binding API uniform, exactly as the WinForms UserControl fields did — no raw stock inputs
/// in the views.
/// </summary>
public abstract class FormFieldBase : UserControl, IFormField
{
    public static readonly StyledProperty<string?> LabelTextProperty =
        AvaloniaProperty.Register<FormFieldBase, string?>(nameof(LabelText));

    public static readonly StyledProperty<string?> HelpTextProperty =
        AvaloniaProperty.Register<FormFieldBase, string?>(nameof(HelpText));

    /// <summary>The field caption shown above the input (or, for the checkbox field, beside it).</summary>
    public string? LabelText
    {
        get => GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    /// <summary>Tooltip text for the "?" help glyph. When null/empty the glyph is hidden.</summary>
    public string? HelpText
    {
        get => GetValue(HelpTextProperty);
        set => SetValue(HelpTextProperty, value);
    }

    /// <summary>
    /// A field's <c>IsEnabled</c> disables its input, never its "?" help glyph: help must stay hoverable on a
    /// locked field (e.g. every server setting while the server runs). Avalonia disables a control's whole
    /// subtree and routes pointer-over (cursor, tooltip) only to enabled elements, so the field root itself stays
    /// enabled and each field template binds its input part(s) to <c>IsEnabled</c> instead.
    /// </summary>
    protected override bool IsEnabledCore => true;
}
