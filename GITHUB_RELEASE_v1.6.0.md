# Rotatonator v1.6.0 - Discord Webhook Integration, Cloud Sync & Voice Alerts

## 🎯 What's New in v1.6.0

Rotatonator v1.6.0 brings seamless **Discord Webhook integration**, **Cloud Chain Synchronization**, **Natural Windows Voice Alerts**, and a brand new **Advanced Configs** tab!

---

### 📡 Discord Webhook Integration
Share live in-game events with your guild on Discord directly from Rotatonator:
- **PVP Event Publishing**:
  - Automatically parses server `[PVP]` broadcasts:
    `[PVP] Caedis of <Haven> has died to a grimling deathbringer in combat in Acrylia Caverns!`
    &rarr; 💀 **[PVP]** **Caedis** `<Haven>` *has died to* **a grimling deathbringer** in *Acrylia Caverns*!
  - Fully supports simulated in-game testing via `/say [PVP] test`.
  - Error-tolerant fallback handles custom announcements gracefully.
- **Raid Kill Publishing**:
  - Automatically parses deity world broadcasts:
    `Druzzil Ro tells the guild, 'Marosu of <Dungeons and Dragons> has killed Lady Vox in Permafrost Caverns!'`
    &rarr; 🏆 **[Raid Kill]** **Marosu** `<Dungeons and Dragons>` has slain **Lady Vox** in *Permafrost Caverns*! 👑🐉
- **Instant Validation**: Click the new **🔔 Test Webhook** button to verify connectivity, view error diagnostics, and hear victory tones.

---

### 🔊 Audio & Voice Alerts
- **Natural Voice Announcement**: Windows Speech Synthesis (`System.Speech.Synthesis`) announces *"Chain updated"* whenever a rotation is updated via log or cloud.
- **PVP Alert Tone**: 1 short alert tone (750 Hz) sounds on every detected PVP event.
- **Raid Kill Victory Tones**: 2 ascending fanfare tones (600 Hz &rarr; 900 Hz) sound when a raid boss is defeated.
- Runs entirely in background tasks for zero UI lag.

---

### ☁️ Cloud Chain Synchronization & Web Companion
- **Web App (`rotatonator-web`)**: Live web companion deployed at `https://rotatonator-web.vercel.app/`.
- **Serverless Cloud Sync API (`/api/chain`)**: Fast, resilient backend powered by **Upstash Redis / Vercel KV REST API**.
- **Real-Time Push & Pull**:
  - Exporting a chain automatically publishes it to the cloud.
  - Running desktop clients with the same chain prefix auto-import the latest rotation within 5 seconds.

---

### 🛠️ "Advanced Configs" Tab
Settings have been streamlined into a dedicated 3rd tab:
- Cloud Sync Web API Server URL
- Discord Webhook URL & **🔔 Test Webhook** button
- `[ ] Publish PVP events to Discord` toggle
- `[ ] Publish raid kills to Discord` toggle
- Live event status notifications

---

## 📥 Installation

### Option 1: Standalone Package (Recommended)
1. Download **`Rotatonator-v1.6.0-win-x64.zip`** from the release assets below.
2. Extract to any folder.
3. Run `Rotatonator.exe`.

### Option 2: Build from Source
```powershell
git clone https://github.com/peetar/rotatonator.git
cd rotatonator
git checkout v1.6.0
.\build.ps1
```

---

## ✨ Features at a Glance

| Feature | Status |
|---------|--------|
| Real-time EverQuest Log Monitoring | ✅ |
| Complete Heal Detection via Prefix & Append Token | ✅ |
| Transparent Overlay with Timers | ✅ |
| DDR Graphical Mode & Score Tracking | ✅ |
| Discord Webhook Integration (PVP & Raid Kills) | ✨ **NEW** |
| Discord Webhook Connection Tester | ✨ **NEW** |
| Audio Alert Tones (PVP & Raid Victory) | ✨ **NEW** |
| Windows TTS Voice ("Chain updated") | ✨ **NEW** |
| Cloud Sync API & Web Companion | ✨ **NEW** |
| Advanced Configs Tab | ✨ **NEW** |

---

## 🔄 Upgrade Notes
- Fully backward compatible with existing v1.4 and v1.5 settings.
- All new features (Cloud Sync, Discord Webhooks) are opt-in and default to disabled until configured.

---

**Release Date**: September 10, 2026  
**Build**: v1.6.0 (Windows x64)  
**Framework**: .NET 8.0  
