using System.Collections.Generic;
using ValheimServerGUI.App.Views;

namespace ValheimServerGUI.App.Infrastructure;

/// <summary>
/// Tracks the open <see cref="MainWindow"/> instances (multi-window, §2.2) and disposes each window's
/// view-model — and therefore its transient <see cref="ValheimServerGUI.Game.ValheimServer"/> — when the
/// window closes. Process exit is governed by the lifetime's <c>OnLastWindowClose</c>; this manager only
/// owns the per-window bookkeeping the tray and "New Window" build on.
/// </summary>
internal sealed class WindowManager
{
    private readonly List<MainWindow> _windows = new();

    public IReadOnlyList<MainWindow> Windows => _windows;

    public void Register(MainWindow window)
    {
        _windows.Add(window);
        window.Closed += OnWindowClosed;
    }

    private void OnWindowClosed(object? sender, System.EventArgs e)
    {
        if (sender is not MainWindow window) return;

        window.Closed -= OnWindowClosed;
        _windows.Remove(window);
        window.ViewModel?.Dispose();
    }
}
