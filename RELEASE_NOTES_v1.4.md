# Rotatonator v1.4 Release Notes

**Release Date**: February 11, 2026  
**Branch**: v1.4  
**Commit**: `9f9aeb7`

## Overview

v1.4 focuses on making DDR mode a practical tool for heal chain visualization while improving the overall user experience. DDR mode now defaults to "serious" operation without silly sounds, but players can optionally enable music/sound effects if desired. The single-overlay visualization system allows seamless switching between Classic and DDR modes.

## ✨ Major Features

### 1. DDR Mode Enhancements
- **New "Silly Mode" Toggle**: Enable/disable background music and sound effects
  - **Default: OFF** - DDR mode starts in serious visualization mode
  - Gives players full control over whether they want ambient effects
  - Serious mode focuses on heal chain visualization without distractions

### 2. Visualization Mode Toggle
- **Single Overlay Window**: Switch between Classic and DDR visualizations without managing multiple windows
  - Classic mode: Traditional heal bar visualization
  - DDR mode: Vertical lane-based visualization with in-progress heal tracking
  - Modes are mutually exclusive - selecting one hides the other
  - All overlay configuration persists across mode switches

### 3. Improved In-Progress Heal Tracking (DDR Mode)
- **New "IN PROGRESS" Lane**: Dedicated column shows heals in-flight to their targets
  - Floating target name indicators descend from top to bottom over 10 seconds
  - Visual target line at bottom aligns with healer names
  - Indicators fade and disappear when reaching the target
  - Different visual style (purple-striped background) distinguishes it from healer lanes
  - Tracks ALL heals including out-of-chain "aaa" heals

### 4. Enhanced Score Management
- **Export Scores to Chat**: Copy formatted score data to clipboard with single click
  - Format: `/rs HealerName: Score, HealerName: Score, ...`
  - Ready-paste into game chat (/rs command)
- **Reset Scores**: Clear all score data instantly
  - Removes confirmation dialogs for efficiency during raids
  - Instant visual feedback

### 5. Better Controls & Configuration
- **Centralized Mute Button**: Moved from DDR overlay to main window
  - Easier access during gameplay
  - Works for both Classic and DDR visualizations
  - Visual indicator showing mute status (🔊 Unmute / 🔇 Mute)

### 6. Overlay Window Improvements
- **Resize Grip**: Drag bottom-right corner to dynamically size the overlay
  - Resize Mode enabled for flexible window sizing
  - Minimum window constraints enforced (600x300)
  - Perfect for adapting to different screen layouts

## 🎨 Visual Improvements

### Countdown Timer Text
- Changed from yellow (#FFD700) to red (#FF4444)
- Improved readability when heals are imminent
- Better contrast against overlay background

### Removed Distracting Elements
- Removed pulsing yellow circle timer that was too distracting
- Cleaner, more focused visual design

### Target Indicator (DDR In-Progress Lane)
- Bright cyan glowing rounded rectangle at bottom
- Visual anchor showing where heal targets need to land
- Matches the style of descending heal indicators

## 🔧 Technical Details

### Configuration Changes
- New `EnableDDRSillyMode` property in `RotationConfig`
- Persists to application settings
- Settings UI includes new checkbox in DDR Controls section

### Audio System Enhancement
- `DDRAudioService` now respects silly mode flag
- Conditional audio playback: only plays when both `SillyModeEnabled` AND `!IsMuted`
- Maintains backward compatibility with existing audio files

### Bug Fixes
- ✅ Fixed duplicate ResizeGrip event handler (CS0111 error)
- ✅ Fixed out-of-chain heals breaking countdown timer
- ✅ Fixed classic overlay appearing when DDR mode enabled
- ✅ Fixed window resize permissions for DDR overlay
- ✅ Fixed classic overlay auto-showing after 30 seconds in DDR mode

## 📋 Files Modified

### Core Features
- `DDRGraphicalOverlay.xaml` - Updated layout with new in-progress lane
- `DDRGraphicalOverlay.xaml.cs` - Heal tracking, progress lane animation, target line
- `MainWindow.xaml` - DDR silly mode checkbox, mute button, export/reset buttons
- `MainWindow.xaml.cs` - Event handlers for new controls

### Configuration & Services
- `Models/RotationConfig.cs` - Added `EnableDDRSillyMode` property
- `Services/SettingsManager.cs` - Persists silly mode setting
- `Services/DDRAudioService.cs` - Silly mode conditional audio playback

### Visualization
- `OverlayWindow.xaml` - Yellow to red countdown text update
- `OverlayWindow.xaml.cs` - Enhanced DDR mode compatibility

### Documentation
- `Audio/DDR/README.txt` - Updated with silly vs normal mode info
- `V1.4_PLAN.md` - Living document tracking all changes

## 🚀 Installation & Usage

### First Launch
1. Enable **DDR Mode** checkbox in main window
2. Healing chain will automatically switch to DDR visualization
3. Optional: Enable **DDR Silly Mode** if you want background music/effects

### In-Progress Heal Lane
- Watch the right-side lane to see heals descending toward their targets
- Each indicator shows the target player's name
- They disappear when they reach the cyan target line (exactly 10 seconds)

### Export Scores
1. Click **Export DDR Scores** during combat
2. Score data automatically copies to clipboard
3. Paste into chat: `/rs [paste]`

### Resize Overlay
- Drag the bottom-right corner of the DDR window to resize
- Works smoothly while monitoring is active
- Window won't shrink below minimum constraints

## ⚙️ Build Information

- **Framework**: .NET 8.0-windows
- **Build Status**: ✅ Successful
- **Warnings**: 3 (nullable reference types - expected)
- **Errors**: 0

### Build Commands
```powershell
dotnet build "Rotatonator.sln"
cd Rotatonator
dotnet run
```

## 📝 Known Limitations

- Silly mode music loops don't sync to actual heal rotation timing (ambient feature)
- Progress lane shows ALL heals including out-of-chain casts (intentional for visibility)
- Resize grip only appears when DDR window is active

## 🔄 Migration from v1.3

All existing settings are preserved. The new changes are backward compatible:
- DDR Silly Mode defaults to **OFF** for all users
- Classic mode unchanged
- Existing rotation configs work as-is

## 📊 Testing Checklist

- ✅ DDR mode toggle works smoothly
- ✅ Silly mode checkbox enables/disables audio
- ✅ Mute button controls audio independently
- ✅ In-progress heals descend to target line over ~10 seconds
- ✅ Export scores formats correctly for chat
- ✅ Reset scores clears all data instantly
- ✅ Resize grip allows dynamic window sizing
- ✅ Classic mode overlay shows when DDR mode disabled
- ✅ Settings persist across application restart
- ✅ Build completes with 0 errors

## 🤝 Contributing

Found issues? Have suggestions for v1.5? Please create an issue or pull request on GitHub.

## 📄 License

Same as Rotatonator main project.

---

**Questions?** See [V1.4_PLAN.md](V1.4_PLAN.md) for development details and decision records.
