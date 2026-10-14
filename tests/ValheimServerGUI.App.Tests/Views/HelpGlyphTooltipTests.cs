using System;
using System.Linq;
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

// The "?" help glyph is exempt from field disabling: on a disabled field (e.g. every server setting while the
// server runs) the input locks, but the glyph stays enabled, so hovering it gets the pointer (cursor) and opens
// its tooltip. Avalonia routes pointer-over only to enabled elements, so a glyph inside a disabled subtree fails
// the IsPointerOver check even if its tooltip is forced open.
public class HelpGlyphTooltipTests
{
    private const string Help = "What this field does.";

    public static TheoryData<string> Fields => new() { "text", "filename", "numeric", "duration", "dropdown", "checkbox", "radio" };

    private static Control Make(string kind) => kind switch
    {
        "text" => new TextFormField { LabelText = "Name", HelpText = Help },
        "filename" => new FilenameFormField { LabelText = "Path", HelpText = Help },
        "numeric" => new NumericFormField { LabelText = "Port", HelpText = Help },
        "duration" => new DurationFormField { LabelText = "Interval", HelpText = Help },
        "dropdown" => new DropdownFormField { LabelText = "World", HelpText = Help },
        "checkbox" => new CheckBoxFormField { LabelText = "Public", HelpText = Help },
        "radio" => new RadioFormField { LabelText = "New", HelpText = Help, GroupName = "g" },
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    // The field's interactive input parts (everything but the help glyph).
    private static Control[] Inputs(Control field) => field.GetVisualDescendants().OfType<Control>()
        .Where(c => c is TextBox or NumericUpDown or ComboBox or CheckBox or RadioButton or Button)
        .ToArray();

    private sealed record Hover(bool PointerOver, bool TooltipOpen, bool GlyphEnabled, bool AnyInputEnabled);

    private static Hover HoverHelp(string kind, bool fieldEnabled)
    {
        var field = Make(kind);
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
        Dispatcher.UIThread.RunJobs();

        var result = new Hover(glyph.IsPointerOver, ToolTip.GetIsOpen(glyph), glyph.IsEffectivelyEnabled,
            Inputs(field).Any(c => c.IsEffectivelyEnabled));
        window.Close();
        return result;
    }

    [AvaloniaTheory]
    [MemberData(nameof(Fields))]
    public void Help_is_hoverable_on_an_enabled_field(string kind)
    {
        var hover = HoverHelp(kind, fieldEnabled: true);

        Assert.True(hover.PointerOver);
        Assert.True(hover.TooltipOpen);
        Assert.True(hover.AnyInputEnabled);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Fields))]
    public void Help_is_hoverable_on_a_disabled_field_whose_input_is_locked(string kind)
    {
        var hover = HoverHelp(kind, fieldEnabled: false);

        Assert.True(hover.GlyphEnabled);
        Assert.True(hover.PointerOver);
        Assert.True(hover.TooltipOpen);
        Assert.False(hover.AnyInputEnabled);
    }
}
