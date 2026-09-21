using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>A minimal modal yes/no confirmation. Closes with a <see cref="bool"/> result.</summary>
public partial class ConfirmWindow : DialogWindow
{
    public ConfirmWindow()
    {
        InitializeComponent();
    }

    public ConfirmWindow(string title, string message) : this()
    {
        Title = title;
        MessageText.Text = message;
    }

    /// <summary>
    /// As <see cref="ConfirmWindow(string, string)"/>, but with custom button captions (the AXAML defaults are
    /// "Yes"/"No"). The confirm button still returns <c>true</c>, the cancel button <c>false</c>.
    /// </summary>
    public ConfirmWindow(string title, string message, string confirmText, string cancelText)
        : this(title, message)
    {
        YesButton.Content = confirmText;
        NoButton.Content = cancelText;
    }

    /// <summary>The chosen answer, mirrored here so a non-modal (ownerless) show can read it on close.</summary>
    public bool Result { get; private set; }

    private void OnYes(object? sender, RoutedEventArgs e)
    {
        Result = true;
        Close(true);
    }

    private void OnNo(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close(false);
    }
}
