using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.ViewModels;

/// <summary>
/// Server Details tab (§10.2): connection details (External/Internal/Local IP + a session invite code)
/// and statistics (uptime, last/average world-save). Read-only, refreshed while the tab is visible via a
/// 1s timer plus the live server/IP events. The IP shown appends <c>:port</c> only for a non-default port.
/// </summary>
public partial class ServerDetailsViewModel : ViewModelBase
{
    private const string LoadingText = "Loading…";

    private readonly ValheimServer _server;
    private readonly IIpAddressProvider _ip;
    private readonly Func<int> _portProvider;
    private readonly Queue<decimal> _worldSaveTimes = new();
    private readonly DispatcherTimer _uptimeTimer;

    private string? _rawExternalIp;
    private string? _rawInternalIp;
    private DateTimeOffset? _startedAt;
    private bool _isActive;

    public ServerDetailsViewModel(ValheimServer server, IIpAddressProvider ip, Func<int> portProvider)
    {
        _server = server;
        _ip = ip;
        _portProvider = portProvider;

        _uptimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _uptimeTimer.Tick += (_, _) => RefreshUptime();

        _rawExternalIp = _ip.ExternalIpAddress;
        _rawInternalIp = _ip.InternalIpAddress;

        _server.StatusChanged += OnStatusChanged;
        _server.WorldSaved += OnWorldSaved;
        _server.InviteCodeReady += OnInviteCodeReady;
        _ip.ExternalIpChanged += OnExternalIpChanged;
        _ip.InternalIpChanged += OnInternalIpChanged;

        RefreshIpLabels();
    }

    [ObservableProperty] private string _externalIp = LoadingText;
    [ObservableProperty] private string _internalIp = LoadingText;
    [ObservableProperty] private string _localIp = "127.0.0.1";
    [ObservableProperty] private string _inviteCode = "N/A";
    [ObservableProperty] private bool _inviteCodeCopyable;
    [ObservableProperty] private string _uptime = "00:00:00";
    [ObservableProperty] private string _lastWorldSave = "N/A";
    [ObservableProperty] private string _averageWorldSave = "N/A";

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
        if (_server.Status != ServerStatus.Running || _startedAt is null) return;

        var elapsed = DateTimeOffset.Now - _startedAt.Value;
        var text = elapsed.ToServerElapsedFormat();
        if (elapsed.Days == 1) text = $"1 day + {text}";
        else if (elapsed.Days > 1) text = $"{elapsed.Days} days + {text}";
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
        if (string.IsNullOrWhiteSpace(ip)) return LoadingText;
        var port = _portProvider();
        return port == CoreConstants.DefaultServerPort ? ip : $"{ip}:{port}";
    }

    private void OnStatusChanged(object? sender, ServerStatus status) => RunOnUi(() =>
    {
        if (status == ServerStatus.Running)
        {
            _startedAt = DateTimeOffset.Now;
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
            SetInviteCode("Loading…", copyable: false);
    });

    private void OnWorldSaved(object? sender, decimal durationMs) => RunOnUi(() =>
    {
        LastWorldSave = $"{DateTime.Now:G} ({durationMs:F0}ms)";

        if (_worldSaveTimes.Count >= 10) _worldSaveTimes.Dequeue();
        _worldSaveTimes.Enqueue(durationMs);
        AverageWorldSave = $"{_worldSaveTimes.Average():F0}ms";
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
            InviteCode = "N/A";
            InviteCodeCopyable = false;
            return;
        }

        InviteCode = code;
        InviteCodeCopyable = copyable;
    }

    protected override void DisposeCore()
    {
        _uptimeTimer.Stop();
        _server.StatusChanged -= OnStatusChanged;
        _server.WorldSaved -= OnWorldSaved;
        _server.InviteCodeReady -= OnInviteCodeReady;
        _ip.ExternalIpChanged -= OnExternalIpChanged;
        _ip.InternalIpChanged -= OnInternalIpChanged;
    }
}
