using System;

namespace ValheimServerGUI.Game
{
    public class ValheimServerOptions : IValheimServerOptions
    {
        public string Name { get; set; }

        public string Password { get; set; }

        public string WorldName { get; set; }

        public bool NewWorld { get; set; }

        public bool Public { get; set; }

        public int Port { get; set; }

        public bool Crossplay { get; set; }

        public int SaveInterval { get; set; }

        public int Backups { get; set; }

        public int BackupShort { get; set; }

        public int BackupLong { get; set; }

        public string AdditionalArgs { get; set; }

        public void Validate()
        {
            // Ensure all required fields exist
            if (string.IsNullOrWhiteSpace(Name)) throw new ArgumentException($"Server name must be defined.");
            if (string.IsNullOrWhiteSpace(WorldName)) throw new ArgumentException($"World name must be defined.");

            // WorldName validation
            // todo: Validate world name is available? Or do we trust it from the UI control?
            if (NewWorld)
            {
                if (WorldName.Length < 5 || WorldName.Length > 20) throw new ArgumentException($"World name must be 5-20 characters long.");
            }

            // Name validation
            if (Name == WorldName) throw new ArgumentException($"The server name cannot be the same as the world name ({WorldName}).");

            // Password validation
            if (string.IsNullOrWhiteSpace(Password))
            {
                if (Public) throw new ArgumentException($"A password is required for all public games. Either set a password or disable the Community Server option.");
            }
            else
            {
                if (Password.Length < 5) throw new ArgumentException($"Password must be at least 5 characters.");
                if (Password.Contains(Name)) throw new ArgumentException($"Password cannot contain your server name ({Name}).");
                if (Password.Contains(WorldName)) throw new ArgumentException($"Password cannot contain your world name ({WorldName}).");
            }

            // Port validation
            if (Port < 1 || Port > 65535) throw new ArgumentException($"Port must be between 1 - 65535");

            // Save and Backup validation
            if (SaveInterval < 1) throw new ArgumentException($"Save interval must be greater than 0.");
            if (BackupShort < 1) throw new ArgumentException($"Short backup interval must be greater than 0.");
            if (BackupLong < 1) throw new ArgumentException($"Long backup interval must be greater than 0.");
            if (SaveInterval > BackupShort || SaveInterval > BackupLong) throw new ArgumentException($"Save interval must be less than or equal to the backup intervals.");
            if (BackupShort > BackupLong) throw new ArgumentException($"Short backup interval must be less than or equal to the long backup interval.");
        }
    }

    public interface IValheimServerOptions
    {
        string Name { get; }

        string Password { get; }

        string WorldName { get; }

        bool Public { get; }

        int Port { get; }

        bool Crossplay { get; }

        int SaveInterval { get; }

        int Backups { get; }

        int BackupShort { get; }

        int BackupLong { get; }

        string AdditionalArgs { get; }
    }
}
