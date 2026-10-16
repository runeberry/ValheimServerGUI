using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ValheimServerGUI.Game
{
    /// <summary>
    /// Feeds BepInEx's disk log (<c>BepInEx/LogOutput.log</c>) into the server log on Windows. There, BepInEx's
    /// console logger writes to the console device, which VSG's windowless launch never shows, so plugin loading and
    /// plugin output reach VSG only through this file. (On Linux the same lines arrive on stdout.)
    /// <list type="bullet">
    /// <item>A file not written since <c>notBeforeUtc</c> is a previous boot's and is ignored until it is rewritten.</item>
    /// <item>Lines up to "Preloader finished" are held back: BepInEx replays them to stdout once the chainloader
    /// starts. If the process ends before that, nothing was replayed, so <see cref="Complete"/> emits them.</item>
    /// <item>Lines from the <c>Unity Log</c> source (BepInEx's optional <c>WriteUnityLog</c>) are copies of stdout and
    /// are dropped.</item>
    /// </list>
    /// </summary>
    public sealed class BepInExLogTail : IDisposable
    {
        private static readonly Regex UnityLogSource = new(@"^\[\w+\s*:\s*Unity Log\]");
        private const string PreloaderFinished = "] Preloader finished";
        private const char ByteOrderMark = (char)0xFEFF;

        private readonly string _path;
        private readonly DateTime _notBeforeUtc;
        private readonly Action<string> _onLine;
        private readonly object _lock = new();
        private readonly List<string> _preloaderLines = new();
        private long _position;
        private bool _preloaderFinished;
        private Timer? _timer;

        public BepInExLogTail(string path, DateTime notBeforeUtc, Action<string> onLine)
        {
            _path = path;
            _notBeforeUtc = notBeforeUtc;
            _onLine = onLine;
        }

        /// <summary>Polls the file on a background timer until <see cref="Complete"/> or <see cref="Dispose"/>.</summary>
        public void Start(TimeSpan interval)
        {
            _timer = new Timer(_ => Poll(), null, interval, interval);
        }

        /// <summary>Emits every complete line written since the last poll.</summary>
        public void Poll()
        {
            lock (_lock)
            {
                try
                {
                    ReadNewLines();
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    // Being created, replaced or locked right now; the next poll retries.
                }
            }
        }

        /// <summary>The process has exited: stops polling, reads what is left, and releases held-back lines.</summary>
        public void Complete()
        {
            _timer?.Dispose();
            Poll();
            lock (_lock)
            {
                if (!_preloaderFinished) foreach (var line in _preloaderLines) _onLine(line);
                _preloaderLines.Clear();
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private void ReadNewLines()
        {
            if (!File.Exists(_path) || File.GetLastWriteTimeUtc(_path) < _notBeforeUtc) return;

            using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (stream.Length < _position)
            {
                // Recreated: a new boot of BepInEx.
                _position = 0;
                _preloaderFinished = false;
                _preloaderLines.Clear();
            }
            if (stream.Length == _position) return;

            var atStart = _position == 0;
            stream.Seek(_position, SeekOrigin.Begin);
            var bytes = new byte[stream.Length - _position];
            stream.ReadExactly(bytes);

            // Only complete lines: a partial last line stays in the file for the next poll.
            var end = Array.LastIndexOf(bytes, (byte)'\n');
            if (end < 0) return;
            _position += end + 1;

            var text = Encoding.UTF8.GetString(bytes, 0, end);
            if (atStart) text = text.TrimStart(ByteOrderMark);
            foreach (var raw in text.Split('\n')) Emit(raw.TrimEnd('\r'));
        }

        private void Emit(string line)
        {
            if (UnityLogSource.IsMatch(line)) return;

            if (!_preloaderFinished)
            {
                _preloaderLines.Add(line);
                if (line.EndsWith(PreloaderFinished, StringComparison.Ordinal))
                {
                    _preloaderFinished = true;
                    _preloaderLines.Clear();
                }
                return;
            }

            _onLine(line);
        }
    }
}
