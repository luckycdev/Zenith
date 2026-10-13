using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ServerShared.Logging;

namespace ServerShared.Permissions
{
    public class PermissionRank
    {
        public string Name { get; set; }

        /// <summary>Used for hierarchy only (who can kick/ban/set the rank of who, player list order). Does not grant permissions.</summary>
        public int Weight { get; set; }

        /// <summary>
        /// Ranks this rank inherits permissions from (e.g. Admin inherits Moderator). Inheritance is recursive.
        /// If several inherited ranks set the same permission, the first one listed wins.
        /// </summary>
        public List<string> Inherits { get; set; }

        /// <summary>
        /// Permission nodes set on this rank: true = allow, false = deny. A node that isn't listed is unset and comes from inherited ranks.
        /// Wildcards are supported: "zenith.kick.*" matches every node under zenith.kick, "*" matches everything.
        /// </summary>
        public Dictionary<string, bool> Permissions { get; set; } = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
    }

    public class PermissionStore
    {
        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            // Camel-case property names, but keep permission node keys exactly as written.
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = false }
            }
        };

        public List<PermissionRank> Ranks { get; set; } = new List<PermissionRank>();

        /// <summary>
        /// Every permission node whose default has already been given to its default rank. Defaults are only applied once, so
        /// removing (unsetting) a default permission from a rank sticks instead of being re-added on the next start.
        /// </summary>
        public List<string> RegisteredPermissions { get; set; }

        private HashSet<string> registeredPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> warnedMissingRanks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static PermissionStore CreateDefault()
        {
            var store = new PermissionStore { RegisteredPermissions = new List<string>() };

            foreach (var name in new[] { DefaultRanks.Member, DefaultRanks.Moderator, DefaultRanks.Admin })
            {
                store.Ranks.Add(new PermissionRank
                {
                    Name = name,
                    Weight = DefaultRanks.GetDefaultWeight(name).Value,
                    Inherits = DefaultRanks.GetDefaultInherits(name)
                });
            }

            return store;
        }

        public static PermissionStore Load(string directory)
        {
            string path = Path.Combine(directory, "permissions.json");
            Logger.LogDebug($"Loading permissions from {path}");

            if (!File.Exists(path))
            {
                var store = CreateDefault();
                store.Save(directory);
                Logger.LogWarning($"Created new permissions at {path}");
                return store;
            }

            try
            {
                string json = File.ReadAllText(path);
                var store = JsonConvert.DeserializeObject<PermissionStore>(json, serializerSettings) ?? new PermissionStore();

                if (store.Normalize())
                    store.Save(directory);

                store.Validate();
                Logger.LogDebug($"Loaded permissions from {path}");
                return store;
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException)
            {
                Logger.LogException("Failed to load permissions.", ex);
                return new PermissionStore();
            }
        }

        /// <summary>
        /// Fills in missing values and fixes invalid ones in a loaded permissions.json. Returns true if anything changed.
        /// </summary>
        private bool Normalize()
        {
            bool changed = false;

            if (Ranks == null)
            {
                Ranks = new List<PermissionRank>();
                changed = true;
            }

            changed |= Ranks.RemoveAll(rank => rank == null || string.IsNullOrWhiteSpace(rank.Name)) > 0;

            if (RegisteredPermissions == null)
            {
                RegisteredPermissions = new List<string>();
                changed = true;
            }

            foreach (var rank in Ranks)
            {
                // Make permission lookups case-insensitive regardless of how the dictionary was deserialized.
                var permissions = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                if (rank.Permissions != null)
                {
                    foreach (var entry in rank.Permissions)
                        permissions[entry.Key] = entry.Value;
                }
                else
                {
                    changed = true;
                }

                rank.Permissions = permissions;

                if (rank.Inherits == null)
                {
                    rank.Inherits = new List<string>();
                    changed = true;
                }

                if (rank.Weight < DefaultRanks.MinWeight || rank.Weight > DefaultRanks.MaxWeight)
                {
                    int clamped = ClampWeight(rank.Weight);
                    Logger.LogWarning($"Rank '{rank.Name}' has weight {rank.Weight}, which is outside {DefaultRanks.MinWeight}-{DefaultRanks.MaxWeight}. Changed it to {clamped}.");
                    rank.Weight = clamped;
                    changed = true;
                }
            }

            registeredPermissions = new HashSet<string>(RegisteredPermissions.Where(node => node != null), StringComparer.OrdinalIgnoreCase);
            return changed;
        }

        public static int ClampWeight(int weight)
        {
            return Math.Max(DefaultRanks.MinWeight, Math.Min(DefaultRanks.MaxWeight, weight));
        }

        /// <summary>Warns about configuration problems that can't be fixed automatically.</summary>
        private void Validate()
        {
            foreach (var duplicate in Ranks.GroupBy(rank => rank.Name, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
                Logger.LogWarning($"permissions.json has more than one rank named '{duplicate.Key}'. Only the first one is used.");

            if (FindRank(DefaultRanks.Member) == null)
                Logger.LogWarning($"permissions.json has no '{DefaultRanks.Member}' rank. Players without a rank will have no permissions.");

            foreach (var rank in Ranks)
            {
                foreach (var parent in rank.Inherits)
                {
                    if (string.IsNullOrWhiteSpace(parent))
                        continue;

                    if (parent.Equals(rank.Name, StringComparison.OrdinalIgnoreCase))
                        Logger.LogWarning($"Rank '{rank.Name}' inherits itself. This is ignored.");
                    else if (FindRank(parent) == null)
                        Logger.LogWarning($"Rank '{rank.Name}' inherits '{parent}', which doesn't exist.");
                }
            }
        }

        public bool Save(string directory)
        {
            try
            {
                RegisteredPermissions = registeredPermissions.OrderBy(node => node, StringComparer.OrdinalIgnoreCase).ToList();

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

        /// <summary>Finds a rank by exact name (case-insensitive). Returns null if it doesn't exist.</summary>
        public PermissionRank FindRank(string rankName)
        {
            if (string.IsNullOrWhiteSpace(rankName))
                return null;

            rankName = rankName.Trim();
            return Ranks.FirstOrDefault(r => r.Name.Equals(rankName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a player's effective rank. An empty or unknown rank name means the player is a Member
        /// (the rank everyone has without a rank entry). Returns null if that doesn't exist either.
        /// </summary>
        public PermissionRank GetRank(string rankName)
        {
            return FindRank(rankName) ?? FindRank(DefaultRanks.Member);
        }

        public int GetRankWeight(string rankName)
        {
            return GetRank(rankName)?.Weight ?? 0;
        }

        /// <summary>
        /// Gives a permission node to its default rank the first time the node is seen. Returns true if permissions.json changed.
        /// After that the node is never re-added, so server owners can unset or deny it.
        /// </summary>
        public bool RegisterDefaultPermission(string permission, string defaultRank)
        {
            if (string.IsNullOrWhiteSpace(permission))
                throw new ArgumentNullException(nameof(permission));

            if (registeredPermissions.Contains(permission))
                return false;

            registeredPermissions.Add(permission);

            // Already set somewhere (e.g. added by hand before the plugin was installed): leave it alone.
            if (Ranks.Any(rank => rank.Permissions.ContainsKey(permission)))
                return true;

            var target = FindRank(defaultRank);
            if (target == null)
            {
                if (warnedMissingRanks.Add(defaultRank ?? string.Empty))
                    Logger.LogWarning($"Rank '{defaultRank}' doesn't exist in permissions.json, so its default permissions (like '{permission}') weren't given to anyone. Add them to a rank with /rank permission set.");

                return true;
            }

            target.Permissions[permission] = true;
            return true;
        }

        /// <summary>Creates a rank. Returns null if a rank with that name already exists.</summary>
        public PermissionRank CreateRank(string rankName, int weight)
        {
            if (string.IsNullOrWhiteSpace(rankName) || FindRank(rankName) != null)
                return null;

            var rank = new PermissionRank { Name = rankName.Trim(), Weight = ClampWeight(weight), Inherits = new List<string>() };
            Ranks.Add(rank);
            return rank;
        }

        /// <summary>Deletes a rank and removes it from every other rank's inherits list.</summary>
        public bool DeleteRank(string rankName)
        {
            var rank = FindRank(rankName);
            if (rank == null)
                return false;

            Ranks.Remove(rank);
            foreach (var other in Ranks)
                other.Inherits?.RemoveAll(parent => parent != null && parent.Equals(rank.Name, StringComparison.OrdinalIgnoreCase));

            return true;
        }

        /// <summary>Returns true if <paramref name="rank"/> inherits <paramref name="ancestorName"/>, directly or indirectly.</summary>
        public bool InheritsFrom(PermissionRank rank, string ancestorName)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pending = new Stack<PermissionRank>();
            pending.Push(rank);

            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (!visited.Add(current.Name) || current.Inherits == null)
                    continue;

                foreach (var parentName in current.Inherits)
                {
                    if (parentName != null && parentName.Equals(ancestorName, StringComparison.OrdinalIgnoreCase))
                        return true;

                    var parent = FindRank(parentName);
                    if (parent != null)
                        pending.Push(parent);
                }
            }

            return false;
        }

        public bool HasPermission(string rankName, string permission)
        {
            var rank = GetRank(rankName);
            if (rank == null)
                return false;

            return ResolvePermission(rank, permission, new HashSet<string>(StringComparer.OrdinalIgnoreCase), out _) ?? false;
        }

        /// <summary>
        /// Works out a permission for a rank: true (allowed), false (denied) or null (unset everywhere, so not allowed).
        /// <paramref name="source"/> is the rank and node that decided it.
        /// </summary>
        public bool? GetPermissionValue(string rankName, string permission, out string source)
        {
            source = null;
            var rank = FindRank(rankName);
            if (rank == null)
                return null;

            return ResolvePermission(rank, permission, new HashSet<string>(StringComparer.OrdinalIgnoreCase), out source);
        }

        /// <summary>
        /// Looks up a permission on a rank (exact node first, then wildcards from most to least specific),
        /// then on the ranks it inherits, recursively and in listed order. Returns null if nothing in the chain sets it.
        /// </summary>
        private bool? ResolvePermission(PermissionRank rank, string permission, HashSet<string> visited, out string source)
        {
            source = null;

            if (!visited.Add(rank.Name)) // Inheritance loop, or already checked through another path.
                return null;

            foreach (var node in GetMatchingNodes(permission))
            {
                if (rank.Permissions.TryGetValue(node, out bool allowed))
                {
                    source = $"{rank.Name}: {node}";
                    return allowed;
                }
            }

            if (rank.Inherits == null)
                return null;

            foreach (var parentName in rank.Inherits)
            {
                var parent = FindRank(parentName);
                if (parent == null)
                    continue;

                bool? inherited = ResolvePermission(parent, permission, visited, out source);
                if (inherited.HasValue)
                    return inherited;
            }

            return null;
        }

        /// <summary>"a.b.c" -> "a.b.c", "a.b.*", "a.*", "*".</summary>
        private static IEnumerable<string> GetMatchingNodes(string permission)
        {
            yield return permission;

            string current = permission;
            int dot;
            while ((dot = current.LastIndexOf('.')) > 0)
            {
                current = current.Substring(0, dot);
                yield return current + ".*";
            }

            yield return "*";
        }
    }
}
