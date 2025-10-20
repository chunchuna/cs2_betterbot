# CS2 测试插件

## 功能描述
这是一个简单的CS2测试插件，用于验证CounterStrikeSharp框架是否正常工作。

## 功能特性
- ✅ 插件加载时在聊天框显示"插件加载成功"
- ✅ 提供测试命令 `!test` 或控制台命令 `css_test`

## 安装方法
1. 将整个 `TestPlugin` 文件夹放置在 `csgo/addons/counterstrikesharp/plugins/` 目录下
2. 编译项目生成 DLL 文件（如果需要）
3. 重启服务器或使用 `css_plugins reload` 命令重载插件

## 编译方法
```bash
cd counterstrikesharp/plugins/TestPlugin
dotnet build
```

编译后的DLL文件会生成在 `bin` 目录下。

## 使用说明
- 插件加载后会自动在聊天框显示绿色的加载成功消息
- 玩家可以在聊天框输入 `!test` 或在控制台输入 `css_test` 来测试插件命令

## 版本
- 版本: 1.0.0
- 需要: CounterStrikeSharp API
- 目标框架: .NET 8.0

## 注意事项
- 确保服务器已正确安装 CounterStrikeSharp
- 确保 .NET 8.0 运行时已安装 