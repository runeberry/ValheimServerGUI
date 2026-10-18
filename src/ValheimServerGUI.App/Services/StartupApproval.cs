namespace ValheimServerGUI.App.Services;

/// <summary>
/// Decodes a Windows <c>StartupApproved</c> value, the 12-byte REG_BINARY Task Manager and Settings write when
/// the user toggles a startup entry. The low bit of the first byte is set when disabled (02 = on, 03 = off).
/// </summary>
public static class StartupApproval
{
    public static bool IsDisabled(byte[] data) => data.Length > 0 && (data[0] & 1) == 1;
}
