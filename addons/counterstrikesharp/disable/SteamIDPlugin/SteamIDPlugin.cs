using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;

namespace SteamIDPlugin;

public class SteamIDPlugin : BasePlugin
{
    public override string ModuleName => "Steam ID 显示插件";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Your Name";
    public override string ModuleDescription => "显示玩家的SteamID";

    public override void Load(bool hotReload)
    {
        Console.WriteLine("[SteamIDPlugin] 插件已加载");
        
        RegisterAllAttributes();
        
        RegisterListener<Listeners.OnClientConnected>(OnClientConnected);
    }

    private void OnClientConnected(int playerSlot)
    {
        var player = Utilities.GetPlayerFromSlot(playerSlot);
        if (player == null || !player.IsValid) return;
        
        // 显示玩家的SteamID
        string steamId2 = player.SteamID.ToString();
        string steamId3 = player.SteamID.ToString();
        
        Console.WriteLine($"玩家 {player.PlayerName} 连接, SteamID: {steamId2}");
        Server.PrintToChatAll($" {ChatColors.Green}[Steam ID]{ChatColors.Default} 玩家 {player.PlayerName} 的 SteamID: {steamId2}");
    }

    [ConsoleCommand("css_myid", "显示你的SteamID")]
    public void OnMyIdCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null) 
        {
            Console.WriteLine("此命令需要在游戏中执行");
            return;
        }
        
        string steamId2 = player.SteamID.ToString();
        player.PrintToChat($" {ChatColors.Green}[Steam ID]{ChatColors.Default} 你的 SteamID: {steamId2}");
        Console.WriteLine($"玩家 {player.PlayerName} 的 SteamID: {steamId2}");
    }

    [ConsoleCommand("css_showadmins", "显示管理员配置")]
    public void OnShowAdminsCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null) return;
        
        player.PrintToChat($" {ChatColors.Green}[Admin]{ChatColors.Default} 正在检查管理员配置...");
        
        // 检查玩家是否有管理员权限
        bool isAdmin = AdminManager.PlayerHasPermissions(player, "@css/generic");
        player.PrintToChat($" {ChatColors.Green}[Admin]{ChatColors.Default} 你{(isAdmin ? "有" : "没有")}管理员权限");
        
        // 显示当前玩家拥有的所有标志
        var flags = AdminManager.GetPlayerPermissions(player);
        if (flags != null && flags.Count > 0)
        {
            player.PrintToChat($" {ChatColors.Green}[Admin]{ChatColors.Default} 你的权限标志: {string.Join(", ", flags)}");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Green}[Admin]{ChatColors.Default} 你没有任何权限标志");
        }
    }
} 