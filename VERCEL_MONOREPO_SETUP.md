# Vercel Monorepo Setup (Desktop + Web Subdirectory)

This repository is a monorepo with:
- Desktop app at repository root (`Rotatonator/`)
- Web app in subdirectory (`rotatonator-web/`)

## Does Vercel support this layout?
Yes. Set **Root Directory** to `rotatonator-web` in Vercel project settings.

## Recommended Git + Vercel Model
1. Keep both desktop and web in the same repo.
2. Deploy only the web app from `rotatonator-web`.
3. Use branch-based previews (default Vercel behavior).
4. Keep desktop release process unchanged.

## Initial Vercel Project Configuration
In Vercel dashboard (Project -> Settings -> General):
- **Framework Preset**: auto-detect after scaffold (React/Next/etc.)
- **Root Directory**: `rotatonator-web`
- **Install Command**: use framework default (or set after scaffold)
- **Build Command**: use framework default (or set after scaffold)
- **Output Directory**: use framework default (or set after scaffold)

## Git Trigger Behavior
With root directory set to `rotatonator-web`:
- Commits changing only desktop files generally do not affect web build output.
- Commits changing web files trigger deploys as expected.

## Optional: Restrict Auto-Deploy Scope
If needed later, configure **Ignored Build Step** in Vercel so deploys only run when files under `rotatonator-web/` change.

Example concept:
- Run build only if `git diff` includes `rotatonator-web/**`

(Exact command depends on your final CI preferences.)

## AI Agent Rules for Web Deploy Safety
- Do not move the web project out of `rotatonator-web`.
- Keep deployment assumptions documented in `rotatonator-web/README.md`.
- When adding framework config, ensure it works with subdirectory root deployment.

## Local Log Access Note (Web Runtime)
- MVP uses browser File System Access API (best on Chromium-based browsers).
- Because browser support varies, retain companion-service fallback in architecture plan.

## Pre-Scaffold Checklist
- [x] Web folder exists: `rotatonator-web/`
- [x] Cross-project AI docs exist in both roots
- [x] Repo ignores `.vercel` and web build artifacts
- [ ] Scaffold web framework in `rotatonator-web`
- [ ] Connect Vercel project to this repo and set Root Directory
