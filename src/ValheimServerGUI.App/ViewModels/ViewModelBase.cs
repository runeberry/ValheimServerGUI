using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ValheimServerGUI.App.ViewModels;

/// <summary>
/// Base for all view-models. Provides the marshal-and-guard primitive (<see cref="RunOnUi"/>) that every
/// Core event handler must funnel through: Core events fire off the UI thread, so handlers marshal onto
/// <see cref="Dispatcher.UIThread"/>, and any callback that arrives after the view-model is torn down is
/// dropped. This is the Avalonia equivalent of v2.4's <c>BuildEventHandler</c> <c>InvokeRequired</c> +
/// <c>IsDisposed</c> guard.
/// </summary>
public abstract class ViewModelBase : ObservableObject, IDisposable
{
    private bool _disposed;

    /// <summary>True once <see cref="Dispose"/> has run. Late Core callbacks check this and no-op.</summary>
    protected bool IsDisposed => _disposed;

    /// <summary>
    /// Runs <paramref name="action"/> on the UI thread, dropping it if the view-model has been disposed.
    /// Executes inline when already on the UI thread; otherwise posts to the dispatcher.
    /// </summary>
    protected void RunOnUi(Action action)
    {
        if (_disposed) return;

        if (Dispatcher.UIThread.CheckAccess())
        {
            if (!_disposed) action();
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!_disposed) action();
            });
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    /// <summary>Override to unsubscribe from Core events and release owned resources.</summary>
    protected virtual void DisposeCore()
    {
    }
}
