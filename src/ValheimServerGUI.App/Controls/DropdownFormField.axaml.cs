using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's dropdown field: a caption + help glyph over a combo box. Replaces raw
/// <see cref="Avalonia.Controls.ComboBox"/> in the data-bound views.
/// </summary>
/// <remarks>Design (markup) lives in <c>DropdownFormField.axaml</c>; functional code in <c>DropdownFormField.cs</c>.</remarks>
public partial class DropdownFormField : FormFieldBase
{
    public DropdownFormField() => AvaloniaXamlLoader.Load(this);
}
