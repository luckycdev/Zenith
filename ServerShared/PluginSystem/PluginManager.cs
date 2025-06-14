using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ServerShared.Plugins
{
    public class PluginManager
    {
        public string pluginPath = "plugins";
        private readonly List<IPlugin> loadedPlugins = new List<IPlugin>();

        public void LoadPlugins(string pluginsPath)
        {
            if (!Directory.Exists(pluginsPath))
            {
                Console.WriteLine($"[Zenith] Plugin folder not found: {pluginsPath}");
                return;
            }

            string[] dllFiles = Directory.GetFiles(pluginsPath, "*.dll");

            foreach (var dll in dllFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dll);
                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract)
                        {
                            var plugin = (IPlugin)Activator.CreateInstance(type);
                            plugin.Initialize();
                            loadedPlugins.Add(plugin);
                            Console.WriteLine($"[Zenith] Loaded plugin: {type.FullName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Zenith] Failed to load plugin '{dll}': {ex.Message}");
                }
            }
        }

        public void UnloadPlugins()
        {
            foreach (var plugin in loadedPlugins)
            {
                try
                {
                    plugin.Shutdown();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Zenith] Error shutting down plugin {plugin.Name}: {ex.Message}");
                }
            }

            loadedPlugins.Clear();
        }
    }
}
