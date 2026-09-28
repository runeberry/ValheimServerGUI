using Avalonia.Controls;
using ValheimServerGUI.App.ViewModels.Dialogs;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>One Manage Players tab, bound to a <see cref="PlayerListSectionViewModel"/>.</summary>
public partial class PlayerListSectionView : UserControl
{
    public PlayerListSectionView()
    {
        InitializeComponent();

        // Grid columns sit outside the visual tree (no DataContext to bind, no generated name field), so the
        // Default Role column — declared last — is hidden here: the Banned list has no role to show or change.
        DataContextChanged += (_, _) =>
            AccountsList.Columns[^1].IsVisible = DataContext is PlayerListSectionViewModel { HasRoleCommands: true };
    }
}
