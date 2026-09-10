# AI Collaboration Contract - Desktop Root

## This project has a sibling web project
- Web project root: `c:\code\rotatonator\rotatonator-web`
- Migration plan: `MIGRATION_PLAN_WEB.md`

## Rules for AI agents working in desktop root
1. Treat desktop behavior as current source of truth for parser and rotation logic.
2. When changing parser/timing/alert behavior, record equivalent update needed for web project.
3. Use explicit references to impacted desktop files so web agents can mirror behavior.

## High-priority behavior references
- Log parsing + append token handling: `Rotatonator/Services/LogMonitor.cs`
- Rotation sequencing/timing: `Rotatonator/Services/RotationManager.cs`
- Position encoding/decoding: `Rotatonator/Services/PositionHelper.cs`
- CH export and append export UX: `Rotatonator/MainWindow.xaml.cs`

## Parallel Development Workflow
- Desktop-first bugfixes should open follow-up tasks for web parity.
- If a fix is behavior-critical (timing/parsing), add a note in changelog/release notes indicating parity impact.
- Keep web parity notes concise and implementation-focused.
