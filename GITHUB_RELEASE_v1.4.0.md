# GitHub Release: Rotatonator v1.4.0

**This document contains the content to post as a GitHub Release**

---

## 📢 Rotatonator v1.4.0 - DDR Mode Enhancements & Improvements

**Release Date**: February 11, 2026  
**Tag**: `v1.4.0`

### ✨ What's New in v1.4

DDR mode is now a practical tool for visualizing heal chains! This release transforms DDR from a gimmicky visualization into a professional option while keeping the fun aspects optional.

### 🎯 Major Features

#### DDR Mode Serious Option
- **New "Silly Mode" Toggle** - Enable/disable background music and sound effects
- **Defaults to OFF** - DDR mode now starts in professional visualization mode
- Gives players full control over ambient effects without being forced into entertainment mode

#### Single Overlay Visualization
- **Toggle between Classic and DDR** without managing multiple windows
- Seamless switching preserves all overlay configuration
- Modes are mutually exclusive - selecting one automatically hides the other

#### In-Progress Heal Tracking
- **New dedicated lane** shows heals in-flight to their targets
- Floating target names descend from top to bottom over 10 seconds
- Cyan target line marks where heals need to land
- Tracks ALL heals including out-of-chain "aaa" casts
- Indicators fade when reaching the target

#### Score Management
- **Export Scores** - Copy formatted data to clipboard with one click
  - Format: `/rs HealerName: Score, HealerName: Score, ...`
  - Ready to paste into game chat
- **Reset Scores** - Clear all data instantly without confirmation

#### Improved Controls
- **Mute Button moved to main window** for easier access
- **Overlay resizing** - drag bottom-right corner to size dynamically
- All controls centralized in main window

#### Visual Improvements
- Countdown timer text changed from yellow to **red** for better readability
- Removed distracting pulsing yellow circle
- Cleaner, more professional overlay designs

### 🔧 Technical Highlights

- **0 build errors**, 3 expected warnings
- Fully backward compatible with v1.3
- All settings persist across restarts
- New configuration option for silly mode

### 📋 Complete Change List

**Added:**
- DDR silly mode toggle (disabled by default)
- Single overlay toggle between Classic/DDR
- In-progress heal visualization lane
- Score export to clipboard feature
- Score reset functionality
- Mute button in main window
- Overlay resize grip
- Red countdown timer text
- Normal mode audio clips

**Fixed:**
- Out-of-chain heals breaking countdown
- Classic overlay conflicts in DDR mode
- Overlay auto-showing behavior
- Event handler duplicates
- Window resize permissions

**Removed:**
- Distracting pulsing yellow circle
- "Show overlay" checkbox (always on now)

### 📚 Documentation

- **[RELEASE_NOTES_v1.4.md](RELEASE_NOTES_v1.4.md)** - Comprehensive feature guide
- **[CHANGELOG.md](CHANGELOG.md)** - Full project changelog  
- **[V1.4_PLAN.md](V1.4_PLAN.md)** - Development plan & decisions
- **[PULL_REQUEST_v1.4.md](PULL_REQUEST_v1.4.md)** - Technical PR details

### 🚀 Installation

Download the latest build from the Assets section below.

### 🧪 Testing

All features have been thoroughly tested:
- ✅ DDR/Classic toggle
- ✅ Silly mode audio
- ✅ In-progress heal lane
- ✅ Score export/reset
- ✅ Overlay resizing
- ✅ Settings persistence
- ✅ All 8 planned v1.4 tasks

### 🔄 Upgrading from v1.3

Simply replace your executable. All settings are preserved:
- DDR Silly Mode defaults to OFF for all users
- Classic mode completely unchanged
- Existing rotation configs work as-is

### 💡 Try It Out

1. **Professional Mode**: Enable DDR Mode without silly mode for clean heal visualization
2. **For Fun**: Enable silly mode if you want background music/effects
3. **Export Scores**: Click export button during combat, paste into chat
4. **Resize Overlay**: Drag corner to fit your screen layout

### 📝 Known Limitations

- Silly mode music doesn't sync to actual heal rotation (intentionally ambient)
- In-progress lane shows ALL heals (intentional for visibility)

### 🤝 Feedback & Issues

Found a bug? Have a suggestion for v1.5? Please open an issue on GitHub!

### 📊 Release Statistics

| Metric | Value |
|--------|-------|
| Commits | 2 |
| Files Changed | 15 |
| Lines Added | 1,000+ |
| Build Errors | 0 |
| Tests Passed | All |
| Branch | v1.4 → main |

---

**Thanks for using Rotatonator! Happy healing! 🎵**

---

## Assets

Compiled binaries available below for Windows .NET 8.0-windows platforms.

---

**Release prepared**: February 11, 2026
