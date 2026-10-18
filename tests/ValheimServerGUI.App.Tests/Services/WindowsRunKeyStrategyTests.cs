using System;
using System.Collections.Generic;
using ValheimServerGUI.App.Services;
using Xunit;

namespace ValheimServerGUI.App.Tests.Services;

// Windows autostart via a Run key value: enable, disable, stale-path repair, idempotence, spaced paths, and a
// disable made in Task Manager / Settings (the StartupApproved value).
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

    [Fact]
    public void Is_enabled_only_when_the_value_points_at_this_executable()
    {
        Assert.False(Make().IsEnabled());

        Make(exec: @"C:\other\ValheimServerGUI.exe").Apply(true);
        Assert.False(Make().IsEnabled());

        Make().Apply(true);
        Assert.True(Make().IsEnabled());
        Assert.True(Make(exec: @"c:\VSG\valheimservergui.exe").IsEnabled()); // Windows paths are case-insensitive
    }

    [Fact]
    public void A_task_manager_disable_reads_as_not_enabled()
    {
        var strat = Make();
        strat.Apply(true);

        _runKey.Approved[ValueName] = TaskManagerDisabled;

        Assert.False(strat.IsEnabled());
        Assert.True(_runKey.Values.ContainsKey(ValueName)); // the Run value alone does not make it enabled
    }

    [Fact]
    public void An_enabled_approval_value_still_reads_as_enabled()
    {
        var strat = Make();
        strat.Apply(true);

        _runKey.Approved[ValueName] = TaskManagerEnabled;

        Assert.True(strat.IsEnabled());
    }

    [Fact]
    public void Enable_clears_a_task_manager_disable()
    {
        var strat = Make();
        strat.Apply(true);
        _runKey.Approved[ValueName] = TaskManagerDisabled;

        Assert.True(strat.Apply(true));

        Assert.True(strat.IsEnabled());
        Assert.False(_runKey.Approved.ContainsKey(ValueName));
    }

    [Fact]
    public void Disable_clears_the_value_and_its_approval()
    {
        var strat = Make();
        strat.Apply(true);
        _runKey.Approved[ValueName] = TaskManagerDisabled;

        Assert.True(strat.Apply(false));

        Assert.False(_runKey.Values.ContainsKey(ValueName));
        Assert.False(_runKey.Approved.ContainsKey(ValueName));
    }

    [Theory]
    [InlineData(new byte[] { 0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, false)]
    [InlineData(new byte[] { 0x06, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, false)]
    [InlineData(new byte[] { 0x03, 0x1f, 0x2e, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, true)]
    [InlineData(new byte[] { 0x07, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, true)]
    [InlineData(new byte[0], false)]
    public void Approval_bytes_decode_by_the_low_bit_of_the_first_byte(byte[] data, bool disabled)
    {
        Assert.Equal(disabled, StartupApproval.IsDisabled(data));
    }

    private static readonly byte[] TaskManagerDisabled = { 0x03, 0x8a, 0x41, 0x2c, 0x5e, 0x3f, 0xdb, 0x01, 0, 0, 0, 0 };
    private static readonly byte[] TaskManagerEnabled = { 0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    private sealed class FakeRunKey : IRunKey
    {
        // Registry value names are case-insensitive.
        public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

        public string? GetValue(string name) => Values.TryGetValue(name, out var value) ? value : null;

        public void SetValue(string name, string value) => Values[name] = value;

        public void DeleteValue(string name) => Values.Remove(name);

        public Dictionary<string, byte[]> Approved { get; } = new(StringComparer.OrdinalIgnoreCase);

        public bool IsDisabledByUser(string name)
            => Approved.TryGetValue(name, out var data) && StartupApproval.IsDisabled(data);

        public void ClearUserDisable(string name) => Approved.Remove(name);
    }
}
