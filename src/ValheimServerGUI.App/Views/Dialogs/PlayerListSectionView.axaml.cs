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
        // Default Role column — declared last — is hidden here: every row on the Banned tab is Banned.
        DataContextChanged += (_, _) =>
            AccountsList.Columns[^1].IsVisible = DataContext is PlayerListSectionViewModel { IsBanned: false };
    }
}
