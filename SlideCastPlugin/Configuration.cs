using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Numerics;

namespace SlideCastPlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool Enabled { get; set; } = true;
        public int SlideTime { get; set; } = 50;
        public Vector4 SlideCol { get; set; } = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}