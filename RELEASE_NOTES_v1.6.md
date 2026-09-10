# Rotatonator v1.6 Release Notes

## Highlights

### 1. Discord Webhook Integration
- **PVP Event Publishing**: Automatically detects `[PVP]` death and combat events in EverQuest logs and publishes styled Discord messages with `⚔️` and `💀` emojis.
  - Supports live server broadcasts: `[PVP] Caedis of <Haven> has died to a grimling deathbringer in combat in Acrylia Caverns!`
  - Supports in-game `/say` test simulations: `You say, '[PVP] test'`
  - Error-tolerant fallback gracefully handles custom text (e.g. `[PVP] BLARG`).
- **Raid Kill Publishing**: Automatically detects deity guild broadcasts (`Druzzil Ro tells the guild...` / `has killed`) and publishes celebration announcements to Discord with `🏆`, `👑`, and `🐉` emojis.
  - Formats player, guild, boss name, and zone cleanly.
- **Dedicated "🔔 Test Webhook" Button**: Test your Discord webhook URL instantly from the UI with audio feedback and pop-up diagnostics.

### 2. Audio & Natural Voice Alerts
- **Voice Announcement ("Chain updated")**: Natural Windows Text-To-Speech (`System.Speech.Synthesis`) announces *"Chain updated"* whenever a chain is imported via log parse or cloud synchronization.
- **PVP Alert Tone**: Plays a dedicated single tone (750 Hz, 130ms) whenever a PVP event is detected.
- **Raid Kill Victory Tones**: Plays two ascending victory tones (600 Hz &rarr; 900 Hz) whenever a raid boss kill is announced.
- Fully asynchronous audio processing ensuring zero UI or log monitoring latency.

### 3. Web & Cloud Synchronization
- **`rotatonator-web` Application**: A React + Vite web companion deployed on Vercel at `https://rotatonator-web.vercel.app/`.
- **Serverless Cloud Sync API (`/api/chain`)**:
  - GET / POST endpoints supporting real-time chain sharing by prefix.
  - Integrated with **Upstash Redis / Vercel KV REST API** with fuzzy environment variable detection and fallback in-memory cache.
- **Desktop Cloud Sync**:
  - Automatically pushes chain updates to the cloud when clicking "Export Chain to Clipboard".
  - Polls for updates in the background every 5 seconds with timestamp deduplication and visual status updates.

### 4. "Advanced Configs" UI Tab
- Added a dedicated 3rd tab in the main window:
  - Cloud Sync Server URL configuration.
  - Discord Webhook URL input and "🔔 Test Webhook" button.
  - Independent toggles for publishing PVP events and Raid Kills.
  - Real-time event dispatch status display.

---

## What's Changed & Fixed
- **Thread Affinity Fix**: Resolved an `InvalidOperationException` that occurred when background log monitoring threads accessed UI checkbox controls; all checks now use thread-safe service properties and dispatch UI notifications safely.
- **In-Game Simulation Parsing**: Flexible regex detection isolates `[PVP]` and `tells the guild` within any chat context (including `/say`, `/rs`, `/gsay`) and strips surrounding quotes.
- **Reliable Packaging**: Updated `package.ps1` for v1.6.0 with UTF-8 safe console output.

---

## Upgrade & Compatibility
- **100% Backward Compatible**: All existing prefix and append macro configurations persist and remain fully supported.
- **Zero-Dependency Core**: All audio tones, voice synthesis, and webhooks use standard .NET 8, NAudio, and System.Speech components.
