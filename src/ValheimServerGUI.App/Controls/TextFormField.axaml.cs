using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's single text field: a caption + help glyph over one text box, with an optional trailing slot
/// for adornments (e.g. the password copy button + show toggle). Replaces every raw
/// <see cref="Avalonia.Controls.TextBox"/> in the data-bound views so text inputs are sized and styled
/// identically everywhere.
/// </summary>
/// <remarks>Design (markup) lives in <c>TextFormField.axaml</c>; functional code in <c>TextFormField.cs</c>.</remarks>
public partial class TextFormField : FormFieldBase
{
    public TextFormField() => AvaloniaXamlLoader.Load(this);
}
