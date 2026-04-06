using System.Reflection;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;

namespace ServerShared
{
    public class PlayerCommandManager : BaseCommandManager<BaseCommand>
    {
        public PlayerCommandManager(GameServer server, Assembly executingAssembly)
            : base(server, prefix: "/", searchAssemblies: executingAssembly)
        {
        }

        protected override void OnParseError(object sender, string error)
        {
            CurrentCaller?.SendChatMessage(error, SharedConstants.ColorRed);
        }

        public void RegisterPluginCommands(Assembly pluginAssembly)
        {
            RegisterCommandsFromAssembly(pluginAssembly);
        }
    }

    public class ServerCommandManager : BaseCommandManager<BaseCommand>
    {
        public ServerCommandManager(GameServer server, Assembly executingAssembly)
            : base(server, prefix: "", searchAssemblies: executingAssembly)
        {
        }

        protected override void OnParseError(object sender, string error)
        {
            if (CurrentCaller != null)
                CurrentCaller.SendConsoleMessage(error, LogMessageType.Error);
            else
                Logger.LogError(error);
        }

        public void RegisterPluginCommands(Assembly pluginAssembly)
        {
            RegisterCommandsFromAssembly(pluginAssembly);
        }
    }
}
