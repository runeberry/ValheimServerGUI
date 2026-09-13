using System.Collections;
using System.Collections.Specialized;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ValheimServerGUI.App.Infrastructure;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's console log frame (the WinForms <c>LogViewer</c> equivalent): a bordered, monospace, read-only
/// <see cref="SelectableTextBlock"/> that shows the lines from <see cref="ItemsSource"/> as one contiguous
/// block so they copy-paste in bulk. New lines append and the view sticks to the bottom while the reader is
/// already there; the source collection is bounded upstream, so the rendered text never grows without limit.
/// </summary>
public partial class LogViewer : UserControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<LogViewer, IEnumerable?>(nameof(ItemsSource));

    private readonly SelectableTextBlock _text;
    private readonly ScrollViewer _scroll;
    private INotifyCollectionChanged? _observed;

    public LogViewer()
    {
        AvaloniaXamlLoader.Load(this);
        _text = this.FindControl<SelectableTextBlock>("TextView")!;
        _scroll = this.FindControl<ScrollViewer>("Scroll")!;
    }

    /// <summary>The log lines to display (ViewModel-owned).</summary>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty) OnItemsSourceChanged();
    }

    private void OnItemsSourceChanged()
    {
        if (_observed is not null) _observed.CollectionChanged -= OnLinesChanged;

        _observed = ItemsSource as INotifyCollectionChanged;
        if (_observed is not null) _observed.CollectionChanged += OnLinesChanged;

        Rebuild();
        ScrollToBottomLater();
    }

    private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Re-join the (bounded) lines and re-pin to the bottom if the reader was already there. Rebuilding
        // wholesale is simple and, with the upstream cap, cheap; it does drop any live text selection, which
        // matches the original's append-to-end behaviour.
        var stick = IsAtBottom();
        Rebuild();
        if (stick) ScrollToBottomLater();
    }

    private void Rebuild()
    {
        var sb = new StringBuilder();
        if (ItemsSource is not null)
        {
            foreach (var item in ItemsSource)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(item);
            }
        }

        _text.Text = sb.ToString();
    }

    private bool IsAtBottom()
    {
        // Treat "no scrollbar yet" (content shorter than the viewport) as at-bottom so early lines pin down.
        var slack = _scroll.Extent.Height - _scroll.Viewport.Height;
        return slack <= 0 || _scroll.Offset.Y >= slack - 4;
    }

    private void ScrollToBottomLater() =>
        // The extent only reflects the new text after a layout pass, so defer the scroll until then.
        Dispatcher.UIThread.Post(() => _scroll.Offset = _scroll.Offset.WithY(_scroll.Extent.Height),
            DispatcherPriority.Background);

    private async void CopySelection(object? sender, RoutedEventArgs e) =>
        await ClipboardHelper.CopyTextAsync(this, _text.SelectedText);

    private void SelectAllText(object? sender, RoutedEventArgs e)
    {
        _text.SelectionStart = 0;
        _text.SelectionEnd = _text.Text?.Length ?? 0;
    }
}
