using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's console log frame (the WinForms <c>LogViewer</c> equivalent): a bordered, monospace, read-only
/// list bound to <see cref="ItemsSource"/>. The ViewModel owns the lines; this is a thin display wrapper so
/// every log surface looks identical.
/// </summary>
/// <remarks>Design (markup) lives in <c>LogViewer.axaml</c>; functional code in <c>LogViewer.cs</c>.</remarks>
public partial class LogViewer : UserControl
{
    public LogViewer() => AvaloniaXamlLoader.Load(this);
}
