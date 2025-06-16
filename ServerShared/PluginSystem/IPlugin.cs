using System;

namespace ServerShared.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        string Version { get; }
        string Author { get; }
        void Initialize();
        void Shutdown();
    }
}
