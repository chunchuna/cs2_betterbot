using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;

namespace BotMimicCSS;

/// <summary>
/// Manages menus for the Bot Mimic plugin
/// </summary>
public class MenuManager
{
    private readonly BotMimicCSS _plugin;
    private readonly FileManager _fileManager;
    private readonly RecordManager _recordManager;
    private readonly PlaybackManager _playbackManager;

    private readonly Dictionary<int, string> _selectedCategory = new();
    private readonly Dictionary<int, string> _selectedRecord = new();
    private readonly Dictionary<int, string> _selectedBookmark = new();

    public MenuManager(BotMimicCSS plugin, FileManager fileManager, RecordManager recordManager, PlaybackManager playbackManager)
    {
        _plugin = plugin;
        _fileManager = fileManager;
        _recordManager = recordManager;
        _playbackManager = playbackManager;
    }

    /// <summary>
    /// Shows the main category selection menu
    /// </summary>
    public void ShowMainMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu("Bot Mimic - Categories");

        menu.AddMenuOption("Record New Movement", (p, option) =>
        {
            if (p.PlayerPawn?.Value == null || !p.PawnIsAlive)
            {
                p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} You must be alive to record!");
                return;
            }

            if (_recordManager.IsPlayerRecording(p))
            {
                p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} You are already recording!");
                ShowRecordingMenu(p);
                return;
            }

