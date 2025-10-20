using System.Numerics;
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;

namespace BotMimicCSS;

/// <summary>
/// Handles reading and writing of record files
/// </summary>
public class FileManager
{
    private readonly string _baseRecordPath;
    private readonly Dictionary<string, FileHeader> _loadedRecords;
    private readonly Dictionary<string, List<AdditionalTeleport>> _loadedAdditionalTeleports;
    private readonly Dictionary<string, string> _loadedRecordsCategory;
    private readonly List<string> _sortedRecordList;
    private readonly List<string> _sortedCategoryList;

    public FileManager(string baseRecordPath)
    {
        _baseRecordPath = baseRecordPath;
        _loadedRecords = new Dictionary<string, FileHeader>();
        _loadedAdditionalTeleports = new Dictionary<string, List<AdditionalTeleport>>();
        _loadedRecordsCategory = new Dictionary<string, string>();
        _sortedRecordList = new List<string>();
        _sortedCategoryList = new List<string>();

        EnsureDirectoryExists(_baseRecordPath);
    }

    public IReadOnlyList<string> GetSortedRecordList() => _sortedRecordList.AsReadOnly();
    public IReadOnlyList<string> GetSortedCategoryList() => _sortedCategoryList.AsReadOnly();

    /// <summary>
    /// Ensures a directory exists, creates it if it doesn't
    /// </summary>
    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    /// <summary>
    /// Writes a record to disk
    /// </summary>
    public void WriteRecordToDisk(string path, FileHeader fileHeader, List<AdditionalTeleport>? additionalTeleports)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(fs);

            // Write magic number
            writer.Write(Constants.BM_MAGIC);

            // Write binary format version
            writer.Write((byte)fileHeader.BinaryFormatVersion);

            // Write record end time
            writer.Write(fileHeader.RecordEndTime);

            // Write record name
            var nameBytes = Encoding.UTF8.GetBytes(fileHeader.RecordName);
            writer.Write((byte)nameBytes.Length);
            writer.Write(nameBytes);

            // Write initial position (3 floats)
            writer.Write(fileHeader.InitialPosition.X);
            writer.Write(fileHeader.InitialPosition.Y);
            writer.Write(fileHeader.InitialPosition.Z);

            // Write initial angles (2 floats - pitch and yaw)
            writer.Write(fileHeader.InitialAngles.X);
            writer.Write(fileHeader.InitialAngles.Y);

            // Write tick count
            writer.Write(fileHeader.TickCount);

            // Write bookmark count
            writer.Write(fileHeader.BookmarkCount);

            // Write bookmarks
            foreach (var bookmark in fileHeader.Bookmarks)
            {
                writer.Write(bookmark.Frame);
                writer.Write(bookmark.AdditionalTeleportTick);
                WriteString(writer, bookmark.Name);
            }

            // Write frames
            int atIndex = 0;
            for (int i = 0; i < fileHeader.TickCount; i++)
            {
                var frame = fileHeader.Frames[i];
                WriteFrame(writer, frame);

                // Write additional teleport data if present
                if (additionalTeleports != null && 
                    (frame.AdditionalFieldsFlags & (AdditionalFields.TeleportedOrigin | 
                                                     AdditionalFields.TeleportedAngles | 
                                                     AdditionalFields.TeleportedVelocity)) != 0)
                {
                    if (atIndex < additionalTeleports.Count)
                    {
                        var at = additionalTeleports[atIndex];
                        if ((frame.AdditionalFieldsFlags & AdditionalFields.TeleportedOrigin) != 0)
                        {
                            writer.Write(at.Origin.X);
                            writer.Write(at.Origin.Y);
                            writer.Write(at.Origin.Z);
                        }
                        if ((frame.AdditionalFieldsFlags & AdditionalFields.TeleportedAngles) != 0)
                        {
                            writer.Write(at.Angles.X);
                            writer.Write(at.Angles.Y);
                            writer.Write(at.Angles.Z);
                        }
                        if ((frame.AdditionalFieldsFlags & AdditionalFields.TeleportedVelocity) != 0)
                        {
                            writer.Write(at.Velocity.X);
                            writer.Write(at.Velocity.Y);
                            writer.Write(at.Velocity.Z);
                        }
                        atIndex++;
                    }
                }
            }

