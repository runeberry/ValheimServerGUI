using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.ViewModels;

/// <summary>
/// Players tab (§7.4): a live table fed by the shared player repository. Rows update from
/// <c>EntityUpdated</c>/<c>PlayerStatusChanged</c>; the relative "Since" column is recomputed every second
/// while the tab is visible. View-Details is enabled when a row is selected; Remove only when it is Offline.
/// </summary>
public partial class PlayersViewModel : ViewModelBase
{
    private readonly IPlayerDataRepository _repo;
    private readonly Dictionary<string, PlayerRowViewModel> _rows = new();
    private readonly DispatcherTimer _sinceTimer;

    public PlayersViewModel(IPlayerDataRepository repo)
    {
        _repo = repo;

        _sinceTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _sinceTimer.Tick += (_, _) => RefreshSince();

        _repo.EntityUpdated += OnEntityUpdated;
        _repo.PlayerStatusChanged += OnEntityUpdated;
        _repo.EntityRemoved += OnEntityRemoved;
        _repo.DataUpdated += OnDataReloaded;
        _repo.DataReady += OnDataReloaded;

        ReloadAll();
    }

    public ObservableCollection<PlayerRowViewModel> Players { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanViewDetails), nameof(CanRemove))]
    [NotifyCanExecuteChangedFor(nameof(ViewDetailsCommand), nameof(RemoveCommand))]
    private PlayerRowViewModel? _selectedPlayer;

    public bool CanViewDetails => SelectedPlayer is not null;

    /// <summary>Remove is only allowed for an Offline player (§7.4).</summary>
    public bool CanRemove => SelectedPlayer is { IsOffline: true };

    /// <summary>Raised for View Player Details (the dialog is wired in Wave 6).</summary>
    public event Action<PlayerInfo>? ViewDetailsRequested;

    public void SetActive(bool active)
    {
        if (active)
        {
            RefreshSince();
            _sinceTimer.Start();
        }
        else
        {
            _sinceTimer.Stop();
        }
    }

    [RelayCommand(CanExecute = nameof(CanViewDetails))]
    private void ViewDetails()
    {
        if (SelectedPlayer is not null) ViewDetailsRequested?.Invoke(SelectedPlayer.Player);
    }

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove()
    {
        if (SelectedPlayer is { IsOffline: true } row)
            _repo.Remove(row.Key);
    }

    private void RefreshSince()
    {
        foreach (var row in Players) row.RefreshSince();
    }

    private void OnEntityUpdated(object? sender, PlayerInfo player) => RunOnUi(() => Upsert(player));

    private void OnEntityRemoved(object? sender, PlayerInfo player) => RunOnUi(() =>
    {
        if (_rows.Remove(player.Key, out var row))
        {
            Players.Remove(row);
            if (ReferenceEquals(SelectedPlayer, row)) SelectedPlayer = null;
        }
    });

    private void OnDataReloaded(object? sender, EventArgs e) => RunOnUi(ReloadAll);

    private void Upsert(PlayerInfo player)
    {
        if (_rows.TryGetValue(player.Key, out var row))
        {
            row.Update(player);
            if (ReferenceEquals(SelectedPlayer, row))
                OnPropertyChanged(nameof(CanRemove)); // offline-ness may have changed
        }
        else
        {
            var newRow = new PlayerRowViewModel(player);
            _rows[player.Key] = newRow;
            Players.Add(newRow);
        }
    }

    private void ReloadAll()
    {
        _rows.Clear();
        Players.Clear();
        foreach (var player in _repo.Data)
        {
            var row = new PlayerRowViewModel(player);
            _rows[player.Key] = row;
            Players.Add(row);
        }
    }

    protected override void DisposeCore()
    {
        _sinceTimer.Stop();
        _repo.EntityUpdated -= OnEntityUpdated;
        _repo.PlayerStatusChanged -= OnEntityUpdated;
        _repo.EntityRemoved -= OnEntityRemoved;
        _repo.DataUpdated -= OnDataReloaded;
        _repo.DataReady -= OnDataReloaded;
    }
}
