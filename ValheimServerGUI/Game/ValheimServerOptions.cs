using System;
using System.IO;

namespace ValheimServerGUI.Game
{
    public class ValheimServerOptions : IValheimServerOptions
    {
        public string Name { get; set; }

        public string ExePath { get; set; }

        public string Password { get; set; }

        public string WorldName { get; set; }

        public bool Public { get; set; }

        public ushort Port { get; set; }

        public void Validate()
        {
            // Ensure all required fields exist
            if (string.IsNullOrWhiteSpace(this.Name)) throw new ArgumentException($"{nameof(Name)} must be defined.");
            if (string.IsNullOrWhiteSpace(this.ExePath)) throw new ArgumentException($"{nameof(ExePath)} must be defined.");
            if (string.IsNullOrWhiteSpace(this.Password)) throw new ArgumentException($"{nameof(Password)} must be defined.");
            if (string.IsNullOrWhiteSpace(this.WorldName)) throw new ArgumentException($"{nameof(WorldName)} must be defined.");

            // ExePath validation
            if (!this.ExePath.EndsWith(".exe")) throw new ArgumentException($"{nameof(ExePath)} must point to a valid .exe file.");
            if (!File.Exists(this.ExePath)) throw new FileNotFoundException($"No file found at {nameof(ExePath)}: {this.ExePath}"); // todo: move this out of validation

            // WorldName validation
            // todo: Validate world exists? Or do we trust it from the UI control?

            // Name validation
            if (this.Name == this.WorldName) throw new ArgumentException($"{nameof(Name)} cannot be the same as your {nameof(WorldName)} ({this.WorldName}).");

            // Password validation
            if (this.Password.Length < 5) throw new ArgumentException($"{nameof(Password)} must be at least 5 characters.");
            if (this.Password.Contains(this.Name)) throw new ArgumentException($"{nameof(Password)} cannot contain your {nameof(Name)} ({this.Name}).");
            if (this.Password.Contains(this.WorldName)) throw new ArgumentException($"{nameof(Password)} cannot contain your {nameof(WorldName)} ({this.WorldName}).");
        }
    }

    public interface IValheimServerOptions
    {
        string Name { get; }

        string ExePath { get; }

        string Password { get; }

        string WorldName { get; }

        bool Public { get; }

        ushort Port { get; set; }
    }
}
