using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's integer field: a caption + help glyph over the compact vertically-stacked spinner. Replaces
/// raw <see cref="Avalonia.Controls.NumericUpDown"/> so numeric inputs are consistent everywhere.
/// </summary>
/// <remarks>Design (markup) lives in <c>NumericFormField.axaml</c>; functional code in <c>NumericFormField.cs</c>.</remarks>
public partial class NumericFormField : FormFieldBase
{
    public NumericFormField() => AvaloniaXamlLoader.Load(this);
}
