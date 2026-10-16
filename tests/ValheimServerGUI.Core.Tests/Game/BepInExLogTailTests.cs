using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game;

/// <summary>
/// BepInEx's disk log fed into the server log (Windows): only this boot's file, only complete lines, the preloader
/// section held back (BepInEx replays it to stdout) unless the process ends first, and Unity Log copies dropped.
/// Driven by <see cref="BepInExLogTail.Poll"/> on a real temp file, no timer.
/// </summary>
public sealed class BepInExLogTailTests : IDisposable
{
    private const string Preloader = "[Message:   BepInEx] Preloader started";
    private const string PreloaderFinished = "[Message:   BepInEx] Preloader finished";
    private const string Loading = "[Info   :   BepInEx] Loading [Server Devcommands 1.115] (server_devcommands)";
    private const string Complete = "[Message:   BepInEx] Chainloader startup complete (1 loaded, 0 skipped, 0 failed)";

    private readonly DirectoryInfo _dir = Directory.CreateTempSubdirectory("vsg-bepinex-log-");
    private readonly string _path;
    private readonly List<string> _lines = new();

    public BepInExLogTailTests()
    {
        _path = Path.Join(_dir.FullName, "LogOutput.log");
    }

    public void Dispose() => _dir.Delete(recursive: true);

    private BepInExLogTail Tail(DateTime? notBeforeUtc = null)
        => new(_path, notBeforeUtc ?? DateTime.UtcNow.AddMinutes(-1), _lines.Add);

    private void Append(string text) => File.AppendAllText(_path, text);

    [Fact]
    public void A_missing_file_emits_nothing()
    {
        var tail = Tail();

        tail.Poll();
        tail.Complete();

        Assert.Empty(_lines);
    }

    [Fact]
    public void Lines_after_the_preloader_are_emitted_once_each_as_they_are_written()
    {
        var tail = Tail();
        // BepInEx writes its file with a byte-order mark.
        File.WriteAllText(_path, $"{Preloader}\r\n{PreloaderFinished}\r\n{Loading}\r\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        tail.Poll();
        Assert.Equal(new[] { Loading }, _lines);

        Append($"{Complete}\r\n");
        tail.Poll();
        tail.Poll();
        Assert.Equal(new[] { Loading, Complete }, _lines);
    }

    [Fact]
    public void A_partial_line_waits_for_its_end()
    {
        var tail = Tail();
        Append($"{PreloaderFinished}\n[Info   :   BepInEx] Load");

        tail.Poll();
        Assert.Empty(_lines);

        Append("ing [Server Devcommands 1.115] (server_devcommands)\n");
        tail.Poll();
        Assert.Equal(new[] { Loading }, _lines);
    }

    // BepInEx replays the preloader's lines to stdout when the chainloader starts. If the process ends before
    // that, they were never replayed, so they are released at the end instead of being lost.
    [Fact]
    public void Preloader_lines_are_released_only_if_the_preloader_never_finished()
    {
        var finished = Tail();
        Append($"{Preloader}\n{PreloaderFinished}\n");
        finished.Poll();
        finished.Complete();
        Assert.Empty(_lines);

        File.Delete(_path);
        var crashed = Tail();
        Append($"{Preloader}\n[Fatal  :   BepInEx] Could not run preloader\n");
        crashed.Poll();
        Assert.Empty(_lines);
        crashed.Complete();
        Assert.Equal(new[] { Preloader, "[Fatal  :   BepInEx] Could not run preloader" }, _lines);
    }

    [Fact]
    public void Unity_log_copies_are_dropped()
    {
        var tail = Tail();
        Append($"{PreloaderFinished}\n[Info   : Unity Log] Game server connected\n{Loading}\n");

        tail.Poll();

        Assert.Equal(new[] { Loading }, _lines);
    }

    [Fact]
    public void A_previous_boots_file_is_ignored_until_it_is_rewritten()
    {
        Append($"{PreloaderFinished}\n[Info   :   BepInEx] from the last boot\n");
        File.SetLastWriteTimeUtc(_path, DateTime.UtcNow.AddMinutes(-10));
        var tail = Tail(notBeforeUtc: DateTime.UtcNow.AddMinutes(-5));

        tail.Poll();
        Assert.Empty(_lines);

        File.WriteAllText(_path, $"{PreloaderFinished}\n{Loading}\n");
        tail.Poll();
        Assert.Equal(new[] { Loading }, _lines);
    }

    [Fact]
    public void A_recreated_file_is_read_from_its_start()
    {
        var tail = Tail();
        Append($"{PreloaderFinished}\n{Loading}\n{Complete}\n");
        tail.Poll();

        File.WriteAllText(_path, $"{PreloaderFinished}\n{Loading}\n");
        tail.Poll();

        Assert.Equal(new[] { Loading, Complete, Loading }, _lines);
    }
}
