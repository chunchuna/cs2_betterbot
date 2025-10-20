# BotMimic CSS - 安装指南

## 系统要求

- Counter-Strike 2 服务器
- CounterStrikeSharp (CSS) 框架已安装
- .NET 8.0 Runtime

## 快速安装

### 方法 1: 使用预编译版本

1. 将 `botmimiccss` 文件夹复制到你的服务器：
   ```
   csgo/addons/counterstrikesharp/plugins/botmimiccss/
   ```

2. 确保文件夹包含：
   - `BotMimicCSS.dll` (编译后的插件)
   - 所有 `.cs` 源文件
   - `BotMimicCSS.csproj` 项目文件
   - `README.md` 和 `INSTALL.md`

3. 重启服务器或热加载插件：
   ```
   css_plugins load botmimiccss
   ```

### 方法 2: 从源码编译

1. 进入插件目录：
   ```bash
   cd csgo/addons/counterstrikesharp/plugins/botmimiccss
   ```

2. 编译项目：
   ```bash
   dotnet build -c Release
   ```

3. 编译后的文件将位于：
   ```
   bin/Release/net8.0/BotMimicCSS.dll
   ```

4. 复制编译结果到插件目录或直接在原位置使用

5. 重启服务器或加载插件

## 目录结构

安装完成后，你的服务器应该有以下结构：

```
csgo/
├── addons/
│   └── counterstrikesharp/
│       ├── plugins/
│       │   └── botmimiccss/
│       │       ├── BotMimicCSS.cs          (主插件文件)
│       │       ├── DataStructures.cs       (数据结构)
│       │       ├── FileManager.cs          (文件管理)
│       │       ├── RecordManager.cs        (录制管理)
│       │       ├── PlaybackManager.cs      (播放管理)
│       │       ├── MenuManager.cs          (菜单管理)
│       │       ├── BotMimicCSS.csproj      (项目文件)
│       │       ├── README.md               (使用说明)
│       │       └── INSTALL.md              (本文件)
│       └── configs/
│           └── plugins/
│               └── BotMimicCSS/            (自动创建)
└── plugins/
    └── BotMimicCSS/
        └── records/                         (录像存储目录，自动创建)
            ├── default/
            │   └── de_dust2/
            │       └── 1234567890.rec
            └── surf/
                └── surf_utopia/
                    └── 1234567891.rec
```

## 权限配置

### 基本权限

编辑 `csgo/addons/counterstrikesharp/configs/admins.json`:

```json
{
  "YourSteamID64": {
    "identity": "YourSteamID64",
    "flags": ["@css/admin"]
  }
}
```

### 高级权限 (删除录像)

需要 root 权限才能删除录像：

```json
{
  "YourSteamID64": {
    "identity": "YourSteamID64",
    "flags": ["@css/root"]
  }
}
```

## 验证安装

1. 启动服务器

2. 在服务器控制台查看：
   ```
   [BotMimic] Plugin loaded successfully
   ```

3. 在游戏中输入：
   ```
   !mimic
   ```
   或
   ```
   css_mimic
   ```

4. 应该看到 Bot Mimic 菜单

## 首次使用

### 录制你的第一个动作

1. 进入服务器并确保你已出生

2. 打开聊天输入：
   ```
   !mimic
   ```

3. 选择 "Record New Movement"

4. 执行你想要录制的动作（移动、跳跃等）

5. 再次打开菜单 `!mimic` 并选择 "Save Recording"

6. 录像将自动保存到：
   ```
   csgo/plugins/BotMimicCSS/records/default/[地图名]/[时间戳].rec
   ```

### 让机器人播放录像

1. 添加一个机器人到服务器：
   ```
   bot_add
   ```

2. 打开菜单 `!mimic`

3. 导航到你的录像

4. 选择 "Play on Bot"

5. 选择一个机器人

6. 机器人现在会重复你录制的动作！

## 常见问题

### Q: 插件没有加载

**A:** 检查以下内容：
- CounterStrikeSharp 是否正确安装
- .NET 8.0 Runtime 是否已安装
- 插件文件是否在正确的目录
- 查看服务器控制台的错误信息

### Q: 没有权限使用命令

**A:** 
- 确保你的 SteamID 添加到了 `admins.json`
- 确保你有 `@css/admin` 权限
- 重启服务器使权限生效

### Q: 录像不保存

**A:**
- 检查目录权限，确保服务器可以写入文件
- 查看控制台是否有错误信息
- 确保磁盘空间充足

### Q: 机器人不移动

**A:**
- 确保录像是在同一张地图上录制的
- 确保机器人在正确的队伍
- 尝试重新加载录像
- 检查服务器的 tick rate 设置

### Q: 菜单显示不正常

**A:**
- 确保使用的是兼容版本的 CounterStrikeSharp
- 尝试使用控制台命令代替菜单
- 重启客户端

## 更新插件

1. 备份你的录像文件：
   ```
   csgo/plugins/BotMimicCSS/records/
   ```

2. 替换插件文件

3. 重新编译（如果从源码安装）

4. 重启服务器

5. 录像文件应该仍然兼容

## 卸载

1. 停止服务器

2. 删除插件文件夹：
   ```
   csgo/addons/counterstrikesharp/plugins/botmimiccss/
   ```

3. （可选）删除录像文件：
   ```
   csgo/plugins/BotMimicCSS/
   ```

4. （可选）从 `admins.json` 中移除相关权限配置

5. 启动服务器

## 技术支持

如遇问题：

1. 检查服务器控制台的详细错误信息
2. 确保使用最新版本的 CounterStrikeSharp
3. 验证所有文件完整性
4. 检查文件和目录权限

## 性能优化

对于大型服务器或长时间录像：

1. 定期清理旧的录像文件
2. 使用类别组织录像
3. 考虑使用 SSD 存储录像文件
4. 监控磁盘使用情况

## 下一步

- 阅读 [README.md](README.md) 了解详细功能
- 尝试录制不同类型的动作
- 使用书签标记关键点
- 创建自定义类别组织你的录像
- 与其他玩家分享你的录像文件

祝你使用愉快！

