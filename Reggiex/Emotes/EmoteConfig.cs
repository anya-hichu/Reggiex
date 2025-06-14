using System;
using System.Collections.Generic;

namespace Reggiex.Emotes;

[Serializable]
public class EmoteConfig
{
    public bool Enabled { get; set; } = false;
    public int Priority { get; set; } = 0;
    public string InstigatorPattern { get; set; } = string.Empty;

    public bool CheckTargetSelf { get; set; } = true;
    public bool CheckInstigatorNotTarget { get; set; } = true;


    public HashSet<ushort> EmoteIds { get; set; } = [];
    public string Command { get; set; } = string.Empty;
}
