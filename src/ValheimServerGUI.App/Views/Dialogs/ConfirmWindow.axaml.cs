using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>A minimal modal yes/no confirmation. Closes with a <see cref="bool"/> result.</summary>
public partial class ConfirmWindow : Window
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
