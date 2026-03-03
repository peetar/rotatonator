# Rotatonator - EverQuest Complete Heal Rotation Manager

A Windows desktop application that enhances the EverQuest experience for healers participating in Complete Heal (CH) rotations. Similar to GINA, but specifically designed for managing CH chains.

## Features

- **Real-time Log Monitoring**: Monitors your EverQuest log file to detect Complete Heal (CH) casts using configured prefix format
- **Append Macro Detection (v1.5)**: Detects `rotat:<number>,<target>` tokens anywhere in log lines (no whitespace), so it can be appended to existing CH macros
- **Transparent Overlay**: Non-interactive overlay window showing active CH casts with countdown timers
- **Flexible Chain Configuration**: Define your rotation chain with any number of healers and set your position
- **Adjustable Timing**: Slider to set chain interval from 1-10 seconds between heals
- **Visual Feedback**: Color-coded timer bars showing each healer's cast progress
- **Audio Alerts**: Optional beep notification when it's your turn to cast (can be muted)
- **Player Turn Warning**: Get advance notice (5 seconds) before your turn
- **DDR Mode**: Optional graphical visualization with score tracking for casual gameplay
- **Overlay Customization**: Resizable overlay window to fit your screen layout
- **Score Management**: Export scores to clipboard for chat or reset with one click
- **Log Utility Controls**: Live monitoring indicator, log file size display, and one-click archive/truncate for large logs

## Requirements

- Windows 10/11
- .NET 8.0 Runtime
- EverQuest with log file enabled

## Installation

1. Download the latest release (v1.4.0 or later) from [Releases](https://github.com/peetar/rotatonator/releases)
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
   - Ensure your group is using the standard prefix format (e.g., `D&D 111 CH - %t - %n`)

3. **Enable Features**:
   - Classic Mode: Shows traditional countdown timer overlay
   - DDR Mode: Shows graphical visualization with score tracking
   - Audio Alerts: Beeps when it's your turn (can be muted)
   - Silly Mode (v1.4+): Optional background music in DDR mode

4. **Share Configuration**:
   - **Adjust Chain Timing**: Click the +/- buttons to adjust chain interval - this automatically copies your current rotation config to clipboard for easy sharing in raid say
   - **Export Append Macro**: Click "Export append macro" to copy `rotat:<your_position>, %t` (example: `rotat:3, %t`) and append it to any existing CH macro or use standalone
   - **Export Scores** (DDR Mode): Click "Export Scores" to copy healer scores in chat format (`/rs HealerName: Score, ...`) ready to paste

5. **Start Monitoring**: Click "Start Monitoring" to begin

## How It Works

The application monitors your EverQuest log file for chat/prefix-based CH rotation lines. The expected format is:
```
<Prefix> 111 CH - TargetName - HealerName
```

Example:
```
D&D 111 CH - Paladin - Cleric1
```

It also supports append tokens embedded in any macro line or standalone:
```
rotat:3, %t
```

When any healer casts (detected via this format), the app:
1. Displays their cast with a 10-second countdown (CH cast time)
2. Calculates when you should cast based on chain position and interval
3. Provides visual warning 5 seconds before your turn
4. Triggers audio alert when it's your turn

**Important**: You must configure your EverQuest rotation to use the expected prefix format for the application to detect heals correctly.

## v1.4 Features

**Professional DDR Mode**:
- Toggle between classic countdown view and graphical DDR visualization
- In-progress heal tracking shows all casts descending toward their targets
- Score tracking for each healer in DDR mode

**Enhanced Controls**:
- Resizable overlay - drag the corner to adjust size
- Export scores to clipboard for easy sharing in chat
- Reset scores with one click
- Mute button for quick audio control

**Optional Silly Mode** (v1.4+):
- Enable background music and sound effects in DDR mode
- Defaults to OFF for professional gameplay
- Toggle anytime without restarting

## Configuration Tips

- **Chain Interval**: Set this to match your group's agreed interval between heals (usually 6 seconds)
- **Overlay Position**: Drag the overlay to your preferred screen location on first launch
- **Audio Feedback**: Use audio alerts to know when it's your turn instead of relying on visual cues alone

## Known Limitations

- Requires EverQuest logging to be enabled (`/log on`)
- Out-of-rotation heals detected via macro format provide visual feedback but may not trigger audio if healer is not in configured chain

## License

MIT License - See LICENSE file for details

## Credits

Inspired by GINA (Gina Is Not ACT) and the EverQuest healing community.
