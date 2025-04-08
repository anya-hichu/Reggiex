using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Reggiex.UI;
using System.Linq;

namespace Reggiex.Configs;

public class ConfigWindow : Window
{
    private Config Config { get; set; }

    public ConfigWindow(Config config) : base("Reggiex Config##configWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(400, 250),
            MaximumSize = new(float.MaxValue, float.MaxValue)
        };

        Config = config;
    }

    public override void Draw()
    {
        var globalEnabled = Config.Enabled;
        if (ImGui.Checkbox($"Enable Globally###Enabled", ref globalEnabled))
        {
            Config.Enabled = globalEnabled;
            Config.Save();
        }
        ImGuiComponents.HelpMarker("/echo command can be used to the different conditions without sending messages");


        ImGui.SameLine(ImGui.GetWindowWidth() - 100);

        if (ImGui.Button("New Condition###newCondition"))
        {
            Config.Conditions.Add(new());
            Config.Save();
        }

        using (var table = ImRaii.Table("conditions", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY, new(ImGui.GetWindowWidth() - ImGui.GetCursorPosX(), ImGui.GetWindowHeight() - 70)))
        {
            if (table)
            {
                ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.None, 0.5f);
                ImGui.TableSetupColumn("Pattern", ImGuiTableColumnFlags.None, 5);
                ImGui.TableSetupColumn("Replacement", ImGuiTableColumnFlags.None, 5);
                ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.None, 0.5f);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                var conditions = Config.Conditions.OrderBy(c => c.Priority).ToList();
                var clipper = UIListClipper.Build();

                clipper.Begin(conditions.Count, 27);
                while (clipper.Step())
                {
                    for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    {
                        var condition = conditions.ElementAt(i);
                        var hash = condition.GetHashCode();

                        if (ImGui.TableNextColumn())
                        {
                            var enabled = condition.Enabled;
                            if (ImGui.Checkbox($"###condition{hash}Enabled", ref enabled))
                            {
                                condition.Enabled = enabled;
                                Config.Save();
                            }
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip("Enable");
                            }

                            ImGui.SameLine();
                            var priority = condition.Priority;
                            ImGui.SetNextItemWidth(50);
                            if (ImGui.InputInt($"###condition{hash}Priority", ref priority, 0))
                            {
                                condition.Priority = priority;
                                Config.Save();
                            }
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip("Priority");
                            }
                        }

                        if (ImGui.TableNextColumn())
                        {
                            var pattern = condition.Pattern;
                            ImGui.SetNextItemWidth(-1);
                            if (ImGui.InputText($"###condition{hash}Pattern", ref pattern, ushort.MaxValue))
                            {
                                condition.Pattern = pattern;
                                Config.Save();
                            }
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip("C# regex format");
                            }
                        }

                        if (ImGui.TableNextColumn())
                        {
                            var replacement = condition.Replacement;
                            ImGui.SetNextItemWidth(-1);
                            if (ImGui.InputText($"###condition{hash}Replacement", ref replacement, ushort.MaxValue))
                            {
                                condition.Replacement = replacement;
                                Config.Save();
                            }
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip("Supports capture group replacement with $<index>");
                            }
                        }

                        if (ImGui.TableNextColumn())
                        {
                            var inline = condition.Inline;
                            if (ImGui.Checkbox($"###condition{hash}Inline", ref inline))
                            {
                                condition.Inline = inline;
                                Config.Save();
                            }
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip("Enable inline replacement");
                            }
                            ImGui.SameLine();
                            if (ImGui.Button($"X###condition{hash}remove") && ImGui.IsKeyDown(ImGuiKey.ModCtrl))
                            {
                                Config.Conditions.Remove(condition);
                                Config.Save();
                            }
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip("Remove (hold control)");
                            }
                        }
                    }
                }
            }
        }
    }
}
