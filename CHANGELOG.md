# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.4.0] - 2026-02-11

### Added
- **DDR Mode Silly Mode Toggle**: Enable/disable background music and sound effects (defaults to OFF)
- **Single Overlay Visualization**: Toggle between Classic and DDR modes without managing multiple windows
- **In-Progress Heal Lane**: New dedicated column in DDR mode showing heals descending toward targets
  - Floating target name indicators with 10-second descent timing
  - Visual target line with cyan glow aligned to healer names
  - Tracks all heals including out-of-chain "aaa" casts
- **Export Scores Feature**: Copy formatted DDR scores to clipboard (`/rs HealerName: Score, ...`)
- **Reset Scores Button**: Clear all score data with single click (no confirmation needed)
- **Centralized Mute Control**: Move mute button from DDR overlay to main window
- **Overlay Resize Functionality**: Drag bottom-right corner to dynamically resize DDR window
- **Improved Countdown Visualization**: Changed timer text color from yellow to red for better readability
- **New Audio Mode**: Normal (non-silly) sound clips for professional DDR visualization
- **Comprehensive Release Documentation**: V1.4 plan, release notes, and changelog

### Changed
- DDR Mode now defaults to serious visualization without silly mode enabled
- Classic and DDR modes are mutually exclusive (single overlay window approach)
- Countdown timer text color changed from yellow (#FFD700) to red (#FF4444)
- Mute button moved from DDR overlay to main window for centralized control
- Removed "Show Overlay Window" checkbox (overlay always visible when not in DDR mode)
- Enhanced incoming heal visualization with better timing and visual feedback

### Fixed
- Fixed duplicate ResizeGrip_MouseLeftButtonDown event handler
- Fixed out-of-chain heals (e.g., "aaa" casts) breaking countdown timer
- Fixed classic overlay appearing unintentionally when DDR mode enabled
- Fixed classic overlay auto-showing after 30-second timeout in DDR mode
- Fixed window resize mode permissions for DDR overlay

### Removed
- Pulsing yellow circle timer (too distracting for serious mode)
- "Show Overlay Window" checkbox (replaced with always-on behavior)

### Technical
- Added `EnableDDRSillyMode` property to RotationConfig
- Enhanced DDRAudioService with conditional audio playback based on mode and mute state
- Improved event handling in overlay windows to prevent mode conflicts
- Better visual separation of DDR in-progress lane with purple stripe pattern
- All features tested and verified working with 0 build errors

---

## [1.3.0] - Previous Release

[See previous changelog entries below]

### Added
- DDR Mode visualization
- Score tracking system
- Audio alerts
- Overlay window management

---

## Format Guide

- **Added**: for new features.
- **Changed**: for changes in existing functionality.
- **Deprecated**: for soon-to-be removed features.
- **Removed**: for now removed features.
- **Fixed**: for any bug fixes.
- **Security**: in case of security vulnerabilities.

## Version Links

- [v1.4.0](https://github.com/yourusername/rotatonator/releases/tag/v1.4.0) - 2026-02-11
- [v1.3.0](https://github.com/yourusername/rotatonator/releases/tag/v1.3.0)

## Unreleased

(Future v1.5 features will be added here as they're developed)

---

*For more details on v1.4 development, see [V1.4_PLAN.md](V1.4_PLAN.md)*
