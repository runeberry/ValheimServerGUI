using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>The user's answer to the start-time player-role conflict prompt.</summary>
public enum RoleConflictChoice { UseServerProfile, UseRolesFromFile, Cancel }

/// <summary>
/// Shown at server start when the profile's roles disagree with the on-disk list files. Closes with a
/// <see cref="RoleConflictChoice"/>: keep the profile (regenerate, backing up the files), keep the files
/// (skip generation), or cancel the start.
/// </summary>
public partial class RoleConflictWindow : Window
{
    public RoleConflictWindow()
    {
        InitializeComponent();
    }

    public RoleConflictWindow(string message) : this()
    {
        MessageText.Text = message;
    }

    private void OnUseServerProfile(object? sender, RoutedEventArgs e) => Close(RoleConflictChoice.UseServerProfile);
    private void OnUseRolesFromFile(object? sender, RoutedEventArgs e) => Close(RoleConflictChoice.UseRolesFromFile);
    private void OnCancel(object? sender, RoutedEventArgs e) => Close(RoleConflictChoice.Cancel);
}
