using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// Base for the OK / Cancel / Restore-Defaults dialog VMs (§11.2 reusable shape): load on open, flush on
/// OK. Tracks a dirty flag (any edit after load) to back the unsaved-changes guard (§13.3). Derivations
/// implement <see cref="ApplyDefaults"/> and typically a <c>Save</c> the window calls on OK.
/// </summary>
public abstract partial class ModalEditViewModel : ObservableObject
{
    private bool _suppressDirty;

    protected ModalEditViewModel()
    {
        PropertyChanged += (_, e) =>
        {
            if (!_suppressDirty && e.PropertyName != nameof(IsDirty))
                IsDirty = true;
        };
    }

    /// <summary>True once the user has changed any field since load (drives the unsaved-changes guard).</summary>
    [ObservableProperty]
    private bool _isDirty;

    /// <summary>Loads initial values without tripping the dirty flag.</summary>
    protected void LoadClean(Action load)
    {
        _suppressDirty = true;
        try { load(); }
        finally { _suppressDirty = false; }
        IsDirty = false;
    }

    /// <summary>Restores default values (marks dirty — the user must still confirm with OK).</summary>
    public abstract void ApplyDefaults();
}
