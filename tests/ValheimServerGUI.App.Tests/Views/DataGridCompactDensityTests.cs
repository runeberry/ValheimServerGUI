using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ValheimServerGUI.App.Controls;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// § V3 styling: the shared grid renders at the app body font (12px, not Fluent's touch-sized 15px cell
// text) with half of Fluent's default cell/header padding — vertical (row/header height 32 -> 16) and
// horizontal (cell text inset 12,0,12,0 -> 6,0,6,0 ; header 12,0,0,0 -> 6,0,0,0). Pinned so a Fluent
// theme bump that re-inflates the grid is caught.
public class DataGridCompactDensityTests
{
    private static DataListView Realize()
    {
        var grid = new DataListView { ItemsSource = new[] { "a", "b", "c" } };
        grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Avalonia.Data.Binding(".") });
        var window = new Window { Content = grid, Width = 300, Height = 200 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(300, 200));
        window.Arrange(new Rect(new Size(300, 200)));
        Dispatcher.UIThread.RunJobs();
        return grid;
    }

    [AvaloniaFact]
    public void Cells_use_app_font_and_halved_padding()
    {
        var grid = Realize();

        var cell = grid.GetVisualDescendants().OfType<DataGridCell>().First();
        Assert.Equal(12, cell.FontSize);
        Assert.Equal(16, cell.MinHeight);

        var cellText = cell.GetVisualDescendants().OfType<TextBlock>().First();
        Assert.Equal(12, cellText.FontSize);
        Assert.Equal(new Thickness(6, 0, 6, 0), cellText.Margin);
    }

    [AvaloniaFact]
    public void Headers_use_app_font_and_halved_padding()
    {
        var grid = Realize();

        var header = grid.GetVisualDescendants().OfType<DataGridColumnHeader>()
            .First(h => h.Content is not null);
        Assert.Equal(12, header.FontSize);
        Assert.Equal(16, header.MinHeight);
        Assert.Equal(new Thickness(6, 0, 0, 0), header.Padding);
    }
}
