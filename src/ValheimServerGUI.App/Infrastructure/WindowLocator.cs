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
}
