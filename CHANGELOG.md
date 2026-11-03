# Changelog

All notable DADS changes are recorded here. DADS follows semantic versioning after the draft period.

## 1.0.21 — 2026-11-03

### Added

- Runtime-first ASP.NET Core definition discovery for existing applications. `AddDevXStreamDads()` now documents mapped endpoints when no `api.dads.json` file is present.
- Structured runtime validation diagnostics and a safe fallback from an unavailable definition file to endpoint discovery.
- .NET 10 target support across the DADS package set, with validation and ASP.NET Core runtime checks in the release workflow.

### Compatibility

- Existing file-based and custom-provider definitions remain supported. Set `DefinitionSource.File` with runtime fallback disabled to require a static contract.

## 1.0.0 — 2026-10-21

### Added

- DADS v1 schema, JSON/YAML loading, structural and semantic validation.
- Documentation-data and OpenAPI 3.1 projections with request snippets.
- DevXStream branded static documentation portal, search, accessibility checks, runtime branding, and controlled Try-it policy.
- Security limits for document size, nesting depth, modules, endpoints, and schemas.

### Compatibility

- This is the first stable DADS v1 release. `DADS 1.0-draft` definitions remain accepted by the current tools as the v1 compatibility baseline.
- No fields are deprecated in v1.0.0.
