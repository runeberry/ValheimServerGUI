namespace ValheimServerGUI.Game
{
    public interface IStartupArgsProvider
    {
        public string ServerName { get; }
    }

    public class StartupArgsProvider : IStartupArgsProvider
    {
        public StartupArgsProvider(string[] args)
        {
            if (args == null || args.Length == 0) return;
            this.ServerName = args[0];
        }

        #region IStartupArgsProvider implementation

        public string ServerName { get; private set; }

        #endregion
    }
}
