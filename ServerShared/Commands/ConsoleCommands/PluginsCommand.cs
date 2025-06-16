using System.Linq;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Plugins;
using ServerShared.Logging;

namespace ServerShared.Commands.ConsoleCommands
{
    [Command("Plugins", new[] { "plugins" }, "Manage plugins (list or reload).")]
    public class PluginsCommand : ConsoleCommand
    {
        public override void Handle(string[] args)
        {
            if (args.Length == 0)
            {
                SendMessage("[Zenith] Usage: 'plugins list', 'plugins reload' or 'plugins reload [plugin name]'", LogMessageType.Error);
                return;
            }

            string subCommand = args[0].ToLower();

            if (subCommand == "list")
            {
                var plugins = PluginManager.GetLoadedPluginInfo().ToList();
                if (plugins.Count == 0)
                {
                    SendMessage("[Zenith] No plugins are currently loaded.", LogMessageType.Warning);
                    return;
                }

                SendMessage($"[Zenith] Loaded plugins: ({plugins.Count})", LogMessageType.Info);
                foreach (var (name, version, author) in plugins)
                    SendMessage($"[Zenith] - {name} version {version} by {author}", LogMessageType.Info);
            }
            else if (subCommand == "reload")
            {
                if (args.Length == 1)
                {
                    PluginManager.ReloadAllPlugins();
                    SendMessage($"[Zenith] Reloaded {PluginManager.GetLoadedPluginsCount()} plugin(s).", LogMessageType.Info);
                }
                else
                {
                    string pluginName = string.Join(" ", args.Skip(1)).Trim();
                    bool success = PluginManager.ReloadPlugin(pluginName);

                    if (!success)
                        SendMessage($"[Zenith] Plugin '{pluginName}' was not found or failed to reload.", LogMessageType.Error);
                }
            }
            else
            {
                SendMessage("[Zenith] Invalid subcommand. Use 'plugins list', 'plugins reload' or 'plugins reload [plugin name]'", LogMessageType.Error);
            }
        }
    }
}
