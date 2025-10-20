using System.Numerics;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace BotMimicCSS;

/// <summary>
/// Represents a single frame of recorded movement data
/// </summary>
public class FrameInfo
{
    public ulong Buttons { get; set; }
    public int Impulse { get; set; }
    public Vector3 ActualVelocity { get; set; }
    public Vector3 PredictedVelocity { get; set; }
    public QAngle PredictedAngles { get; set; }
    public Vector3 Origin { get; set; }
    public string? NewWeapon { get; set; }
    public int PlayerSubtype { get; set; }
    public int PlayerSeed { get; set; }
    public AdditionalFields AdditionalFieldsFlags { get; set; }

    public FrameInfo()
    {
        ActualVelocity = new Vector3();
        PredictedVelocity = new Vector3();
        PredictedAngles = new QAngle();
        Origin = new Vector3();
        AdditionalFieldsFlags = AdditionalFields.None;
    }
}

/// <summary>
/// Flags indicating additional teleport data is present
/// </summary>
[Flags]
public enum AdditionalFields
{
    None = 0,
    TeleportedOrigin = 1 << 0,
    TeleportedAngles = 1 << 1,
    TeleportedVelocity = 1 << 2
}

/// <summary>
/// Additional teleport information for a frame
/// </summary>
public class AdditionalTeleport
{
    public Vector3 Origin { get; set; }
    public QAngle Angles { get; set; }
    public Vector3 Velocity { get; set; }
    public AdditionalFields Flags { get; set; }

    public AdditionalTeleport()
    {
        Origin = new Vector3();
        Angles = new QAngle();
        Velocity = new Vector3();
        Flags = AdditionalFields.None;
    }
}

/// <summary>
/// Bookmark saved during recording
/// </summary>
public class Bookmark
{
    public int Frame { get; set; }
    public int AdditionalTeleportTick { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// File header information
/// </summary>
public class FileHeader
{
    public int BinaryFormatVersion { get; set; }
    public int RecordEndTime { get; set; }
    public string RecordName { get; set; } = string.Empty;
    public int TickCount { get; set; }
    public int BookmarkCount { get; set; }
    public Vector3 InitialPosition { get; set; }
    public QAngle InitialAngles { get; set; }
    public List<Bookmark> Bookmarks { get; set; }
    public List<FrameInfo> Frames { get; set; }

    public FileHeader()
    {
        InitialPosition = new Vector3();
        InitialAngles = new QAngle();
        Bookmarks = new List<Bookmark>();
        Frames = new List<FrameInfo>();
    }
}

/// <summary>
/// Recording session data for a player
/// </summary>
public class RecordingSession
{
    public List<FrameInfo> Frames { get; set; }
    public List<AdditionalTeleport> AdditionalTeleports { get; set; }
    public List<Bookmark> Bookmarks { get; set; }
    public Vector3 InitialPosition { get; set; }
    public QAngle InitialAngles { get; set; }
    public int RecordedTicks { get; set; }
    public int CurrentAdditionalTeleportIndex { get; set; }
    public bool IsPaused { get; set; }
    public bool SaveFullSnapshot { get; set; }
    public string RecordName { get; set; }
    public string RecordCategory { get; set; }
    public string RecordSubDir { get; set; }
    public string RecordPath { get; set; }
    public int PreviousWeapon { get; set; }
    public int OriginSnapshotInterval { get; set; }

    public RecordingSession()
    {
        Frames = new List<FrameInfo>();
        AdditionalTeleports = new List<AdditionalTeleport>();
        Bookmarks = new List<Bookmark>();
        InitialPosition = new Vector3();
        InitialAngles = new QAngle();
        RecordName = string.Empty;
        RecordCategory = string.Empty;
        RecordSubDir = string.Empty;
        RecordPath = string.Empty;
        RecordedTicks = 0;
        CurrentAdditionalTeleportIndex = 0;
        IsPaused = false;
        SaveFullSnapshot = false;
        PreviousWeapon = -1;
        OriginSnapshotInterval = 0;
    }
}

/// <summary>
/// Playback session data for a bot
/// </summary>
public class PlaybackSession
{
    public List<FrameInfo> Frames { get; set; }
    public string RecordPath { get; set; }
    public int CurrentTick { get; set; }
    public int RecordTickCount { get; set; }
    public Vector3 InitialPosition { get; set; }
    public QAngle InitialAngles { get; set; }
    public int CurrentAdditionalTeleportIndex { get; set; }
    public int ActiveWeapon { get; set; }
    public bool SwitchedWeapon { get; set; }
    public bool ValidTeleportCall { get; set; }
    public int NextBookmarkFrame { get; set; }
    public int NextBookmarkIndex { get; set; }

    public PlaybackSession(List<FrameInfo> frames, string recordPath)
    {
        Frames = frames;
        RecordPath = recordPath;
        CurrentTick = 0;
        RecordTickCount = frames.Count;
        InitialPosition = new Vector3();
        InitialAngles = new QAngle();
        CurrentAdditionalTeleportIndex = 0;
        ActiveWeapon = -1;
        SwitchedWeapon = false;
        ValidTeleportCall = false;
        NextBookmarkFrame = -1;
        NextBookmarkIndex = -1;
    }
}

/// <summary>
/// Error codes for Bot Mimic operations
/// </summary>
public enum BMError
{
    NoError = 0,
    BadClient = 1,
    FileNotFound = 2,
    BadFile = 3,
    NewerBinaryVersion = 4
}

/// <summary>
/// Constants used throughout the plugin
/// </summary>
public static class Constants
{
    public const uint BM_MAGIC = 0xDEADBEEF;
    public const int BINARY_FORMAT_VERSION = 0x02;
    public const string DEFAULT_RECORD_FOLDER = "plugins/BotMimicCSS/records/";
    public const string DEFAULT_CATEGORY = "default";
    public const int MAX_RECORD_NAME_LENGTH = 128;
    public const int MAX_BOOKMARK_NAME_LENGTH = 64;
    public const int DEFAULT_SNAPSHOT_INTERVAL = 10000;
}

