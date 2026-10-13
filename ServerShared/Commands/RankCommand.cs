using System;
using System.Linq;
using System.Text;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;
using ServerShared.Permissions;

namespace ServerShared.Commands
{
    /// <summary>
    /// Manage ranks in-game, similar to LuckPerms' /lp group commands. Every change is saved to permissions.json right away.
    /// Players can only change ranks with a weight up to their own; the server console can change anything.
    /// </summary>
    [Command("Rank", "rank", "Manage ranks and their permissions. Use 'rank help' to see all subcommands.")]
    [CommandPermission("zenith.rank", DefaultRanks.Admin)]
    [RegisterPermission("zenith.rank.list", DefaultRanks.Admin)]
    [RegisterPermission("zenith.rank.info", DefaultRanks.Admin)]
    [RegisterPermission("zenith.rank.create", DefaultRanks.Admin)]
    [RegisterPermission("zenith.rank.delete", DefaultRanks.Admin)]
    [RegisterPermission("zenith.rank.rename", DefaultRanks.Admin)]
    [RegisterPermission("zenith.rank.weight", DefaultRanks.Admin)]
    [RegisterPermission("zenith.rank.permission", DefaultRanks.Admin)]
    [RegisterPermission("zenith.rank.parent", DefaultRanks.Admin)]
    public class RankCommand : BaseCommand
    {
        private static readonly string[] reservedNames = { "list", "create", "delete", "help" };

        private PermissionStore Store => Server.Config.Permissions;

        public override void Handle(string[] args)
        {
            if (!HasAccess())
                return;

            args = args.Select(arg => arg.Trim('"').Trim()).Where(arg => arg.Length > 0).ToArray();

            if (args.Length == 0 || Is(args[0], "help"))
            {
                SendUsage();
                return;
            }

            if (Is(args[0], "list"))
            {
                List();
                return;
            }

            if (Is(args[0], "create"))
            {
                Create(args);
                return;
            }

            if (Is(args[0], "delete"))
            {
                Delete(args);
                return;
            }

            var rank = Store.FindRank(args[0]);
            if (rank == null)
            {
                SendMessage($"Unknown rank '{args[0]}'. Use 'rank list' to see all ranks.", LogMessageType.Error);
                return;
            }

            string action = args.Length > 1 ? args[1].ToLowerInvariant() : "info";

            switch (action)
            {
                case "info":
                    Info(rank);
                    break;
                case "permission":
                case "perm":
                    PermissionSubcommand(rank, args);
                    break;
                case "parent":
                    ParentSubcommand(rank, args);
                    break;
                case "setweight":
                    SetWeight(rank, args);
                    break;
                case "rename":
                    Rename(rank, args);
                    break;
                default:
                    SendUsage();
                    break;
            }
        }

        private void SendUsage()
        {
            SendMessage(string.Join("\n", new[]
            {
                "Rank commands:",
                "rank list",
                "rank create <name> <weight>",
                "rank delete <name>",
                "rank <rank> info",
                "rank <rank> permission set <node> [true|false]",
                "rank <rank> permission unset <node>",
                "rank <rank> permission check <node>",
                "rank <rank> parent add <rank>",
                "rank <rank> parent remove <rank>",
                "rank <rank> setweight <weight>",
                "rank <rank> rename <new name>",
                "true = allow, false = deny, unset = inherit from parent ranks. Wildcards like 'zenith.kick.*' and '*' work.",
            }), LogMessageType.Info);
        }

        private void List()
        {
            if (!HasAccess("zenith.rank.list"))
                return;

            var builder = new StringBuilder();
            builder.Append($"Ranks ({Store.Ranks.Count}):");

            foreach (var rank in Store.Ranks.OrderByDescending(r => r.Weight))
            {
                builder.Append($"\n- {rank.Name} (weight {rank.Weight}");
                if (rank.Inherits != null && rank.Inherits.Count > 0)
                    builder.Append($", inherits {string.Join(", ", rank.Inherits)}");
                builder.Append(")");
            }

            SendMessage(builder.ToString(), LogMessageType.Info);
        }

        private void Info(PermissionRank rank)
        {
            if (!HasAccess("zenith.rank.info"))
                return;

            var builder = new StringBuilder();
            builder.Append($"Rank {rank.Name}\nWeight: {rank.Weight}");
            builder.Append($"\nInherits: {(rank.Inherits != null && rank.Inherits.Count > 0 ? string.Join(", ", rank.Inherits) : "nothing")}");
            builder.Append($"\nPermissions set on this rank ({rank.Permissions.Count}):");

            foreach (var entry in rank.Permissions.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase))
                builder.Append($"\n- {entry.Key} = {(entry.Value ? "true" : "false")}");

