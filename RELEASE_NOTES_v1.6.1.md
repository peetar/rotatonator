# Release Notes - Rotatonator v1.6.1

**Release Date:** September 27, 2026  
**Version:** 1.6.1  
**Target Platform:** Windows x64 (.NET 8.0 Windows Desktop)

---

## Highlights

Rotatonator v1.6.1 adds automated raid lockout and PvP respawn window tracking with Discord webhook integration, persistent local storage, interactive management UI, and in-app simulation testing.

### New Features

1. **Raid Lockout & PvP Respawn Timers**:
   - Automatic log detection for PvE (`Druzzil Ro tells the guild...`) and PvP (`[PVP] ... has killed ...`) raid kills.
   - 421 raid bosses extracted directly from the Quarm database dump (`quarm_2025-11-02-07_55.sql`).
   - Exact PvE lockouts (18h, 2.75d, 6.75d).
   - PvP ±20% ($0.8\times$ to $1.2\times$) respawn window with single warning alert at 80% mark.
   - Follow-up Discord alert with clock emoji (`⏰`).

2. **Persistent Storage**:
   - Saved to `%APPDATA%\Rotatonator\respawn_timers.json`.
   - Restores automatically on startup.

3. **Active Timers UI (`ActiveTimersWindow`)**:
   - Accessible via the "⏱️ Active Timers" button in Tab 3 ("🛠️ Advanced Configs").
   - Shows live countdowns, status, zone, mode, and killer info.
   - "⚡ Trigger Discord Alert Now" button to instantly fire the follow-up notification.
   - Buttons to remove timers or clear completed timers.

4. **Simulation Testing**:
   - "🧪 Simulate PvE Vox Kill"
   - "🧪 Simulate PvP Vox Kill"
