# DevXStream API Definition Specification (DADS)

DADS is a versioned, language-neutral definition format for REST API contracts and the engineering metadata needed to generate, validate, and document production applications.

## Sprint 0 baseline

- [v1 scope and glossary](docs/v1-scope-and-glossary.md)
- [Versioning and extension policy](docs/versioning-and-extensions.md)
- [Canonical model and mapping](docs/canonical-model.md)
- [Documentation portal design baseline](docs/docs-portal-design.md)
- [DADS v1 draft JSON Schema](specification/v1/dads.schema.json)
- [Repository standards](CONTRIBUTING.md)

The ECommerce reference definition is in [samples/ecommerce.dads.json](samples/ecommerce.dads.json).

## Validate a definition

```powershell
dotnet run --project src/DevXStream.DADS.Cli -- validate samples/ecommerce.dads.json
```

Run the Sprint 1 validation fixtures with:

```powershell
dotnet run --project tests/DevXStream.DADS.Validation.Tests
```

## Generate projections

```powershell
dotnet run --project src/DevXStream.DADS.Cli -- export-openapi samples/ecommerce.dads.json --output ecommerce.openapi.json
dotnet run --project src/DevXStream.DADS.Cli -- build-docs-data samples/ecommerce.dads.json --output ecommerce.docs.json
```

The OpenAPI exporter creates an OpenAPI 3.1.1 projection. DADS-only engineering metadata is preserved as the `x-devxstream-engineering` extension.

## Build the branded portal

```powershell
npm install --prefix apps/dads-docs-web
npm run build:dads --prefix apps/dads-docs-web
```

The command projects `samples/ecommerce.dads.json`, copies the generated documentation/OpenAPI data into the Angular portal, and produces the static site at `apps/dads-docs-web/dist/dads-docs-web/browser`.

See the [authoring guide](docs/authoring-guide.md) for the DADS format and [CI/CD guide](docs/ci-cd.md) for validation and static-site build examples.

## Developer preview quality gates

Run the preview verification suite after building the portal:

```powershell
dotnet run --project tests/DevXStream.DADS.Validation.Tests --no-build
npm run verify:preview --prefix apps/dads-docs-web
npm run verify:a11y --prefix apps/dads-docs-web
```

See the [security and processing limits](docs/security-and-limits.md) and [developer preview guide](docs/developer-preview.md) for rollout, feedback, and rollback guidance.

Live request execution is opt-in and sandbox-only. See the [try-it policy](docs/try-it-policy.md) before enabling it, and the [OpenAPI import assessment](docs/openapi-import-assessment.md) for compatibility scope.

## DADS v1.0 release

- [v1.0 specification](docs/specification-v1.md)
- [Draft-to-v1 migration notes](docs/migration-to-v1.md)
- [FAQ and support path](docs/faq.md)
- [Changelog](CHANGELOG.md)
- [Release checklist](docs/release-checklist.md) and [quality gate](docs/release-quality-gate.md)

The library packages use the MIT license; see [LICENSE](LICENSE). The ECommerce sample remains the complete installation, validation, portal-generation, and OpenAPI-export reference.

## Run a DADS-integrated Web API

The [Northstar Commerce API sample](samples/Northstar.Commerce.Api/README.md) is an ASP.NET Core 10 application that installs the public DADS packages, validates the ECommerce definition on startup, and generates its `/openapi.json` endpoint directly from that same definition.
