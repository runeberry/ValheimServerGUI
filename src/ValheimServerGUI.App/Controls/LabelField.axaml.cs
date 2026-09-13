using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's read-only caption + value row (the WinForms <c>LabelField</c> equivalent): a right-aligned
/// <see cref="FormFieldBase.LabelText"/> caption, a selectable read-only <see cref="Value"/>, the help glyph,
/// and an optional <see cref="TrailingContent"/> slot (e.g. a <see cref="CopyButton"/>). Replaces the raw
/// caption/value grids in the Server Details and Player Details surfaces.
/// </summary>
/// <remarks>Design (markup) lives in <c>LabelField.axaml</c>; functional code in <c>LabelField.cs</c>.</remarks>
public partial class LabelField : FormFieldBase
{
    public LabelField() => AvaloniaXamlLoader.Load(this);
}
