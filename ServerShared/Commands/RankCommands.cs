using System;
using System.Linq;
using System.Net;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;
using ServerShared.Permissions;
using ServerShared.Player;

namespace ServerShared.Commands
{
    [Command("Set rank by steam id", "setrank", "Set a player's rank by steam id. The rank must exist in permissions.json (e.g. 'member', 'moderator', 'admin').")]
    [CommandPermission("zenith.rank.set", DefaultRanks.Admin)]
    public class SetRankBySteamIdCommand : BaseRankCommand
    {
        [CommandArgument("Steam id")]
        public ulong SteamId { get; set; }

        [CommandArgument("Rank")]
        public string RankName { get; set; }

        public override void Handle(string[] args)
        {
            if (!HasAccess())
                return;

            if (!TryResolveAssignableRank(RankName, out var rank))
                return;

            var existingIdentity = Server.Config.PlayerData.PlayerRanks
                .FirstOrDefault(identity => identity.Type == IdentityType.SteamId && identity.SteamId == SteamId);

            if (!CanModifyTarget(existingIdentity?.GetRankName() ?? DefaultRanks.Member))
                return;

            Server.SetRank(SteamId, rank.Name);
            SendMessage($"{SteamId} rank set to {rank.Name}.", LogMessageType.Info);
        }
    }

    [Command("Set rank by name and IP", "setrankip", "Set a player's rank by IP only when the player name and IP match. The rank must exist in permissions.json (e.g. 'member', 'moderator', 'admin').")]
    [CommandPermission("zenith.rank.setip", DefaultRanks.Admin)]
    public class SetRankByIpCommand : BaseRankCommand
    {
        [CommandArgument("Player name")]
        public string PlayerName { get; set; }

        [CommandArgument("IP")]
        public string IP { get; set; }

        [CommandArgument("Rank")]
        public string RankName { get; set; }

        public override void Handle(string[] args)
        {
            if (!HasAccess())
                return;

            if (!TryResolveAssignableRank(RankName, out var rank))
                return;

            if (!IPAddress.TryParse(IP, out var ipAddress))
            {
                SendMessage("Invalid IP specified.", LogMessageType.Error);
                return;
            }

            var player = Server.Players.Values
                .FirstOrDefault(plr => plr.Name.Equals(PlayerName, StringComparison.OrdinalIgnoreCase));

            if (player == null)
            {
                SendMessage("No connected player matches that name.", LogMessageType.Error);
                return;
            }

            if (player.Peer.RemoteEndPoint.Address == null || !player.Peer.RemoteEndPoint.Address.Equals(ipAddress))
            {
                SendMessage("Player name and IP do not match.", LogMessageType.Error);
                return;
            }

            if (!CanModifyTarget(player.RankName))
                return;

            var previousRank = player.RankName;
            Server.SetRank(ipAddress, rank.Name, player.Name);
            SendMessage($"{player.Name} ({ipAddress}) rank set to {player.RankName} (was {previousRank})", LogMessageType.Info);
        }
    }

    /// <summary>
    /// Shared helpers for commands that assign permission ranks.
    /// </summary>
    public abstract class BaseRankCommand : BaseCommand
    {
        /// <summary>
        /// Resolves a rank by exact name (no aliases, no fallback) and checks that the caller is allowed to hand it out.
        /// </summary>
        protected bool TryResolveAssignableRank(string rankName, out PermissionRank rank)
        {
            rank = Server.Config.Permissions.FindRank(rankName);

            if (rank == null)
            {
                var available = Server.Config.Permissions.Ranks
                    .OrderBy(r => r.Weight)
                    .Select(r => r.Name);

                SendMessage($"Unknown rank '{rankName}'. Available ranks: {string.Join(", ", available)}.", LogMessageType.Error);
                return false;
            }

            if (Caller != null && rank.Weight > Caller.RankWeight)
            {
                SendMessage("You can't assign a rank higher than your own.", LogMessageType.Error);
                rank = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks that the caller outranks (or equals) the target's current rank.
        /// </summary>
        protected bool CanModifyTarget(string targetRankName)
        {
            if (Caller == null)
                return true;

            if (Server.Config.Permissions.GetRankWeight(targetRankName) > Caller.RankWeight)
            {
                SendMessage("You can't change the rank of someone with a higher rank than yourself.", LogMessageType.Error);
                return false;
            }

            return true;
        }
    }
}
