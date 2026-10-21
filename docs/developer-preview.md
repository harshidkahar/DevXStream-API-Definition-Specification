# DADS developer preview

The developer preview contains the DADS CLI, validation libraries, the ECommerce sample, and a static DevXStream documentation portal.

## Install and try it

Build the prerelease package locally:

```powershell
dotnet pack src/DevXStream.DADS.Models -c Release --output artifacts/packages
dotnet run --project src/DevXStream.DADS.Cli -- validate samples/ecommerce.dads.json
npm ci --prefix apps/dads-docs-web
npm run build:dads --prefix apps/dads-docs-web
```

Serve `apps/dads-docs-web/dist/dads-docs-web/browser` from any static host. The site reads its branding at runtime from `data/branding.json`; retain `assets/devxstream-logo.png` when using the default DevXStream identity.

## Feedback and rollback

Report preview feedback with the DADS file (redact private URLs/tokens), CLI version, command output, and browser/version. Keep the last successful static build and NuGet package as the rollback artefact. If a preview issue affects consumer-facing documentation, restore that prior static folder, then redeploy it from the same immutable build artefact.

## Known preview limits

- The portal is a static renderer and does not execute API requests.
- The initial preview package contains the models package; broader package publication is planned for v1.0.
- Accessibility is tested by automated structural checks and keyboard-focused interaction coverage; a formal third-party WCAG audit remains a release-readiness activity.
