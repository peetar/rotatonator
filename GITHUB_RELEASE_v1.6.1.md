# Rotatonator v1.6.1 - Automated Raid Lockout & PvP Respawn Timers

## 🎯 What's New in v1.6.1

Rotatonator v1.6.1 introduces **Automated Raid Lockout (PvE) and Respawn Window (PvP) Timers** integrated with Discord webhooks, complete with persistent storage across app restarts and interactive timer management!

---

### ⏱️ Automated Raid Lockout & Respawn Timers
- **Boss Kill Detection**: Automatically parses raid kill broadcasts in your EverQuest logs:
  - **PvE**: `Druzzil Ro tells the guild, '<Killer> of <<Guild>> has killed <Mob> in <Zone>!'`
  - **PvP**: `[PVP] <Killer> of <<Guild>> has killed <Mob> in <Zone>!`
- **421 Boss Database**: Precompiled database extracted from Quarm server exports (`quarm_2025-11-02-07_55.sql`), mapping each boss to its exact server base respawn/lockout timer (e.g. Lady Vox & Lord Nagafen: 6.75 days; Trakanon & Venril Sathir: 2.75 days).
- **PvE Lockout Tracking**: Tracks exact 100% lockout durations and sends a follow-up notification with the clock emoji (`⏰`) when the target comes off lockout.
- **PvP Respawn Window Tracking**:
  - Automatically calculates the **$\pm 20\%$ ($0.8\times$ to $1.2\times$)** respawn window.
  - Sends a single warning alert to Discord with `⏰` right at the **80% mark** when the respawn window opens.
- **Dynamic Discord Timestamps**: Uses Discord native timestamp formatting (`<t:unix:f>`, `<t:unix:R>`) so countdowns stay synchronized and live in Discord across timezones.

---

### 💾 Persistent Local Storage
- All active and completed timers automatically persist to `%APPDATA%\Rotatonator\respawn_timers.json`.
- Restarting Rotatonator or rebooting your computer seamlessly restores active timers without losing track of ongoing windows.

---

### 🖥️ Active Timers Management Window
- Click the new **⏱️ Active Timers** button in the "🛠️ Advanced Configs" tab to open the timer manager:
  - View live countdowns, target name, zone, mode, killer, and ready dates.
  - **⚡ Trigger Discord Alert Now**: Instantly tests and delivers the `⏰` expiration alert to Discord.
  - **🗑️ Remove Timer**: Delete an unwanted timer.
  - **🧹 Clear Completed**: Prune old, finished timers.

---

### 🧪 One-Click In-App Testing
- Added **`🧪 Simulate PvE Vox Kill`** and **`🧪 Simulate PvP Vox Kill`** buttons under the Advanced Configs tab so you can test end-to-end webhook delivery and timer scheduling right from the desktop UI.

---

## 📥 Installation

### Option 1: Standalone Package (Recommended)
1. Download **`Rotatonator-v1.6.1-win-x64.zip`** from the release assets below.
2. Extract to any folder.
3. Run `Rotatonator.exe`.

### Option 2: Build from Source
```powershell
git clone https://github.com/peetar/rotatonator.git
cd rotatonator/Rotatonator
dotnet build -c Release
```
