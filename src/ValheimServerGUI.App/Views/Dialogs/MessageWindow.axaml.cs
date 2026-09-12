using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>A simple informational dialog with a single OK button (errors, warnings).</summary>
public partial class MessageWindow : Window
{
    public MessageWindow()
    {
        InitializeComponent();
    }

    public MessageWindow(string title, string message) : this()
    {
        Title = title;
        MessageText.Text = message;
    }

    private void OnOk(object? sender, RoutedEventArgs e) => Close();
}
