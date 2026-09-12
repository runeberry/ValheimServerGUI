using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's boolean field: a compact check box whose caption is <see cref="FormFieldBase.LabelText"/>,
/// with the help glyph beside it. Replaces raw <see cref="Avalonia.Controls.CheckBox"/> in data-bound views.
/// </summary>
public partial class CheckBoxFormField : FormFieldBase
{
    public static readonly StyledProperty<bool> ValueProperty =
        AvaloniaProperty.Register<CheckBoxFormField, bool>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public CheckBoxFormField() => AvaloniaXamlLoader.Load(this);

    public bool Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
}
