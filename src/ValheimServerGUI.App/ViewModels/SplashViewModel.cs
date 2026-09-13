using System;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimServerGUI.App.Startup;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.ViewModels;

/// <summary>Backs the splash window: a status line and a determinate progress bar (0–100).</summary>
public partial class SplashViewModel : ViewModelBase, IProgress<StartupProgress>
{
    /// <summary>App version shown under the name on the splash (e.g. "v3.0.0-rc.1").</summary>
    public string Version { get; } = "v" + AssemblyHelper.GetApplicationVersion();

    [ObservableProperty]
    private string _statusText = "Starting…";

    /// <summary>Progress as a percentage (0–100) for a determinate <c>ProgressBar</c>.</summary>
    [ObservableProperty]
    private double _progressPercent;

    public void Report(StartupProgress value) => RunOnUi(() =>
    {
        StatusText = value.Message;
        ProgressPercent = Math.Clamp(value.Fraction, 0, 1) * 100;
    });
}
