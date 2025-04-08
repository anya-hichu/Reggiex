using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Reggiex.Configs;
using Reggiex.Chat;
using Dalamud.Game;

namespace Reggiex;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;


    private const string CommandName = "/reggiex";
    private const string CommandHelpMessage = $"Available subcommands for {CommandName} are config, enable and disable";

    public Config Config { get; init; }

    public readonly WindowSystem WindowSystem = new("Reggiex");

    private ChatInputDetour ChatInputDetour { get; init; }
    private ConfigWindow ConfigWindow { get; init; }

    public Plugin()
    {
        Config = PluginInterface.GetPluginConfig() as Config ?? new Config();
        ConfigWindow = new ConfigWindow(Config);
        

        WindowSystem.AddWindow(ConfigWindow);
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = CommandHelpMessage
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUI;
        ChatInputDetour = new(Config, GameInteropProvider, PluginLog);
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(CommandName);
        ChatInputDetour.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        var subcommand = args.Split(" ", 2)[0];
        if (subcommand == "config")
        {
            ToggleConfigUI();
        }
        else if (subcommand == "enable")
        {
            Config.Enabled = true;
            Config.Save();
        }
        else if (subcommand == "disable")
        {
            Config.Enabled = false;
            Config.Save();
        }
        else
        {
            ChatGui.Print(CommandHelpMessage);
        }
    }


    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
}
