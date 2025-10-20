# BotMimic CSS - 快速开始指南

## 🎯 已修复的问题

✅ **录像立即显示** - 保存后立刻出现在菜单
✅ **Bot AI 禁用** - 自动禁用bot原生AI，确保完全受录像控制  
✅ **延迟应用** - 等待bot完全加载后再应用录像
✅ **路径修复** - 正确扫描所有分类文件夹

## 🚀 快速测试流程

### 第1步：重新加载插件

在服务器控制台输入：
```
css_plugins reload BotMimicCSS
```

你应该看到：
```
[BotMimic] Plugin loaded successfully
[BotMimic] Bot AI has been disabled for better playback
[BotMimic] Map started: de_dust2, loading records...
[BotMimic] Loading records from category 'default' for map 'de_dust2'
```

### 第2步：录制一个动作

```
!mimic          # 打开菜单
!1              # Record New Movement
```

现在你应该看到：
```
[BotMimic] Started recording!
```

然后：
- 走几步
- 跳一下
- 开几枪
- 等等...

### 第3步：保存录像

```
!mimic          # 再次打开菜单
!2              # Save Recording
```

你会看到：
```
[BotMimic] Recording saved! Use !mimic to play it.
```

### 第4步：立即播放

**不需要重新加载！直接：**

```
!mimic
```

现在你应该看到 **3个选项**：
```
!1 Record New Movement
!2 Stop All Bots
!3 Category: default    <-- 你的录像在这里！
```

继续：
```
!3              # 进入 default 分类
```

你会看到：
```
!1 < Back to Categories
!2 Record New Movement
!3 record_1729428000_1   <-- 你刚才保存的录像！
```

选择录像：
```
!3              # 选择你的录像
```

会显示录像详情：
```
!1 < Back to Records
!2 Play on Bot
!3 Add Bot and Play    <-- 选这个！
!4 Stop All Playing This
!5 Ticks: 234
!6 Recorded: 2024/10/20 20:00
```

添加bot并播放：
```
!3              # Add Bot and Play
```

选择队伍：
```
!1 < Back
!2 Terrorist        <-- 选这个
!3 Counter-Terrorist
```

输入：
```
!2              # 添加T bot
```

### 第5步：观察bot

你会看到消息：
```
[BotMimic] Adding T bot...
[BotMimic] Bot bot_name is now mimicking!
```

**bot现在应该会：**
- ✅ 不使用原生AI
- ✅ 完全按照你的录像移动
- ✅ 重复你的动作

## 🔧 如果bot还在使用原生AI

### 手动禁用bot AI

在服务器控制台输入：
```
bot_stop 1
bot_dont_shoot 1
bot_freeze 0
bot_crouch 0
bot_zombie 1
```

### 检查bot是否真的在mimicking

在服务器控制台查看：
```
[BotMimic] Bot bot_name is now mimicking!
```

## 🎮 使用现有bot播放

如果服务器已经有bot了：

```
!mimic
!3              # Category: default
!3              # 你的录像
!2              # Play on Bot (而不是Add Bot)
!1              # 选择一个现有的bot
```

## 🐛 调试信息

### 查看服务器控制台

应该看到这些日志：
```
[BotMimic] Plugin loaded successfully
[BotMimic] Bot AI has been disabled for better playback
[BotMimic] Started recording for PlayerName
[BotMimic] Saved recording for PlayerName to [路径]
[BotMimic] Recorded 234 ticks with 0 bookmarks
[BotMimic] Bot bot_name started mimicking record from [路径]
```

### 如果bot不移动

1. 确保bot存活
2. 确认录像在同一张地图
3. 查看控制台是否有错误
4. 尝试：
   ```
   css_stopmimic bot_name
   css_playrecord bot_name [录像路径]
   ```

## 📝 重要提示

1. **Bot AI 已全局禁用** - 插件加载时自动禁用
2. **每次spawn都会重新禁用** - 确保bot不会恢复AI
3. **延迟应用** - bot创建后等待0.5秒再应用录像
4. **重试机制** - 如果第一次没找到bot，会重试最多5秒

## ✨ 高级功能

### 使用书签

录制时：
```
css_savebookmark 起跳点
css_savebookmark 落地点
```

播放时可以跳转到书签！

### 暂停/恢复录制

```
css_pauserecord     # 暂停
# 准备下一个动作
css_resumerecord    # 继续录制
```

---

**现在重新加载插件并测试吧！** 🎮

