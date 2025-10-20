using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CSSVector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace BotMimicCSS;

/// <summary>
/// Manages playback of recorded movements for bots
/// </summary>
public class PlaybackManager
{
    private readonly Dictionary<int, PlaybackSession> _activeSessions;
    private readonly FileManager _fileManager;

    public PlaybackManager(FileManager fileManager)
    {
        _activeSessions = new Dictionary<int, PlaybackSession>();
        _fileManager = fileManager;
    }

    /// <summary>
    /// Checks if a player is currently mimicking
    /// </summary>
    public bool IsPlayerMimicking(CCSPlayerController player)
    {
        return _activeSessions.ContainsKey(player.Slot);
    }

    /// <summary>
    /// Starts playback for a bot
    /// </summary>
    public BMError PlayRecordFromFile(CCSPlayerController player, string path, bool forceReload = false)
    {
        if (!player.IsBot)
        {
            return BMError.BadClient;
        }

        if (!File.Exists(path))
        {
            return BMError.FileNotFound;
        }

        // Load the record
        var error = _fileManager.LoadRecordFromFile(path, Constants.DEFAULT_CATEGORY, out var fileHeader, 
            onlyHeader: false, forceReload: forceReload);

        if (error != BMError.NoError || fileHeader == null)
        {
            return error;
        }

        if (fileHeader.Frames.Count == 0)
        {
            return BMError.BadFile;
        }

        // Create playback session
        var session = new PlaybackSession(fileHeader.Frames, path)
        {
            InitialPosition = fileHeader.InitialPosition,
            InitialAngles = fileHeader.InitialAngles,
            RecordTickCount = fileHeader.TickCount
        };

        // Update next bookmark
        UpdateNextBookmarkTick(session, fileHeader);

        _activeSessions[player.Slot] = session;

        Server.PrintToConsole($"[BotMimic] Bot {player.PlayerName} (Slot {player.Slot}) started mimicking record from {path}");
        Server.PrintToConsole($"[BotMimic] Record has {fileHeader.TickCount} frames, {fileHeader.BookmarkCount} bookmarks");
        Server.PrintToConsole($"[BotMimic] Initial position: {fileHeader.InitialPosition}");
        Server.PrintToConsole($"[BotMimic] Session stored in slot {player.Slot}, IsPlayerMimicking should now return true");
        
        return BMError.NoError;
    }

