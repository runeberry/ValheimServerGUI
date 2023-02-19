namespace ValheimServerGUI.Game
{
    public interface IStartupArgsProvider
    {
        public string ServerProfileName { get; }
    }

    public class StartupArgsProvider : IStartupArgsProvider
    {
        public StartupArgsProvider(string[] args)
        {
            if (args == null || args.Length == 0) return;
            ServerProfileName = args[0];
        }

        #region IStartupArgsProvider implementation

        public string ServerProfileName { get; private set; }

        #endregion
    }
}
