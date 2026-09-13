using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's console log frame (the WinForms <c>LogViewer</c> equivalent): a bordered, monospace, read-only
/// list bound to <see cref="ItemsSource"/>. The ViewModel owns the lines; this is a thin display wrapper so
/// every log surface looks identical.
/// </summary>
public partial class LogViewer : UserControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<LogViewer, IEnumerable?>(nameof(ItemsSource));

    public LogViewer() => AvaloniaXamlLoader.Load(this);

    /// <summary>The log lines to display (ViewModel-owned).</summary>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
}
