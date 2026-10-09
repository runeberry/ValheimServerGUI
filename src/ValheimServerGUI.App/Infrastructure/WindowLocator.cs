using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace ValheimServerGUI.App.Infrastructure;

/// <summary>Finds a window to parent a dialog on, from the classic desktop lifetime's open windows.</summary>
internal static class WindowLocator
{
    /// <summary>
    /// The best owner for a modal dialog: the currently-active window, else any open window, else null
    /// (e.g. a crash during startup before any window has opened).
    /// </summary>
    public static Window? ActiveWindow
    {
        get
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return null;

            return desktop.Windows.FirstOrDefault(w => w.IsActive)
                   ?? desktop.MainWindow
                   ?? desktop.Windows.FirstOrDefault();
        }
    }

    /// <summary>
    /// The app window a dialog that has no owner should appear over: the active window other than the dialog itself,
    /// else the main window, else any other open window; null when it is the only window.
    /// </summary>
    public static Window? AnchorFor(Window dialog)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;

        var others = desktop.Windows.Where(w => w != dialog && w.IsVisible).ToList();
        return others.FirstOrDefault(w => w.IsActive)
               ?? (desktop.MainWindow is { } main && main != dialog && main.IsVisible ? main : null)
               ?? others.FirstOrDefault();
    }
}
