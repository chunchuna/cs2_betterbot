using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using System.Numerics;

namespace BotMimicCSS;

public class BotMimicCSS : BasePlugin
{
    public override string ModuleName => "Bot Mimic CSS";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Converted from SourceMod version by Peace-Maker";
    public override string ModuleDescription => "Record and playback player movements with bots";

    private FileManager? _fileManager;
    private RecordManager? _recordManager;
    private PlaybackManager? _playbackManager;
    private MenuManager? _menuManager;

    private readonly Dictionary<int, ulong> _lastButtons = new();
    private readonly Dictionary<int, Vector3> _lastVelocity = new();
    private readonly Dictionary<int, QAngle> _lastAngles = new();

    public override void Load(bool hotReload)
    {
        // Initialize managers
        string recordPath = Path.Combine(Server.GameDirectory, "csgo", Constants.DEFAULT_RECORD_FOLDER);
        _fileManager = new FileManager(recordPath);
        _recordManager = new RecordManager(_fileManager);
        _playbackManager = new PlaybackManager(_fileManager);
        _menuManager = new MenuManager(this, _fileManager, _recordManager, _playbackManager);

        // Register event handlers
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterListener<Listeners.OnMapStart>(OnMapStart);

        // Register commands
        AddCommand("css_mimic", "Opens the bot mimic menu", CommandMimic);
        AddCommand("css_record", "Start recording", CommandRecord);
        AddCommand("css_stoprecord", "Stop recording", CommandStopRecord);
        AddCommand("css_pauserecord", "Pause recording", CommandPauseRecord);
        AddCommand("css_resumerecord", "Resume recording", CommandResumeRecord);
        AddCommand("css_savebookmark", "Save a bookmark", CommandSaveBookmark);
        AddCommand("css_playrecord", "Play a record on a bot", CommandPlayRecord);
        AddCommand("css_stopmimic", "Stop a bot from mimicking", CommandStopMimic);
        AddCommand("css_deleterecord", "Delete a record", CommandDeleteRecord);

        // Register tick handler
        RegisterListener<Listeners.OnTick>(OnTick);

        // Disable bot AI for better mimic playback
        Server.ExecuteCommand("bot_stop 1");
        Server.ExecuteCommand("bot_dont_shoot 1");
        Server.ExecuteCommand("bot_freeze 0");

        Server.PrintToConsole("[BotMimic] Plugin loaded successfully");
        Server.PrintToConsole("[BotMimic] Bot AI has been disabled for better playback");

        if (hotReload)
        {
            Server.PrintToConsole("[BotMimic] Hot reload detected, loading records for current map");
            LoadRecordsForCurrentMap();
        }
    }

    public override void Unload(bool hotReload)
    {
        _recordManager?.ClearAllSessions();
        _playbackManager?.ClearAllSessions();

        Server.PrintToConsole("[BotMimic] Plugin unloaded");
    }

