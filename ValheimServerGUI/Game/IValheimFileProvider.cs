using System.Collections.Generic;
using System.IO;

namespace ValheimServerGUI.Game
{
    public interface IValheimFileProvider
    {
        FileInfo GameExe { get; }

        FileInfo ServerExe { get; }

        DirectoryInfo WorldsFolder { get; }
    }
}
