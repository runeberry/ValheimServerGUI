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

        public void Validate()
        {
            // Ensure all required fields exist
            if (string.IsNullOrWhiteSpace(this.Name)) throw new ArgumentException($"{nameof(Name)} must be defined.");
            if (string.IsNullOrWhiteSpace(this.Password)) throw new ArgumentException($"{nameof(Password)} must be defined.");
            if (string.IsNullOrWhiteSpace(this.WorldName)) throw new ArgumentException($"{nameof(WorldName)} must be defined.");

            // WorldName validation
            // todo: Validate world exists? Or do we trust it from the UI control?
            if (this.NewWorld)
            {
                if (this.WorldName.Length < 5 || this.WorldName.Length > 20) throw new ArgumentException($"{nameof(WorldName)} must be 5-20 characters long.");
            }

            // Name validation
            if (this.Name == this.WorldName) throw new ArgumentException($"{nameof(Name)} cannot be the same as your {nameof(WorldName)} ({this.WorldName}).");

            // Password validation
            if (this.Password.Length < 5) throw new ArgumentException($"{nameof(Password)} must be at least 5 characters.");
            if (this.Password.Contains(this.Name)) throw new ArgumentException($"{nameof(Password)} cannot contain your {nameof(Name)} ({this.Name}).");
            if (this.Password.Contains(this.WorldName)) throw new ArgumentException($"{nameof(Password)} cannot contain your {nameof(WorldName)} ({this.WorldName}).");

            // Port validation
            if (this.Port < 1 || this.Port > 65535) throw new ArgumentException($"{nameof(Port)} must be between 1 - 65535");

            // Save and Backup validation
            if (this.SaveInterval < 1) throw new ArgumentException($"{nameof(SaveInterval)} must be greater than 0.");
            if (this.BackupShort < 1) throw new ArgumentException($"{nameof(BackupShort)} must be greater than 0.");
            if (this.BackupLong < 1) throw new ArgumentException($"{nameof(BackupLong)} must be greater than 0.");
            if (this.SaveInterval > this.BackupShort || this.SaveInterval > this.BackupLong) throw new ArgumentException($"{nameof(SaveInterval)} must be less than or equal to the backup intervals.");
            if (this.BackupShort > this.BackupLong) throw new ArgumentException($"{nameof(BackupShort)} must be less than or equal to {nameof(BackupLong)}.");
        }
    }

    public interface IValheimServerOptions
    {
        string Name { get; }

        string Password { get; }

        string WorldName { get; }

        bool Public { get; }

        int Port { get; set; }

        bool Crossplay { get; }

        int SaveInterval { get; }

        int Backups { get; }

        int BackupShort { get; }

        int BackupLong { get; }
    }
}
