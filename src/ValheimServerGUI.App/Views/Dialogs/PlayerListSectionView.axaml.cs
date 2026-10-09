using System.ComponentModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.App.Infrastructure;
using ValheimServerGUI.App.ViewModels.Dialogs;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>One Manage Players tab, bound to a <see cref="PlayerListSectionViewModel"/>.</summary>
public partial class PlayerListSectionView : UserControl
{
    private readonly AsyncRelayCommand _copyId;
    private PlayerListSectionViewModel? _wired;

    public PlayerListSectionView()
    {
        InitializeComponent();

        // Copying needs this view's clipboard, so the menu item's command lives here (the footer uses CopyButton).
        _copyId = new AsyncRelayCommand(
            () => ClipboardHelper.CopyTextAsync(this, Vm?.SelectedAccount?.PlatformId),
            () => Vm?.HasSelection == true);
        CopyIdMenuItem.Command = _copyId;

        DataContextChanged += (_, _) =>
        {
            // Grid columns sit outside the visual tree (no DataContext to bind, no generated name field), so the
            // Default Role column — declared last — is hidden here: every row on the Banned tab is Banned.
            AccountsList.Columns[^1].IsVisible = Vm is { IsBanned: false };
            Wire();
        };
    }

    private PlayerListSectionViewModel? Vm => DataContext as PlayerListSectionViewModel;

    // Re-evaluate the copy command's enablement as the selection changes.
    private void Wire()
    {
        if (_wired is not null) _wired.PropertyChanged -= OnVmPropertyChanged;
        _wired = Vm;
        if (_wired is not null) _wired.PropertyChanged += OnVmPropertyChanged;
        _copyId.NotifyCanExecuteChanged();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayerListSectionViewModel.HasSelection)) _copyId.NotifyCanExecuteChanged();
    }
}
