# BotMimic CSS - 中文使用说明

## 项目概述

BotMimic CSS 是将经典的 SourceMod Bot Mimic 插件移植到 Counter-Strike 2 的 CounterStrikeSharp 框架的完整实现。

### 功能特性

✅ **完整功能实现**
- 录制玩家动作（位置、角度、速度、按键输入）
- 机器人播放录像
- 书签系统（标记关键帧）
- 暂停/恢复录制
- 分类管理录像
- 交互式菜单系统
- 二进制文件格式（兼容原版）

### 项目结构

```
botmimiccss/
├── BotMimicCSS.cs           # 主插件类，处理事件和命令
├── DataStructures.cs        # 数据结构定义（FrameInfo, FileHeader等）
├── FileManager.cs           # 文件读写管理
├── RecordManager.cs         # 录制功能管理
├── PlaybackManager.cs       # 播放功能管理
├── MenuManager.cs           # 菜单系统
├── BotMimicCSS.csproj      # 项目配置文件
├── README.md               # 英文文档
├── INSTALL.md              # 安装指南
└── USAGE_CN.md             # 本文件
```

## 主要命令

### 管理员命令 (需要 @css/admin 权限)

| 命令 | 说明 | 示例 |
|------|------|------|
| `css_mimic` 或 `!mimic` | 打开主菜单 | `!mimic` |
| `css_record [名称] [分类]` | 开始录制 | `css_record 跳跃路线 bhop` |
| `css_stoprecord` | 停止并保存录制 | `css_stoprecord` |
| `css_pauserecord` | 暂停录制 | `css_pauserecord` |
| `css_resumerecord` | 恢复录制 | `css_resumerecord` |
| `css_savebookmark <名称>` | 保存书签 | `css_savebookmark 检查点1` |
| `css_playrecord <机器人名> <录像名>` | 播放录像 | `css_playrecord bot1 我的路线` |
| `css_stopmimic <机器人名>` | 停止播放 | `css_stopmimic bot1` |

### ROOT 命令 (需要 @css/root 权限)

| 命令 | 说明 | 示例 |
|------|------|------|
| `css_deleterecord <文件路径>` | 删除录像 | `css_deleterecord /path/to/record.rec` |

## 使用教程

### 1. 录制你的第一个动作

**方法 A: 使用菜单**
1. 在聊天框输入 `!mimic`
2. 选择 "Record New Movement"
3. 执行你的动作
4. 再次打开菜单 `!mimic`
5. 选择 "Save Recording"

**方法 B: 使用命令**
```
css_record 测试录像 default
// 执行动作
css_stoprecord
```

### 2. 保存书签

在录制过程中标记重要位置：
```
css_savebookmark 起跳点
css_savebookmark 落地点
css_savebookmark 终点
```

### 3. 让机器人播放录像

**使用菜单：**
1. `!mimic`
2. 浏览到你的录像
3. 选择 "Play on Bot"
4. 选择一个机器人

**使用命令：**
```
// 先添加机器人
bot_add

// 播放录像
css_playrecord bot_name 测试录像
```

### 4. 跳转到书签

在播放过程中：
1. 打开菜单 `!mimic`
2. 找到正在播放的录像
3. 选择 "Bookmarks"
4. 选择书签
5. 选择要跳转的机器人

## 数据结构说明

### FrameInfo (帧信息)
每一帧包含：
- `Buttons`: 按键状态 (ulong)
- `Origin`: 位置坐标
- `Velocity`: 速度
- `Angles`: 视角
- `NewWeapon`: 切换的武器
- `AdditionalFields`: 额外传送数据标志

### FileHeader (文件头)
- `RecordName`: 录像名称
- `TickCount`: 总帧数
- `BookmarkCount`: 书签数量
- `InitialPosition/Angles`: 起始位置和角度
- `Frames`: 所有帧数据
- `Bookmarks`: 所有书签

## 文件格式

### 二进制格式 (.rec)
```
[Magic Number: 0xDEADBEEF]
[Version: 0x02]
[Record Time]
[Record Name Length][Record Name]
[Initial Position (3 floats)]
[Initial Angles (2 floats)]
[Tick Count]
[Bookmark Count]
[Bookmarks Data...]
[Frames Data...]
  - Frame 1
    - Buttons
    - Velocities
    - Angles
    - Origin
    - Weapon
    - Additional Teleport (if any)
  - Frame 2
  - ...
```

### 存储位置
```
csgo/plugins/BotMimicCSS/records/
├── default/
│   └── de_dust2/
│       ├── 1234567890.rec
│       └── 1234567891.rec
├── bhop/
│   └── bhop_monster/
│       └── 1234567892.rec
└── surf/
    └── surf_utopia/
        └── 1234567893.rec
```

## API 说明

### 对于其他插件开发者

虽然这是一个独立插件，但你可以通过文件系统访问录像文件：

```csharp
// 读取录像文件
var fileManager = new FileManager(recordPath);
var error = fileManager.LoadRecordFromFile(path, category, out var fileHeader);

if (error == BMError.NoError && fileHeader != null)
{
    Console.WriteLine($"录像: {fileHeader.RecordName}");
    Console.WriteLine($"帧数: {fileHeader.TickCount}");
    Console.WriteLine($"书签: {fileHeader.BookmarkCount}");
}
```

## 技术细节

### 与 SourceMod 版本的差异

1. **语言**: SourcePawn → C#
2. **框架**: SourceMod → CounterStrikeSharp
3. **按钮类型**: PlayerButtons enum → ulong
4. **向量类型**: 使用 System.Numerics.Vector3
5. **武器切换**: 由于 CS2 限制，暂时无法完全实现
6. **菜单系统**: SourceMod MenuStyle → CSS ChatMenu

### 已知限制

1. **武器切换**: CS2 中 ActiveWeapon 属性是只读的，需要使用其他方法
2. **精确度**: 可能存在轻微的位置偏差
3. **性能**: 大型录像文件可能影响服务器性能

### 优化建议

1. 定期清理旧录像
2. 使用合理的快照间隔（默认10000帧）
3. 避免过长的录像（建议<10000帧）
4. 使用分类组织录像

## 故障排除

### 机器人不移动
- 确保录像在同一张地图录制
- 检查机器人队伍
- 查看控制台错误信息

### 录像不保存
- 检查磁盘权限
- 确保路径存在
- 查看控制台错误

### 编译错误
- 确保 .NET 8.0 SDK 已安装
- 确保 CounterStrikeSharp API 路径正确
- 运行 `dotnet clean` 后重新编译

## 开发计划

未来可能的改进：
- [ ] 改进武器切换机制
- [ ] 添加录像预览功能
- [ ] 支持录像合并
- [ ] 添加录像编辑功能
- [ ] 性能优化
- [ ] 添加录像压缩

## 贡献

欢迎提交 Pull Request 或报告问题！

## 鸣谢

- 原版 SourceMod Bot Mimic 插件作者：Peace-Maker
- CounterStrikeSharp 框架开发团队
- CS2 社区的支持和反馈

## 许可证

基于原版 Bot Mimic 的开源协议移植。

---

**享受使用 BotMimic CSS！** 🎮

