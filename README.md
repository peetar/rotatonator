# Rotatonator - EverQuest Complete Heal Rotation Manager

A Windows desktop application that enhances the EverQuest experience for healers participating in Complete Heal (CH) rotations. Similar to GINA, but specifically designed for managing CH chains.

## Features

- **Real-time Log Monitoring**: Monitors your EverQuest log file to detect when healers in the rotation cast Complete Heal
- **Transparent Overlay**: Non-interactive overlay window showing active CH casts with countdown timers
- **Flexible Chain Configuration**: Define your rotation chain with any number of healers and set your position
- **Adjustable Timing**: Slider to set chain interval from 1-10 seconds between heals
- **Visual Feedback**: Color-coded timer bars showing each healer's cast progress
- **Audio Alerts**: Optional beep notification when it's your turn to cast
- **Auto-Cast**: Optional keyboard automation to automatically press your CH hotkey
- **Player Turn Warning**: Get advance notice (5 seconds) before your turn

## Requirements

- Windows 10/11
- .NET 8.0 Runtime
- EverQuest with log file enabled

## Installation

1. Download the latest release
2. Extract to a folder of your choice
3. Run `Rotatonator.exe`

## Building from Source

Requirements:
- Visual Studio 2022 or later
- .NET 8.0 SDK

```powershell
git clone <repository-url>
cd rotatonator
dotnet restore
dotnet build
```

## Usage

1. **Select Log File**: Click "Browse" to select your EverQuest log file (usually in `C:\Program Files (x86)\Sony\EverQuest\Logs\eqlog_CharacterName_ServerName.txt`)

2. **Configure Chain**: 
   - Enter healer names in rotation order, one per line
   - Enter your character name
   - Adjust chain interval slider to match your group's timing

3. **Enable Features**:
   - Show Overlay: Displays transparent timer overlay
   - Audio Alerts: Beeps when it's your turn
   - Auto-Cast: Automatically presses your hotkey (specify which key)

4. **Start Monitoring**: Click "Start Monitoring" to begin

## How It Works

The application monitors your EverQuest log file for lines like:
```
[Mon Jan 19 14:30:45 2026] Healer1 begins to cast a spell.
```

When a healer in your rotation casts, the app:
1. Displays their cast with a 10-second countdown (CH cast time)
2. Calculates when you should cast based on chain position and interval
3. Provides visual warning 5 seconds before your turn
4. Triggers audio/auto-cast when it's your turn

## Configuration Tips

- **Chain Interval**: Set this to match your group's agreed interval between heals (usually 6 seconds)
- **Auto-Cast Safety**: Only enable auto-cast after testing the hotkey works correctly
- **Overlay Position**: Drag the overlay to your preferred screen location on first launch

## Known Limitations

- Requires EverQuest logging to be enabled (`/log on`)
- Only detects casts from healers explicitly listed in your rotation
- Auto-cast sends keystrokes globally (ensure EQ is the active window)

## License

MIT License - See LICENSE file for details

## Credits

Inspired by GINA (Gina Is Not ACT) and the EverQuest healing community.
