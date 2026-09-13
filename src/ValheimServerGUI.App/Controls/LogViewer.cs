using System.Collections;
using Avalonia;

namespace ValheimServerGUI.App.Controls;

/// <summary>Functional code for <see cref="LogViewer"/> (properties), split from the XAML-paired code-behind
/// in <c>LogViewer.axaml.cs</c>.</summary>
public partial class LogViewer
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<LogViewer, IEnumerable?>(nameof(ItemsSource));

    /// <summary>The log lines to display (ViewModel-owned).</summary>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
}
