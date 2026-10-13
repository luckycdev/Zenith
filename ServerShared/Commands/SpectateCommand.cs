using System.Collections.Generic;
using System.Linq;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;
using ServerShared.Player;

namespace ServerShared.Commands
{
    [Command("Spectate by name", "spectate", "Start spectating another player by their name.")]
    [CommandPermission("zenith.spectate.spectate", DefaultRanks.Member)]
    [RequireCaller]
    public class SpectateCommand : BaseCommand
    {
        [CommandArgument("Player name")]
        public string PlayerName { get; set; }

        public override void Handle(string[] args)
        {
            if (!HasAccess())
                return;

            string lowerName = PlayerName.ToLower();
            NetPlayer target;
            List<NetPlayer> players = Server.FindPlayers(lowerName, NameSearchOption.Contains).Where(plr => !plr.Spectating).ToList();

            if (players.Count == 0)
            {
                SendMessage("There is no player with this name.", LogMessageType.Error);
                return;
            }

            if (players.Count > 1)
            {
                SendMessage("Found more than 1 player with this name. Try be more specific or use /spectateid and specify their id instead.", LogMessageType.Error);
                return;
            }

            target = players.First();

            if (target == Caller)
            {
                SendMessage("You can't spectate yourself.", LogMessageType.Error);
                return;
            }

            Caller.Spectate(target);
        }
    }

    [Command("Spectate by id", "spectateid", "Start spectating another player by their id.")]
    [CommandPermission("zenith.spectate.spectateid", DefaultRanks.Member)]
    [RequireCaller]
    public class SpectateIdCommand : BaseCommand
    {
        [CommandArgument("Player id")]
        public int PlayerId { get; set; }

        public override void Handle(string[] args)
        {
            if (!HasAccess())
                return;

            NetPlayer target = Server.FindPlayer(PlayerId);

            if (target == null || target.Spectating)
            {
                SendMessage("There is no player with this id.", LogMessageType.Error);
                return;
            }

            if (target == Caller)
            {
                SendMessage("You can't spectate yourself.", LogMessageType.Error);
                return;
            }

            Caller.Spectate(target);
        }
    }
}
