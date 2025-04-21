using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Lumina.Excel;
using Reggiex.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Reggiex.Configs;

public class ConfigWindow : Window
{
    private Config Config { get; set; }
    private Dictionary<ushort, HashSet<string>> CommandsByEmoteId { get; init; }

    public ConfigWindow(Config config, ExcelSheet<Emote> emoteSheet) : base("Reggiex Config##configWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(400, 250),
            MaximumSize = new(float.MaxValue, float.MaxValue)
        };

        Config = config;

        CommandsByEmoteId = emoteSheet.Where(s => s.TextCommand.IsValid).ToDictionary(s => Convert.ToUInt16(s.RowId), s => {
            var textCommand = s.TextCommand.Value;
            var commands = new HashSet<string>();
            if (!textCommand.Alias.IsEmpty)
            {
                commands.Add(textCommand.Alias.ToString());
            }
            commands.Add(textCommand.Command.ToString());
            return commands;
        });
    }

    public override void Draw()
    {
        var globalEnabled = Config.Enabled;
        if (ImGui.Checkbox($"Enable Globally###Enabled", ref globalEnabled))
        {
            Config.Enabled = globalEnabled;
            Config.Save();
        }

        using (ImRaii.TabBar("configTabs"))
        {
            using (var tab = ImRaii.TabItem("Chat###chatConfigsTab"))
            {
                if (tab)
                {
                    ImGui.SameLine(ImGui.GetWindowWidth() - 75);
                    if (ImGui.Button("New Entry###newChatConfig"))
                    {
                        Config.ChatConfigs.Add(new());
                        Config.Save();
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Tip: /echo command can be used to test the different configs without sending messages");
                    }

                    using (var table = ImRaii.Table("chatConfigs", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY, new(ImGui.GetWindowWidth() - ImGui.GetCursorPosX(), ImGui.GetWindowHeight() - 95)))
                    {
                        if (table)
                        {
                            ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.None, 0.5f);
                            ImGui.TableSetupColumn("Pattern", ImGuiTableColumnFlags.None, 5);
                            ImGui.TableSetupColumn("Replacement", ImGuiTableColumnFlags.None, 5);
                            ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.None, 0.5f);

                            ImGui.TableSetupScrollFreeze(0, 1);
                            ImGui.TableHeadersRow();

                            var chatConfigs = Config.ChatConfigs.OrderBy(c => c.Priority).ToList();
                            var clipper = UIListClipper.Build();

                            clipper.Begin(chatConfigs.Count, 27);
                            while (clipper.Step())
                            {
                                for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                                {
                                    var chatConfig = chatConfigs.ElementAt(i);
                                    var hash = chatConfig.GetHashCode();

                                    if (ImGui.TableNextColumn())
                                    {
                                        var enabled = chatConfig.Enabled;
                                        if (ImGui.Checkbox($"###chatConfig{hash}Enabled", ref enabled))
                                        {
                                            chatConfig.Enabled = enabled;
                                            Config.Save();
                                        }
                                        if (ImGui.IsItemHovered())
                                        {
                                            ImGui.SetTooltip("Enable");
                                        }

                                        ImGui.SameLine();
                                        var priority = chatConfig.Priority;
                                        ImGui.SetNextItemWidth(50);
                                        if (ImGui.InputInt($"###chatConfig{hash}Priority", ref priority, 0))
                                        {
                                            chatConfig.Priority = priority;
                                            Config.Save();
                                        }
                                        if (ImGui.IsItemHovered())
                                        {
                                            ImGui.SetTooltip("Priority");
                                        }
                                    }

                                    if (ImGui.TableNextColumn())
                                    {
                                        var pattern = chatConfig.Pattern;
                                        ImGui.SetNextItemWidth(-1);
                                        if (ImGui.InputText($"###chatConfig{hash}Pattern", ref pattern, ushort.MaxValue))
                                        {
                                            chatConfig.Pattern = pattern;
                                            Config.Save();
                                        }
                                        if (ImGui.IsItemHovered())
                                        {
                                            ImGui.SetTooltip("C# regex format");
                                        }
                                    }

                                    if (ImGui.TableNextColumn())
                                    {
                                        var replacement = chatConfig.Replacement;
                                        ImGui.SetNextItemWidth(-1);
                                        if (ImGui.InputText($"###chatConfig{hash}Replacement", ref replacement, ushort.MaxValue))
                                        {
                                            chatConfig.Replacement = replacement;
                                            Config.Save();
                                        }
                                        if (ImGui.IsItemHovered())
                                        {
                                            ImGui.SetTooltip("Supports capture group replacement with $<index>");
                                        }
                                    }

                                    if (ImGui.TableNextColumn())
                                    {
                                        var inline = chatConfig.Inline;
                                        if (ImGui.Checkbox($"###chatConfig{hash}Inline", ref inline))
                                        {
                                            chatConfig.Inline = inline;
                                            Config.Save();
                                        }
                                        if (ImGui.IsItemHovered())
                                        {
                                            ImGui.SetTooltip("Enable inline replacement");
                                        }
                                        ImGui.SameLine();
                                        if (ImGui.Button($"X###chatConfig{hash}remove") && ImGui.IsKeyDown(ImGuiKey.ModCtrl))
                                        {
                                            Config.ChatConfigs.Remove(chatConfig);
                                            Config.Save();
                                        }
                                        if (ImGui.IsItemHovered())
                                        {
                                            ImGui.SetTooltip("Hold CTRL to confirm");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            using (var tab = ImRaii.TabItem("Emote###emoteConfigsTab"))
            {
                if (tab)
                {
                    ImGui.SameLine(ImGui.GetWindowWidth() - 75);
                    if (ImGui.Button("New Entry###newEmoteConfig"))
                    {
                        Config.EmoteConfigs.Add(new());
                        Config.Save();
                    }

                    using (var table = ImRaii.Table("emoteConfigs", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY, new(ImGui.GetWindowWidth() - ImGui.GetCursorPosX(), ImGui.GetWindowHeight() - 95)))
                    {
                        if (table)
                        {
                            ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.None, 0.5f);
                            ImGui.TableSetupColumn("Instigator", ImGuiTableColumnFlags.None, 4);
                            ImGui.TableSetupColumn("Emotes", ImGuiTableColumnFlags.None, 4);
                            ImGui.TableSetupColumn("Command", ImGuiTableColumnFlags.None, 4);
                            ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.None, 0.5f);

                            ImGui.TableSetupScrollFreeze(0, 1);
                            ImGui.TableHeadersRow();

                            var clipper = UIListClipper.Build();

                            clipper.Begin(Config.EmoteConfigs.Count, 27);
                            while (clipper.Step())
                            {
                                for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                                {
                                    var emoteConfig = Config.EmoteConfigs.ElementAt(i);
                                    var hash = emoteConfig.GetHashCode();

                                    if (ImGui.TableNextColumn())
                                    {
                                        var enabled = emoteConfig.Enabled;
                                        if (ImGui.Checkbox($"###emoteConfig{hash}Enabled", ref enabled))
                                        {
                                            emoteConfig.Enabled = enabled;
                                            Config.Save();
                                        }
                                        if (ImGui.IsItemHovered())
                                        {
                                            ImGui.SetTooltip("Enable");
                                        }
                                    }

                                    if (ImGui.TableNextColumn())
                                    {
                                        var instigatorPattern = emoteConfig.InstigatorPattern;
                                        ImGui.SetNextItemWidth(-1);
                                        if (ImGui.InputText($"###emoteConfig{hash}SourcePattern", ref instigatorPattern, ushort.MaxValue))
                                        {
                                            emoteConfig.InstigatorPattern = instigatorPattern;
                                            Config.Save();
                                        }
                                        if (ImGui.IsItemHovered())
                                        {
                                            ImGui.SetTooltip("C# regex format");
                                        }
                                    }

                                    if (ImGui.TableNextColumn())
                                    {
                                        var emoteIds = emoteConfig.EmoteIds;
                                        var joinedCommands = string.Join(',', CommandsByEmoteId.Where(p => emoteIds.Contains(p.Key)).Select(c => c.Value.FirstOrDefault()));
                                        ImGui.SetNextItemWidth(-1);
                                        if (ImGui.InputText($"###emoteConfig{hash}JoinedCommands", ref joinedCommands, 255))
                                        {
                                            var commands = joinedCommands.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                                            emoteConfig.EmoteIds = [.. CommandsByEmoteId.Where(c => c.Value.Intersect(commands).Any()).Select(c => c.Key)];
                                            Config.Save();
                                        }
                                        if (ImGui.IsItemHovered())
                                        {
                                            ImGui.SetTooltip("Comma-separated list of emote commands to track");
                                        }
                                    }


                                    if (ImGui.TableNextColumn())
                                    {
                                        var command = emoteConfig.Command;
                                        ImGui.SetNextItemWidth(-1);
                                        if (ImGui.InputText($"###emoteConfig{hash}Command", ref command, ushort.MaxValue))
                                        {
                                            emoteConfig.Command = command;
                                            Config.Save();
                                        }
                                        if (ImGui.IsItemHovered())
                                        {
                                            ImGui.SetTooltip("Supports capture group replacement with $<index>");
                                        }
                                    }

                                    if (ImGui.TableNextColumn())
                                    {
                                        if (ImGui.Button($"X###emoteConfig{hash}remove") && ImGui.IsKeyDown(ImGuiKey.ModCtrl))
                                        {
                                            Config.EmoteConfigs.Remove(emoteConfig);
                                            Config.Save();
                                        }
                                        if (ImGui.IsItemHovered())
                                        {
                                            ImGui.SetTooltip("Hold CTRL to confirm");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
    }
}
