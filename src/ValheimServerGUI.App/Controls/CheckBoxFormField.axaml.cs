using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's boolean field: a compact check box whose caption is <see cref="FormFieldBase.LabelText"/>,
/// with the help glyph beside it. Replaces raw <see cref="Avalonia.Controls.CheckBox"/> in data-bound views.
/// </summary>
/// <remarks>Design (markup) lives in <c>CheckBoxFormField.axaml</c>; functional code in <c>CheckBoxFormField.cs</c>.</remarks>
public partial class CheckBoxFormField : FormFieldBase
{
    public CheckBoxFormField() => AvaloniaXamlLoader.Load(this);
}
