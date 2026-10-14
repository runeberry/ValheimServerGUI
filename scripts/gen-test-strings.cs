// Regenerates src/ValheimServerGUI.Localization/Strings.qps-ploc.resx (the key-echo TEST culture)
// from the neutral English Strings.resx. Run via scripts/gen-test-strings.sh.
//
// Every value becomes "⟦Key⟧", or "⟦Key {0} {1}⟧" when the English value has format placeholders,
// so formatted strings still accept the same arguments. Tests pin this culture, and a manual
// VSG_LANG=qps-ploc run shows keys everywhere: any plain English left on screen is unextracted.
// Group comments are carried over; per-entry <comment> elements are dropped.

using System.Text.RegularExpressions;
using System.Xml.Linq;

var dir = args.Length > 0 ? args[0] : "src/ValheimServerGUI.Localization";
var neutralPath = Path.Combine(dir, "Strings.resx");
var testPath = Path.Combine(dir, "Strings.qps-ploc.resx");

var doc = XDocument.Load(neutralPath, LoadOptions.PreserveWhitespace);
var placeholder = new Regex(@"\{\d+(,[^}:]*)?(:[^}]*)?\}");
var count = 0;

foreach (var data in doc.Root!.Elements("data"))
{
    var key = data.Attribute("name")!.Value;
    var value = data.Element("value")?.Value ?? "";
    var holders = placeholder.Matches(value).Select(m => m.Value).Distinct();
    var echo = string.Join(' ', new[] { key }.Concat(holders));

    data.Element("value")!.Value = $"⟦{echo}⟧";
    data.Element("comment")?.Remove();
    count++;
}

// Written as UTF-8 without a BOM, to match the hand-maintained neutral file.
File.WriteAllText(testPath, $"{doc.Declaration}\n{doc.Root.ToString(SaveOptions.DisableFormatting)}\n");
Console.WriteLine($"Wrote {count} keys to {testPath}");
