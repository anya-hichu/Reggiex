using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace Reggiex.Configs;

[Serializable]
public class Config : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool Enabled { get; set; } = true;

    public List<ConditionConfig> Conditions { get; set; } = [];

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
