using Avalonia.Controls;
using Avalonia.Interactivity;
using ValheimServerGUI.App.ViewModels;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>Move / Copy / Cancel prompt for hosting a Steam Cloud world. Closes with a <see cref="CloudImportChoice"/>.</summary>
public partial class CloudImportWindow : Window
{
    public CloudImportWindow()
    {
        InitializeComponent();
    }

    public CloudImportWindow(string worldName) : this()
    {
        HeadingText.Text = $"Host the cloud world '{worldName}'?";
    }

    private void OnMove(object? sender, RoutedEventArgs e) => Close(CloudImportChoice.Move);
    private void OnCopy(object? sender, RoutedEventArgs e) => Close(CloudImportChoice.Copy);
    private void OnCancel(object? sender, RoutedEventArgs e) => Close(CloudImportChoice.Cancel);
}
