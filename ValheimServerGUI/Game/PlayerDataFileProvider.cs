using Microsoft.Extensions.Logging;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools.Data;

namespace ValheimServerGUI.Game
{
    public interface IPlayerDataFileProvider : IDataFileProvider
    {
    }

    public class PlayerDataFileProvider : JsonDataFileProvider, IPlayerDataFileProvider
    {
        public PlayerDataFileProvider(ILogger logger) : base(Resources.PlayerListFilePath, logger)
        {
        }
    }
}
