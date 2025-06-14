using System;

namespace ServerShared.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        void Initialize();
        void Shutdown();
    }
}
