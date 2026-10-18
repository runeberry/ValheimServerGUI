namespace ValheimServerGUI.App.Services;

/// <summary>
/// The per-user Windows <c>Run</c> registry key that <see cref="WindowsRunKeyStrategy"/> reads and writes, plus
/// the matching <c>StartupApproved\Run</c> value where Task Manager and Settings record that the user turned an
/// entry off. Injected so the strategy's logic can be tested on any OS without touching the real registry.
/// </summary>
public interface IRunKey
{
    /// <summary>The value's data, or null if the value does not exist.</summary>
    string? GetValue(string name);

    void SetValue(string name, string value);

    /// <summary>Deletes the value; does nothing if it does not exist.</summary>
    void DeleteValue(string name);

    /// <summary>
    /// True if Task Manager or Settings has marked the entry disabled. Windows then skips the Run value at
    /// login even though it is still present.
    /// </summary>
    bool IsDisabledByUser(string name);

    /// <summary>Deletes the entry's approval value (a missing value means enabled); does nothing if absent.</summary>
    void ClearUserDisable(string name);
}
