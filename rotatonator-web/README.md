# Rotatonator Web

Web application workspace for the Rotatonator migration.

## Status
- React + TypeScript + Vite scaffold complete.
- Deployment target: Vercel.
- Vercel project should use Root Directory: `rotatonator-web`.
- Local log access MVP: browser File System Access API.

## Companion Docs
- Migration plan: `../MIGRATION_PLAN_WEB.md`
- Vercel monorepo setup: `../VERCEL_MONOREPO_SETUP.md`
- AI collaboration rules: `./AI_COLLABORATION.md`

## Desktop Reference Requirement
When implementing parser/timing features, use desktop files as behavior reference:
- `../Rotatonator/Services/LogMonitor.cs`
- `../Rotatonator/Services/RotationManager.cs`
- `../Rotatonator/Services/PositionHelper.cs`
- `../Rotatonator/MainWindow.xaml.cs`

## Development Commands
```bash
npm install
npm run dev
npm run build
npm run preview
```

## Next Milestone
Build parser parity layer and event pipeline on top of File System Access API, then add optional companion-service fallback.