            SendMessage(builder.ToString(), LogMessageType.Info);
        }

        private void Create(string[] args)
        {
            if (!HasAccess("zenith.rank.create"))
                return;

            if (args.Length < 3)
            {
                SendMessage("Usage: rank create <name> <weight>", LogMessageType.Error);
                return;
            }

            string name = args[1];

            if (!ValidateNewName(name) || !TryParseWeight(args[2], out int weight))
                return;

            Store.CreateRank(name, weight);
            Save();
            SendMessage($"Created rank {name} with weight {weight}. It doesn't inherit anything yet, use 'rank {name} parent add <rank>' to change that.", LogMessageType.Info);
        }

        private void Delete(string[] args)
        {
            if (!HasAccess("zenith.rank.delete"))
                return;

            if (args.Length < 2)
            {
                SendMessage("Usage: rank delete <name>", LogMessageType.Error);
                return;
            }

            var rank = Store.FindRank(args[1]);
            if (rank == null)
            {
                SendMessage($"Unknown rank '{args[1]}'.", LogMessageType.Error);
                return;
            }

            if (Is(rank.Name, DefaultRanks.Member))
            {
                SendMessage($"The {DefaultRanks.Member} rank can't be deleted, it's the rank everyone has by default.", LogMessageType.Error);
                return;
            }

            if (!CanEdit(rank))
                return;

            Store.DeleteRank(rank.Name);
            int removedPlayers = Server.Config.PlayerData.PlayerRanks.RemoveAll(identity => Is(identity.RankName, rank.Name));

            Save();
            Server.Config.Save();
            RefreshPlayers();

            SendMessage($"Deleted rank {rank.Name}. {removedPlayers} player(s) with this rank are now {DefaultRanks.Member}.", LogMessageType.Info);
        }

        private void Rename(PermissionRank rank, string[] args)
        {
            if (!HasAccess("zenith.rank.rename"))
                return;

            if (args.Length < 3)
            {
                SendMessage("Usage: rank <rank> rename <new name>", LogMessageType.Error);
                return;
            }

            if (Is(rank.Name, DefaultRanks.Member))
            {
                SendMessage($"The {DefaultRanks.Member} rank can't be renamed, it's the rank everyone has by default.", LogMessageType.Error);
                return;
            }

            string newName = args[2];

            if (!CanEdit(rank) || !ValidateNewName(newName))
                return;

            string oldName = rank.Name;
            rank.Name = newName;

            foreach (var other in Store.Ranks)
            {
                if (other.Inherits == null)
                    continue;

                for (int i = 0; i < other.Inherits.Count; i++)
                {
                    if (Is(other.Inherits[i], oldName))
                        other.Inherits[i] = newName;
                }
            }

            foreach (var identity in Server.Config.PlayerData.PlayerRanks.Where(identity => Is(identity.RankName, oldName)))
                identity.RankName = newName;

            Save();
            Server.Config.Save();
            RefreshPlayers();

            SendMessage($"Renamed rank {oldName} to {newName}.", LogMessageType.Info);
        }

        private void SetWeight(PermissionRank rank, string[] args)
        {
            if (!HasAccess("zenith.rank.weight"))
                return;

            if (args.Length < 3)
            {
                SendMessage("Usage: rank <rank> setweight <weight>", LogMessageType.Error);
                return;
            }

            if (!CanEdit(rank) || !TryParseWeight(args[2], out int weight))
                return;

            rank.Weight = weight;
            Save();
            SendMessage($"Set the weight of {rank.Name} to {weight}.", LogMessageType.Info);
        }

