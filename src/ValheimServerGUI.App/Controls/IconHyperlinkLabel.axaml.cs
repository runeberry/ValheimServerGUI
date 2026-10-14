using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// A <see cref="HyperlinkLabel"/> with an <see cref="IconImage"/> in front of it (e.g. the About dialog's brand
/// links). The icon and the text form one link: clicking either invokes <see cref="Command"/>, and hovering either
/// highlights the text. Like <see cref="HyperlinkLabel"/>, it is a label, not a button, so it has no pressed state.
/// </summary>
public partial class IconHyperlinkLabel : UserControl
{
    public static readonly StyledProperty<string?> IconNameProperty =
        AvaloniaProperty.Register<IconHyperlinkLabel, string?>(nameof(IconName));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<IconHyperlinkLabel, string?>(nameof(Text));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<IconHyperlinkLabel, ICommand?>(nameof(Command));

    public IconHyperlinkLabel() => AvaloniaXamlLoader.Load(this);

    /// <summary>The <see cref="AppIcons"/> key of the glyph shown before the text.</summary>
    public string? IconName
    {
        get => GetValue(IconNameProperty);
        set => SetValue(IconNameProperty, value);
    }

    /// <summary>The link text.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Command invoked when the link (icon or text) is clicked.</summary>
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        HyperlinkLabel.InvokeOnClick(e, Command, null);
    }
}
