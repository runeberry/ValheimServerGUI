using System.Runtime.Versioning;
using Microsoft.Win32;

namespace ValheimServerGUI.App.Services;

/// <summary>
/// <see cref="IRunKey"/> backed by <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c>. The per-user
/// key is used because HKLM would need admin rights.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class RegistryRunKey : IRunKey
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public string? GetValue(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(name) as string;
    }

    public void SetValue(string name, string value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key.SetValue(name, value);
    }

    public void DeleteValue(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(name, throwOnMissingValue: false);
    }
}