        private void PermissionSubcommand(PermissionRank rank, string[] args)
        {
            if (args.Length < 4)
            {
                SendMessage("Usage: rank <rank> permission <set|unset|check> <node> [true|false]", LogMessageType.Error);
                return;
            }

            string action = args[2].ToLowerInvariant();
            string node = args[3];

            if (node.Any(char.IsWhiteSpace))
            {
                SendMessage("Permission nodes can't contain spaces.", LogMessageType.Error);
                return;
            }

            if (action == "check")
            {
                if (!HasAccess("zenith.rank.info"))
                    return;

                bool? value = Store.GetPermissionValue(rank.Name, node, out string source);
                string result = value == null ? "not set (denied)" : value.Value ? $"allowed (from {source})" : $"denied (from {source})";
                SendMessage($"{rank.Name}: {node} is {result}.", LogMessageType.Info);
                return;
            }

            if (!HasAccess("zenith.rank.permission") || !CanEdit(rank))
                return;

            if (action == "set")
            {
                bool value = true;

                if (args.Length > 4 && !bool.TryParse(args[4], out value))
                {
                    SendMessage("The value has to be 'true' (allow) or 'false' (deny).", LogMessageType.Error);
                    return;
                }

                rank.Permissions[node] = value;
                Save();
                SendMessage($"Set {node} to {(value ? "true" : "false")} on {rank.Name}.", LogMessageType.Info);
            }
            else if (action == "unset")
            {
                if (!rank.Permissions.Remove(node))
                {
                    SendMessage($"{rank.Name} doesn't have {node} set.", LogMessageType.Error);
                    return;
                }

                Save();
                SendMessage($"Unset {node} on {rank.Name}. It's now inherited from parent ranks.", LogMessageType.Info);
            }
            else
            {
                SendMessage("Usage: rank <rank> permission <set|unset|check> <node> [true|false]", LogMessageType.Error);
            }
        }

        private void ParentSubcommand(PermissionRank rank, string[] args)
        {
            if (!HasAccess("zenith.rank.parent"))
                return;

            if (args.Length < 4 || !(Is(args[2], "add") || Is(args[2], "remove")))
            {
                SendMessage("Usage: rank <rank> parent <add|remove> <rank>", LogMessageType.Error);
                return;
            }

            if (!CanEdit(rank))
                return;

            if (Is(args[2], "remove"))
            {
                if (rank.Inherits.RemoveAll(parentName => Is(parentName, args[3])) == 0)
                {
                    SendMessage($"{rank.Name} doesn't inherit {args[3]}.", LogMessageType.Error);
                    return;
                }

                Save();
                SendMessage($"{rank.Name} no longer inherits {args[3]}.", LogMessageType.Info);
                return;
            }

            var parent = Store.FindRank(args[3]);
            if (parent == null)
            {
                SendMessage($"Unknown rank '{args[3]}'.", LogMessageType.Error);
                return;
            }

            if (Is(parent.Name, rank.Name) || Store.InheritsFrom(parent, rank.Name))
            {
                SendMessage($"{rank.Name} can't inherit {parent.Name}, that would make an inheritance loop.", LogMessageType.Error);
                return;
            }

            if (rank.Inherits.Any(parentName => Is(parentName, parent.Name)))
            {
                SendMessage($"{rank.Name} already inherits {parent.Name}.", LogMessageType.Error);
                return;
            }

            if (Caller != null && parent.Weight > Caller.RankWeight)
            {
                SendMessage("You can't make a rank inherit a rank higher than your own.", LogMessageType.Error);
                return;
            }

            rank.Inherits.Add(parent.Name);
            Save();
            SendMessage($"{rank.Name} now inherits {parent.Name}.", LogMessageType.Info);
        }

        private bool CanEdit(PermissionRank rank)
        {
            if (Caller != null && rank.Weight > Caller.RankWeight)
            {
                SendMessage("You can't change a rank higher than your own.", LogMessageType.Error);
                return false;
            }

            return true;
        }

        private bool ValidateNewName(string name)
        {
            if (name.Any(char.IsWhiteSpace) || reservedNames.Any(reserved => Is(name, reserved)))
            {
                SendMessage($"'{name}' can't be used as a rank name.", LogMessageType.Error);
                return false;
            }

            if (Store.FindRank(name) != null)
            {
                SendMessage($"A rank named '{name}' already exists.", LogMessageType.Error);
                return false;
            }

            return true;
        }

        private bool TryParseWeight(string text, out int weight)
        {
            if (!int.TryParse(text, out weight) || weight < DefaultRanks.MinWeight || weight > DefaultRanks.MaxWeight)
            {
                SendMessage($"The weight has to be a number from {DefaultRanks.MinWeight} to {DefaultRanks.MaxWeight}.", LogMessageType.Error);
                return false;
            }

            if (Caller != null && weight > Caller.RankWeight)
            {
                SendMessage("You can't use a weight higher than your own rank's.", LogMessageType.Error);
                return false;
            }

            return true;
        }

        private void Save()
        {
            Store.Save(Server.Config.Directory);
        }

        private void RefreshPlayers()
        {
            foreach (var player in Server.Players.Values.ToList())
                Server.UpdateRank(player);
        }

        private static bool Is(string a, string b)
        {
            return a != null && a.Equals(b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
