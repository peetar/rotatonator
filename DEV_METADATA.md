# Rotatonator Quick Development Metadata & Guide

## 1. Project Overview & Architecture
**Rotatonator** is an EverQuest Complete Heal (CH) chain rotation manager.
It monitors EQ log files in real-time, calculates heal rotation sequencing and timing intervals, alerts players when it is their turn to cast (visual flash, TTS announcements, beeps), tracks DDR-style rhythm timing and scores, and supports in-game chain sharing via macro exports and chat imports.

### Monorepo Structure
- **Desktop Application** (`Rotatonator/`): C# / WPF / .NET 8.0 on Windows (Source of Truth for parsing, sequencing, and timing logic).
- **Web Application** (`rotatonator-web/`): React 19 / TypeScript / Vite web app for cross-platform log monitoring via File System Access API.
- **Root Documentation**:
  - `AI_COLLABORATION.md`: Governance contract between desktop and web projects.
  - `MIGRATION_PLAN_WEB.md`: Multi-phase plan for web parity.
  - `BUILD.md` / `build.ps1` / `package.ps1`: Build and packaging scripts.

---

## 2. Key Components & File Map

### Desktop Application (`Rotatonator/`)
| Component | File Path | Responsibilities |
| :--- | :--- | :--- |
| **Main Window** | `Rotatonator/MainWindow.xaml`<br>`Rotatonator/MainWindow.xaml.cs` | UI controls for log selection, healer chain, timing buttons, export actions, DDR/audio toggles, and status updates. |
| **Log Monitor** | `Rotatonator/Services/LogMonitor.cs` | File tailing via `FileSystemWatcher`, circular buffer (1000 lines), regex parsing for CH casts, append tokens, chain imports, out-of-sync adjustment, and NPC detection. |
| **Rotation Manager** | `Rotatonator/Services/RotationManager.cs` | Rotation state engine; calculates who is next, dispatches `HealCastDetected`, `PlayerTurnStarting`, `PlayerTurnNow`, and `ChainImported` events; manages interval timer. |
| **Position Helper** | `Rotatonator/Services/PositionHelper.cs` | Conversion between 1-based chain position (1..35) and repeated strings (`111`..`999`, `AAA`..`ZZZ`). |
| **Classic Overlay** | `Rotatonator/OverlayWindow.xaml`<br>`Rotatonator/OverlayWindow.xaml.cs` | Click-through transparent overlay (`WS_EX_TRANSPARENT`), countdown bars for active heals, visual flash warnings. |
| **Overlay Anchor** | `Rotatonator/OverlayAnchor.xaml`<br>`Rotatonator/OverlayAnchor.xaml.cs` | Movable placeholder window to reposition the overlay prior to monitoring. |
| **DDR Overlay** | `Rotatonator/DDRGraphicalOverlay.xaml`<br>`Rotatonator/DDRGraphicalOverlay.xaml.cs` | Graphical rhythm visualization, falling notes, in-progress lane, starfield animation, music loops, and score display. |
| **Audio & TTS** | `Rotatonator/Services/TextToSpeechService.cs`<br>`Rotatonator/Services/DDRAudioService.cs`<br>`Rotatonator/Services/SoundService.cs`<br>`Rotatonator/AudioAlertConfigDialog.xaml(.cs)` | `System.Speech` voice announcements, NAudio feedback beeps, DDR sound effects, and chain update notification chimes. |
| **Cloud Sync** | `Rotatonator/Services/CloudSyncService.cs`<br>`rotatonator-web/api/chain.ts` | Opt-in Vercel-hosted REST synchronization: pushes chain on export, polls updates every 5s, supports Vercel KV / Upstash Redis with in-memory fallback. |
| **Configuration** | `Rotatonator/Models/RotationConfig.cs`<br>`Rotatonator/Models/AudioAlertConfig.cs`<br>`Rotatonator/Services/SettingsManager.cs` | Application state and JSON persistence in `%APPDATA%\Rotatonator\settings.json`. |

---

## 3. Log Parsing & Formatting Specifications

### CH Rotation Prefix Format
- Standard chat log format: `[timestamp] HealerName says, 'PREFIX ### CH - TargetName - HealerName'`
- Regex pattern: `^\[.*?\]\s+.+?,\s+'{prefixPattern}\s+(\d+|[A-Za-z]+)\s+CH(?:\s+-\s+([^-]+))?`
- Tolerant prefix matching (`BuildTolerantPrefixPattern`): Allows spaces or separators in prefixes (e.g. `D&D` matches `D D`).

### Append Macro Token Format (v1.5)
- Inline or standalone token: `rotat:<number_in_chain>,\s*<target>`
- Example: `rotat:3, %t` or `rotat:3,%t`
- Synthesizes an internal CH message line so it flows through the exact same processing pipeline.

### Chain Import & Delay Sync
- Full import: `/rs Rotatonator set_chain: 111 Name1, 222 Name2, set_delay: 3`
- Delay only: `/rs Rotatonator set_delay: 3`
- Regex pattern: `Rotatonator\s+(?:set_chain:\s*(.+?)\s*,\s*)?set_delay:\s*(\d+)`

### Out-of-Sync Timing Adjustment
- Checks recent log line buffer (last 1000 lines) for `<HealerName> begins to cast a spell` or `You begin casting`.
- If cast start occurred within 4.0s before the CH macro call, adjusts cast timestamp to the presumed start time.

### Target Classification
- Heuristic: If `TargetName` contains spaces (e.g., `A Sand Giant`), it is treated as an NPC target and triggers "Bad target" TTS warning if enabled.

---

## 4. Build, Run, and Package Commands

### Desktop
```powershell
# Quick build
dotnet build

# Release build
dotnet build -c Release

# Run executable directly
.\Rotatonator\bin\Debug\net8.0-windows\Rotatonator.exe

# Package release zip (guards against duplicate runtime folders)
powershell .\package.ps1 -Version "1.5.0"
```

### Web
```powershell
cd rotatonator-web
npm install
npm run build
npm run dev
```

---

## 5. Development Guidelines & Rules of Thumb
1. **Desktop is the Source of Truth**: Before adding or changing parser, timing, or export behavior, implement and verify in desktop first, and document parity requirements for the web app (`AI_COLLABORATION.md`).
2. **Process Lock Caution**: If `dotnet build` fails with file access errors, check whether `Rotatonator.exe` is running in the background.
3. **Dispatcher & Threading**: Log tailing (`FileSystemWatcher`) runs on background threads; UI updates must be marshaled to the UI thread via `Dispatcher.Invoke()`.
4. **Preserve Settings Backward Compatibility**: Ensure new settings in `AppSettings` / `RotationConfig` have sensible default values when deserializing older `settings.json` files.
