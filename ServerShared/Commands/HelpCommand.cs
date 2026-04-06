using System.Text;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;
using ServerShared.Player;

namespace ServerShared.Commands
{
    [Command("Help", "help", "Shows all available commands and their descriptions.")]
    [CommandPermission("zenith.help", AccessLevel.Player)]
    public class HelpCommand : BaseCommand
    {
        public override void Handle(string[] args)
        {
            if (!HasAccess())
                return;

            var builder = new StringBuilder();

            builder.AppendLine("Commands:");

            for (int i = 0; i < Parser.Commands.Count; ++i)
            {
                var command = Parser.Commands[i];

                if (!Server.PlayerCommands.CanCurrentCallerRunCommand(command))
                    continue;

                if (Caller == null && command.RequireCaller)
                    continue;

                builder.Append($"{command.Name} [{string.Join(", ", command.Aliases.ToArray())}]: {command.Description}");

                if (i < Parser.Commands.Count - 1)
                    builder.AppendLine();
            }

            SendMessage(builder.ToString(), LogMessageType.Info);
        }
    }
}
