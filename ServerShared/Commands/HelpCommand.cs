using System.Collections.Generic;
using System.Text;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;
using ServerShared.Player;

namespace ServerShared.Commands
{
    [Command("Help", "help", "Shows all available commands and their descriptions.")]
    [CommandPermission("zenith.help", DefaultRanks.Member)]
    public class HelpCommand : BaseCommand
    {
        public override void Handle(string[] args)
        {
            if (!HasAccess())
                return;

            var lines = new List<string>();

            foreach (var command in Parser.Commands)
            {
                if (Caller == null && command.RequireCaller)
                    continue;

                if (Caller != null && command.ConsoleOnly)
                    continue;

                // Only list commands the caller's rank is allowed to run (checks the command's permission node).
                if (!string.IsNullOrEmpty(command.CanExecute?.Invoke(command)))
                    continue;

                lines.Add($"{command.Name} [{string.Join(", ", command.Aliases.ToArray())}]: {command.Description}");
            }

            var builder = new StringBuilder();
            builder.AppendLine("Commands:");
            builder.Append(string.Join("\n", lines));

            SendMessage(builder.ToString(), LogMessageType.Info);
        }
    }
}
