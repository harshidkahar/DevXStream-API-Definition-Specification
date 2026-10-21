# DADS v1.0 quality gate

The v1.0 release gate requires all of the following evidence from a clean checkout:

| Area | Required evidence |
| --- | --- |
| Contract | Schema, sample validation, OpenAPI snapshot, and documentation projection tests pass. |
| Portal | Production static build, preview checks, and axe WCAG 2.2 A/AA semantic checks pass. |
| Security | Definition limits are tested, Try-it remains disabled by default, no secrets are present, and production npm audit reports no vulnerabilities. |
| Dependencies | `dotnet list DevXStream.DADS.slnx package --vulnerable` reports no known vulnerable packages. |
| Licensing | Repository and NuGet metadata identify MIT; release packages contain the README and repository metadata. Package signing is deferred until DevXStream has a registered trusted code-signing certificate. |
| Performance | Initial estimated portal transfer remains below 100 KB and deferred schemas avoid eagerly rendering large models. |
| Reproducibility | All five library packages build as version `1.0.0` using deterministic build settings and pass `scripts/verify-release.ps1`. |

Run the commands in [release-checklist.md](release-checklist.md) before a tag is created. The GitHub release workflow repeats build/test/package steps and publishes through NuGet Trusted Publishing. Add certificate signing as a subsequent hardening step when a trusted code-signing certificate is available.
