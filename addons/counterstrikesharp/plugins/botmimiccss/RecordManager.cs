using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace BotMimicCSS;

/// <summary>
/// Manages recording of player movements
/// </summary>
public class RecordManager
{
    private readonly Dictionary<int, RecordingSession> _activeSessions;
    private readonly FileManager _fileManager;
    private readonly int _snapshotInterval;

    public RecordManager(FileManager fileManager, int snapshotInterval = Constants.DEFAULT_SNAPSHOT_INTERVAL)
    {
        _activeSessions = new Dictionary<int, RecordingSession>();
        _fileManager = fileManager;
        _snapshotInterval = snapshotInterval;
    }

    /// <summary>
    /// Checks if a player is currently recording
    /// </summary>
    public bool IsPlayerRecording(CCSPlayerController player)
    {
        return _activeSessions.ContainsKey(player.Slot);
    }

    /// <summary>
    /// Checks if recording is paused for a player
    /// </summary>
    public bool IsRecordingPaused(CCSPlayerController player)
    {
        if (_activeSessions.TryGetValue(player.Slot, out var session))
        {
            return session.IsPaused;
        }
        return false;
    }

    /// <summary>
    /// Starts recording for a player
    /// </summary>
    public bool StartRecording(CCSPlayerController player, string recordName, string category = Constants.DEFAULT_CATEGORY, string subDir = "")
    {
        if (IsPlayerRecording(player))
        {
            Server.PrintToConsole($"[BotMimic] Player {player.PlayerName} is already recording");
            return false;
        }

        if (player.PlayerPawn?.Value == null)
        {
            Server.PrintToConsole($"[BotMimic] Player {player.PlayerName} has no valid pawn");
            return false;
        }

        var pawn = player.PlayerPawn.Value;
        var session = new RecordingSession
        {
            RecordName = recordName,
            RecordCategory = string.IsNullOrEmpty(category) ? Constants.DEFAULT_CATEGORY : category,
            RecordSubDir = subDir
        };

        // Get initial position and angles
        if (pawn.AbsOrigin != null)
        {
            session.InitialPosition = new Vector3(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z);
        }

        if (pawn.EyeAngles != null)
        {
            session.InitialAngles = new QAngle(pawn.EyeAngles.X, pawn.EyeAngles.Y, pawn.EyeAngles.Z);
        }

        // Build record path
        string basePath = Path.Combine(
            Server.GameDirectory, 
            "csgo",
            Constants.DEFAULT_RECORD_FOLDER,
            session.RecordCategory
        );

        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

        session.RecordPath = basePath;

        _activeSessions[player.Slot] = session;

        Server.PrintToConsole($"[BotMimic] Started recording for {player.PlayerName}");
        return true;
    }

    /// <summary>
    /// Pauses recording for a player
    /// </summary>
    public bool PauseRecording(CCSPlayerController player)
    {
        if (!_activeSessions.TryGetValue(player.Slot, out var session))
        {
            return false;
        }

        if (session.IsPaused)
        {
            return false;
        }

        session.IsPaused = true;
        Server.PrintToConsole($"[BotMimic] Paused recording for {player.PlayerName}");
        return true;
    }

    /// <summary>
    /// Resumes recording for a player
    /// </summary>
    public bool ResumeRecording(CCSPlayerController player)
    {
        if (!_activeSessions.TryGetValue(player.Slot, out var session))
        {
            return false;
        }

        if (!session.IsPaused)
        {
            return false;
        }

        session.SaveFullSnapshot = true;
        session.IsPaused = false;
        Server.PrintToConsole($"[BotMimic] Resumed recording for {player.PlayerName}");
        return true;
    }

