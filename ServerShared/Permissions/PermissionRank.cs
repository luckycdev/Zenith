using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ServerShared.Logging;
using ServerShared.Player;

namespace ServerShared.Permissions
{
    public class PermissionRank
    {
        public string Name { get; set; }
        public int Weight { get; set; }
        public Dictionary<string, bool> Permissions { get; set; } = new Dictionary<string, bool>();
    }

    public class PermissionStore
    {
        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public List<PermissionRank> Ranks { get; set; } = new List<PermissionRank>();

        private static string GetRankName(AccessLevel accessLevel)
        {
            return accessLevel == AccessLevel.Player ? "Default" : accessLevel.ToString();
        }

        private static int GetDefaultWeight(AccessLevel accessLevel)
        {
            switch (accessLevel)
            {
                case AccessLevel.Player:
                    return 0;
                case AccessLevel.Moderator:
                    return 10;
                case AccessLevel.Admin:
                    return 20;
                case AccessLevel.Console:
                    return 9999;
                default:
                    return 0;
            }
        }

        public static PermissionStore Load(string directory)
        {
            string path = Path.Combine(directory, "permissions.json");
            Logger.LogDebug($"Loading permissions from {path}");

            if (!File.Exists(path))
            {
                var store = new PermissionStore();
                // Add default ranks
                store.Ranks.Add(new PermissionRank { Name = "Default", Weight = 0 });
                store.Ranks.Add(new PermissionRank { Name = "Moderator", Weight = 10 });
                store.Ranks.Add(new PermissionRank { Name = "Admin", Weight = 20 });
                store.Ranks.Add(new PermissionRank { Name = "Console", Weight = 9999 });
                store.Save(directory);
                Logger.LogWarning($"Created new permissions at {path}");
                return store;
            }

            try
            {
                string json = File.ReadAllText(path);
                var store = JsonConvert.DeserializeObject<PermissionStore>(json, serializerSettings) ?? new PermissionStore();
                Logger.LogDebug($"Loaded permissions from {path}");
                return store;
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException)
            {
                Logger.LogException("Failed to load permissions.", ex);
                return new PermissionStore();
            }
        }

        public bool Save(string directory)
        {
            try
            {
                using (var writer = File.CreateText(Path.Combine(directory, "permissions.json")))
                {
                    string json = JsonConvert.SerializeObject(this, serializerSettings);
                    writer.Write(json);
                }
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException)
            {
                Logger.LogException("Failed to save permissions.", ex);
                return false;
            }
        }

        public PermissionRank GetRank(AccessLevel accessLevel)
        {
            string rankName = GetRankName(accessLevel);
            return Ranks.FirstOrDefault(r => r.Name.Equals(rankName, StringComparison.OrdinalIgnoreCase))
                ?? Ranks.OrderBy(r => r.Weight).FirstOrDefault();
        }

        public PermissionRank GetRank(string rankName)
        {
            if (string.IsNullOrWhiteSpace(rankName))
                rankName = "Default";

            return Ranks.FirstOrDefault(r => r.Name.Equals(rankName, StringComparison.OrdinalIgnoreCase))
                ?? Ranks.OrderBy(r => r.Weight).FirstOrDefault();
        }

        public int GetRankWeight(string rankName)
        {
            return GetRank(rankName)?.Weight ?? 0;
        }

        public AccessLevel GetMappedAccessLevel(string rankName)
        {
            var rank = GetRank(rankName);
            if (rank == null)
                return AccessLevel.Player;

            if (rank.Name.Equals("Default", StringComparison.OrdinalIgnoreCase) || rank.Weight <= 0)
                return AccessLevel.Player;

            if (rank.Name.Equals("Moderator", StringComparison.OrdinalIgnoreCase) || rank.Weight <= 10)
                return AccessLevel.Moderator;

            if (rank.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase) || rank.Weight <= 20)
                return AccessLevel.Admin;

            return AccessLevel.Console;
        }

        public bool RegisterDefaultPermission(string permission, AccessLevel defaultAccessLevel)
        {
            if (string.IsNullOrWhiteSpace(permission))
                throw new ArgumentNullException(nameof(permission));

            if (Ranks.Any(permissionRank => permissionRank.Permissions.ContainsKey(permission)))
                return false;

            var rank = GetRank(defaultAccessLevel);
            if (rank == null)
            {
                rank = new PermissionRank
                {
                    Name = GetRankName(defaultAccessLevel),
                    Weight = GetDefaultWeight(defaultAccessLevel)
                };
                Ranks.Add(rank);
            }

            rank.Permissions[permission] = true;
            return true;
        }

        public bool HasPermission(AccessLevel accessLevel, string permission)
        {
            return HasPermission(GetRankName(accessLevel), permission);
        }

        public bool HasPermission(string rankName, string permission)
        {
            var rank = GetRank(rankName);
            if (rank == null)
                return false;

            // 1) Exact rank value wins (true or false)
            if (rank.Permissions.TryGetValue(permission, out bool ownAllowed))
                return ownAllowed;

            // 2) If undefined on this rank, walk strictly lower ranks only.
            //    Ranks with equal weight are NOT inherited.
            var lowerRanks = Ranks
                .Where(r => r.Weight < rank.Weight)
                .OrderByDescending(r => r.Weight);

            foreach (var lowerRank in lowerRanks)
            {
                if (lowerRank.Permissions.TryGetValue(permission, out bool allowed))
                    return allowed;
            }

            return false;
        }
    }
}
