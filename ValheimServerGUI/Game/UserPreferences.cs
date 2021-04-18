using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using ValheimServerGUI.Properties;

namespace ValheimServerGUI.Game
{
    public class UserPreferences
    {
        [JsonIgnore]
        public static UserPreferences Default => new()
        {
            ValheimGamePath = Resources.DefaultGamePath,
            ValheimServerPath = Resources.DefaultServerPath,
            ValheimWorldsFolder = Resources.DefaultWorldsFolder,
            StartWithWindows = false,
            StartServerAutomatically = false,
            ServerPort = int.Parse(Resources.DefaultServerPort),
        };

        [JsonProperty("valheimGamePath")]
        public string ValheimGamePath { get; set; }

        [JsonProperty("valheimServerPath")]
        public string ValheimServerPath { get; set; }

        [JsonProperty("valheimWorldsFolder")]
        public string ValheimWorldsFolder { get; set; }

        [JsonProperty("startWithWindows")]
        public bool StartWithWindows { get; set; }

        [JsonProperty("startServerAutomatically")]
        public bool StartServerAutomatically { get; set; }

        [JsonProperty("servers")]
        public List<ServerPreferences> Servers { get; set; }

        [JsonIgnore]
        public string ServerName
        {
            get => this.Servers.FirstOrDefault()?.Name;
            set => this.SetServerValue(s => s.Name = value);
        }

        [JsonIgnore]
        public string ServerPassword
        {
            get => this.Servers.FirstOrDefault()?.Password;
            set => this.SetServerValue(s => s.Password = value);
        }

        [JsonIgnore]
        public string ServerWorldName
        {
            get => this.Servers.FirstOrDefault()?.WorldName;
            set => this.SetServerValue(s => s.WorldName = value);
        }

        [JsonIgnore]
        public bool ServerPublic
        {
            get => this.Servers.FirstOrDefault()?.CommunityServer ?? default;
            set => this.SetServerValue(s => s.CommunityServer = value);
        }

        [JsonIgnore]
        public int ServerPort
        {
            get => this.Servers.FirstOrDefault()?.Port ?? default;
            set => this.SetServerValue(s => s.Port = value);
        }

        private void SetServerValue(Action<ServerPreferences> action)
        {
            if (this.Servers == null) this.Servers = new();

            var server = this.Servers.FirstOrDefault();
            if (server == null)
            {
                server = new ServerPreferences();
                this.Servers.Add(server);
            }

            action(server);
        }

        public class ServerPreferences
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("password")]
            public string Password { get; set; }

            [JsonProperty("world")]
            public string WorldName { get; set; }

            [JsonProperty("community")]
            public bool CommunityServer { get; set; }

            [JsonProperty("port")]
            public int Port { get; set; }
        }
    }
}