    /// <summary>
    /// Saves a bookmark during recording
    /// </summary>
    public bool SaveBookmark(CCSPlayerController player, string bookmarkName)
    {
        if (!_activeSessions.TryGetValue(player.Slot, out var session))
        {
            return false;
        }

        // Check if bookmark name already exists
        if (session.Bookmarks.Any(b => b.Name.Equals(bookmarkName, StringComparison.OrdinalIgnoreCase)))
        {
            Server.PrintToConsole($"[BotMimic] Bookmark '{bookmarkName}' already exists");
            return false;
        }

        if (player.PlayerPawn?.Value == null || session.RecordedTicks == 0)
        {
            return false;
        }

        var pawn = player.PlayerPawn.Value;

        // Save current state
        var teleport = new AdditionalTeleport();

        if (pawn.AbsOrigin != null)
        {
            teleport.Origin = new Vector3(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z);
        }

        if (pawn.EyeAngles != null)
        {
            teleport.Angles = new QAngle(pawn.EyeAngles.X, pawn.EyeAngles.Y, pawn.EyeAngles.Z);
        }

        if (pawn.AbsVelocity != null)
        {
            teleport.Velocity = new Vector3(pawn.AbsVelocity.X, pawn.AbsVelocity.Y, pawn.AbsVelocity.Z);
        }

        teleport.Flags = AdditionalFields.TeleportedOrigin | 
                        AdditionalFields.TeleportedAngles | 
                        AdditionalFields.TeleportedVelocity;

        // Get the last frame
        var lastFrame = session.Frames[session.RecordedTicks - 1];

        // Check if there's already teleport data for this frame
        if ((lastFrame.AdditionalFieldsFlags & teleport.Flags) != 0)
        {
            // Replace it
            if (session.CurrentAdditionalTeleportIndex > 0)
            {
                session.AdditionalTeleports[session.CurrentAdditionalTeleportIndex - 1] = teleport;
            }
        }
        else
        {
            session.AdditionalTeleports.Add(teleport);
            session.CurrentAdditionalTeleportIndex++;
        }

        lastFrame.AdditionalFieldsFlags |= teleport.Flags;

        // Save weapon if needed
        if (string.IsNullOrEmpty(lastFrame.NewWeapon))
        {
            var activeWeapon = pawn.WeaponServices?.ActiveWeapon?.Value;
            if (activeWeapon != null && activeWeapon.DesignerName != null)
            {
                lastFrame.NewWeapon = activeWeapon.DesignerName;
            }
        }

        // Create bookmark
        var bookmark = new Bookmark
        {
            Frame = session.RecordedTicks - 1,
            AdditionalTeleportTick = session.CurrentAdditionalTeleportIndex - 1,
            Name = bookmarkName
        };

        session.Bookmarks.Add(bookmark);

        Server.PrintToConsole($"[BotMimic] Saved bookmark '{bookmarkName}' at frame {bookmark.Frame}");
        return true;
    }

