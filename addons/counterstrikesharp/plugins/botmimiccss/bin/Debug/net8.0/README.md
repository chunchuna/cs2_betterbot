# BotMimic CSS - Counter-Strike 2 Bot Recording & Playback

This plugin is a port of the popular SourceMod Bot Mimic plugin to CounterStrikeSharp for CS2. It allows you to record player movements and have bots play them back.

## Features

- **Record Player Movements**: Record your movements, including position, angles, velocity, and button inputs
- **Bot Playback**: Have bots mimic your recorded movements
- **Bookmarks**: Save bookmarks during recording to mark important moments
- **Pause/Resume**: Pause and resume recording without losing progress
- **Categories**: Organize recordings into categories
- **Interactive Menus**: Easy-to-use chat menus for managing recordings
- **Multiple Formats**: Binary file format compatible with the original Bot Mimic

## Installation

1. Make sure you have CounterStrikeSharp installed on your CS2 server
2. Copy the `botmimiccss` folder to `csgo/addons/counterstrikesharp/plugins/`
3. Compile the plugin or use the pre-compiled DLL
4. Restart your server or load the plugin with `css_plugins load botmimiccss`

## Building from Source

```bash
cd addons/counterstrikesharp/plugins/botmimiccss
dotnet build
```

The compiled DLL will be in `bin/Debug/net8.0/BotMimicCSS.dll`

## Commands

### Admin Commands (Requires `@css/admin` permission)

- `css_mimic` - Opens the main Bot Mimic menu
- `css_record [name] [category]` - Start recording your movements
- `css_stoprecord` - Stop and save the current recording
- `css_pauserecord` - Pause the current recording
- `css_resumerecord` - Resume a paused recording
- `css_savebookmark <name>` - Save a bookmark at the current position
- `css_playrecord <bot_name> <record_name_or_path>` - Make a bot play a recording
- `css_stopmimic <bot_name>` - Stop a bot from mimicking

### Root Commands (Requires `@css/root` permission)

- `css_deleterecord <file_path>` - Delete a recording file

## Usage Examples

### Recording Your Movements

1. Make sure you're alive and spawned
2. Type `!mimic` in chat or use `css_record MyRecord`
3. Perform your movements
4. Type `css_stoprecord` to save
5. Optionally use `css_savebookmark checkpoint1` to mark important points

### Playing Back on a Bot

**Using the Menu:**
1. Type `!mimic` in chat
2. Select a category
3. Select a recording
4. Choose "Play on Bot" and select a bot, or "Add Bot and Play" to add a new one

**Using Commands:**
```
css_playrecord bot_name MyRecord
```

### Managing Recordings

All recordings are stored in:
```
csgo/plugins/BotMimicCSS/records/[category]/[map_name]/[timestamp].rec
```

- `[category]` - The category you chose (default: "default")
- `[map_name]` - The map the recording was made on
- `[timestamp]` - Unix timestamp when the recording was saved

## File Format

The plugin uses a binary file format (.rec) with the following structure:

1. Magic number (0xDEADBEEF)
2. Binary format version (0x02)
3. Record metadata (name, timestamp, tick count)
4. Initial position and angles
5. Bookmarks (if any)
6. Frame data (position, velocity, buttons, etc. for each tick)

## Advanced Features

### Bookmarks

Bookmarks allow you to mark specific points in a recording:

1. While recording, use `css_savebookmark <name>` to save a checkpoint
2. During playback, use the menu to jump bots to specific bookmarks
3. Useful for testing specific sections of a route

### Categories

Organize your recordings by purpose:
- `default` - General recordings
- `surf` - Surf map routes
- `bhop` - Bunny hop routes
- `kz` - Climb/KZ routes
- Custom categories of your choice

### Pause/Resume

Pause recording to prepare for the next section:

1. Use `css_pauserecord` to pause
2. Set up your position
3. Use `css_resumerecord` to continue
4. The plugin will save a snapshot of your position when resuming

## Troubleshooting

### Bot not moving correctly
- Make sure the bot is on the correct team
- Check that the recording was made on the same map
- Try reloading the record with `forceReload: true`

### Recording not saving
- Check file permissions on the records directory
- Make sure you have enough disk space
- Check server console for error messages

### Menu not showing
- Make sure you have the required permissions (`@css/admin`)
- Check that CounterStrikeSharp is properly installed
- Try using commands instead of the menu

## Permissions

Add these to your `configs/admin_groups.json`:

```json
{
  "#css/admin": {
    "flags": ["@css/admin"]
  },
  "#css/root": {
    "flags": ["@css/root"]
  }
}
```

## Credits

- Original SourceMod plugin by Peace-Maker
- Ported to CounterStrikeSharp for CS2
- Based on the Bot Mimic and Bot Mimic Menu plugins

## License

This is a port of open-source SourceMod plugins. Please respect the original licenses.

## Support

For issues, questions, or suggestions:
- Check the server console for detailed error messages
- Ensure you're using the latest version of CounterStrikeSharp
- Report bugs with detailed reproduction steps

## Version History

### 1.0.0
- Initial port from SourceMod to CounterStrikeSharp
- Core recording and playback functionality
- Menu system
- Bookmark support
- Category organization

