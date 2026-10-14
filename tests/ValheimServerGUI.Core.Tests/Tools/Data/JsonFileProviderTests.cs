using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using ValheimServerGUI.Tools.Data;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Tools.Data
{
    /// <summary>
    /// §15 #8: JSON persistence is atomic (temp file + rename, no stray temp left behind) and a
    /// corrupt file is backed up aside rather than silently overwritten (E33, E37).
    /// </summary>
    public class JsonFileProviderTests : IDisposable
    {
        private class TestData
        {
            public string? Value { get; set; }
            public int Number { get; set; }
        }

        private readonly string _dir;
        private readonly string _file;
        private readonly JsonFileProvider _provider;

        public JsonFileProviderTests()
        {
            _dir = Path.Join(Path.GetTempPath(), "vsg-json-" + Guid.NewGuid().ToString("n"));
            Directory.CreateDirectory(_dir);
            _file = Path.Join(_dir, "data.json");
            // A no-op Serilog logger (no sinks).
            _provider = new JsonFileProvider(new LoggerConfiguration().CreateLogger());
        }

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        [Fact]
        public async Task SaveThenLoad_RoundTrips()
        {
            await _provider.SaveAsync(_file, new TestData { Value = "hello", Number = 42 });

            var loaded = await _provider.LoadAsync<TestData>(_file);

            Assert.NotNull(loaded);
            Assert.Equal("hello", loaded!.Value);
            Assert.Equal(42, loaded.Number);
        }

        [Fact]
        public async Task Save_LeavesNoTempFileBehind()
        {
            await _provider.SaveAsync(_file, new TestData { Value = "x" });

            Assert.True(File.Exists(_file));
            Assert.False(File.Exists(_file + ".tmp"));
            Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
        }

        // E33: a corrupt file must be backed up aside, not silently clobbered, and load yields default.
        [Fact]
        public async Task Load_CorruptFile_BacksItUpAndReturnsNull()
        {
            File.WriteAllText(_file, "{ this is not valid json ]");

            var loaded = await _provider.LoadAsync<TestData>(_file);

            Assert.Null(loaded);
            var backups = Directory.GetFiles(_dir, "data.json.corrupt-*");
            Assert.Single(backups);
            Assert.Equal("{ this is not valid json ]", File.ReadAllText(backups.Single()));
        }

        [Fact]
        public async Task Load_MissingFile_ReturnsNull()
        {
            var loaded = await _provider.LoadAsync<TestData>(_file);
            Assert.Null(loaded);
        }
    }
}