    private void OnMapStart(string mapName)
    {
        Server.PrintToConsole($"[BotMimic] Map started: {mapName}, loading records...");
        LoadRecordsForCurrentMap();
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        // Disable AI for all bots to allow mimic control
        if (player.IsBot)
        {
            Server.NextFrame(() =>
            {
                // Ensure bot AI is disabled
                Server.ExecuteCommand("bot_stop 1");
                Server.ExecuteCommand("bot_dont_shoot 1");
            });
        }

        // If bot is mimicking, restart from beginning
        if (_playbackManager != null && _playbackManager.IsPlayerMimicking(player))
        {
            _playbackManager.ResetPlayback(player);
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        // Stop recording if player dies
        if (_recordManager != null && _recordManager.IsPlayerRecording(player))
        {
            _recordManager.StopRecording(player, save: true);
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Recording stopped due to death. Recording saved.");
        }

        // Stop mimicking if bot dies
        if (_playbackManager != null && _playbackManager.IsPlayerMimicking(player))
        {
            _playbackManager.StopPlayerMimic(player);
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        // Clean up on disconnect
        if (_recordManager != null && _recordManager.IsPlayerRecording(player))
        {
            _recordManager.StopRecording(player, save: false);
        }

        if (_playbackManager != null && _playbackManager.IsPlayerMimicking(player))
        {
            _playbackManager.StopPlayerMimic(player);
        }

        _lastButtons.Remove(player.Slot);
        _lastVelocity.Remove(player.Slot);
        _lastAngles.Remove(player.Slot);

        return HookResult.Continue;
    }

    private int _tickCounter = 0;

    private void OnTick()
    {
        if (_recordManager == null || _playbackManager == null)
            return;

        _tickCounter++;
        
        // Iterate through all player slots to ensure we catch bots
        for (int i = 0; i < 64; i++)
        {
            var player = Utilities.GetPlayerFromSlot(i);
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null)
                continue;

            var pawn = player.PlayerPawn.Value;

            // Get current state
            var buttons = pawn.MovementServices?.Buttons?.ButtonStates[0] ?? 0UL;
            var velocity = pawn.AbsVelocity != null ? new Vector3(pawn.AbsVelocity.X, pawn.AbsVelocity.Y, pawn.AbsVelocity.Z) : new Vector3();
            var angles = pawn.EyeAngles ?? new QAngle();

            // Recording
            if (_recordManager.IsPlayerRecording(player))
            {
                _recordManager.RecordFrame(player, buttons, velocity, angles);
            }

            // Playback - this is where the magic happens for bots
            if (_playbackManager.IsPlayerMimicking(player))
            {
                // Debug log every 100 ticks
                if (_tickCounter % 100 == 0)
                {
                    Server.PrintToConsole($"[BotMimic] OnTick: Processing bot {player.PlayerName} (IsBot={player.IsBot})");
                }
                
                if (_playbackManager.ProcessTick(player, out var frame) && frame != null)
                {
                    // Apply frame data to bot
                    ApplyFrameToPlayer(player, pawn, frame);
                }
                else if (_tickCounter % 100 == 0)
                {
                    Server.PrintToConsole($"[BotMimic] ProcessTick returned false or null frame for {player.PlayerName}");
                }
            }

            // Store last state
            _lastButtons[player.Slot] = buttons;
            _lastVelocity[player.Slot] = velocity;
            _lastAngles[player.Slot] = angles;
        }
    }

    private void ApplyFrameToPlayer(CCSPlayerController player, CCSPlayerPawn pawn, FrameInfo frame)
    {
        // Debug log every 100 ticks
        if (_tickCounter % 100 == 0)
        {
            Server.PrintToConsole($"[BotMimic] ApplyFrame to {player.PlayerName}: Buttons={frame.Buttons}, Origin={frame.Origin}");
        }
        
        // Set buttons - this makes the bot "press" the recorded buttons
        if (pawn.MovementServices?.Buttons != null)
        {
            pawn.MovementServices.Buttons.ButtonStates[0] = frame.Buttons;
        }

        // The position, velocity and angles are already set by PlaybackManager in ProcessTick
        // This ensures the bot follows the exact recorded path
    }

    [ConsoleCommand("css_mimic", "Opens the bot mimic menu")]
    [RequiresPermissions("@css/admin")]
    public void CommandMimic(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
        {
            command.ReplyToCommand("[BotMimic] This command can only be used by players");
            return;
        }

        _menuManager?.ShowMainMenu(player);
    }

    [ConsoleCommand("css_record", "Start recording")]
    [RequiresPermissions("@css/admin")]
    public void CommandRecord(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
        {
            command.ReplyToCommand("[BotMimic] This command can only be used by players");
            return;
        }

        if (_recordManager == null)
        {
            command.ReplyToCommand("[BotMimic] Record manager not initialized");
            return;
        }

        if (_recordManager.IsPlayerRecording(player))
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} You are already recording!");
            return;
        }

