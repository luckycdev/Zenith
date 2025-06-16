using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ServerShared.Logging;

namespace ServerShared.Plugins
{
    public static class PluginManager
    {
        private static readonly List<IPlugin> loadedPlugins = new List<IPlugin>();

        public static void LoadPlugins()
        {
            if (!Directory.Exists("plugins"))
            {
                Directory.CreateDirectory("plugins");
                Logger.LogWarning("[Zenith] Plugins folder was not found, so one was created.");
                return;
            }

            var pluginFolders = Directory.GetDirectories("plugins");

            foreach (var folder in pluginFolders)
            {
                var dllFiles = Directory.GetFiles(folder, "*.dll");

                foreach (var dllPath in dllFiles)
                {
                    Assembly assembly;
                    try
                    {
                        assembly = Assembly.LoadFrom(dllPath);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"[Zenith] Failed to load assembly '{dllPath}': {ex.Message}");
                        continue;
                    }

                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract)
                        {
                            try
                            {
                                var plugin = (IPlugin)Activator.CreateInstance(type);
                                plugin.Initialize();
                                loadedPlugins.Add(plugin);
                                Logger.LogInfo($"[Zenith] Loaded plugin: {plugin.Name} version {plugin.Version} by {plugin.Author}");
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError($"[Zenith] Failed to initialize plugin {type.FullName}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            if (loadedPlugins.Count == 0)
            {
                Logger.LogInfo($"[Zenith] Finished loading plugins. No plugins are currently loaded.");
            }
            else
            {
                Logger.LogInfo($"[Zenith] Finished loading plugins. {loadedPlugins.Count} plugin(s) loaded.");
            }
        }

        public static void UnloadPlugins()
        {
            foreach (var plugin in loadedPlugins)
            {
                try
                {
                    plugin.Shutdown();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[Zenith] Error shutting down plugin {plugin.Name}: {ex.Message}");
                }
            }

            loadedPlugins.Clear();
        }

        public static IEnumerable<IPlugin> GetLoadedPlugins()
        {
            return loadedPlugins.AsReadOnly();
        }
        public static int GetLoadedPluginsCount()
        {
            return loadedPlugins.Count;
        }

        public static IEnumerable<(string Name, string Version, string Author)> GetLoadedPluginInfo()
        {
            return loadedPlugins
                .Select(p => (p.Name, p.Version, p.Author))
                .Distinct()
                .OrderBy(p => p.Name);
        }

        public static void ReloadAllPlugins()
        {
            UnloadPlugins();
            LoadPlugins();
        }

        public static bool ReloadPlugin(string pluginName)
        {
            var pluginsToReload = loadedPlugins
                .Where(p => p.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (pluginsToReload.Count == 0)
                return false; // none

            foreach (var plugin in pluginsToReload)
            {
                try
                {
                    plugin.Shutdown();
                    loadedPlugins.Remove(plugin);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[Zenith] Error shutting down plugin {plugin.Name}: {ex.Message}");
                }
            }

            bool reloaded = false;

            var pluginFolders = Directory.GetDirectories("plugins");

            foreach (var folder in pluginFolders)
            {
                var dllFiles = Directory.GetFiles(folder, "*.dll");

                foreach (var dllPath in dllFiles)
                {
                    Assembly assembly;
                    try
                    {
                        assembly = Assembly.LoadFrom(dllPath);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"[Zenith] Failed to load assembly '{dllPath}': {ex.Message}");
                        continue;
                    }

                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract)
                        {
                            try
                            {
                                var newPlugin = (IPlugin)Activator.CreateInstance(type);
                                if (!newPlugin.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase))
                                    continue;

                                newPlugin.Initialize();
                                loadedPlugins.Add(newPlugin);
                                Logger.LogInfo($"[Zenith] Reloaded plugin: {newPlugin.Name}");
                                reloaded = true;
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError($"[Zenith] Failed to initialize plugin {type.FullName}: {ex.Message}");
                            }
                        }
                    }
                }
            }

            return reloaded;
        }
    }
}
