# DevXStream DADS v1.0.0

DevXStream API Definition Specification (DADS) v1.0.0 is the first stable release of the language-neutral REST API definition format and static documentation toolchain.

## Highlights

- Versioned JSON/YAML definitions with deterministic structural and semantic validation.
- DADS-generated documentation data, OpenAPI 3.1 export, client request snippets, and a DevXStream-branded static portal.
- Search, schema exploration, runtime branding, accessibility checks, and safe-by-default Try-it controls.
- Definition size/depth/reference safeguards and a documented compatibility/extension policy.
- NuGet packages for Models, Serialization, Validation, OpenAPI, and Documentation projection libraries.

## Getting started

```powershell
dotnet run --project src/DevXStream.DADS.Cli -- validate samples/ecommerce.dads.json
npm ci --prefix apps/dads-docs-web
npm run build:dads --prefix apps/dads-docs-web
```

Read the [v1 specification](docs/specification-v1.md), [migration notes](docs/migration-to-v1.md), [FAQ](docs/faq.md), and [release checklist](docs/release-checklist.md). The release workflow publishes through NuGet Trusted Publishing without a stored API key. Package signing will be added after DevXStream obtains a trusted code-signing certificate.