            string recordName = $"record_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{p.Slot}";
            if (_recordManager.StartRecording(p, recordName, Constants.DEFAULT_CATEGORY))
            {
                p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Started recording!");
                ShowRecordingMenu(p);
            }
        });

        menu.AddMenuOption("Stop All Bots", (p, option) =>
        {
            int count = 0;
            foreach (var bot in Utilities.GetPlayers())
            {
                if (bot.IsBot && _playbackManager.IsPlayerMimicking(bot))
                {
                    _playbackManager.StopPlayerMimic(bot);
                    count++;
                }
            }
            p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Stopped {count} bots");
            ShowMainMenu(p);
        });

        // Add categories
        foreach (var category in _fileManager.GetSortedCategoryList())
        {
            menu.AddMenuOption($"Category: {category}", (p, option) =>
            {
                _selectedCategory[p.Slot] = category;
                ShowRecordListMenu(p, category);
            });
        }

        CounterStrikeSharp.API.Modules.Menu.MenuManager.OpenChatMenu(player, menu);
    }

    /// <summary>
    /// Shows the recording in progress menu
    /// </summary>
    public void ShowRecordingMenu(CCSPlayerController player)
    {
        if (!_recordManager.IsPlayerRecording(player))
        {
            ShowMainMenu(player);
            return;
        }

        var session = _recordManager.GetRecordingSession(player);
        var menu = new ChatMenu($"Recording... ({session?.RecordedTicks ?? 0} ticks)");

        if (_recordManager.IsRecordingPaused(player))
        {
            menu.AddMenuOption("Resume Recording", (p, option) =>
            {
                _recordManager.ResumeRecording(p);
                p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Recording resumed");
                ShowRecordingMenu(p);
            });
        }
        else
        {
            menu.AddMenuOption("Pause Recording", (p, option) =>
            {
                _recordManager.PauseRecording(p);
                p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Recording paused");
                ShowRecordingMenu(p);
            });
        }

        menu.AddMenuOption("Save Recording", (p, option) =>
        {
            if (_recordManager.StopRecording(p, save: true))
            {
                p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Recording saved!");
            }
        });

        menu.AddMenuOption("Discard Recording", (p, option) =>
        {
            _recordManager.StopRecording(p, save: false);
            p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Recording discarded");
            ShowMainMenu(p);
        });

        CounterStrikeSharp.API.Modules.Menu.MenuManager.OpenChatMenu(player, menu);
    }

    /// <summary>
    /// Shows the record list for a category
    /// </summary>
    public void ShowRecordListMenu(CCSPlayerController player, string category)
    {
        var menu = new ChatMenu($"Records - {category}");

        menu.AddMenuOption("< Back to Categories", (p, option) =>
        {
            _selectedCategory.Remove(p.Slot);
            ShowMainMenu(p);
        });

        menu.AddMenuOption("Record New Movement", (p, option) =>
        {
            if (p.PlayerPawn?.Value == null || !p.PawnIsAlive)
            {
                p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} You must be alive to record!");
                return;
            }

            if (_recordManager.IsPlayerRecording(p))
            {
                p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} You are already recording!");
                ShowRecordingMenu(p);
                return;
            }

            string recordName = $"record_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{p.Slot}";
            if (_recordManager.StartRecording(p, recordName, category))
            {
                p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Started recording!");
                ShowRecordingMenu(p);
            }
        });

        // Add records from this category
        int recordCount = 0;
        foreach (var path in _fileManager.GetSortedRecordList())
        {
            if (_fileManager.TryGetCategory(path, out var recordCategory) && 
                recordCategory != null && 
                recordCategory.Equals(category, StringComparison.OrdinalIgnoreCase))
            {
                if (_fileManager.TryGetFileHeader(path, out var header) && header != null)
                {
                    // Count how many bots are playing this
                    int playingCount = 0;
                    foreach (var bot in Utilities.GetPlayers())
                    {
                        if (bot.IsBot && _playbackManager.IsPlayerMimicking(bot))
                        {
                            var botRecordPath = _playbackManager.GetRecordPlayerMimics(bot);
                            if (botRecordPath == path)
                            {
                                playingCount++;
                            }
                        }
                    }

                    string displayName = header.RecordName;
                    if (playingCount > 0)
                    {
                        displayName += $" (Playing {playingCount}x)";
                    }

                    string recordPath = path; // Capture for lambda
                    menu.AddMenuOption(displayName, (p, option) =>
                    {
                        _selectedRecord[p.Slot] = recordPath;
                        ShowRecordDetailMenu(p, recordPath);
                    });

                    recordCount++;
                }
            }
        }

        if (recordCount == 0)
        {
            menu.AddMenuOption("No records found", (p, option) => { }, disabled: true);
        }

        CounterStrikeSharp.API.Modules.Menu.MenuManager.OpenChatMenu(player, menu);
    }

    /// <summary>
    /// Shows the detail menu for a specific record
    /// </summary>
    public void ShowRecordDetailMenu(CCSPlayerController player, string recordPath)
    {
        if (!_fileManager.TryGetFileHeader(recordPath, out var header) || header == null)
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Record not found!");
            ShowMainMenu(player);
            return;
        }

        var menu = new ChatMenu($"Record: {header.RecordName}");

        menu.AddMenuOption("< Back to Records", (p, option) =>
        {
            _selectedRecord.Remove(p.Slot);
            if (_selectedCategory.TryGetValue(p.Slot, out var category))
            {
                ShowRecordListMenu(p, category);
            }
            else
            {
                ShowMainMenu(p);
            }
        });

        menu.AddMenuOption("Play on Bot", (p, option) =>
        {
            ShowBotSelectionMenu(p, recordPath);
        });

        menu.AddMenuOption("Add Bot and Play", (p, option) =>
        {
            ShowTeamSelectionMenu(p, recordPath);
        });

        menu.AddMenuOption("Stop All Playing This", (p, option) =>
        {
            int count = 0;
            foreach (var bot in Utilities.GetPlayers())
            {
                if (bot.IsBot && _playbackManager.IsPlayerMimicking(bot))
                {
                    var path = _playbackManager.GetRecordPlayerMimics(bot);
                    if (path == recordPath)
                    {
                        _playbackManager.StopPlayerMimic(bot);
                        count++;
                    }
                }
            }
            p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Stopped {count} bots");
            ShowRecordDetailMenu(p, recordPath);
        });

        if (header.BookmarkCount > 0)
        {
            menu.AddMenuOption($"Bookmarks ({header.BookmarkCount})", (p, option) =>
            {
                ShowBookmarkListMenu(p, recordPath);
            });
        }

        menu.AddMenuOption($"Ticks: {header.TickCount}", (p, option) => { }, disabled: true);

        var date = DateTimeOffset.FromUnixTimeSeconds(header.RecordEndTime).LocalDateTime;
        menu.AddMenuOption($"Recorded: {date:g}", (p, option) => { }, disabled: true);

        CounterStrikeSharp.API.Modules.Menu.MenuManager.OpenChatMenu(player, menu);
    }

    /// <summary>
    /// Shows bot selection menu
    /// </summary>
    public void ShowBotSelectionMenu(CCSPlayerController player, string recordPath)
    {
        var menu = new ChatMenu("Select Bot");

        menu.AddMenuOption("< Back", (p, option) =>
        {
            ShowRecordDetailMenu(p, recordPath);
        });

        bool hasBot = false;
        foreach (var bot in Utilities.GetPlayers())
        {
            if (bot.IsBot && bot.TeamNum >= 2)
            {
                hasBot = true;
                string teamName = bot.TeamNum == 2 ? "T" : "CT";
                string displayName = $"{bot.PlayerName} [{teamName}]";

                if (_playbackManager.IsPlayerMimicking(bot))
                {
                    displayName += " (Playing)";
                }

                CCSPlayerController targetBot = bot; // Capture for lambda
                menu.AddMenuOption(displayName, (p, option) =>
                {
                    var error = _playbackManager.PlayRecordFromFile(targetBot, recordPath, forceReload: true);
                    if (error == BMError.NoError)
                    {
                        p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Bot {ChatColors.Lime}{targetBot.PlayerName}{ChatColors.Default} is now playing!");
                    }
                    else
                    {
                        p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Error: {error}");
                    }
                    ShowBotSelectionMenu(p, recordPath);
                });
            }
        }

        if (!hasBot)
        {
            menu.AddMenuOption("No bots available", (p, option) => { }, disabled: true);
        }

        CounterStrikeSharp.API.Modules.Menu.MenuManager.OpenChatMenu(player, menu);
    }

    /// <summary>
    /// Shows team selection menu for adding a new bot
    /// </summary>
    public void ShowTeamSelectionMenu(CCSPlayerController player, string recordPath)
    {
        var menu = new ChatMenu("Select Team for New Bot");

        menu.AddMenuOption("< Back", (p, option) =>
        {
            ShowRecordDetailMenu(p, recordPath);
        });

        menu.AddMenuOption("Terrorist", (p, option) =>
        {
            // Store the record path for the next bot that joins
            string pathToPlay = recordPath;
            
            p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Adding T bot...");
            
            // Get player count before adding bot
            int beforeCount = Utilities.GetPlayers().Count;
            Server.PrintToConsole($"[BotMimic] Player count before adding bot: {beforeCount}");
            
            // Disable bot AI and add bot
            Server.ExecuteCommand("bot_stop 1");
            Server.ExecuteCommand("bot_dont_shoot 1");
            Server.ExecuteCommand("bot_add_t");
            Server.PrintToConsole($"[BotMimic] Executed bot_add_t command");
            
            // Wait for bot to fully load and spawn
            int attempts = 0;
            void TryApplyRecord()
            {
                attempts++;
                Server.PrintToConsole($"[BotMimic] Attempt {attempts} to find and apply record to T bot");
                
                if (attempts > 100) // Max 10 seconds (100 * 0.1s)
                {
                    p.PrintToChat($" {ChatColors.Red}[BotMimic]{ChatColors.Default} Failed to find bot. Please use css_playrecord manually.");
                    return;
                }
                
                bool found = false;
                
                // Try to get all players including newly added bots
                // Iterate through all possible player slots
                int validPlayers = 0;
                for (int i = 0; i < 64; i++)
                {
                    var bot = Utilities.GetPlayerFromSlot(i);
                    if (bot == null || !bot.IsValid || !bot.IsBot)
                        continue;
                    
                    validPlayers++;
                    Server.PrintToConsole($"[BotMimic] Slot {i} - Player: {bot.PlayerName}, IsBot={bot.IsBot}, Team={bot.TeamNum}, Connected={bot.Connected}, PawnValid={bot.PlayerPawn?.IsValid}");
                    
                    // Check for T bot that's not already mimicking
                    if (bot.TeamNum == 2 && !_playbackManager.IsPlayerMimicking(bot))
                    {
                        // Check if pawn exists
                        if (bot.PlayerPawn?.Value == null || !bot.PlayerPawn.IsValid)
                        {
                            Server.PrintToConsole($"[BotMimic] Bot {bot.PlayerName} pawn not ready yet (null or invalid), will retry");
                            continue;
                        }
                        
                        Server.PrintToConsole($"[BotMimic] Found suitable T bot: {bot.PlayerName}, attempting to apply record");
                        var result = _playbackManager.PlayRecordFromFile(bot, pathToPlay, forceReload: true);
                        Server.PrintToConsole($"[BotMimic] Attempted to apply record to {bot.PlayerName}, result: {result}");
                        
                        if (result == BMError.NoError)
                        {
                            p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Bot {ChatColors.Lime}{bot.PlayerName}{ChatColors.Default} is now mimicking!");
                            Server.PrintToConsole($"[BotMimic] Successfully applied record to {bot.PlayerName}");
                            found = true;
                            break;
                        }
                    }
                }
                
                Server.PrintToConsole($"[BotMimic] Total valid bots found: {validPlayers}");
                
                if (!found)
                {
                    // Try again after a short delay
                    _plugin.AddTimer(0.1f, TryApplyRecord);
                }
            }
            
            // Start trying to apply the record after a longer delay to ensure bot spawns
            _plugin.AddTimer(1.0f, TryApplyRecord);
        });

        menu.AddMenuOption("Counter-Terrorist", (p, option) =>
        {
            // Store the record path for the next bot that joins
            string pathToPlay = recordPath;
            
            p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Adding CT bot...");
            
            // Get player count before adding bot
            int beforeCount = Utilities.GetPlayers().Count;
            Server.PrintToConsole($"[BotMimic] Player count before adding bot: {beforeCount}");
            
            // Disable bot AI and add bot
            Server.ExecuteCommand("bot_stop 1");
            Server.ExecuteCommand("bot_dont_shoot 1");
            Server.ExecuteCommand("bot_add_ct");
            Server.PrintToConsole($"[BotMimic] Executed bot_add_ct command");
            
            // Wait for bot to fully load and spawn
            int attempts = 0;
            void TryApplyRecord()
            {
                attempts++;
                Server.PrintToConsole($"[BotMimic] Attempt {attempts} to find and apply record to CT bot");
                
                if (attempts > 100) // Max 10 seconds (100 * 0.1s)
                {
                    p.PrintToChat($" {ChatColors.Red}[BotMimic]{ChatColors.Default} Failed to find bot. Please use css_playrecord manually.");
                    return;
                }
                
                bool found = false;
                
                // Try to get all players including newly added bots
                // Iterate through all possible player slots
                int validPlayers = 0;
                for (int i = 0; i < 64; i++)
                {
                    var bot = Utilities.GetPlayerFromSlot(i);
                    if (bot == null || !bot.IsValid || !bot.IsBot)
                        continue;
                    
                    validPlayers++;
                    Server.PrintToConsole($"[BotMimic] Slot {i} - Player: {bot.PlayerName}, IsBot={bot.IsBot}, Team={bot.TeamNum}, Connected={bot.Connected}, PawnValid={bot.PlayerPawn?.IsValid}");
                    
                    // Check for CT bot that's not already mimicking
                    if (bot.TeamNum == 3 && !_playbackManager.IsPlayerMimicking(bot))
                    {
                        // Check if pawn exists
                        if (bot.PlayerPawn?.Value == null || !bot.PlayerPawn.IsValid)
                        {
                            Server.PrintToConsole($"[BotMimic] Bot {bot.PlayerName} pawn not ready yet (null or invalid), will retry");
                            continue;
                        }
                        
                        Server.PrintToConsole($"[BotMimic] Found suitable CT bot: {bot.PlayerName}, attempting to apply record");
                        var result = _playbackManager.PlayRecordFromFile(bot, pathToPlay, forceReload: true);
                        Server.PrintToConsole($"[BotMimic] Attempted to apply record to {bot.PlayerName}, result: {result}");
                        
                        if (result == BMError.NoError)
                        {
                            p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Bot {ChatColors.Lime}{bot.PlayerName}{ChatColors.Default} is now mimicking!");
                            Server.PrintToConsole($"[BotMimic] Successfully applied record to {bot.PlayerName}");
                            found = true;
                            break;
                        }
                    }
                }
                
                Server.PrintToConsole($"[BotMimic] Total valid bots found: {validPlayers}");
                
                if (!found)
                {
                    // Try again after a short delay
                    _plugin.AddTimer(0.1f, TryApplyRecord);
                }
            }
            
            // Start trying to apply the record after a longer delay to ensure bot spawns
            _plugin.AddTimer(1.0f, TryApplyRecord);
        });

        CounterStrikeSharp.API.Modules.Menu.MenuManager.OpenChatMenu(player, menu);
    }

    /// <summary>
    /// Shows bookmark list for a record
    /// </summary>
    public void ShowBookmarkListMenu(CCSPlayerController player, string recordPath)
    {
        if (!_fileManager.TryGetFileHeader(recordPath, out var header) || header == null)
        {
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Record not found!");
            ShowMainMenu(player);
            return;
        }

        var menu = new ChatMenu($"Bookmarks - {header.RecordName}");

        menu.AddMenuOption("< Back", (p, option) =>
        {
            ShowRecordDetailMenu(p, recordPath);
        });

        foreach (var bookmark in header.Bookmarks)
        {
            string bookmarkName = bookmark.Name;
            menu.AddMenuOption($"{bookmarkName} (Frame {bookmark.Frame})", (p, option) =>
            {
                _selectedBookmark[p.Slot] = bookmarkName;
                ShowBookmarkBotSelectionMenu(p, recordPath, bookmarkName);
            });
        }

        CounterStrikeSharp.API.Modules.Menu.MenuManager.OpenChatMenu(player, menu);
    }

    /// <summary>
    /// Shows bot selection for jumping to a bookmark
    /// </summary>
    public void ShowBookmarkBotSelectionMenu(CCSPlayerController player, string recordPath, string bookmarkName)
    {
        var menu = new ChatMenu($"Jump to: {bookmarkName}");

        menu.AddMenuOption("< Back", (p, option) =>
        {
            ShowBookmarkListMenu(p, recordPath);
        });

        bool hasBot = false;
        foreach (var bot in Utilities.GetPlayers())
        {
            if (bot.IsBot && _playbackManager.IsPlayerMimicking(bot))
            {
                var botRecordPath = _playbackManager.GetRecordPlayerMimics(bot);
                if (botRecordPath == recordPath)
                {
                    hasBot = true;
                    CCSPlayerController targetBot = bot;
                    menu.AddMenuOption($"{bot.PlayerName}", (p, option) =>
                    {
                        if (_playbackManager.GoToBookmark(targetBot, bookmarkName))
                        {
                            p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Bot jumped to bookmark {ChatColors.Lime}{bookmarkName}{ChatColors.Default}");
                        }
                        else
                        {
                            p.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Failed to jump to bookmark");
                        }
                        ShowBookmarkBotSelectionMenu(p, recordPath, bookmarkName);
                    });
                }
            }
        }

        if (!hasBot)
        {
            menu.AddMenuOption("No bots playing this record", (p, option) => { }, disabled: true);
        }

        CounterStrikeSharp.API.Modules.Menu.MenuManager.OpenChatMenu(player, menu);
    }
}

