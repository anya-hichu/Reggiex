using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.Shell;
using Reggiex.Configs;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Reggiex.Chats;

// Reference: https://github.com/Project-GagSpeak/client/blob/1.2.1.6/ProjectGagSpeak/UpdateMonitoring/Chat/ChatInputProcessor.cs

public unsafe class ChatHook : IDisposable
{
    private Config Config { get; init; }
    private IPluginLog PluginLog { get; init; }

    private Hook<ShellCommandModule.Delegates.ExecuteCommandInner>? ExecuteCommandInnerHook { get; init; }

    public ChatHook(Config config, IGameInteropProvider gameInteropProvider, IPluginLog pluginLog)
    {
        Config = config;
        PluginLog = pluginLog;
        ExecuteCommandInnerHook = gameInteropProvider.HookFromAddress<ShellCommandModule.Delegates.ExecuteCommandInner>(
            ShellCommandModule.MemberFunctionPointers.ExecuteCommandInner,
            DetourExecuteCommand
        );
        ExecuteCommandInnerHook.Enable();
    }

    public void Dispose()
    {
        ExecuteCommandInnerHook?.Dispose();
    }

    private unsafe void DetourExecuteCommand(ShellCommandModule* commandModule, Utf8String* rawMessage, UIModule* uiModule)
    {
        try
        {
            if (!Config.Enabled)
            {
                ExecuteCommandInnerHook!.Original(commandModule, rawMessage, uiModule);
                return;
            }

            var message = (*rawMessage).ToString();
            if (message.IsNullOrWhitespace())
            {
                ExecuteCommandInnerHook!.Original(commandModule, rawMessage, uiModule);
                return;
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
                        if(!TrySendMessage(commandModule, uiModule, replacedMessage))
                        {
                            PluginLog.Debug($"Failed to send message: {replacedMessage}");
                        }
                    }
                }
            }

            if(!modified)
            {
                ExecuteCommandInnerHook!.Original(commandModule, rawMessage, uiModule);
                return;
            }

            if (!TrySendMessage(commandModule, uiModule, currentMessage))
            {
                PluginLog.Error("Message was longer than max message length");
                ExecuteCommandInnerHook!.Original(commandModule, rawMessage, uiModule);
            }
            return;
        }
        catch (Exception e)
        {
            PluginLog.Error($"Error sending message to chat box: {e}");
        }
        ExecuteCommandInnerHook!.Original(commandModule, rawMessage, uiModule);
    }

    private bool TrySendMessage(ShellCommandModule* commandModule, UIModule* uiModule, string message)
    {
        var builder = new SeStringBuilder();
        builder.Add(new TextPayload(message));
        var seString = builder.BuiltString;

        if (seString.TextValue.Length <= 500)
        {
            var utf8String = Utf8String.FromString(".");
            utf8String->SetString(seString.Encode());
            ExecuteCommandInnerHook!.Original(commandModule, utf8String, uiModule);
            return true;
        }
        return false;
    }
}
