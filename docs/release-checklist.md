# DADS v1.0 release checklist

## Before release

- [ ] Review `CHANGELOG.md`, `docs/specification-v1.md`, migration notes, FAQ, and samples.
- [ ] Run `dotnet build DevXStream.DADS.slnx --nologo` and the validation test project.
- [ ] Run `npm ci --prefix apps/dads-docs-web`, `npm run build:dads --prefix apps/dads-docs-web`, `npm run verify:preview --prefix apps/dads-docs-web`, and `npm run verify:a11y --prefix apps/dads-docs-web`.
- [ ] Confirm `npm audit --omit=dev --prefix apps/dads-docs-web` reports no production vulnerabilities.
- [ ] Check package metadata, MIT license, README, and deterministic builds. Package signing is optional for v1.0.0 and can be enabled in a future release after DevXStream registers a trusted code-signing certificate with NuGet.org.
- [ ] Review Try-it configuration: disabled by default and no production allow-list.

## Release

- [ ] Create and protect a `v1.0.0` tag from the reviewed commit.
- [ ] Run the release workflow with NuGet signing/publishing secrets.
- [ ] Attach signed NuGet packages and the static portal archive to the release.
- [ ] Publish release notes from `CHANGELOG.md` and link the v1 specification, migration notes, FAQ, and support route.

## Rollback

- [ ] Unlist a defective NuGet package; do not overwrite an immutable version.
- [ ] Restore the previous signed package version in consumer guidance.
- [ ] Redeploy the previous immutable static portal build.
- [ ] Mark the release notes with the incident and create a corrective patch release.
