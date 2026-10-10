using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ValheimServerGUI.App.Controls;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// The "?" help glyph is exempt from field disabling: its tooltip must open on hover even when the field it
// belongs to is disabled (e.g. every server setting while the server runs).
public class HelpGlyphTooltipTests
{
    private const string Help = "What this field does.";

    // Hovers the field's "?" glyph and reports whether its tooltip opened.
    private static bool HoverOpensHelp(Control field, bool fieldEnabled)
    {
        field.IsEnabled = fieldEnabled;
        var window = new Window { Content = field, Width = 400, Height = 200 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var glyph = field.GetVisualDescendants().OfType<HelpLabel>().Single()
            .GetVisualDescendants().OfType<TextBlock>().Single();
        var center = glyph.TranslatePoint(new Point(glyph.Bounds.Width / 2, glyph.Bounds.Height / 2), window)!.Value;

        // The headless dispatcher does not run the show-delay timer; open immediately instead.
        ToolTip.SetShowDelay(glyph, 0);
        window.MouseMove(center, RawInputModifiers.None);

        var sw = Stopwatch.StartNew();
        while (!ToolTip.GetIsOpen(glyph) && sw.ElapsedMilliseconds < 2000)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(20);
        }
        var open = ToolTip.GetIsOpen(glyph);
        window.Close();
        return open;
    }

    public static TheoryData<string> Fields => new() { "text", "filename", "numeric", "checkbox", "radio" };

    private static Control Make(string kind) => kind switch
    {
        "text" => new TextFormField { LabelText = "Name", HelpText = Help },
        "filename" => new FilenameFormField { LabelText = "Path", HelpText = Help },
        "numeric" => new NumericFormField { LabelText = "Port", HelpText = Help },
        "checkbox" => new CheckBoxFormField { LabelText = "Public", HelpText = Help },
        "radio" => new RadioFormField { LabelText = "New", HelpText = Help, GroupName = "g" },
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    [AvaloniaTheory]
    [MemberData(nameof(Fields))]
    public void Help_tooltip_opens_on_an_enabled_field(string kind)
    {
        Assert.True(HoverOpensHelp(Make(kind), fieldEnabled: true));
    }

    [AvaloniaTheory]
    [MemberData(nameof(Fields))]
    public void Help_tooltip_opens_on_a_disabled_field(string kind)
    {
        Assert.True(HoverOpensHelp(Make(kind), fieldEnabled: false));
    }
}
