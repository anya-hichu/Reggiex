using Dalamud.Configuration;
using Reggiex.Chats;
using Reggiex.Emotes;
using System;
using System.Collections.Generic;

namespace Reggiex.Configs;

[Serializable]
public class Config : IPluginConfiguration
{
    private static readonly int LATEST = 1;

    public int Version { get; set; } = LATEST;

    public bool Enabled { get; set; } = true;

    public List<ChatConfig> ChatConfigs { get; set; } = [];
    public List<EmoteConfig> EmoteConfigs { get; set; } = [];

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
