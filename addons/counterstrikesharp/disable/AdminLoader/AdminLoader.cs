using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using System.Reflection;

namespace AdminLoader;

public class AdminLoader : BasePlugin
{
    public override string ModuleName => "管理员加载器";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Helper";
    public override string ModuleDescription => "绕过权限限制的插件加载器";

    public override void Load(bool hotReload)
    {
        Console.WriteLine("[AdminLoader] 插件已加载");
        
        // 在加载时输出当前目录信息
        Console.WriteLine($"[AdminLoader] 模块目录: {ModuleDirectory}");
        Console.WriteLine($"[AdminLoader] 游戏目录: {Server.GameDirectory}");
        
        // 在聊天框中显示加载信息
        Server.NextFrame(() => 
        {
            Server.PrintToChatAll($" \x04[管理员加载器]\x01 插件已加载，输入 !reload 可以重新加载插件");
        });
    }
    
    [ConsoleCommand("css_test_reload", "测试重新加载插件")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnReloadCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player != null)
        {
            player.PrintToChat($" \x04[管理员加载器]\x01 正在尝试重新加载插件...");
        }
        
        // 尝试通过反射调用插件管理器的重新加载方法
        try
        {
            // 获取PluginManager类型
            Type? pluginManagerType = Type.GetType("CounterStrikeSharp.API.Core.PluginManager, CounterStrikeSharp.API");
            if (pluginManagerType != null)
            {
                // 获取Instance属性
                PropertyInfo? instanceProperty = pluginManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProperty != null)
                {
                    // 获取PluginManager实例
                    object? pluginManager = instanceProperty.GetValue(null);
                    if (pluginManager != null)
                    {
                        // 获取ReloadPlugins方法
                        MethodInfo? reloadMethod = pluginManagerType.GetMethod("ReloadPlugins", BindingFlags.Public | BindingFlags.Instance);
                        if (reloadMethod != null)
                        {
                            // 调用ReloadPlugins方法
                            reloadMethod.Invoke(pluginManager, new object[] { });
                            
                            if (player != null)
                            {
                                player.PrintToChat($" \x04[管理员加载器]\x01 插件重新加载成功！");
                            }
                            Console.WriteLine("[AdminLoader] 插件重新加载成功");
                            return;
                        }
                    }
                }
            }
            
            if (player != null)
            {
                player.PrintToChat($" \x02[管理员加载器]\x01 无法找到插件管理器，重新加载失败");
            }
            Console.WriteLine("[AdminLoader] 无法找到插件管理器，重新加载失败");
        }
        catch (Exception ex)
        {
            if (player != null)
            {
                player.PrintToChat($" \x02[管理员加载器]\x01 重新加载插件时发生错误");
            }
            Console.WriteLine($"[AdminLoader] 重新加载插件时发生错误: {ex.Message}");
        }
    }
    
    [ChatCommand("reload", "重新加载插件")]
    public void OnChatReloadCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null) return;
        
        OnReloadCommand(player, command);
    }
} 