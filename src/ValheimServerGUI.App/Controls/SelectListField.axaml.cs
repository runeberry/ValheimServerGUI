using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's labelled single-select list (the WinForms <c>SelectListField</c> equivalent): a
/// <see cref="FormFieldBase.LabelText"/> caption + help glyph over a list box. Thin and MVVM-idiomatic — the
/// ViewModel owns the items via <see cref="ItemsSource"/> and the selection via <see cref="Value"/>; the
/// control does not replicate the WinForms imperative add/remove API.
/// </summary>
/// <remarks>Design (markup) lives in <c>SelectListField.axaml</c>; functional code in <c>SelectListField.cs</c>.</remarks>
public partial class SelectListField : FormFieldBase
{
    public SelectListField() => AvaloniaXamlLoader.Load(this);
}
