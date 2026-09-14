using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ValheimServerGUI.App.Controls;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// The thin collection wrappers realize headlessly and pass their bound data through to the inner control.
public class CollectionWrapperTests
{
    private static void Realize(Control content)
    {
        var window = new Window { Content = content, Width = 300, Height = 200 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
    }

    private static DataGrid InnerGrid(DataListView view)
        => view.GetVisualDescendants().OfType<DataGrid>().First();

    [AvaloniaFact]
    public void DataListView_applies_shared_defaults_and_binds_items()
    {
        var view = new DataListView { ItemsSource = new[] { "a", "b", "c" } };
        view.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Avalonia.Data.Binding(".") });
        Realize(view);

        var grid = InnerGrid(view);
        Assert.True(grid.IsReadOnly);
        Assert.False(grid.CanUserReorderColumns);
        Assert.Equal(DataGridGridLinesVisibility.Horizontal, grid.GridLinesVisibility);
        // The consumer-declared column is forwarded to the real grid.
        Assert.Single(grid.Columns);
        Assert.Equal("Name", grid.Columns[0].Header);
    }

    [AvaloniaFact]
    public void DataListView_footer_hidden_until_a_slot_is_filled()
    {
        var bare = new DataListView { ItemsSource = new[] { "a" } };
        Realize(bare);
        Assert.False(bare.HasFooter);

        var withFooter = new DataListView
        {
            ItemsSource = new[] { "a" },
            FooterLeft = new IconButton { IconName = "Add_16x" },
            FooterRight = new IconButton { IconName = "Cancel_16x" },
        };
        Realize(withFooter);

        Assert.True(withFooter.HasFooter);
        // Both footer buttons are realized under the control.
        Assert.Equal(2, withFooter.GetVisualDescendants().OfType<IconButton>().Count());
    }

    [AvaloniaFact]
    public void DataListView_selection_flows_back_to_bound_property()
    {
        var view = new DataListView { ItemsSource = new[] { "Ragnar", "Odin" } };
        view.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Avalonia.Data.Binding(".") });
        Realize(view);

        InnerGrid(view).SelectedItem = "Odin";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Odin", view.SelectedItem);
    }

    [AvaloniaFact]
    public void LogViewer_binds_lines_to_inner_list()
    {
        var viewer = new LogViewer { ItemsSource = new ObservableCollection<string> { "line 1", "line 2" } };
        Realize(viewer);

        // The lines render as one contiguous, newline-joined block (see LogViewerTests for the full contract).
        var text = viewer.GetVisualDescendants().OfType<SelectableTextBlock>().First();
        Assert.Equal("line 1\nline 2", text.Text);
    }
}
