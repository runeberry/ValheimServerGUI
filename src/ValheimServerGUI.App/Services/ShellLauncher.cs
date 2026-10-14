using System;
using System.IO;
using Serilog;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.Services;

/// <summary>
/// Opens folders and web addresses in the OS shell (§12). Windows shell-executes; Linux uses
/// <c>xdg-open</c> with a <c>gio open</c> fallback. Inputs are validated first — a URL must be
/// http/https and a directory/file path must actually exist — so a malformed value is logged and dropped
/// rather than handed to the shell (E54). The OS boundary is injected (<see cref="ISystemShell"/>) so both
/// per-OS branches are assertable in tests.
/// </summary>
internal sealed class ShellLauncher : IShellLauncher
{
    private readonly ISystemShell _shell;
    private readonly ILogger _logger;
    private readonly bool _isWindows;

    public ShellLauncher(ISystemShell shell, ILogger logger)
        : this(shell, logger, OperatingSystem.IsWindows())
    {
    }

    internal ShellLauncher(ISystemShell shell, ILogger logger, bool isWindows)
    {
        _shell = shell;
        _logger = logger;
        _isWindows = isWindows;
    }

    public void OpenDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.Warning("OpenDirectory called with an empty path");
            return;
        }

        // Accept a file too: open its containing directory (parity with the v2.4 open-folder buttons).
        string? target = _shell.DirectoryExists(path)
            ? path
            : _shell.FileExists(path)
                ? Path.GetDirectoryName(path)
                : null;

        if (string.IsNullOrEmpty(target))
        {
            _logger.Warning("OpenDirectory: path does not exist: {Path}", path);
            return;
        }

        Open(target);
    }

    public void OpenWebAddress(string url)
    {
        if (!IsValidWebAddress(url))
        {
            _logger.Warning("OpenWebAddress: not an http/https URL: {Url}", url);
            return;
        }

        Open(url);
    }

    private static bool IsValidWebAddress(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private void Open(string target)
    {
        try
        {
            if (_isWindows)
            {
                _shell.ShellOpen(target);
                return;
            }

            try
            {
                _shell.Start("xdg-open", target);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "xdg-open failed for {Target}; falling back to gio open", target);
                _shell.Start("gio", "open", target);
            }
        }
        catch (Exception ex)
        {
            // Don't crash the UI because the shell handler is missing/misconfigured.
            _logger.Error(ex, "Failed to open {Target} in the OS shell", target);
        }
    }
}
