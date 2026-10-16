using System;
using System.Collections.Generic;
using System.Linq;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game;

/// <summary>
/// The "Additional Environment Variables" syntax: whitespace-separated NAME=VALUE assignments with shell-like
/// quoting, no expansion, and backslashes kept outside quotes so Windows paths work unescaped.
/// </summary>
public class EnvironmentVariableParserTests
{
    private static List<(string Name, string Value)> Parse(string? text)
        => EnvironmentVariableParser.Parse(text).Select(kvp => (kvp.Key, kvp.Value)).ToList();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \t ")]
    public void Blank_input_has_no_variables(string? text)
    {
        Assert.Empty(Parse(text));
    }

    [Fact]
    public void Assignments_are_separated_by_any_whitespace_and_keep_their_order()
    {
        Assert.Equal(
            new[] { ("B", "2"), ("A", "1"), ("_C9", "3") },
            Parse("  B=2\tA=1 \n _C9=3 "));
    }

    [Theory]
    [InlineData(@"P=C:\games\valheim", @"C:\games\valheim")]
    [InlineData(@"P=""C:\Program Files (x86)\Steam""", @"C:\Program Files (x86)\Steam")] // other backslashes kept in double quotes
    [InlineData(@"P='/opt/my dir'", "/opt/my dir")]
    [InlineData(@"P='it\""s'", @"it\""s")] // single quotes: fully literal
    [InlineData(@"P=""say \""hi\""""", @"say ""hi""")] // \" in double quotes
    [InlineData(@"P=""a\\b""", @"a\b")] // \\ in double quotes
    [InlineData(@"P=pre""fix x""'y z'", "prefix xy z")] // adjacent parts join
    [InlineData("P=$HOME/x", "$HOME/x")] // no expansion
    [InlineData("P=a=b", "a=b")] // only the first = splits
    [InlineData("P=", "")]
    [InlineData(@"P=""""", "")]
    public void Values_follow_the_quoting_rules(string text, string expected)
    {
        Assert.Equal(expected, Assert.Single(Parse(text)).Value);
    }

    [Fact]
    public void A_repeated_name_keeps_its_last_value_in_its_first_position()
    {
        Assert.Equal(new[] { ("A", "3"), ("B", "2") }, Parse("A=1 B=2 A=3"));
    }

    [Theory]
    [InlineData("FOO", "FOO")]
    [InlineData("A=1 FOO BAR=2", "FOO")]
    [InlineData(@"P=C:\Program Files", "Files")] // an unquoted value stops at whitespace
    public void A_token_without_equals_is_rejected_by_name(string text, string token)
    {
        var ex = Assert.Throws<ArgumentException>(() => Parse(text));
        Assert.Equal(string.Format(Strings.Validation_EnvVarMissingEquals, token), ex.Message);
    }

    [Theory]
    [InlineData("1A=x", "1A")]
    [InlineData("MY-VAR=x", "MY-VAR")]
    [InlineData(@"""A""=x", @"""A""")]
    [InlineData("=value", "=value")]
    public void An_invalid_name_is_rejected_by_name(string text, string name)
    {
        var ex = Assert.Throws<ArgumentException>(() => Parse(text));
        Assert.Equal(string.Format(Strings.Validation_EnvVarInvalidName, name), ex.Message);
    }

    [Theory]
    [InlineData(@"A=1 B=""open", @"B=""open")]
    [InlineData("A='open B=2", "A='open B=2")]
    [InlineData(@"A=""ends in escape\""", @"A=""ends in escape\""")]
    public void An_unterminated_quote_is_rejected(string text, string shown)
    {
        var ex = Assert.Throws<ArgumentException>(() => Parse(text));
        Assert.Equal(string.Format(Strings.Validation_EnvVarUnterminatedQuote, shown), ex.Message);
    }
}