    /// <summary>
    /// Stops recording for a player
    /// </summary>
    public bool StopRecording(CCSPlayerController player, bool save = true)
    {
        if (!_activeSessions.TryGetValue(player.Slot, out var session))
        {
            return false;
        }

        _activeSessions.Remove(player.Slot);

        if (!save)
        {
            Server.PrintToConsole($"[BotMimic] Discarded recording for {player.PlayerName}");
            return true;
        }

        try
        {
            // Build file path
            string mapName = Server.MapName;
            string categoryPath = Path.Combine(
                Server.GameDirectory,
                "csgo",
                Constants.DEFAULT_RECORD_FOLDER,
                session.RecordCategory
            );

            if (!Directory.Exists(categoryPath))
            {
                Directory.CreateDirectory(categoryPath);
            }

            string mapPath = Path.Combine(categoryPath, mapName);
            if (!Directory.Exists(mapPath))
            {
                Directory.CreateDirectory(mapPath);
            }

            if (!string.IsNullOrEmpty(session.RecordSubDir))
            {
                mapPath = Path.Combine(mapPath, session.RecordSubDir);
                if (!Directory.Exists(mapPath))
                {
                    Directory.CreateDirectory(mapPath);
                }
            }

            int endTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string filePath = Path.Combine(mapPath, $"{endTime}.rec");

            // Create file header
            var fileHeader = new FileHeader
            {
                BinaryFormatVersion = Constants.BINARY_FORMAT_VERSION,
                RecordEndTime = endTime,
                RecordName = session.RecordName,
                TickCount = session.Frames.Count,
                BookmarkCount = session.Bookmarks.Count,
                InitialPosition = session.InitialPosition,
                InitialAngles = session.InitialAngles,
                Frames = session.Frames,
                Bookmarks = session.Bookmarks
            };

            // Write to disk
            _fileManager.WriteRecordToDisk(filePath, fileHeader, session.AdditionalTeleports);

            // Immediately load the record so it appears in the menu
            _fileManager.LoadRecordFromFile(filePath, session.RecordCategory, out _, onlyHeader: true, forceReload: false);

            Server.PrintToConsole($"[BotMimic] Saved recording for {player.PlayerName} to {filePath}");
            Server.PrintToConsole($"[BotMimic] Recorded {session.RecordedTicks} ticks with {session.Bookmarks.Count} bookmarks");
            player.PrintToChat($" {ChatColors.Green}[BotMimic]{ChatColors.Default} Recording saved! Use {ChatColors.Lime}!mimic{ChatColors.Default} to play it.");

            return true;
        }
        catch (Exception ex)
        {
            Server.PrintToConsole($"[BotMimic] Error saving recording: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Records a frame for a player
    /// </summary>
    public void RecordFrame(CCSPlayerController player, ulong buttons, Vector3 velocity, QAngle angles)
    {
        if (!_activeSessions.TryGetValue(player.Slot, out var session))
        {
            return;
        }

        if (session.IsPaused)
        {
            return;
        }

        var pawn = player.PlayerPawn?.Value;
        if (pawn == null)
        {
            return;
        }

        var frame = new FrameInfo
        {
            Buttons = buttons,
            Impulse = 0,
            PredictedVelocity = velocity,
            PredictedAngles = angles,
            PlayerSubtype = 0,
            PlayerSeed = 0
        };

        // Get actual velocity
        if (pawn.AbsVelocity != null)
        {
            frame.ActualVelocity = new Vector3(pawn.AbsVelocity.X, pawn.AbsVelocity.Y, pawn.AbsVelocity.Z);
        }

        // Get origin
        if (pawn.AbsOrigin != null)
        {
            frame.Origin = new Vector3(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z);
        }

        // Save full snapshot if needed
        if (session.SaveFullSnapshot)
        {
            var teleport = new AdditionalTeleport();

            if (pawn.AbsOrigin != null)
            {
                teleport.Origin = new Vector3(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z);
            }

            if (pawn.EyeAngles != null)
            {
                teleport.Angles = new QAngle(pawn.EyeAngles.X, pawn.EyeAngles.Y, pawn.EyeAngles.Z);
            }

            if (pawn.AbsVelocity != null)
            {
                teleport.Velocity = new Vector3(pawn.AbsVelocity.X, pawn.AbsVelocity.Y, pawn.AbsVelocity.Z);
            }

            teleport.Flags = AdditionalFields.TeleportedOrigin | 
                           AdditionalFields.TeleportedAngles | 
                           AdditionalFields.TeleportedVelocity;

            session.AdditionalTeleports.Add(teleport);
            session.SaveFullSnapshot = false;
        }
        else if (_snapshotInterval > 0 && session.OriginSnapshotInterval > _snapshotInterval)
        {
            // Save periodic snapshot
            var teleport = new AdditionalTeleport();

            if (pawn.AbsOrigin != null)
            {
                teleport.Origin = new Vector3(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z);
            }

            teleport.Flags = AdditionalFields.TeleportedOrigin;
            session.AdditionalTeleports.Add(teleport);
            session.OriginSnapshotInterval = 0;
        }

        session.OriginSnapshotInterval++;

        // Check for additional teleports
        if (session.AdditionalTeleports.Count > session.CurrentAdditionalTeleportIndex)
        {
            var teleport = session.AdditionalTeleports[session.CurrentAdditionalTeleportIndex];
            frame.AdditionalFieldsFlags |= teleport.Flags;
            session.CurrentAdditionalTeleportIndex++;
        }

        // Check for weapon changes
        var activeWeapon = pawn.WeaponServices?.ActiveWeapon?.Value;
        if (activeWeapon != null && activeWeapon.IsValid)
        {
            int currentWeaponIndex = (int)activeWeapon.Index;

            if (session.RecordedTicks == 0 || session.PreviousWeapon != currentWeaponIndex)
            {
                if (activeWeapon.DesignerName != null && !string.IsNullOrEmpty(activeWeapon.DesignerName))
                {
                    frame.NewWeapon = activeWeapon.DesignerName;
                    session.PreviousWeapon = currentWeaponIndex;
                    
                    // Debug: log weapon changes
                    Server.PrintToConsole($"[BotMimic] Recording weapon change for {player.PlayerName}: {activeWeapon.DesignerName} (tick {session.RecordedTicks})");
                }
            }
        }

        session.Frames.Add(frame);
        session.RecordedTicks++;
    }

    /// <summary>
    /// Gets the recording session for a player
    /// </summary>
    public RecordingSession? GetRecordingSession(CCSPlayerController player)
    {
        _activeSessions.TryGetValue(player.Slot, out var session);
        return session;
    }

    /// <summary>
    /// Clears all active recording sessions
    /// </summary>
    public void ClearAllSessions()
    {
        _activeSessions.Clear();
    }
}

