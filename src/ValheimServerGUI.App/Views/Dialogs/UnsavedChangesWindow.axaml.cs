using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>The unsaved-changes choice for PlayerDetails / WorldPreferences (§13.3).</summary>
public enum UnsavedChangesChoice { Save, Discard, Cancel }

public partial class UnsavedChangesWindow : DialogWindow
{
    public UnsavedChangesWindow()
    {
        InitializeComponent();
    }

    private void OnSave(object? sender, RoutedEventArgs e) => Close(UnsavedChangesChoice.Save);
    private void OnDiscard(object? sender, RoutedEventArgs e) => Close(UnsavedChangesChoice.Discard);
    private void OnCancel(object? sender, RoutedEventArgs e) => Close(UnsavedChangesChoice.Cancel);
}
