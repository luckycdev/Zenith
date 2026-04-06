using System;
using System.Linq;
using System.Net;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;
using ServerShared.Player;

namespace ServerShared.Commands
{
    [Command("Set access level by steam id", "accesslevel", "Set the access level by steam id. Allowed values are 'player', 'moderator', and 'admin'.")]
    [CommandPermission("zenith.moderator.accesslevel", AccessLevel.Admin)]
    public class SetAccessLevelBySteamId : BaseCommand
    {
        [CommandArgument("Steam id")]
        public ulong SteamId { get; set; }

        [CommandArgument("Access level")]
        public string AccessLevelName { get; set; }

        public override void Handle(string[] args)
        {
            if (!HasAccess())
                return;

            var rank = Server.Config.Permissions.GetRank(AccessLevelName);
            if (rank == null)
            {
                SendMessage("Invalid access level specified.", LogMessageType.Error);
                return;
            }

            Server.SetAccessLevel(SteamId, AccessLevelName);
            SendMessage($"{SteamId} access level set to {rank.Name}.", LogMessageType.Info);
        }
    }

    [Command("Set access level by name and IP", "accesslevelip", "Set the access level by IP only when the player name and IP match. Allowed values are 'player', 'moderator', and 'admin'.")]
    [CommandPermission("zenith.moderator.accesslevelip", AccessLevel.Admin)]
    public class SetAccessLevelByIp : BaseCommand
    {
        [CommandArgument("Player name")]
        public string PlayerName { get; set; }

        [CommandArgument("IP")]
        public string IP { get; set; }

        [CommandArgument("Access level")]
        public string AccessLevelName { get; set; }

        public override void Handle(string[] args)
        {
            if (!HasAccess())
                return;

            var rank = Server.Config.Permissions.GetRank(AccessLevelName);
            if (rank == null)
            {
                SendMessage("Invalid access level specified.", LogMessageType.Error);
                return;
            }

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

            var previousAccessLevel = player.RankName;
            Server.SetAccessLevel(ipAddress, AccessLevelName, player.Name);
            SendMessage($"{player.Name} ({ipAddress}) access level set to {player.RankName} (was {previousAccessLevel})", LogMessageType.Info);
        }
    }
}
