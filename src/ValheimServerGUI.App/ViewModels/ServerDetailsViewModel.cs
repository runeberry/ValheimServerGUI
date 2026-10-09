using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.ViewModels;

/// <summary>
/// Server Details tab (§10.2): connection details (External/Internal/Local IP + a session invite code)
/// and statistics (uptime, last/average world-save). Read-only, refreshed while the tab is visible via a
/// 1s timer plus the live server/IP events. The IP shown appends <c>:port</c> only for a non-default port.
/// </summary>
public partial class ServerDetailsViewModel : ViewModelBase
{
    private ValheimServer? _server;
    private readonly IIpAddressProvider _ip;
    private readonly Func<int> _portProvider;
    private readonly Queue<decimal> _worldSaveTimes = new();
    private readonly DispatcherTimer _uptimeTimer;

    private string? _rawExternalIp;
    private string? _rawInternalIp;
    private DateTimeOffset? _startedAt;
    private bool _isActive;

    public ServerDetailsViewModel(IIpAddressProvider ip, Func<int> portProvider)
    {
        _ip = ip;
        _portProvider = portProvider;

        _uptimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _uptimeTimer.Tick += (_, _) => RefreshUptime();

        _rawExternalIp = _ip.ExternalIpAddress;
        _rawInternalIp = _ip.InternalIpAddress;

        // IP subscriptions are on the shared singleton provider — wired once, never re-targeted.
        _ip.ExternalIpChanged += OnExternalIpChanged;
        _ip.InternalIpChanged += OnInternalIpChanged;

        RefreshIpLabels();
    }

    /// <summary>
    /// Re-targets the tab onto <paramref name="next"/> (profile switch): unsubscribes the previous server's
    /// events, subscribes the new one, and reseeds the derived readouts (invite code, world-save history,
    /// uptime — seeded from the server's own <see cref="ValheimServer.StartedAt"/> so an already-running
    /// server reports its true uptime). The timer and <c>_isActive</c> survive (preferred over recreate).
    /// </summary>
    public void SetServer(ValheimServer next)
    {
        if (ReferenceEquals(next, _server)) return;

        if (_server is not null)
        {
            _server.StatusChanged -= OnStatusChanged;
            _server.WorldSaved -= OnWorldSaved;
            _server.InviteCodeReady -= OnInviteCodeReady;
        }

        _server = next;

        _server.StatusChanged += OnStatusChanged;
        _server.WorldSaved += OnWorldSaved;
        _server.InviteCodeReady += OnInviteCodeReady;

        // Reset the per-server derived state and reseed from the new server.
        _worldSaveTimes.Clear();
        LastWorldSave = Strings.Common_NotAvailable;
        AverageWorldSave = Strings.Common_NotAvailable;
        SetInviteCode(null); // the invite code only arrives via an event; a re-target has missed it
        _startedAt = _server.StartedAt;
        RefreshUptime();
    }

    [ObservableProperty] private string _externalIp = Strings.Common_Loading;
    [ObservableProperty] private string _internalIp = Strings.Common_Loading;
    [ObservableProperty] private string _localIp = "127.0.0.1";
    [ObservableProperty] private string _inviteCode = Strings.Common_NotAvailable;
    [ObservableProperty] private bool _inviteCodeCopyable;
    [ObservableProperty] private string _uptime = "00:00:00";
    [ObservableProperty] private string _lastWorldSave = Strings.Common_NotAvailable;
    [ObservableProperty] private string _averageWorldSave = Strings.Common_NotAvailable;

    /// <summary>Called by the view when the tab becomes visible/hidden — drives the lazy 1s refresh.</summary>
    public void SetActive(bool active)
    {
        _isActive = active;
        if (active)
        {
            RefreshIpLabels();
            RefreshUptime();
            LoadMissingIps();
            _uptimeTimer.Start();
        }
        else
        {
            _uptimeTimer.Stop();
        }
    }

    private void LoadMissingIps()
    {
        if (_rawExternalIp is null) _ = _ip.LoadExternalIpAddressAsync();
        if (_rawInternalIp is null) _ = _ip.LoadInternalIpAddressAsync();
    }

    private void RefreshUptime()
    {
        if (_server is null || _server.Status != ServerStatus.Running || _startedAt is null) return;

        var elapsed = DateTimeOffset.Now - _startedAt.Value;
        var text = elapsed.ToServerElapsedFormat();
        if (elapsed.Days == 1) text = string.Format(Strings.ServerDetails_Uptime_OneDay, text);
        else if (elapsed.Days > 1) text = string.Format(Strings.ServerDetails_Uptime_Days, elapsed.Days, text);
        Uptime = text;
    }

    private void RefreshIpLabels()
    {
        ExternalIp = FormatIp(_rawExternalIp);
        InternalIp = FormatIp(_rawInternalIp);
        LocalIp = FormatIp("127.0.0.1")!;
    }

    private string FormatIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return Strings.Common_Loading;
        var port = _portProvider();
        return port == CoreConstants.DefaultServerPort ? ip : $"{ip}:{port}";
    }

    private void OnStatusChanged(object? sender, ServerStatus status) => RunOnUi(() =>
    {
        if (status == ServerStatus.Running)
        {
            _startedAt = _server?.StartedAt ?? DateTimeOffset.Now;
            RefreshUptime();
        }
        else
        {
            _startedAt = null;
            Uptime = "00:00:00";
        }

        if (status == ServerStatus.Stopped)
            SetInviteCode(null);
        else if (status == ServerStatus.Starting)
            SetInviteCode(Strings.Common_Loading, copyable: false);
    });

    private void OnWorldSaved(object? sender, decimal durationMs) => RunOnUi(() =>
    {
        LastWorldSave = string.Format(Strings.ServerDetails_LastWorldSave_Value, DateTime.Now, durationMs);

        if (_worldSaveTimes.Count >= 10) _worldSaveTimes.Dequeue();
        _worldSaveTimes.Enqueue(durationMs);
        AverageWorldSave = string.Format(Strings.ServerDetails_AvgWorldSave_Value, _worldSaveTimes.Average());
    });

    private void OnInviteCodeReady(object? sender, string code) => RunOnUi(() => SetInviteCode(code));

    private void OnExternalIpChanged(object? sender, string? ip) => RunOnUi(() =>
    {
        _rawExternalIp = ip;
        ExternalIp = FormatIp(ip);
    });

    private void OnInternalIpChanged(object? sender, string? ip) => RunOnUi(() =>
    {
        _rawInternalIp = ip;
        InternalIp = FormatIp(ip);
    });

    private void SetInviteCode(string? code, bool copyable = true)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            InviteCode = Strings.Common_NotAvailable;
            InviteCodeCopyable = false;
            return;
        }

        InviteCode = code;
        InviteCodeCopyable = copyable;
    }

    protected override void DisposeCore()
    {
        _uptimeTimer.Stop();
        if (_server is not null)
        {
            _server.StatusChanged -= OnStatusChanged;
            _server.WorldSaved -= OnWorldSaved;
            _server.InviteCodeReady -= OnInviteCodeReady;
        }
        _ip.ExternalIpChanged -= OnExternalIpChanged;
        _ip.InternalIpChanged -= OnInternalIpChanged;
    }
}
