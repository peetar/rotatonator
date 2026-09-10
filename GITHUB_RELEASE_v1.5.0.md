# Rotatonator v1.5.0 - Append Macro Support & Parser Enhancements

## 🎯 What's New

### Append Macro Support
Now you can append `rotat:<position>, <target>` to **any existing CH macro** without changing your current workflow:
- Works with any CH macro format (e.g., `/rs haven 333 CH - %t - %n`)
- Can be used standalone: just `/say rotat:3, %t` in a macro
- Detected anywhere in the log line with flexible spacing
- Example: `/rs D&D 333 CH - %t - %n rotat:3, %t`

**Export Button**: Click "Export append macro" to auto-generate the correct token for your position in chain.

### Parser Hardening & Compatibility
- **Stripped Special Characters**: Handles logs where `D&D` appears as `D D` (EverQuest sanitization bug)
- **Generalized Prefix Support**: Works with any configured prefix (`D&D`, `haven`, etc.) — no hardcoding
- **Unified Code Path**: Append tokens are converted to synthetic CH lines and flow through the same detection logic as prefix format, ensuring consistent behavior for:
  - NPC target warnings ("Bad target" audio alert)
  - Out-of-sync macro timing adjustment (looks back for cast starts)
  - All audio/visual alerts

### UI & Help Improvements
- **Better Help Text**: In-app guidance now uses bullet points for clarity
- **Always 3-Digit Positions**: CH export macro now always uses standardized format (e.g., `444` instead of `4444` for position 4)
- **Log Utilities**: Live monitor indicator, log file size display, and archive/truncate button

### Audio Alert Enhancements
- **NPC Target Alert Option**: Optional "Audio alert if NPC is CH target" checkbox (triggers "Bad target" TTS)
- Works for both prefix format and append macro format

## ✨ Features at a Glance

| Feature | Status |
|---------|--------|
| Real-time log monitoring | ✅ |
| Complete Heal (CH) detection via prefix | ✅ |
| Complete Heal detection via append token | ✨ **NEW** |
| Transparent overlay with timers | ✅ |
| DDR graphical mode (optional) | ✅ |
| Audio alerts & TTS | ✅ Enhanced |
| NPC target detection | ✨ **NEW** |
| Out-of-sync macro timing adjustment | ✅ |
| Flexible chain import format | ✅ |
| Export configuration to clipboard | ✅ |

## 📥 Installation

### Option 1: Standalone Executable (Recommended)
1. Download `Rotatonator-v1.5.0-win-x64.zip` from the release assets
2. Extract anywhere
3. Run `Rotatonator.exe`
4. No .NET installation required

### Option 2: Build from Source
```powershell
git clone https://github.com/peetar/rotatonator.git
cd rotatonator
git checkout v1.5.0
.\build.ps1
```

## 🚀 Quick Start

1. **Configure Chain**: Enter your healers in order (one per line)
2. **Set Your Position**: Enter your character name (optional if monitoring as tank/leader)
3. **Choose Detection Method**:
   - **Prefix Format** (classic): `/say D&D 333 CH` in your macro
   - **Append Format** (new): Add `rotat:3, %t` to any existing macro
4. **Select Log File**: Browse to your EverQuest logs folder
5. **Start Monitoring**: Click "Start Monitoring"

## 🔄 Upgrade Notes

- **Fully Backward Compatible**: Existing prefix format detection unchanged
- **No Migration Required**: Settings persist from v1.4
- **Opt-in Feature**: Append macro is optional; use it or stick with prefix format

## 📋 What's Fixed & Improved

### Added
- **Append CH Macro Support**: Full detection + export for inline tokens
- **Export Append Macro Button**: Generate correct `rotat:<pos>, %t` for your position
- **RPC Target Audio Alert**: Optional TTS "Bad target" when NPC is healed
- **Log Utility Controls**: Monitor indicator, file size display, archive button
- **Prefix Tolerance**: Handles stripped special characters (`D D` vs `D&D`)

### Changed
- **Help Text Format**: Clearer bullet-point guidance in UI
- **Parser Unification**: Shared code path for prefix and append formats
- **Generalized Prefix**: Works with any configured chain prefix (not hardcoded)

### Fixed
- **CH Export Position Format**: Always uses 3-digit standard (111, 222, AAA)
- **MainWindow Stability**: Removed duplicate field definitions
- **Monitor State Updates**: Restored UI refresh during start/stop

## 🛠️ Technical Details

### Append Macro Detection Regex
```
rotat:(\d+),\s*(\S+)
```
- Matches `rotat:` followed by digits, comma, optional whitespace, then target
- Simple and flexible — works with or without space after comma
- Standalone or appended to existing line

### Prefix Pattern Tolerance
The configured prefix is split into alphanumeric chunks and rejoined with flexible separators:
- `D&D` matches: `D&D`, `D D`, `D  D`, etc.
- `haven` matches: `haven`, anywhere in a chat line
- No hardcoding — respects your configuration

### Unified Detection Flow
```
Append Token Found
  ↓
  Synthesize CH Line (e.g., "[timestamp] You say, 'D&D 333 CH - target - %n'")
  ↓
  [Shared Prefix Handler]
  ├─ Parse position & target
  ├─ Detect NPC targets
  ├─ Trigger audio alerts
  ├─ Lookup healer name
  ├─ Check cast start time
  ├─ Adjust for out-of-sync macros
  └─ Call OnHealCast()
```

## ✅ Validation

- **Build Status**: 0 errors, 3 pre-existing warnings (nullable types)
- **Test Coverage**: All features manually tested
- **Backward Compat**: 100% compatible with v1.4 configs
- **Performance**: Minimal impact; regex compiled and reused

## 🙏 Acknowledgments

Thanks to the EverQuest healing community for suggestions and testing feedback!

Found a bug? Have a suggestion? Please [open an issue](https://github.com/peetar/rotatonator/issues) on GitHub.

---

**Release Date**: March 3, 2026  
**Build**: v1.5.0 (Windows x64)  
**Framework**: .NET 8.0  
**License**: See repository
