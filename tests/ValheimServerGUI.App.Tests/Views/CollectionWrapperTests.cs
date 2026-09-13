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

    [AvaloniaFact]
    public void DataListView_applies_shared_defaults_and_binds_items()
    {
        var grid = new DataListView { ItemsSource = new[] { "a", "b", "c" } };
        Realize(grid);

        Assert.True(grid.IsReadOnly);
        Assert.False(grid.CanUserReorderColumns);
        Assert.Equal(DataGridGridLinesVisibility.Horizontal, grid.GridLinesVisibility);
    }

    [AvaloniaFact]
    public void LogViewer_binds_lines_to_inner_list()
    {
        var viewer = new LogViewer { ItemsSource = new ObservableCollection<string> { "line 1", "line 2" } };
        Realize(viewer);

        var list = viewer.GetVisualDescendants().OfType<ListBox>().First();
        Assert.Equal(2, list.ItemCount);
    }

    [AvaloniaFact]
    public void SelectListField_binds_items_and_selection()
    {
        var field = new SelectListField
        {
            ItemsSource = new[] { "Ragnar", "Odin" },
            Value = "Odin",
        };
        Realize(field);

        var list = field.GetVisualDescendants().OfType<ListBox>().First();
        Assert.Equal(2, list.ItemCount);
        Assert.Equal("Odin", list.SelectedItem);

        list.SelectedItem = "Ragnar";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Ragnar", field.Value);
    }

    [AvaloniaFact]
    public void AddRemoveListField_binds_items_through_inner_selectlist()
    {
        var field = new AddRemoveListField { ItemsSource = new[] { "Ragnar", "Odin", "Freya" } };
        Realize(field);

        var list = field.GetVisualDescendants().OfType<ListBox>().First();
        Assert.Equal(3, list.ItemCount);

        // Three icon buttons (Add/Edit/Remove) are present.
        Assert.Equal(3, field.GetVisualDescendants().OfType<IconButton>().Count());
    }
}
