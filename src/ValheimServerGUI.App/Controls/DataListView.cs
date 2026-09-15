using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's one shared data table (the WinForms <c>DataListView</c> equivalent), used by both the Players
/// grid and the Player Details name list. It wraps a <see cref="DataGrid"/> carrying the shared styling —
/// LayerBase background, horizontal gridlines, read-only / no-column-reorder defaults — and adds an optional
/// compact <b>footer</b> hosting action controls (icon buttons) anchored left and/or right, matching the
/// header's density. The consumer declares its columns via <c>&lt;DataListView.Columns&gt;</c> and its
/// footer via <see cref="FooterLeft"/>/<see cref="FooterRight"/>; the ViewModel owns the data
/// (<see cref="ItemsSource"/>/<see cref="SelectedItem"/>). Any row styling the consumer sets through
/// <c>&lt;DataListView.Styles&gt;</c> still cascades to the inner grid's rows.
/// </summary>
public class DataListView : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<DataListView, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<DataListView, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Content anchored to the left of the footer (e.g. an Add/Edit/Remove icon-button group).</summary>
    public static readonly StyledProperty<object?> FooterLeftProperty =
        AvaloniaProperty.Register<DataListView, object?>(nameof(FooterLeft));

    /// <summary>Content anchored to the right of the footer (e.g. a Remove icon button).</summary>
    public static readonly StyledProperty<object?> FooterRightProperty =
        AvaloniaProperty.Register<DataListView, object?>(nameof(FooterRight));

    public static readonly DirectProperty<DataListView, bool> HasFooterProperty =
        AvaloniaProperty.RegisterDirect<DataListView, bool>(nameof(HasFooter), o => o.HasFooter);

    private DataGrid? _grid;
    private bool _hasFooter;

    public DataListView()
    {
        Columns.CollectionChanged += OnColumnsChanged;
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public object? FooterLeft
    {
        get => GetValue(FooterLeftProperty);
        set => SetValue(FooterLeftProperty, value);
    }

    public object? FooterRight
    {
        get => GetValue(FooterRightProperty);
        set => SetValue(FooterRightProperty, value);
    }

    /// <summary>True when either footer slot is populated (drives the footer bar's visibility in the template).</summary>
    public bool HasFooter
    {
        get => _hasFooter;
        private set => SetAndRaise(HasFooterProperty, ref _hasFooter, value);
    }

    /// <summary>The grid's columns, declared inline by the consumer and forwarded to the inner grid.</summary>
    public ObservableCollection<DataGridColumn> Columns { get; } = new();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _grid = e.NameScope.Find<DataGrid>("PART_Grid");
        if (_grid is null) return;

        // Shared defaults (previously set on the DataGrid subclass).
        _grid.IsReadOnly = true;
        _grid.CanUserReorderColumns = false;
        // No gridlines between rows; rows are separated only by the 2px inter-row spacing (see DataGrid.axaml).
        _grid.GridLinesVisibility = DataGridGridLinesVisibility.None;
        _grid.Bind(BackgroundProperty, _grid.GetResourceObservable("LayerBase"));

        // Forward the consumer-declared columns into the real grid.
        _grid.Columns.Clear();
        foreach (var column in Columns) _grid.Columns.Add(column);

        // Pass the data through. SelectedItem is two-way so selection flows back to the ViewModel.
        _grid.Bind(DataGrid.ItemsSourceProperty, new Binding(nameof(ItemsSource)) { Source = this });
        _grid.Bind(DataGrid.SelectedItemProperty,
            new Binding(nameof(SelectedItem)) { Source = this, Mode = BindingMode.TwoWay });
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FooterLeftProperty || change.Property == FooterRightProperty)
            HasFooter = FooterLeft is not null || FooterRight is not null;
    }

    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_grid is null) return;
        _grid.Columns.Clear();
        foreach (var column in Columns) _grid.Columns.Add(column);
    }
}
