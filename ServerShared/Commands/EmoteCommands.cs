using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Player;
using UnityEngine;

namespace ServerShared.Commands
{
    [Command("Shrug", "shrug", "Writes an optional message followed by ¯\\_(ツ)_/¯.")]
    [CommandPermission("zenith.emote.shrug", AccessLevel.Player)]
    [RequireCaller]
    public class ShrugCommand : BaseCommand
    {
        public override void Handle(string[] args)
        {
            if (!HasAccess())
                return;

            string prefix = string.Join(" ", args);
            prefix = prefix.Trim();

            Server.BroadcastChatMessage($"{prefix} ¯\\_(ツ)_/¯", Color.white, Caller);
        }
    }

    [Command("Tableflip", "tableflip", "Writes an optional message followed by (╯°□°）╯︵ ┻━┻.")]
    [CommandPermission("zenith.emote.tableflip", AccessLevel.Player)]
    [RequireCaller]
    public class TableflipCommand : BaseCommand
    {
        public override void Handle(string[] args)
        {
            if (!HasAccess())
                return;

            string prefix = string.Join(" ", args);
            prefix = prefix.Trim();

            Server.BroadcastChatMessage($"{prefix} (╯°□°）╯︵ ┻━┻", Color.white, Caller);
        }
    }
}
