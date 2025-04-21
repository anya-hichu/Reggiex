using System;

namespace Reggiex.Chat;

[Serializable]
public class ChatConfig
{
    public bool Enabled { get; set; } = false;
    public int Priority { get; set; } = 0;
    public string Pattern { get; set; } = string.Empty;
    public string Replacement { get; set; } = string.Empty;
    public bool Inline { get; set; } = false;
}
