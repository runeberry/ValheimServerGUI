using System.Collections.Generic;
using System.IO;
using System.Linq;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.Tests.Fakes;

/// <summary>In-memory Steam Cloud provider: lists preset cloud worlds and records import calls.</summary>
internal sealed class FakeSteamCloudWorldProvider : ISteamCloudWorldProvider
{
    private readonly List<string> _cloudWorlds;

    public FakeSteamCloudWorldProvider(params string[] cloudWorlds) => _cloudWorlds = cloudWorlds.ToList();

    public List<(string World, bool Move)> Imports { get; } = new();

    public IEnumerable<string> GetCloudWorldNames() => _cloudWorlds.ToList();

    public DirectoryInfo? GetCloudWorldFolder(string worldName) => null;

    public DirectoryInfo ImportCloudWorld(string worldName, DirectoryInfo destSaveFolder, bool move)
    {
        Imports.Add((worldName, move));

        // Simulate the import landing the world locally (legacy ".fwl" format) so it lists as a real world.
        var worlds = Directory.CreateDirectory(Path.Combine(destSaveFolder.FullName, "worlds"));
        File.WriteAllText(Path.Combine(worlds.FullName, worldName + ".fwl"), string.Empty);
        return destSaveFolder;
    }
}
