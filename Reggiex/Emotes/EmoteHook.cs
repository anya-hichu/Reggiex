using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Reggiex.Chats;
using Reggiex.Configs;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Reggiex.Emotes;

public class EmoteHook
{
    private ChatServer ChatServer { get; init; }
    private IClientState ClientState { get; init; }
    private Config Config { get; init; }
    private IGameInteropProvider GameInteropProvider { get; init; }
    private IPluginLog PluginLog { get; init; }
  
    private IObjectTable ObjectTable { get; init; }


    public delegate void OnEmoteFuncDelegate(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2);

    private Hook<OnEmoteFuncDelegate>? HookEmote { get; init; }

    public EmoteHook(ChatServer chatServer, IClientState clientState, Config config, IGameInteropProvider gameInteropProvider, IObjectTable objectTable, IPluginLog pluginLog)
    {
        ChatServer = chatServer;
        ClientState = clientState;
        Config = config;
        GameInteropProvider = gameInteropProvider;
        ObjectTable = objectTable;
        PluginLog = pluginLog;

        try
        {
            HookEmote = GameInteropProvider.HookFromSignature<OnEmoteFuncDelegate>("E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 4C 89 74 24", OnEmoteDetour);
            HookEmote.Enable();
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Failed to hook emotes");
        }
    }

    public void Dispose()
    {
        HookEmote?.Dispose();
    }

    private void OnEmoteDetour(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2)
    {
        if (Config.Enabled)
        {
            var localPlayer = ClientState.LocalPlayer;
            if (localPlayer != null && targetId == localPlayer.GameObjectId && ObjectTable.FirstOrDefault(x => (ulong)x.Address == instigatorAddr) is IPlayerCharacter instigator && instigator.GameObjectId != targetId)
            {
                foreach (var emoteConfig in Config.EmoteConfigs.Where(c => c.Enabled && c.EmoteIds.Contains(emoteId)))
                {
                    if (emoteConfig.InstigatorPattern.IsNullOrWhitespace())
                    {
                        ChatServer.SendMessage(emoteConfig.Command);
                    }
                    else
                    {
                        var instigatorFullName = $"{instigator.Name}@{instigator.HomeWorld.Value.Name}";
                        if (Regex.IsMatch(instigatorFullName, emoteConfig.InstigatorPattern))
                        {
                            var replacedCommand = Regex.Replace(instigatorFullName, emoteConfig.InstigatorPattern, emoteConfig.Command);
                            ChatServer.SendMessage(replacedCommand);
                        }
                    }
                }
            }
        }

        HookEmote?.Original(unk, instigatorAddr, emoteId, targetId, unk2);
    }
}