            Server.PrintToConsole($"[BotMimic] Record saved to: {path}");
        }
        catch (Exception ex)
        {
            Server.PrintToConsole($"[BotMimic] Error writing record to disk: {ex.Message}");
        }
    }

    /// <summary>
    /// Writes a single frame to the binary writer
    /// </summary>
    private void WriteFrame(BinaryWriter writer, FrameInfo frame)
    {
        writer.Write(frame.Buttons);
        writer.Write(frame.Impulse);
        writer.Write(frame.ActualVelocity.X);
        writer.Write(frame.ActualVelocity.Y);
        writer.Write(frame.ActualVelocity.Z);
        writer.Write(frame.PredictedVelocity.X);
        writer.Write(frame.PredictedVelocity.Y);
        writer.Write(frame.PredictedVelocity.Z);
        writer.Write(frame.PredictedAngles.X);
        writer.Write(frame.PredictedAngles.Y);
        writer.Write(frame.Origin.X);
        writer.Write(frame.Origin.Y);
        writer.Write(frame.Origin.Z);
        WriteString(writer, frame.NewWeapon ?? string.Empty);
        writer.Write(frame.PlayerSubtype);
        writer.Write(frame.PlayerSeed);
        writer.Write((int)frame.AdditionalFieldsFlags);
    }

    /// <summary>
    /// Writes a string with null terminator
    /// </summary>
    private void WriteString(BinaryWriter writer, string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        writer.Write(bytes);
        writer.Write((byte)0); // null terminator
    }

    /// <summary>
    /// Loads a record from disk
    /// </summary>
    public BMError LoadRecordFromFile(string path, string category, out FileHeader? fileHeader, bool onlyHeader = false, bool forceReload = false)
    {
        fileHeader = null;

        if (!File.Exists(path))
        {
            return BMError.FileNotFound;
        }

        // Check if already loaded
        if (!forceReload && _loadedRecords.TryGetValue(path, out var cachedHeader))
        {
            if (onlyHeader || cachedHeader.Frames.Count > 0)
            {
                fileHeader = cachedHeader;
                return BMError.NoError;
            }
        }

        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            // Read and verify magic number
            uint magic = reader.ReadUInt32();
            if (magic != Constants.BM_MAGIC)
            {
                return BMError.BadFile;
            }

            // Read binary format version
            int binaryVersion = reader.ReadByte();
            if (binaryVersion > Constants.BINARY_FORMAT_VERSION)
            {
                return BMError.NewerBinaryVersion;
            }

            fileHeader = new FileHeader
            {
                BinaryFormatVersion = binaryVersion
            };

            // Read record end time
            fileHeader.RecordEndTime = reader.ReadInt32();

            // Read record name
            int nameLength = reader.ReadByte();
            var nameBytes = reader.ReadBytes(nameLength);
            fileHeader.RecordName = Encoding.UTF8.GetString(nameBytes);

            // Read initial position
            fileHeader.InitialPosition = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );

            // Read initial angles
            fileHeader.InitialAngles = new QAngle(
                reader.ReadSingle(),
                reader.ReadSingle(),
                0
            );

            // Read tick count
            fileHeader.TickCount = reader.ReadInt32();

            // Read bookmark count (if version >= 0x02)
            if (binaryVersion >= 0x02)
            {
                fileHeader.BookmarkCount = reader.ReadInt32();

                // Read bookmarks
                for (int i = 0; i < fileHeader.BookmarkCount; i++)
                {
                    var bookmark = new Bookmark
                    {
                        Frame = reader.ReadInt32(),
                        AdditionalTeleportTick = reader.ReadInt32(),
                        Name = ReadString(reader)
                    };
                    fileHeader.Bookmarks.Add(bookmark);
                }
            }

            // Cache header
            _loadedRecords[path] = fileHeader;
            _loadedRecordsCategory[path] = category;

            if (!_sortedRecordList.Contains(path))
            {
                _sortedRecordList.Add(path);
            }

            if (!_sortedCategoryList.Contains(category))
            {
                _sortedCategoryList.Add(category);
            }

            SortRecordList();

            if (onlyHeader)
            {
                return BMError.NoError;
            }

            // Read all frames
            var additionalTeleports = new List<AdditionalTeleport>();

            for (int i = 0; i < fileHeader.TickCount; i++)
            {
                var frame = ReadFrame(reader);
                fileHeader.Frames.Add(frame);

                // Read additional teleport data if present
                if ((frame.AdditionalFieldsFlags & (AdditionalFields.TeleportedOrigin | 
                                                     AdditionalFields.TeleportedAngles | 
                                                     AdditionalFields.TeleportedVelocity)) != 0)
                {
                    var at = new AdditionalTeleport();

                    if ((frame.AdditionalFieldsFlags & AdditionalFields.TeleportedOrigin) != 0)
                    {
                        at.Origin = new Vector3(
                            reader.ReadSingle(),
                            reader.ReadSingle(),
                            reader.ReadSingle()
                        );
                    }

                    if ((frame.AdditionalFieldsFlags & AdditionalFields.TeleportedAngles) != 0)
                    {
                        at.Angles = new QAngle(
                            reader.ReadSingle(),
                            reader.ReadSingle(),
                            reader.ReadSingle()
                        );
                    }

                    if ((frame.AdditionalFieldsFlags & AdditionalFields.TeleportedVelocity) != 0)
                    {
                        at.Velocity = new Vector3(
                            reader.ReadSingle(),
                            reader.ReadSingle(),
                            reader.ReadSingle()
                        );
                    }

                    at.Flags = frame.AdditionalFieldsFlags & (AdditionalFields.TeleportedOrigin | 
                                                               AdditionalFields.TeleportedAngles | 
                                                               AdditionalFields.TeleportedVelocity);
                    additionalTeleports.Add(at);
                }
            }

            if (additionalTeleports.Count > 0)
            {
                _loadedAdditionalTeleports[path] = additionalTeleports;
            }

            return BMError.NoError;
        }
        catch (Exception ex)
        {
            Server.PrintToConsole($"[BotMimic] Error loading record from file: {ex.Message}");
            return BMError.BadFile;
        }
    }

    /// <summary>
    /// Reads a single frame from the binary reader
    /// </summary>
    private FrameInfo ReadFrame(BinaryReader reader)
    {
        var frame = new FrameInfo
        {
            Buttons = reader.ReadUInt64(),
            Impulse = reader.ReadInt32(),
            ActualVelocity = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            ),
            PredictedVelocity = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            ),
            PredictedAngles = new QAngle(
                reader.ReadSingle(),
                reader.ReadSingle(),
                0
            ),
            Origin = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            ),
            NewWeapon = ReadString(reader),
            PlayerSubtype = reader.ReadInt32(),
            PlayerSeed = reader.ReadInt32(),
            AdditionalFieldsFlags = (AdditionalFields)reader.ReadInt32()
        };

        if (string.IsNullOrEmpty(frame.NewWeapon))
        {
            frame.NewWeapon = null;
        }

        return frame;
    }

    /// <summary>
    /// Reads a null-terminated string
    /// </summary>
    private string ReadString(BinaryReader reader)
    {
        var bytes = new List<byte>();
        byte b;
        while ((b = reader.ReadByte()) != 0)
        {
            bytes.Add(b);
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    /// <summary>
    /// Sorts the record list by end time
    /// </summary>
    private void SortRecordList()
    {
        _sortedRecordList.Sort((path1, path2) =>
        {
            if (_loadedRecords.TryGetValue(path1, out var header1) &&
                _loadedRecords.TryGetValue(path2, out var header2))
            {
                return header1.RecordEndTime.CompareTo(header2.RecordEndTime);
            }
            return 0;
        });

        _sortedCategoryList.Sort(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the file header for a record
    /// </summary>
    public bool TryGetFileHeader(string path, out FileHeader? fileHeader)
    {
        return _loadedRecords.TryGetValue(path, out fileHeader);
    }

    /// <summary>
    /// Gets additional teleports for a record
    /// </summary>
    public bool TryGetAdditionalTeleports(string path, out List<AdditionalTeleport>? teleports)
    {
        return _loadedAdditionalTeleports.TryGetValue(path, out teleports);
    }

    /// <summary>
    /// Gets the category for a record
    /// </summary>
    public bool TryGetCategory(string path, out string? category)
    {
        return _loadedRecordsCategory.TryGetValue(path, out category);
    }

    /// <summary>
    /// Deletes a record
    /// </summary>
    public bool DeleteRecord(string path)
    {
        if (_loadedRecords.ContainsKey(path))
        {
            _loadedRecords.Remove(path);
        }

        if (_loadedAdditionalTeleports.ContainsKey(path))
        {
            _loadedAdditionalTeleports.Remove(path);
        }

        if (_loadedRecordsCategory.ContainsKey(path))
        {
            _loadedRecordsCategory.Remove(path);
        }

        _sortedRecordList.Remove(path);

        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[BotMimic] Error deleting record file: {ex.Message}");
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Parses records in a directory
    /// </summary>
    public void ParseRecordsInDirectory(string mapName)
    {
        // Check if base record path exists
        if (!Directory.Exists(_baseRecordPath))
        {
            Server.PrintToConsole($"[BotMimic] Base record path does not exist: {_baseRecordPath}");
            return;
        }

        // Scan all category folders
        try
        {
            foreach (var categoryDir in Directory.GetDirectories(_baseRecordPath))
            {
                var categoryName = Path.GetFileName(categoryDir);
                var mapPath = Path.Combine(categoryDir, mapName);
                
                if (Directory.Exists(mapPath))
                {
                    Server.PrintToConsole($"[BotMimic] Loading records from category '{categoryName}' for map '{mapName}'");
                    ParseRecordsRecursive(mapPath, categoryName);
                }
            }
        }
        catch (Exception ex)
        {
            Server.PrintToConsole($"[BotMimic] Error parsing records: {ex.Message}");
        }
    }

    private void ParseRecordsRecursive(string path, string category)
    {
        try
        {
            foreach (var file in Directory.GetFiles(path, "*.rec"))
            {
                LoadRecordFromFile(file, category, out _, onlyHeader: true);
            }

            foreach (var dir in Directory.GetDirectories(path))
            {
                var dirName = Path.GetFileName(dir);
                if (dirName != "." && dirName != "..")
                {
                    ParseRecordsRecursive(dir, category);
                }
            }
        }
        catch (Exception ex)
        {
            Server.PrintToConsole($"[BotMimic] Error parsing records in directory: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears all loaded records
    /// </summary>
    public void ClearLoadedRecords()
    {
        _loadedRecords.Clear();
        _loadedAdditionalTeleports.Clear();
        _loadedRecordsCategory.Clear();
        _sortedRecordList.Clear();
        _sortedCategoryList.Clear();
    }
}

