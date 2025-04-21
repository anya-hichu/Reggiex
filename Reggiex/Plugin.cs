using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Reggiex.Configs;
using Reggiex.Chat;
using Lumina.Excel.Sheets;
using Reggiex.Emotes;
using Dalamud.Game;

namespace Reggiex;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;


    private const string CommandName = "/reggiex";
    private const string CommandHelpMessage = $"Available subcommands for {CommandName} are config, enable and disable";

    public Config Config { get; init; }

    public readonly WindowSystem WindowSystem = new("Reggiex");

    private ChatHook ChatHook { get; init; }
    private ConfigWindow ConfigWindow { get; init; }

    private EmoteHook EmoteHook { get; init; }

    public Plugin()
    {
        Config = PluginInterface.GetPluginConfig() as Config ?? new Config();
        Config.MaybeMigrate();
        ConfigWindow = new ConfigWindow(Config, DataManager.GetExcelSheet<Emote>()!);
        
        WindowSystem.AddWindow(ConfigWindow);
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = CommandHelpMessage
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUI;

        ChatHook = new(Config, GameInteropProvider, PluginLog);
        EmoteHook = new(new(SigScanner), ClientState, Config, GameInteropProvider, ObjectTable, PluginLog);
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(CommandName);
        EmoteHook.Dispose();
        ChatHook.Dispose();
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
