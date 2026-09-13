using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// A real text hyperlink (the WinForms <c>LinkLabel</c> equivalent): a <see cref="TextBlock"/> — not a
/// button — that reads as underlined accent text and invokes <see cref="Command"/> when clicked. The link
/// appearance lives in <c>Styles/AppStyles.axaml</c> (type-targeted), so any <see cref="HyperlinkLabel"/> renders
/// as a link without opting into a style class.
/// </summary>
public class HyperlinkLabel : TextBlock
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<HyperlinkLabel, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<HyperlinkLabel, object?>(nameof(CommandParameter));

    /// <summary>Command invoked when the link is clicked.</summary>
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>Parameter passed to <see cref="Command"/>.</summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (e.InitialPressMouseButton != MouseButton.Left) return;

        var command = Command;
        if (command is not null && command.CanExecute(CommandParameter))
        {
            command.Execute(CommandParameter);
            e.Handled = true;
        }
    }
}
