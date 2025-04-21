using System;
using System.Collections.Generic;

namespace Reggiex.Emotes;

[Serializable]
public class EmoteConfig
{
    public bool Enabled { get; set; } = false;
    public string InstigatorPattern { get; set; } = string.Empty;

    public HashSet<ushort> EmoteIds { get; set; } = [];
    public string Command { get; set; } = string.Empty;
}