    /// <summary>
    /// Plays a record by name (finds most recent)
    /// </summary>
    public BMError PlayRecordByName(CCSPlayerController player, string recordName)
    {
        if (!player.IsBot)
        {
            return BMError.BadClient;
        }

        string? mostRecentPath = null;
        int mostRecentTime = 0;

        foreach (var path in _fileManager.GetSortedRecordList())
        {
            if (_fileManager.TryGetFileHeader(path, out var header) && header != null)
            {
                if (header.RecordName.Equals(recordName, StringComparison.OrdinalIgnoreCase))
                {
                    if (mostRecentTime == 0 || header.RecordEndTime > mostRecentTime)
                    {
                        mostRecentTime = header.RecordEndTime;
                        mostRecentPath = path;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(mostRecentPath))
        {
            return BMError.FileNotFound;
        }

        return PlayRecordFromFile(player, mostRecentPath);
    }

    /// <summary>
    /// Stops playback for a bot
    /// </summary>
    public bool StopPlayerMimic(CCSPlayerController player)
    {
        if (!_activeSessions.ContainsKey(player.Slot))
        {
            return false;
        }

        _activeSessions.Remove(player.Slot);
        Server.PrintToConsole($"[BotMimic] Bot {player.PlayerName} stopped mimicking");
        return true;
    }

    /// <summary>
    /// Processes a tick for a mimicking bot
    /// </summary>
    public bool ProcessTick(CCSPlayerController player, out FrameInfo? frame)
    {
        frame = null;

        if (!_activeSessions.TryGetValue(player.Slot, out var session))
        {
            return false;
        }

        var pawn = player.PlayerPawn?.Value;
        if (pawn == null || !pawn.IsValid)
        {
            Server.PrintToConsole($"[BotMimic] ProcessTick: Pawn invalid for {player.PlayerName}");
            return false;
        }

        // Check if we've reached the end
        if (session.CurrentTick >= session.RecordTickCount)
        {
            Server.PrintToConsole($"[BotMimic] Bot {player.PlayerName} reached end of recording, looping...");
            // Loop the recording instead of stopping
            session.CurrentTick = 0;
            session.CurrentAdditionalTeleportIndex = 0;
        }

        // Debug log every 100 ticks
        if (session.CurrentTick % 100 == 0)
        {
            Server.PrintToConsole($"[BotMimic] Bot {player.PlayerName} playing tick {session.CurrentTick}/{session.RecordTickCount}");
        }

        // Get current frame
        if (session.CurrentTick < 0 || session.CurrentTick >= session.Frames.Count)
        {
            Server.PrintToConsole($"[BotMimic] Invalid tick index {session.CurrentTick} for bot {player.PlayerName}");
            StopPlayerMimic(player);
            return false;
        }

        frame = session.Frames[session.CurrentTick];

        // Handle teleport on first tick
        if (session.CurrentTick == 0)
        {
            session.ValidTeleportCall = true;
            TeleportEntity(pawn, session.InitialPosition, session.InitialAngles, frame.ActualVelocity);
        }
        else
        {
            // Set position and velocity for subsequent ticks
            session.ValidTeleportCall = true;
            SetEntityOrigin(pawn, frame.Origin);
            SetEntityVelocity(pawn, frame.ActualVelocity);
            
            // Set angles
            if (pawn.EyeAngles != null)
            {
                pawn.EyeAngles.X = frame.PredictedAngles.X;
                pawn.EyeAngles.Y = frame.PredictedAngles.Y;
            }
        }

        // Handle additional teleports
        if ((frame.AdditionalFieldsFlags & (AdditionalFields.TeleportedOrigin | 
                                            AdditionalFields.TeleportedAngles | 
                                            AdditionalFields.TeleportedVelocity)) != 0)
        {
            if (_fileManager.TryGetAdditionalTeleports(session.RecordPath, out var teleports) && 
                teleports != null &&
                session.CurrentAdditionalTeleportIndex < teleports.Count)
            {
                var teleport = teleports[session.CurrentAdditionalTeleportIndex];
                session.ValidTeleportCall = true;

                if ((teleport.Flags & AdditionalFields.TeleportedOrigin) != 0)
                {
                    SetEntityOrigin(pawn, teleport.Origin);
                }

                if ((teleport.Flags & AdditionalFields.TeleportedAngles) != 0 && pawn.EyeAngles != null)
                {
                    pawn.EyeAngles.X = teleport.Angles.X;
                    pawn.EyeAngles.Y = teleport.Angles.Y;
                    pawn.EyeAngles.Z = teleport.Angles.Z;
                }

                if ((teleport.Flags & AdditionalFields.TeleportedVelocity) != 0)
                {
                    SetEntityVelocity(pawn, teleport.Velocity);
                }

                session.CurrentAdditionalTeleportIndex++;
            }
        }

        // Handle weapon switching
        if (!string.IsNullOrEmpty(frame.NewWeapon))
        {
            SwitchToWeapon(pawn, frame.NewWeapon);
        }

        // Check for bookmarks
        if (session.CurrentTick == session.NextBookmarkFrame)
        {
            if (_fileManager.TryGetFileHeader(session.RecordPath, out var fileHeader) && 
                fileHeader != null &&
                session.NextBookmarkIndex >= 0 && 
                session.NextBookmarkIndex < fileHeader.Bookmarks.Count)
            {
                var bookmark = fileHeader.Bookmarks[session.NextBookmarkIndex];
                Server.PrintToConsole($"[BotMimic] Bot {player.PlayerName} reached bookmark: {bookmark.Name}");

                // Update to next bookmark
                UpdateNextBookmarkTick(session, fileHeader);
            }
        }

        session.CurrentTick++;
        return true;
    }

    /// <summary>
    /// Resets playback to the beginning
    /// </summary>
    public bool ResetPlayback(CCSPlayerController player)
    {
        if (!_activeSessions.TryGetValue(player.Slot, out var session))
        {
            return false;
        }

        session.CurrentTick = 0;
        session.CurrentAdditionalTeleportIndex = 0;
        session.ValidTeleportCall = false;
        session.NextBookmarkFrame = -1;
        session.NextBookmarkIndex = -1;

        if (_fileManager.TryGetFileHeader(session.RecordPath, out var fileHeader) && fileHeader != null)
        {
            UpdateNextBookmarkTick(session, fileHeader);
        }

        Server.PrintToConsole($"[BotMimic] Reset playback for bot {player.PlayerName}");
        return true;
    }

    /// <summary>
    /// Jumps to a bookmark
    /// </summary>
    public bool GoToBookmark(CCSPlayerController player, string bookmarkName)
    {
        if (!_activeSessions.TryGetValue(player.Slot, out var session))
        {
            return false;
        }

        if (!_fileManager.TryGetFileHeader(session.RecordPath, out var fileHeader) || fileHeader == null)
        {
            return false;
        }

        // Find the bookmark
        for (int i = 0; i < fileHeader.Bookmarks.Count; i++)
        {
            var bookmark = fileHeader.Bookmarks[i];
            if (bookmark.Name.Equals(bookmarkName, StringComparison.OrdinalIgnoreCase))
            {
                session.CurrentTick = bookmark.Frame;
                session.CurrentAdditionalTeleportIndex = bookmark.AdditionalTeleportTick;
                session.NextBookmarkFrame = bookmark.Frame;
                session.NextBookmarkIndex = i;

                Server.PrintToConsole($"[BotMimic] Bot {player.PlayerName} jumped to bookmark: {bookmarkName}");
                return true;
            }
        }

        Server.PrintToConsole($"[BotMimic] Bookmark '{bookmarkName}' not found");
        return false;
    }

    /// <summary>
    /// Gets the playback session for a player
    /// </summary>
    public PlaybackSession? GetPlaybackSession(CCSPlayerController player)
    {
        _activeSessions.TryGetValue(player.Slot, out var session);
        return session;
    }

    /// <summary>
    /// Gets the record path a player is mimicking
    /// </summary>
    public string? GetRecordPlayerMimics(CCSPlayerController player)
    {
        if (_activeSessions.TryGetValue(player.Slot, out var session))
        {
            return session.RecordPath;
        }
        return null;
    }

    /// <summary>
    /// Updates the next bookmark tick
    /// </summary>
    private void UpdateNextBookmarkTick(PlaybackSession session, FileHeader fileHeader)
    {
        if (fileHeader.Bookmarks.Count == 0)
        {
            return;
        }

        int currentIndex = session.NextBookmarkIndex;
        currentIndex++;

        if (currentIndex >= fileHeader.Bookmarks.Count)
        {
            currentIndex = 0;
        }

        var bookmark = fileHeader.Bookmarks[currentIndex];
        session.NextBookmarkFrame = bookmark.Frame;
        session.NextBookmarkIndex = currentIndex;
    }

    /// <summary>
    /// Teleports an entity
    /// </summary>
    private void TeleportEntity(CCSPlayerPawn pawn, Vector3 position, QAngle angles, Vector3 velocity)
    {
        if (pawn.AbsOrigin != null)
        {
            pawn.AbsOrigin.X = position.X;
            pawn.AbsOrigin.Y = position.Y;
            pawn.AbsOrigin.Z = position.Z;
        }
        
        if (pawn.EyeAngles != null)
        {
            pawn.EyeAngles.X = angles.X;
            pawn.EyeAngles.Y = angles.Y;
            pawn.EyeAngles.Z = angles.Z;
        }

        SetEntityVelocity(pawn, velocity);
        
        var cssPos = new CSSVector(position.X, position.Y, position.Z);
        var cssAng = new QAngle(angles.X, angles.Y, angles.Z);
        pawn.Teleport(cssPos, cssAng, pawn.AbsVelocity);
    }

    /// <summary>
    /// Sets entity origin
    /// </summary>
    private void SetEntityOrigin(CCSPlayerPawn pawn, Vector3 position)
    {
        if (pawn.AbsOrigin != null)
        {
            pawn.AbsOrigin.X = position.X;
            pawn.AbsOrigin.Y = position.Y;
            pawn.AbsOrigin.Z = position.Z;
        }

        var cssPos = new CSSVector(position.X, position.Y, position.Z);
        pawn.Teleport(cssPos, pawn.AbsRotation, pawn.AbsVelocity);
    }

    /// <summary>
    /// Sets entity velocity
    /// </summary>
    private void SetEntityVelocity(CCSPlayerPawn pawn, Vector3 velocity)
    {
        if (pawn.AbsVelocity != null)
        {
            pawn.AbsVelocity.X = velocity.X;
            pawn.AbsVelocity.Y = velocity.Y;
            pawn.AbsVelocity.Z = velocity.Z;
        }
    }

    /// <summary>
    /// Switches the bot's weapon
    /// </summary>
    private void SwitchToWeapon(CCSPlayerPawn pawn, string weaponName)
    {
        if (pawn.WeaponServices == null)
        {
            return;
        }

        // Try to find the weapon in the player's inventory
        var weapons = pawn.WeaponServices.MyWeapons;
        if (weapons == null)
        {
            return;
        }

        foreach (var weaponHandle in weapons)
        {
            var weapon = weaponHandle?.Value;
            if (weapon != null && weapon.DesignerName != null)
            {
                if (weapon.DesignerName.Equals(weaponName, StringComparison.OrdinalIgnoreCase))
                {
                    // Found the weapon, switch to it
                    // Note: ActiveWeapon is read-only in CS2, weapon switching needs to be done differently
                    // For now, we'll just mark it - actual weapon switching may need native calls
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Clears all active playback sessions
    /// </summary>
    public void ClearAllSessions()
    {
        _activeSessions.Clear();
    }
}

