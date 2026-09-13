using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>How a <see cref="FilenameFormField"/>'s browse button picks a path.</summary>
public enum FileSelectMode
{
    File,
    Directory,
}

/// <summary>
/// The app's path field: a caption + help glyph over a text box with a built-in "…" browse button (a real
/// file/folder picker via the window's storage provider) plus an optional trailing slot for an extra action
/// such as an open-folder button. Mirrors the WinForms <c>FilenameFormField</c>, which likewise owned its
/// browse button.
/// </summary>
/// <remarks>Design (markup) lives in <c>FilenameFormField.axaml</c>; functional code in <c>FilenameFormField.cs</c>.</remarks>
public partial class FilenameFormField : FormFieldBase
{
    public FilenameFormField() => AvaloniaXamlLoader.Load(this);
}
