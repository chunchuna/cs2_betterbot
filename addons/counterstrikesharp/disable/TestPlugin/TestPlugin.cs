using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;

namespace TestPlugin;

public class TestPlugin : BasePlugin
{
    public override string ModuleName => "测试插件";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Your Name";
    public override string ModuleDescription => "CS2测试插件 - 显示加载成功消息";

    public override void Load(bool hotReload)
    {
        // 插件加载时在聊天框输出消息
        Server.PrintToChatAll($" {ChatColors.Green}[测试插件]{ChatColors.Default} 插件加载成功！");
        
        // 如果是热重载，也显示消息
        if (hotReload)
        {
            Server.PrintToChatAll($" {ChatColors.Yellow}[测试插件]{ChatColors.Default} 插件已热重载");
        }
        
        Console.WriteLine("[TestPlugin] 插件已加载");
        
        // 注册监听器，显示玩家SteamID
        RegisterListener<Listeners.OnClientConnected>(OnClientConnected);
        
        // 控制台输出当前配置路径信息
        Console.WriteLine($"[TestPlugin] 配置文件路径: {ModuleDirectory}");
        Console.WriteLine($"[TestPlugin] 游戏根目录: {Server.GameDirectory}");
    }

    private void OnClientConnected(int playerSlot)
    {
        var player = Utilities.GetPlayerFromSlot(playerSlot);
        if (player == null || !player.IsValid) return;
        
        // 显示玩家的SteamID
        string steamId = player.SteamID.ToString();
        
        Console.WriteLine($"玩家 {player.PlayerName} 连接, SteamID: {steamId}");
        Server.PrintToChatAll($" {ChatColors.Green}[测试插件]{ChatColors.Default} 玩家 {player.PlayerName} 的 SteamID: {steamId}");
        
        // 检查是否是管理员
        if (AdminManager.PlayerHasPermissions(player, "@css/generic"))
        {
            player.PrintToChat($" {ChatColors.Green}[测试插件]{ChatColors.Default} 你拥有管理员权限");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Red}[测试插件]{ChatColors.Default} 你没有管理员权限");
        }
    }

    public override void Unload(bool hotReload)
    {
        Console.WriteLine("[TestPlugin] 插件已卸载");
    }
    
    // 添加一个测试命令
    [ConsoleCommand("css_test", "测试插件命令")]
    public void OnTestCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null) return;
        
        player.PrintToChat($" {ChatColors.Green}[测试插件]{ChatColors.Default} 测试命令执行成功！");
    }
    
    // 添加一个显示Steam ID的命令
    [ConsoleCommand("css_myid", "显示你的SteamID")]
    public void OnMyIdCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null) 
        {
            Console.WriteLine("此命令需要在游戏中执行");
            return;
        }
        
        string steamId = player.SteamID.ToString();
        player.PrintToChat($" {ChatColors.Green}[测试插件]{ChatColors.Default} 你的 SteamID: {steamId}");
        Console.WriteLine($"玩家 {player.PlayerName} 的 SteamID: {steamId}");
    }
} 