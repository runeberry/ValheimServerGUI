using System;
using Avalonia;
using Avalonia.Controls;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's standard grid (the WinForms <c>DataListView</c> equivalent): a <see cref="DataGrid"/> carrying
/// the shared styling — the LayerBase background, horizontal gridlines, and read-only / no-column-reorder
/// defaults — so every grid looks and behaves the same. The consumer still declares its own
/// <c>&lt;DataListView.Columns&gt;</c> and any row styling (the ViewModel owns the data).
/// </summary>
public class DataListView : DataGrid
{
    public DataListView()
    {
        IsReadOnly = true;
        CanUserReorderColumns = false;
        GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;
        // Theme-aware background that tracks the palette resource (and theme switches).
        this.Bind(BackgroundProperty, this.GetResourceObservable("LayerBase"));
    }

    // Use the DataGrid Fluent theme; a subclass otherwise resolves no theme.
    protected override Type StyleKeyOverride => typeof(DataGrid);
}
