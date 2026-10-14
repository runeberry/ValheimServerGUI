using System;
using System.Collections.Generic;
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
    private readonly HashSet<string> _viewStateProperties = new(StringComparer.Ordinal);

    protected ModalEditViewModel()
    {
        PropertyChanged += (_, e) =>
        {
            if (!_suppressDirty && e.PropertyName != nameof(IsDirty)
                && !_viewStateProperties.Contains(e.PropertyName ?? string.Empty))
                IsDirty = true;
        };
    }

    /// <summary>
    /// Marks properties as pure view state (e.g. a list's selected item) so changing them does <b>not</b>
    /// trip the dirty flag. Without this every observable property — selection included — would count as an
    /// edit, so merely selecting a row would falsely prompt "unsaved changes" on close.
    /// </summary>
    protected void IgnoreForDirty(params string[] propertyNames)
    {
        foreach (var name in propertyNames) _viewStateProperties.Add(name);
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
