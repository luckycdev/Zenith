using System.Linq;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;
using ServerShared.Player;

namespace ServerShared.Commands
{
    [Command("Kick by name", "kick", "Kick a player by name with an optional reason.")]
    [CommandPermission("zenith.kick.kick", DefaultRanks.Moderator)]
    public class KickCommand : BaseCommand
    {
        [CommandArgument("Player name")]
        public string PlayerName { get; set; }

        [CommandArgument("Reason", optional: true, defaultValue: null)]
        public string Reason { get; set; }

        public override void Handle(string[] args)
        {
            if (!HasAccess())
                return;

            var players = Server.FindPlayers(PlayerName, NameSearchOption.Contains).ToList();

            if (players.Count == 0)
            {
                SendMessage("Could not find a player with this name.", LogMessageType.Error);
                return;
            }

            if (players.Count > 1)
            {
                SendMessage("Found more than 1 player with this name. Be more specific.", LogMessageType.Error);
                return;
            }

            var target = players.First();

            if (Caller != null && target == Caller)
            {
                SendMessage("You can't kick yourself.", LogMessageType.Error);
                return;
            }

            if (Caller != null && target.RankWeight > Caller.RankWeight)
            {
                SendMessage("You can't kick someone with a higher rank than yourself.", LogMessageType.Error);
                return;
            }

            string rawReason = string.IsNullOrWhiteSpace(Reason)
                ? "Kicked by moderator."
                : Reason.Trim('"').Replace("\r", " ").Replace("\n", " ").Trim();

            if (string.IsNullOrWhiteSpace(rawReason))
                rawReason = "Kicked by moderator.";

            string reason = rawReason.Equals("Kicked by moderator.", System.StringComparison.Ordinal)
                ? rawReason
                : $"Kicked by moderator for reason \"{rawReason}\".";

            Server.KickConnection(target.Peer, DisconnectReason.Invalid, reason);
            SendMessage($"Kicked {target.Name}.", LogMessageType.Info);
        }
    }

    [Command("Kick by id", "kickid", "Kick a player by id with an optional reason.")]
    [CommandPermission("zenith.kick.kickid", DefaultRanks.Moderator)]
    public class KickIdCommand : BaseCommand
    {
        [CommandArgument("Player id")]
        public int PlayerId { get; set; }

        [CommandArgument("Reason", optional: true, defaultValue: null)]
        public string Reason { get; set; }

        public override void Handle(string[] args)
        {
            if (!HasAccess())
                return;

            var target = Server.FindPlayer(PlayerId);

            if (target == null)
            {
                SendMessage($"Could not find a player with the id {PlayerId}.", LogMessageType.Error);
                return;
            }

            if (Caller != null && target == Caller)
            {
                SendMessage("You can't kick yourself.", LogMessageType.Error);
                return;
            }

            if (Caller != null && target.RankWeight > Caller.RankWeight)
            {
                SendMessage("You can't kick someone with a higher rank than yourself.", LogMessageType.Error);
                return;
            }

            string rawReason = string.IsNullOrWhiteSpace(Reason)
                ? "Kicked by moderator."
                : Reason.Trim('"').Replace("\r", " ").Replace("\n", " ").Trim();

            if (string.IsNullOrWhiteSpace(rawReason))
                rawReason = "Kicked by moderator.";

            string reason = rawReason.Equals("Kicked by moderator.", System.StringComparison.Ordinal)
                ? rawReason
                : $"Kicked by moderator for reason \"{rawReason}\".";

            Server.KickConnection(target.Peer, DisconnectReason.Invalid, reason);
            SendMessage($"Kicked {target.Name}.", LogMessageType.Info);
        }
    }
}
