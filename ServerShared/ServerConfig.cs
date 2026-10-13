using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ServerShared.Logging;
using ServerShared.Player;
using ServerShared.Permissions;

namespace ServerShared
{
    public class ServerConfig
    {
        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        [JsonIgnore]
        public string Directory { get; private set; }

        [JsonIgnore]
        public PlayerDataStore PlayerData { get; private set; }

        [JsonIgnore]
        public PermissionStore Permissions { get; private set; }

        private ServerConfig(string directory)
        {
            Directory = directory ?? throw new ArgumentNullException(nameof(directory));
            PlayerData = PlayerDataStore.Load(directory);
            Permissions = PermissionStore.Load(directory);
        }

        /// <summary>Do not use. Used for deserialization.</summary>
        [JsonConstructor]
        private ServerConfig() { }

        public bool Save()
        {
            return SaveConfig(Directory, this) && PlayerData.Save(Directory) && Permissions.Save(Directory);
        }

        public static bool LoadConfig(string directory, out ServerConfig config)
        {
            CheckDirectory(directory);

            string savePath = Path.Combine(directory, "config.json");

            Logger.LogDebug($"Loading config from {savePath}");

            if (!File.Exists(savePath))
            {
                config = new ServerConfig(directory);
                bool saveSuccessful = config.Save();

                if (saveSuccessful)
                    Logger.LogWarning($"Created new config at {savePath}");
                else
                    Logger.LogError($"Failed to create new config at {savePath}");

                return saveSuccessful;
            }

            try
            {
                string json = File.ReadAllText(savePath);
                config = JsonConvert.DeserializeObject<ServerConfig>(json, serializerSettings);
                config.Directory = directory;
                config.PlayerData = PlayerDataStore.Load(directory);
                config.Permissions = PermissionStore.Load(directory);

                // Remove expired bans and save player data if needed.
                if (config.PlayerData.RemoveExpiredBans() > 0)
                    config.PlayerData.Save(directory);

                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException)
            {
                Logger.LogException("Failed to load server config.", ex);
                config = null;
                return false;
            }
        }

        public static bool SaveConfig(string directory, ServerConfig config)
        {
            CheckDirectory(directory);

            try
            {
                using (var writer = File.CreateText(Path.Combine(directory, "config.json")))
                {
                    string json = JsonConvert.SerializeObject(config, serializerSettings);
                    writer.Write(json);
                }

                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException)
            {
                Logger.LogException("Failed to save server config.", ex);
                return false;
            }
        }

        private static void CheckDirectory(string directory)
        {
            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);
        }
    }

    public class PlayerDataStore
    {
        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public List<PlayerBan> Bans { get; private set; } = new List<PlayerBan>();
        public List<PlayerRankIdentity> PlayerRanks { get; private set; } = new List<PlayerRankIdentity>();

        public PlayerDataStore() { }

        public static PlayerDataStore Load(string directory)
        {
            string path = Path.Combine(directory, "players.json");
            Logger.LogDebug($"Loading player data from {path}");

            if (!File.Exists(path))
            {
                var store = new PlayerDataStore();
                store.Save(directory);
                Logger.LogWarning($"Created new player data at {path}");
                return store;
            }

            try
            {
                string json = File.ReadAllText(path);
                var store = JsonConvert.DeserializeObject<PlayerDataStore>(json, serializerSettings) ?? new PlayerDataStore();
                Logger.LogDebug($"Loaded player data from {path}");
                return store;
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException)
            {
                Logger.LogException("Failed to load player data.", ex);
                return new PlayerDataStore();
            }
        }

        public bool Save(string directory)
        {
            try
            {
                using (var writer = File.CreateText(Path.Combine(directory, "players.json")))
                {
                    string json = JsonConvert.SerializeObject(this, serializerSettings);
                    writer.Write(json);
                }
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException)
            {
                Logger.LogException("Failed to save player data.", ex);
                return false;
            }
        }

        /// <summary>Removes all expired bans and returns the amount of bans removed.</summary>
        public int RemoveExpiredBans()
        {
            return Bans.RemoveAll(ban => ban.Expired());
        }
    }
}
