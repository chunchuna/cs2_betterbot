using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace SimpleTest;

public class SimpleTest : BasePlugin
{
    public override string ModuleName => "简单测试插件";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Your Name";
    public override string ModuleDescription => "最简单的CS2测试插件";

    public override void Load(bool hotReload)
    {
        // 插件加载时在控制台输出消息
        Console.WriteLine("[SimpleTest] 插件已加载!");
        
        // 在聊天框中显示消息
        Server.NextFrame(() => 
        {
            Server.PrintToChatAll($" \x04[简单测试]\x01 插件加载成功!");
        });
    }
} 