using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ValheimServerGUI.Localization;
using Xunit;

namespace ValheimServerGUI.App.Tests;

/// <summary>
/// Guards the localization resources: the English set and the key-echo test culture (qps-ploc) stay in
/// step, the test culture never resolves English, and the satellite + fallback chain works.
/// </summary>
public class LocalizationTests
{
    private const string TestCulture = "qps-ploc";

    private static readonly Regex Placeholder = new(@"\{\d+(,[^}:]*)?(:[^}]*)?\}");

    [Fact]
    public void EnglishAndTestCulture_HaveIdenticalKeySets()
    {
        var english = ResxValues("Strings.resx").Keys.ToHashSet();
        var test = ResxValues($"Strings.{TestCulture}.resx").Keys.ToHashSet();

        Assert.NotEmpty(english); // guard: two empty sets would satisfy the comparisons below
        Assert.Empty(english.Except(test)); // run scripts/gen-test-strings.sh
        Assert.Empty(test.Except(english));
    }

    [Fact]
    public void TestCultureValues_EchoTheKeyAndKeepEveryPlaceholder()
    {
        var english = ResxValues("Strings.resx");
        var test = ResxValues($"Strings.{TestCulture}.resx");

        Assert.NotEmpty(test);
        foreach (var (key, value) in test)
        {
            Assert.StartsWith($"⟦{key}", value);
            Assert.EndsWith("⟧", value);
            Assert.Equal(Placeholders(english[key]), Placeholders(value));
        }
    }

    [Fact]
    public void TestCulture_ResolvesFromItsSatellite()
    {
        WithUiCulture(TestCulture, () => Assert.Equal("⟦Common_OK⟧", Strings.Common_OK));
    }

    [Fact]
    public void CultureWithoutSatellite_FallsBackToEnglish()
    {
        var english = Strings.ResourceManager.GetString(nameof(Strings.Common_OK), CultureInfo.InvariantCulture);

        WithUiCulture("fr", () =>
        {
            Assert.NotEqual("⟦Common_OK⟧", Strings.Common_OK);
            Assert.Equal(english, Strings.Common_OK);
        });
    }

    // XAML attributes that carry user-facing text.
    private static readonly HashSet<string> TextAttributes = new()
    {
        "Header", "LabelText", "HelpText", "ToolTip.Tip", "Content", "Text", "Title", "Watermark", "PlaceholderText",
        "EmptyText",
    };

    // Literal values that are not copy: the help glyph and brand names.
    private static readonly HashSet<string> AllowedLiterals = new() { "?", "GitHub", "Discord", "Valheim Server GUI" };

    private static readonly Regex LiteralStringFormat = new(@"StringFormat\s*=\s*'?[^{'\s]");

    [Fact]
    public void Xaml_has_no_literal_copy()
    {
        var files = Directory.GetFiles(Path.Combine(RepoRoot(), "src"), "*.axaml", SearchOption.AllDirectories);
        Assert.NotEmpty(files);

        var literals = new List<string>();
        foreach (var file in files)
        {
            foreach (var attribute in XDocument.Load(file).Descendants().SelectMany(e => e.Attributes()))
            {
                var value = attribute.Value;
                var isLiteralText = TextAttributes.Contains(attribute.Name.LocalName)
                    && value.Length > 0 && !value.StartsWith('{') && !AllowedLiterals.Contains(value);
                if (isLiteralText || LiteralStringFormat.IsMatch(value))
                    literals.Add($"{Path.GetFileName(file)}: {attribute.Name.LocalName}=\"{value}\"");
            }
        }

        Assert.Empty(literals); // move the text to Strings.resx and bind {x:Static loc:Strings.Key}
    }

    private static void WithUiCulture(string name, Action body)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
            body();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    private static HashSet<string> Placeholders(string value)
        => Placeholder.Matches(value).Select(m => m.Value).ToHashSet();

    private static Dictionary<string, string> ResxValues(string fileName)
    {
        var path = Path.Combine(RepoRoot(), "src", "ValheimServerGUI.Localization", fileName);
        Assert.True(File.Exists(path), $"resx not found at {path}");

        return XDocument.Load(path).Root!
            .Elements("data")
            .ToDictionary(e => e.Attribute("name")!.Value, e => e.Element("value")?.Value ?? "");
    }

    // Anchor on this source file, not AppContext.BaseDirectory: build output lives out-of-tree (see
    // Directory.Build.props), so walking up from the test assembly never reaches src/.
    internal static string RepoRoot([CallerFilePath] string thisFile = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", ".."));
}
