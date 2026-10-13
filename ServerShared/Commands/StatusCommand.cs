using System.Linq;
using System.Text;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;
using ServerShared.Player;

namespace ServerShared.Commands
{
    [Command("Status", "status", "Show server status and connected players.")]
    [ConsoleOnly]
    public class StatusCommand : BaseCommand
    {
        public override void Handle(string[] args)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Host: {Server.Peer.Configuration.LocalAddress}:{Server.Port}");
            builder.AppendLine($"Server name: {Server.Name}");
            builder.AppendLine($"Connected players: {Server.Players.Count}/{Server.MaxPlayers}");

            foreach (var player in Server.Players.Values)
            {
                builder.AppendLine($"- {player.Name} ({player.Identity}) (Rank: {player.RankName})");
            }

            builder.AppendLine($"Zenith Version {SharedConstants.ZenithVersion} (GOIMP Version {SharedConstants.Version})");

            SendMessage(builder.ToString().Trim(), LogMessageType.Info);
        }
    }
}
