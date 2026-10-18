namespace ValheimServerGUI.App.Services;

/// <summary>
/// The per-user Windows <c>Run</c> registry key that <see cref="WindowsRunKeyStrategy"/> reads and writes.
/// Injected so the strategy's logic can be tested on any OS without touching the real registry.
/// </summary>
public interface IRunKey
{
    /// <summary>The value's data, or null if the value does not exist.</summary>
    string? GetValue(string name);

    void SetValue(string name, string value);

    /// <summary>Deletes the value; does nothing if it does not exist.</summary>
    void DeleteValue(string name);
}
