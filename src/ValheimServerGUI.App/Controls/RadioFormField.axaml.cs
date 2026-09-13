using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's boolean radio field: a radio button whose caption is <see cref="FormFieldBase.LabelText"/>, with
/// the help glyph beside it. Grouping is by <see cref="GroupName"/> — Avalonia's <c>RadioButton.GroupName</c>
/// already coordinates the selection across containers, so no cross-form discovery is needed (unlike the
/// WinForms version). Replaces raw <see cref="Avalonia.Controls.RadioButton"/> in data-bound views.
/// </summary>
/// <remarks>Design (markup) lives in <c>RadioFormField.axaml</c>; functional code in <c>RadioFormField.cs</c>.</remarks>
public partial class RadioFormField : FormFieldBase
{
    public RadioFormField() => AvaloniaXamlLoader.Load(this);
}
