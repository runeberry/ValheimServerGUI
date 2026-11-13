using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// A resource key no theme supplies fails silently: a brush resolves to nothing and the part it paints disappears. Every
// key the app reads, in XAML or from code, must resolve in both theme variants, unless the file that reads it defines it.
public class ResourceKeyTests
{
    private static readonly Regex XamlReference = new(@"\{(?:Dynamic|Static)Resource ([A-Za-z0-9_.]+)\}");
    private static readonly Regex CodeReference = new(@"GetResourceObservable\(""([A-Za-z0-9_.]+)""\)");
    private static readonly Regex LocalKey = new(@"x:Key=""([A-Za-z0-9_.]+)""");

    private static IEnumerable<(string File, string Key)> References()
    {
        var app = AppSource();
        foreach (var file in Directory.GetFiles(app, "*.axaml", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var local = LocalKey.Matches(text).Select(m => m.Groups[1].Value).ToHashSet();
            foreach (Match m in XamlReference.Matches(text))
                if (!local.Contains(m.Groups[1].Value))
                    yield return (Path.GetRelativePath(app, file), m.Groups[1].Value);
        }
        foreach (var file in Directory.GetFiles(app, "*.cs", SearchOption.AllDirectories))
            foreach (Match m in CodeReference.Matches(File.ReadAllText(file)))
                yield return (Path.GetRelativePath(app, file), m.Groups[1].Value);
    }

    // Anchored on this source file: build output lives out of the tree.
    private static string AppSource([CallerFilePath] string thisFile = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "src", "ValheimServerGUI.App"));

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Every_resource_the_app_reads_resolves(string variantName)
    {
        var variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        var references = References().ToList();
        Assert.NotEmpty(references);

        var missing = references
            .Where(r => !Application.Current!.TryFindResource(r.Key, variant, out _))
            .Select(r => $"{r.File}: {r.Key}")
            .Distinct()
            .ToList();

        Assert.True(missing.Count == 0, "Unresolved resource keys:\n" + string.Join("\n", missing));
    }
}
