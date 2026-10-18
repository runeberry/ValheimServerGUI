using System;
using System.Collections.Generic;
using ValheimServerGUI.App.Services;
using Xunit;

namespace ValheimServerGUI.App.Tests.Services;

// Windows autostart via a Run key value: enable, disable, stale-path repair, idempotence, spaced paths.
public sealed class WindowsRunKeyStrategyTests
{
    private const string ValueName = "ValheimServerGUI";

    private readonly FakeRunKey _runKey = new();

    private WindowsRunKeyStrategy Make(string exec = @"C:\vsg\ValheimServerGUI.exe") => new(_runKey, ValueName, exec);

    [Fact]
    public void Enable_writes_quoted_command()
    {
        var changed = Make().Apply(runOnStartup: true);

        Assert.True(changed);
        Assert.Equal("\"C:\\vsg\\ValheimServerGUI.exe\"", _runKey.Values[ValueName]);
    }

    [Fact]
    public void Enable_quotes_a_path_with_spaces()
    {
        Make(exec: @"C:\Program Files\ValheimServerGUI\ValheimServerGUI.exe").Apply(true);

        Assert.Equal("\"C:\\Program Files\\ValheimServerGUI\\ValheimServerGUI.exe\"", _runKey.Values[ValueName]);
    }

    [Fact]
    public void Enable_is_idempotent()
    {
        var strat = Make();
        Assert.True(strat.Apply(true));
        Assert.False(strat.Apply(true)); // already correct → no change
    }

    [Fact]
    public void Enable_repairs_stale_exec_path()
    {
        Make(exec: @"C:\old\ValheimServerGUI.exe").Apply(true);
        var changed = Make(exec: @"C:\new\ValheimServerGUI.exe").Apply(true);

        Assert.True(changed);
        Assert.Equal("\"C:\\new\\ValheimServerGUI.exe\"", _runKey.Values[ValueName]);
    }

    [Fact]
    public void Disable_removes_value()
    {
        var strat = Make();
        strat.Apply(true);
        Assert.True(strat.Apply(false));
        Assert.False(_runKey.Values.ContainsKey(ValueName));
    }

    [Fact]
    public void Disable_when_absent_is_noop()
    {
        Assert.False(Make().Apply(false));
    }

    [Fact]
    public void Leaves_other_programs_values_alone()
    {
        _runKey.Values["OtherApp"] = "\"C:\\other.exe\"";
        var strat = Make();

        strat.Apply(true);
        strat.Apply(false);

        Assert.Equal("\"C:\\other.exe\"", _runKey.Values["OtherApp"]);
    }

    private sealed class FakeRunKey : IRunKey
    {
        // Registry value names are case-insensitive.
        public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

        public string? GetValue(string name) => Values.TryGetValue(name, out var value) ? value : null;

        public void SetValue(string name, string value) => Values[name] = value;

        public void DeleteValue(string name) => Values.Remove(name);
    }
}