        if (player.PlayerPawn?.Value == null || !player.PawnIsAlive)
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} You must be alive to record!");
            return;
        }

        string recordName = command.ArgCount > 1 ? command.ArgByIndex(1) : $"record_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        string category = command.ArgCount > 2 ? command.ArgByIndex(2) : Constants.DEFAULT_CATEGORY;

        if (_recordManager.StartRecording(player, recordName, category))
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Started recording '{ChatColors.Lime}{recordName}{ChatColors.Default}'");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Failed to start recording");
        }
    }

    [ConsoleCommand("css_stoprecord", "Stop recording")]
    [RequiresPermissions("@css/admin")]
    public void CommandStopRecord(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
        {
            command.ReplyToCommand("[BotMimic] This command can only be used by players");
            return;
        }

        if (_recordManager == null)
        {
            command.ReplyToCommand("[BotMimic] Record manager not initialized");
            return;
        }

        if (!_recordManager.IsPlayerRecording(player))
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} You are not recording!");
            return;
        }

        if (_recordManager.StopRecording(player, save: true))
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Recording stopped and saved");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Failed to stop recording");
        }
    }

    [ConsoleCommand("css_pauserecord", "Pause recording")]
    [RequiresPermissions("@css/admin")]
    public void CommandPauseRecord(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
        {
            command.ReplyToCommand("[BotMimic] This command can only be used by players");
            return;
        }

        if (_recordManager == null)
        {
            command.ReplyToCommand("[BotMimic] Record manager not initialized");
            return;
        }

        if (_recordManager.PauseRecording(player))
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Recording paused");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Failed to pause recording");
        }
    }

    [ConsoleCommand("css_resumerecord", "Resume recording")]
    [RequiresPermissions("@css/admin")]
    public void CommandResumeRecord(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
        {
            command.ReplyToCommand("[BotMimic] This command can only be used by players");
            return;
        }

        if (_recordManager == null)
        {
            command.ReplyToCommand("[BotMimic] Record manager not initialized");
            return;
        }

        if (_recordManager.ResumeRecording(player))
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Recording resumed");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Failed to resume recording");
        }
    }

    [ConsoleCommand("css_savebookmark", "Save a bookmark")]
    [RequiresPermissions("@css/admin")]
    public void CommandSaveBookmark(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
        {
            command.ReplyToCommand("[BotMimic] This command can only be used by players");
            return;
        }

        if (_recordManager == null)
        {
            command.ReplyToCommand("[BotMimic] Record manager not initialized");
            return;
        }

        if (command.ArgCount < 2)
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Usage: css_savebookmark <name>");
            return;
        }

        string bookmarkName = command.ArgByIndex(1);

        if (_recordManager.SaveBookmark(player, bookmarkName))
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Saved bookmark '{ChatColors.Lime}{bookmarkName}{ChatColors.Default}'");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Failed to save bookmark");
        }
    }

    [ConsoleCommand("css_playrecord", "Play a record on a bot")]
    [RequiresPermissions("@css/admin")]
    public void CommandPlayRecord(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
        {
            command.ReplyToCommand("[BotMimic] This command can only be used by players");
            return;
        }

        if (_playbackManager == null || _fileManager == null)
        {
            command.ReplyToCommand("[BotMimic] Playback manager not initialized");
            return;
        }

        if (command.ArgCount < 2)
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Usage: css_playrecord <bot_name> <record_name_or_path>");
            return;
        }

        string botName = command.ArgByIndex(1);
        string recordIdentifier = command.ArgCount > 2 ? command.ArgByIndex(2) : "";

        // Find the bot
        CCSPlayerController? targetBot = null;
        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsBot && p.PlayerName.Contains(botName, StringComparison.OrdinalIgnoreCase))
            {
                targetBot = p;
                break;
            }
        }

        if (targetBot == null)
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Bot not found: {botName}");
            return;
        }

        BMError error;
        if (File.Exists(recordIdentifier))
        {
            error = _playbackManager.PlayRecordFromFile(targetBot, recordIdentifier, forceReload: true);
        }
        else
        {
            error = _playbackManager.PlayRecordByName(targetBot, recordIdentifier);
        }

        if (error == BMError.NoError)
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Bot {ChatColors.Lime}{targetBot.PlayerName}{ChatColors.Default} is now mimicking");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Failed to start playback: {error}");
        }
    }

    [ConsoleCommand("css_stopmimic", "Stop a bot from mimicking")]
    [RequiresPermissions("@css/admin")]
    public void CommandStopMimic(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
        {
            command.ReplyToCommand("[BotMimic] This command can only be used by players");
            return;
        }

        if (_playbackManager == null)
        {
            command.ReplyToCommand("[BotMimic] Playback manager not initialized");
            return;
        }

        if (command.ArgCount < 2)
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Usage: css_stopmimic <bot_name>");
            return;
        }

        string botName = command.ArgByIndex(1);

        // Find the bot
        CCSPlayerController? targetBot = null;
        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsBot && p.PlayerName.Contains(botName, StringComparison.OrdinalIgnoreCase))
            {
                targetBot = p;
                break;
            }
        }

        if (targetBot == null)
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Bot not found: {botName}");
            return;
        }

        if (_playbackManager.StopPlayerMimic(targetBot))
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Bot {ChatColors.Lime}{targetBot.PlayerName}{ChatColors.Default} stopped mimicking");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Bot is not mimicking");
        }
    }

    [ConsoleCommand("css_deleterecord", "Delete a record")]
    [RequiresPermissions("@css/root")]
    public void CommandDeleteRecord(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
        {
            command.ReplyToCommand("[BotMimic] This command can only be used by players");
            return;
        }

        if (_fileManager == null)
        {
            command.ReplyToCommand("[BotMimic] File manager not initialized");
            return;
        }

        if (command.ArgCount < 2)
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Usage: css_deleterecord <file_path>");
            return;
        }

        string path = command.ArgByIndex(1);

        if (_fileManager.DeleteRecord(path))
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Record deleted: {path}");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Failed to delete record");
        }
    }

    private void LoadRecordsForCurrentMap()
    {
        if (_fileManager == null)
            return;

        _fileManager.ParseRecordsInDirectory(Server.MapName);
        Server.PrintToConsole($"[BotMimic] Loaded records for map: {Server.MapName}");
    }

    public FileManager? GetFileManager() => _fileManager;
    public RecordManager? GetRecordManager() => _recordManager;
    public PlaybackManager? GetPlaybackManager() => _playbackManager;
}

