using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.String;
using Reggiex.Configs;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Reggiex.Chats;

// Source: https://github.com/Project-GagSpeak/client/blob/main/ProjectGagSpeak/UpdateMonitoring/Chat/ChatInputProcessor.cs

public unsafe class ChatHook : IDisposable
{
    private Config Config { get; init; }
    private IPluginLog PluginLog { get; init; }

    private unsafe delegate byte ProcessChatInputDelegate(IntPtr uiModule, byte** message, IntPtr a3);
    [Signature("E8 ?? ?? ?? ?? FE 86 ?? ?? ?? ?? C7 86 ?? ?? ?? ?? ?? ?? ?? ??", DetourName = nameof(ProcessChatInputDetour), Fallibility = Fallibility.Auto)]
    private Hook<ProcessChatInputDelegate> ProcessChatInputHook { get; set; } = null!;

    public ChatHook(Config config, IGameInteropProvider gameInteropProvider, IPluginLog pluginLog)
    {
        Config = config;
        PluginLog = pluginLog;


        gameInteropProvider.InitializeFromAttributes(this);
        ProcessChatInputHook.Enable();
    }

    public void Dispose()
    {
        ProcessChatInputHook?.Disable();
        ProcessChatInputHook?.Dispose();
    }

    private unsafe byte ProcessChatInputDetour(IntPtr uiModule, byte** rawMessage, IntPtr a3)
    {
        try
        {
            if (!Config.Enabled)
            {
                return ProcessChatInputHook.Original(uiModule, rawMessage, a3);
            }

            var seMessage = MemoryHelper.ReadSeStringNullTerminated((nint)(*rawMessage));
            var message = seMessage.ToString();
            if (message.IsNullOrWhitespace())
            {
                return ProcessChatInputHook.Original(uiModule, rawMessage, a3);
            }

            var modified = false;
            var currentMessage = message;
            foreach (var chatConfig in Config.ChatConfigs.Where(c => c.Enabled && !c.Pattern.IsNullOrWhitespace() && !c.Replacement.IsNullOrWhitespace()))
            {
                if (Regex.IsMatch(currentMessage, chatConfig.Pattern))
                {
                    var replacedMessage = Regex.Replace(currentMessage, chatConfig.Pattern, chatConfig.Replacement);
                    if (chatConfig.Inline)
                    {
                        currentMessage = replacedMessage;
                        modified = true;
                    }
                    else
                    {
                        if(!TrySendMessage(uiModule, replacedMessage, a3, out var _))
                        {
                            PluginLog.Debug($"Failed to send message: {replacedMessage}");
                        }
                    }
                }
            }

            if(!modified)
            {
                return ProcessChatInputHook.Original(uiModule, rawMessage, a3);
            }

            if (TrySendMessage(uiModule, currentMessage, a3, out var returnValue))
            {
                return returnValue;
            }
            else
            {
                PluginLog.Error("Message was longer than max message length");
                return ProcessChatInputHook.Original(uiModule, rawMessage, a3);
            }
        }
        catch (Exception e)
        {
            PluginLog.Error($"Error sending message to chat box: {e}");
        }
        return ProcessChatInputHook.Original(uiModule, rawMessage, a3);
    }

    private bool TrySendMessage(IntPtr uiModule, string message, IntPtr a3, out byte returnValue)
    {
        var builder = new SeStringBuilder();
        builder.Add(new TextPayload(message));
        var seString = builder.BuiltString;

        if (seString.TextValue.Length <= 500)
        {
            var utf8String = Utf8String.FromString(".");
            utf8String->SetString(seString.Encode());
            returnValue = ProcessChatInputHook.Original(uiModule, (byte**)((nint)utf8String).ToPointer(), a3);
            return true;
        }
        returnValue = default;
        return false;
    }
}
