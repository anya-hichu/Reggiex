using Dalamud.Configuration;
using Reggiex.Chat;
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

    #region deprecated
    [Obsolete("Moved to ChatConfigs in version 1")]
    public List<ChatConfig> Conditions { get; set; } = [];

    public void MaybeMigrate()
    {
        if (Version < LATEST)
        {
            if (Version < 1)
            {
                ChatConfigs.AddRange(Conditions);
                Conditions.Clear();
            }

            Version = LATEST;
            Save();
        }
    }

    #endregion
}
