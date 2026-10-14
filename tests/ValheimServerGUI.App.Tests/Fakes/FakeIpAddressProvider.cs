using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.Tests.Fakes;

/// <summary>
/// Port availability from <see cref="OccupiedPorts"/> instead of the machine's live UDP listeners, so a start
/// flow never depends on what else happens to be bound on the host. No network: addresses stay null.
/// </summary>
internal sealed class FakeIpAddressProvider : IIpAddressProvider
{
    public HashSet<int> OccupiedPorts { get; } = new();

    public string? ExternalIpAddress => null;

    public string? InternalIpAddress => null;

    public event EventHandler<string?>? ExternalIpChanged { add { } remove { } }

    public event EventHandler<string?>? InternalIpChanged { add { } remove { } }

    public Task LoadExternalIpAddressAsync() => Task.CompletedTask;

    public Task LoadInternalIpAddressAsync() => Task.CompletedTask;

    public bool IsLocalUdpPortAvailable(params int[] ports) => !ports.Any(OccupiedPorts.Contains);
}
