# OpenAPI importer spike

## Decision

OpenAPI import is assessed as a guided, non-lossless conversion for the developer-preview period. DADS remains the canonical source and OpenAPI export remains fully supported.

## Supported mapping

| OpenAPI 3.1 field | DADS target | Notes |
| --- | --- | --- |
| `info.title`, `info.version`, `info.description` | `application` | Direct mapping |
| `servers` | `servers` | URL and description map directly |
| `paths`, HTTP operations | `modules[].endpoints[]` | Tags become module names; untagged operations use `Default` |
| `parameters`, `requestBody`, `responses` | Endpoint contract | JSON media type/examples supported |
| `components.schemas` | `schemas` | Object properties, required values, enums, and examples supported |

## Non-lossless or unsupported areas

- OAuth/security schemes, callbacks, links, webhooks, discriminators, and multipart encodings have no DADS v1 equivalent.
- Complex JSON Schema composition (`oneOf`, `allOf`, `not`, conditional schemas), `$dynamicRef`, and arbitrary external references cannot be represented losslessly.
- OpenAPI vendor extensions are retained only when they meet the DADS `x-*` extension policy; engineering metadata cannot be inferred.
- Import must reject remote `$ref` resolution in preview. A future importer should require explicitly supplied local files and report each unsupported field before emitting DADS.

## Recommendation

Build the importer in Sprint 8 only after defining a conversion report format and fixtures for each supported/non-lossless mapping. Never overwrite a human-authored DADS file during import.
