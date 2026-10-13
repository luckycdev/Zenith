using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Player;
using UnityEngine;

namespace ServerShared.Commands
{
    [Command("Exit", new[] { "exit", "quit" }, "Shutdown the server.")]
    [ConsoleOnly]
    public class ExitCommand : BaseCommand
    {
        public override void Handle(string[] args)
        {
            Server.Stop();
        }
    }

    [Command("Say", "say", "Send a chat message to all players.")]
    [ConsoleOnly]
    public class SayCommand : BaseCommand
    {
        public override void Handle(string[] args)
        {
            string message = string.Join(" ", args);
            Server.BroadcastChatMessage(message, new Color(0.94f, 0.86f, 0.79f), 0, "Server");
        }
    }
}
