using System;
using System.Linq;
using System.Net;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;
using ServerShared.Player;

namespace ServerShared.Commands
{
    [Command("Ban by player id", "banid", "Ban the player with the specified id for an optional amount of time in minutes.")]
    [CommandPermission("zenith.ban.banid", DefaultRanks.Moderator)]
    public class BanIdCommand : BaseBanCommand
    {
        [CommandArgument("Player id")]
        public int PlayerId { get; set; }

        [CommandArgument("Ban length", optional: true, defaultValue: "0")]
        public int Minutes { get; set; }

        [CommandArgument("Reason", optional: true, defaultValue: null)]
        public string Reason { get; set; }

        public override void Handle(string[] args)
        {
            if (!HasAccess())
                return;

            var banTarget = Server.FindPlayer(PlayerId);

            if (banTarget == null)
            {
                SendMessage($"Could not find a player with the id {PlayerId}.", LogMessageType.Error);
                return;
            }

            if (!VerifyArgs(Reason, Minutes))
                return;

            Reason = Reason?.Trim('\"'); // Temporary workaround for quote tags being included in argument string.

            if (Caller != null && banTarget == Caller)
            {
                SendMessage("You can't ban yourself.", LogMessageType.Error);
                return;
            }

            if (Caller != null && banTarget.RankWeight > Caller.RankWeight)
            {
                SendMessage("You can't ban someone with a higher rank than yourself.", LogMessageType.Error);
                return;
            }

            Server.BanPlayer(banTarget, Reason, Minutes != 0 ? DateTime.UtcNow.AddMinutes(Minutes) : (DateTime?)null);
            AnnounceBan(banTarget, Minutes);
        }
    }

    [Command("Ban by steam id", "bansteamid", "Ban the specified steam id for an optional amount of time in minutes.")]
    [CommandPermission("zenith.ban.bansteamid", DefaultRanks.Moderator)]
    public class BanSteamIdCommand : BaseBanCommand
    {
        [CommandArgument("Steam id")]
        public ulong SteamId { get; set; }

        [CommandArgument("Ban length", optional: true, defaultValue: "0")]
        public int Minutes { get; set; }

        [CommandArgument("Reason", optional: true, defaultValue: null)]
        public string Reason { get; set; }

        public override void Handle(string[] args)
        {
            if (!HasAccess())
                return;

            if (!VerifyArgs(Reason, Minutes))
                return;

            Reason = Reason?.Trim('\"'); // Temporary workaround for quote tags being included in argument string.

            if (Caller != null && SteamId == Caller.SteamId)
            {
                SendMessage("You can't ban yourself.", LogMessageType.Error);
                return;
            }

            if (Caller != null)
            {
                // Check if steam id has a higher rank than caller.
                var existingUser = Server.Config.PlayerData.PlayerRanks.FirstOrDefault(identity => identity.Type == IdentityType.SteamId && identity.SteamId == SteamId);

                if (existingUser != null)
                {
                    int existingWeight = Server.Config.Permissions.GetRankWeight(existingUser.GetRankName());
                    if (existingWeight > Caller.RankWeight)
                    {
                        SendMessage("You can't ban someone with a higher rank than yourself.", LogMessageType.Error);
                        return;
                    }
                }
            }

            var ban = Server.BanSteamId(SteamId, Reason, Minutes != 0 ? DateTime.UtcNow.AddMinutes(Minutes) : (DateTime?)null);
            var player = Server.FindPlayer(SteamId);

            if (player != null)
            {
                AnnounceBan(player, Minutes);
            }

            SendMessage("The steam id was banned successfully.", LogMessageType.Info);
        }
    }

    [Command("Ban by IP", "banip", "Ban the specified ip for an optional amount of time in minutes.")]
    [CommandPermission("zenith.ban.banip", DefaultRanks.Admin)]
    public class BanIpCommand : BaseBanCommand
    {
        [CommandArgument("IP")]
        public string IP { get; set; }

        [CommandArgument("Ban length", optional: true, defaultValue: "0")]
        public int Minutes { get; set; }

        [CommandArgument("Reason", optional: true, defaultValue: null)]
        public string Reason { get; set; }

        public override void Handle(string[] args)
        {
            if (!HasAccess())
                return;

            if (!VerifyArgs(Reason, Minutes))
                return;

            Reason = Reason?.Trim('\"'); // Temporary workaround for quote tags being included in argument string.

            if (!IPAddress.TryParse(IP, out var ipAddress))
            {
                SendMessage("Invalid ip specified.", LogMessageType.Error);
                return;
            }

            if (Caller != null)
            {
                if (Caller.Peer.RemoteEndPoint.Address.Equals(ipAddress))
                {
                    SendMessage("You can't ban yourself.", LogMessageType.Error);
                    return;
                }

                uint uintIp = ipAddress.ToUint32();
                bool targetsHigherRank = Server.Players.Values.Any(plr => plr.Peer.RemoteEndPoint.Address.Equals(ipAddress) && plr.RankWeight > Caller.RankWeight)
                    || Server.Config.PlayerData.PlayerRanks.Any(identity => identity.Type == IdentityType.Ip && identity.Ip == uintIp
                        && Server.Config.Permissions.GetRankWeight(identity.GetRankName()) > Caller.RankWeight);

                if (targetsHigherRank)
                {
                    SendMessage("You can't ban someone with a higher rank than yourself.", LogMessageType.Error);
                    return;
                }
            }

            var ban = Server.BanIp(ipAddress, Reason, Minutes != 0 ? DateTime.UtcNow.AddMinutes(Minutes) : (DateTime?)null);
            var player = Server.FindPlayer(ipAddress);

            if (player != null)
            {
                AnnounceBan(player, Minutes);
            }

            SendMessage("The specified IP was banned successfully.", LogMessageType.Info);
        }
    }

    public abstract class BaseBanCommand : BaseCommand
    {
        protected bool VerifyArgs(string reason, int minutes)
        {
            if (minutes < 0)
            {
                SendMessage("Invalid time specified (needs to be 0 or higher).", LogMessageType.Error);
                return false;
            }

            return true;
        }

        protected void AnnounceBan(NetPlayer banTarget, int minutes)
        {
            string suffix;

            if (minutes == 1)
                suffix = $"for {minutes} minute";
            else if (minutes > 1)
                suffix = $"for {minutes} minutes";
            else
                suffix = "permanently";

            Server.BroadcastChatMessage($"{banTarget.Name} was banned {suffix}.", SharedConstants.ColorBlue);
        }
    }
}
