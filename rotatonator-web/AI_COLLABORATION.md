# AI Collaboration Contract - Web Root

## Sibling Desktop Project
- Desktop root: `c:\code\rotatonator`
- Desktop app path: `Rotatonator/`
- Migration plan: `../MIGRATION_PLAN_WEB.md`
- Vercel setup: `../VERCEL_MONOREPO_SETUP.md`

## Mandatory Rule: Reference Desktop First
Before implementing parser/rotation features, reference desktop implementation and mirror behavior unless intentionally diverging.

## Mandatory Rule: Vercel Subdirectory Deployment
- This web app is hosted on Vercel with Root Directory set to `rotatonator-web`.
- Do not assume repository root is the deploy root.
- Keep framework/build config compatible with subdirectory deployment.

## Reference-First Map
- Desktop parser: `../Rotatonator/Services/LogMonitor.cs`
- Desktop rotation manager: `../Rotatonator/Services/RotationManager.cs`
- Desktop position helper: `../Rotatonator/Services/PositionHelper.cs`
- Desktop config/export UX: `../Rotatonator/MainWindow.xaml.cs`

## Required PR Notes (parser/timing changes)
1. Which desktop file/function was used as reference.
2. Whether behavior is identical or intentionally different.
3. If different, why and compatibility impact.

## Parity Priorities
1. Prefix CH detection with tolerant prefix matching.
2. Append token support (`rotat:<pos>, <target>`).
3. Out-of-sync timing adjustment logic.
4. NPC target alert behavior.
5. CH/append export format consistency.
