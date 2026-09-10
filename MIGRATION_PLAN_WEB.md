# Rotatonator Desktop -> Web Migration Plan (v1)

## Objectives
- Build a web-based Rotatonator that can read local EverQuest logs.
- Keep desktop app and web app developed in parallel with minimal coupling.
- Reuse proven logic from desktop implementation first, then optimize.

## Project Layout (Monorepo)
- Desktop root: `c:\code\rotatonator` (existing WPF app)
- Web root: `c:\code\rotatonator\rotatonator-web` (new web app workspace)

## Architecture Strategy

### 1) Shared Core Logic (Target State)
Extract pure logic from desktop app into a shared library over time:
- CH parsing and token handling
- Position conversion helpers
- Rotation timing and cast-state transitions
- Target classification (NPC/player heuristic)

Initial approach: copy behavior faithfully into web version with tests against desktop behavior.

### 2) Platform Adapters
Keep platform-specific concerns separate:
- Desktop adapter (existing): file watcher, WPF UI, speech/audio APIs.
- Web adapter (new): browser UI + a local companion service for filesystem access.

### 3) Local Log Access for Web
Primary MVP path uses the browser File System Access API:
1. **File System Access API (recommended MVP)**: user selects log file, web app reads incrementally.
2. **Companion service (fallback/advanced)**: local daemon tails logs and streams events to web UI (WebSocket/HTTP).
3. **Tauri/Electron shell (optional packaging path)**: web UI wrapped in desktop container with native file access.

Recommended MVP: Browser frontend + File System Access API, with companion-service fallback where browser support is limited.

## Phased Delivery Plan

### Phase 0 - Foundations
- Create `rotatonator-web` workspace.
- Establish AI collaboration docs in both project roots.
- Define desktop-to-web feature parity matrix.

### Phase 1 - Parser Parity
- Port CH prefix parsing behavior.
- Port append token behavior (`rotat:<pos>, <target>`).
- Preserve tolerant prefix handling for sanitized separators.
- Preserve out-of-sync timing adjustment logic.

### Phase 2 - Runtime Pipeline
- File System Access API adapter reads selected local log incrementally.
- Web app consumes parsed events and drives overlay/chain state.
- Add diagnostics panel for raw event stream.
- Add optional companion-service adapter for non-supporting browsers.

### Phase 3 - Feature Parity
- Audio alerts and NPC target warning.
- Chain import/export and CH string export.
- Configuration persistence.

### Phase 4 - Parallel Feature Development
- New features implemented in web and desktop using same behavior specs.
- Shared test fixtures for parser and rotation sequencing.

## Desktop-to-Web Mapping (Initial)
- `Rotatonator/Services/LogMonitor.cs` -> web parser service + event normalizer.
- `Rotatonator/Services/RotationManager.cs` -> web rotation state manager.
- `Rotatonator/Services/PositionHelper.cs` -> shared/helper module.
- `Rotatonator/MainWindow.xaml(.cs)` -> web settings/config screens.
- `Rotatonator/AudioAlertConfigDialog*` -> web audio settings panel.

## Quality Gates
- Behavioral parity tests from real log samples.
- Regression tests for:
  - Prefix sanitization (`D&D` vs `D D`)
  - Append token parsing
  - Timing adjustment logic
  - NPC target detection

## Governance for AI Agents
- Any web feature touching parser/rotation logic must reference desktop source behavior first.
- If behavior differs intentionally, document the rationale in web PR notes.
- Keep a running parity checklist in `rotatonator-web` docs.

## Immediate Next Steps
1. Choose web stack (React/Vue/Svelte + TypeScript recommended).
2. Implement File System Access API read loop and parser integration.
3. Build parser parity tests from current desktop logs.
4. Add optional companion runtime (Node/.NET/Rust) for unsupported browsers.
